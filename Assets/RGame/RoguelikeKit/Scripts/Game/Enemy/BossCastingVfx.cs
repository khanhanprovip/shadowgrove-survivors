using UnityEngine;

namespace RGame.RoguelikeKit
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1600)]
    public sealed class BossCastingVfx : MonoBehaviour
    {
        private const float CastWindow = 1.1f;
        private const int HandPointCount = 25;
        private const int ChestPointCount = 25;

        private readonly Vector3[] _leftOuterBuffer = new Vector3[HandPointCount];
        private readonly Vector3[] _leftCoreBuffer = new Vector3[HandPointCount];
        private readonly Vector3[] _rightOuterBuffer = new Vector3[HandPointCount];
        private readonly Vector3[] _rightCoreBuffer = new Vector3[HandPointCount];
        private readonly Vector3[] _chestBuffer = new Vector3[ChestPointCount];

        private BossBrain _brain;
        private EndlessBossEnemy _boss;
        private SpriteRenderer _body;
        private LineRenderer _leftOuter;
        private LineRenderer _leftCore;
        private LineRenderer _rightOuter;
        private LineRenderer _rightCore;
        private LineRenderer _chestRune;

        private void Awake()
        {
            CacheReferences();
            BuildLines();
            Hide();
        }

        private void LateUpdate()
        {
            if (_brain == null || _boss == null || _body == null)
            {
                Hide();
                return;
            }

            float castAge = _brain.CastAge;
            if (!_boss.Casting || castAge < 0f || castAge > CastWindow || !_body.enabled || _body.sprite == null)
            {
                Hide();
                return;
            }

            // LateUpdate is still called while paused; leave the current windup pose frozen.
            if (Time.timeScale <= 0f || Time.deltaTime <= 0f)
                return;

            if (_leftOuter == null)
                BuildLines();

            Bounds bounds = _body.bounds;
            Vector2 bodyCenter = bounds.center;
            float width = Mathf.Max(0.1f, bounds.size.x);
            float height = Mathf.Max(0.1f, bounds.size.y);
            float glyphRadius = Mathf.Max(0.07f, Mathf.Min(width, height) * 0.085f);
            Vector2 leftHand = bodyCenter + new Vector2(-width * 0.30f, height * 0.17f);
            Vector2 rightHand = bodyCenter + new Vector2(width * 0.30f, height * 0.17f);
            Vector2 chest = bodyCenter + new Vector2(0f, height * 0.19f);

            float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(castAge / 0.12f));
            float fadeOut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((CastWindow - castAge) / 0.18f));
            float pulse = 0.88f + 0.12f * Mathf.Sin(castAge * 16f);
            float opacity = fadeIn * fadeOut * pulse;

            Color outer;
            Color core;
            Color rune;
            GetPalette(_brain.Kind, out outer, out core, out rune);
            outer.a = 0.23f * opacity;
            core.a = 0.70f * opacity;
            rune.a = 0.43f * opacity;

            int order = _body.sortingOrder + 1;
            int layer = _body.sortingLayerID;
            SetSorting(_leftOuter, layer, order);
            SetSorting(_leftCore, layer, order);
            SetSorting(_rightOuter, layer, order);
            SetSorting(_rightCore, layer, order);
            SetSorting(_chestRune, layer, order);

            DrawHand(_leftOuter, _leftCore, _leftOuterBuffer, _leftCoreBuffer,
                leftHand, glyphRadius, castAge, 0f, _brain.Kind, outer, core);
            DrawHand(_rightOuter, _rightCore, _rightOuterBuffer, _rightCoreBuffer,
                rightHand, glyphRadius, castAge, Mathf.PI * 0.72f, _brain.Kind, outer, core);
            DrawChest(chest, glyphRadius * 0.82f, castAge, _brain.Kind, rune);
        }

        public void Hide()
        {
            if (_leftOuter != null) _leftOuter.enabled = false;
            if (_leftCore != null) _leftCore.enabled = false;
            if (_rightOuter != null) _rightOuter.enabled = false;
            if (_rightCore != null) _rightCore.enabled = false;
            if (_chestRune != null) _chestRune.enabled = false;
        }

        private void CacheReferences()
        {
            if (_brain == null)
                _brain = GetComponent<BossBrain>();
            if (_boss == null)
                _boss = GetComponent<EndlessBossEnemy>();
            if (_body == null && _boss != null)
                _body = _boss.MySpriteRenderer;
            if (_body == null)
                _body = GetComponentInChildren<SpriteRenderer>(true);
        }

        private void BuildLines()
        {
            if (_leftOuter != null)
                return;

            _leftOuter = BossVfxShared.CreateLine(transform, "Boss Cast Left Glow", 100, 0.13f);
            _leftCore = BossVfxShared.CreateLine(transform, "Boss Cast Left Core", 101, 0.035f);
            _rightOuter = BossVfxShared.CreateLine(transform, "Boss Cast Right Glow", 100, 0.13f);
            _rightCore = BossVfxShared.CreateLine(transform, "Boss Cast Right Core", 101, 0.035f);
            _chestRune = BossVfxShared.CreateLine(transform, "Boss Cast Rune", 101, 0.045f);
        }

        private static void GetPalette(int kind, out Color outer, out Color core, out Color rune)
        {
            switch (kind)
            {
                case 0:
                    outer = new Color(0.12f, 0.68f, 0.43f);
                    core = new Color(0.86f, 1f, 0.76f);
                    rune = new Color(0.43f, 1f, 0.72f);
                    break;
                case 1:
                    outer = new Color(0.35f, 0.20f, 0.09f);
                    core = new Color(1f, 0.78f, 0.33f);
                    rune = new Color(0.46f, 0.76f, 0.32f);
                    break;
                case 2:
                    outer = new Color(0.12f, 0.58f, 0.78f);
                    core = new Color(0.82f, 1f, 1f);
                    rune = new Color(0.40f, 0.92f, 1f);
                    break;
                case 3:
                    outer = new Color(0.73f, 0.12f, 0.19f);
                    core = new Color(1f, 0.82f, 0.34f);
                    rune = new Color(1f, 0.40f, 0.28f);
                    break;
                default:
                    outer = new Color(0.39f, 0.18f, 0.74f);
                    core = new Color(1f, 0.82f, 0.40f);
                    rune = new Color(0.76f, 0.53f, 1f);
                    break;
            }
        }

        private static void SetSorting(LineRenderer line, int layer, int order)
        {
            line.sortingLayerID = layer;
            line.sortingOrder = order;
        }

        private void DrawHand(LineRenderer outerLine, LineRenderer coreLine,
            Vector3[] outerBuffer, Vector3[] coreBuffer, Vector2 hand, float radius,
            float age, float phase, int kind, Color outer, Color core)
        {
            int count = HandPointCount;
            bool loop = kind != 3 && kind != 0;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                float angle = t * Mathf.PI * 2f + phase + age * (kind == 3 ? -0.75f : 0.9f);
                float radial = 1f;
                switch (kind)
                {
                    case 0: // Jade spirit spiral
                        radial = 0.42f + 0.58f * t;
                        break;
                    case 1: // Bark spikes and leaf tips
                        radial = (i % 4 == 0) ? 1.18f : 0.72f;
                        break;
                    case 2: // Faceted ice crystal
                        radial = (i % 4 < 2) ? 0.76f : 1f;
                        break;
                    case 3: // Open scythe crescent
                        angle = t * Mathf.PI * 1.48f + phase + age * -0.75f;
                        break;
                    default: // Oracle eye
                        break;
                }

                Vector2 point;
                if (kind == 4)
                    point = hand + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.47f);
                else
                    point = hand + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius * radial);
                outerBuffer[i] = new Vector3(point.x, point.y, 0f);

                float coreAngle = t * Mathf.PI * 2f + phase + age * 1.4f;
                float coreRadius = radius * (kind == 4 ? 0.22f : 0.30f);
                Vector2 corePoint = hand + new Vector2(Mathf.Cos(coreAngle), Mathf.Sin(coreAngle)) * coreRadius;
                coreBuffer[i] = new Vector3(corePoint.x, corePoint.y, 0f);
            }

            Apply(outerLine, outerBuffer, count, loop, radius * 0.15f, outer);
            Apply(coreLine, coreBuffer, count, true, radius * 0.045f, core);
        }

        private void DrawChest(Vector2 chest, float radius, float age, int kind, Color color)
        {
            bool loop = kind != 3;
            for (int i = 0; i < ChestPointCount; i++)
            {
                float t = (float)i / (ChestPointCount - 1);
                float angle = t * Mathf.PI * 2f + age * 0.7f;
                float radial = 1f;
                if (kind == 1)
                    radial = (i % 4 == 0) ? 1.12f : 0.76f;
                else if (kind == 2)
                    radial = (i % 4 < 2) ? 0.77f : 1f;
                else if (kind == 3)
                    angle = t * Mathf.PI * 1.45f + age * -0.65f;

                Vector2 point;
                if (kind == 4)
                    point = chest + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.45f);
                else
                    point = chest + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius * radial);
                _chestBuffer[i] = new Vector3(point.x, point.y, 0f);
            }

            Apply(_chestRune, _chestBuffer, ChestPointCount, loop, radius * 0.07f, color);
        }

        private static void Apply(LineRenderer line, Vector3[] buffer, int count, bool loop, float width, Color color)
        {
            line.loop = loop;
            line.widthMultiplier = width;
            line.startColor = color;
            line.endColor = color;
            line.positionCount = count;
            line.SetPositions(buffer);
            line.enabled = true;
        }

        private void OnDisable()
        {
            Hide();
        }
    }
}
