using UnityEngine;

namespace RGame.RoguelikeKit
{
    internal static class BossVfxShared
    {
        private static Material _lineMaterial;

        static BossVfxShared()
        {
            Application.quitting += DisposeLineMaterial;
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += DisposeLineMaterial;
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForSubsystem()
        {
            DisposeLineMaterial();
        }

        public static Material LineMaterial
        {
            get
            {
                if (_lineMaterial != null)
                    return _lineMaterial;

                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                    shader = Shader.Find("Unlit/Transparent");
                if (shader == null)
                    return null;

                _lineMaterial = new Material(shader)
                {
                    name = "Shadowgrove Boss VFX Shared",
                    hideFlags = HideFlags.HideAndDontSave
                };
                return _lineMaterial;
            }
        }

        public static LineRenderer CreateLine(Transform parent, string name, int sortingOrder, float width)
        {
            var lineObject = new GameObject(name, typeof(LineRenderer));
            lineObject.transform.SetParent(parent, false);

            var line = lineObject.GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = LineMaterial;
            line.sortingOrder = sortingOrder;
            line.widthMultiplier = width;
            line.numCapVertices = 2;
            line.numCornerVertices = 1;
            line.positionCount = 0;
            line.enabled = false;
            return line;
        }

        public static void BindMaterial(LineRenderer line)
        {
            if (line == null)
                return;

            Material shared = LineMaterial;
            if (line.sharedMaterial != shared)
                line.sharedMaterial = shared;
        }

        private static void DisposeLineMaterial()
        {
            if (_lineMaterial == null)
            {
                _lineMaterial = null;
                return;
            }

#if UNITY_EDITOR
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(_lineMaterial);
            else
                UnityEngine.Object.DestroyImmediate(_lineMaterial);
#else
            UnityEngine.Object.Destroy(_lineMaterial);
#endif
            _lineMaterial = null;
        }
    }

    [DisallowMultipleComponent]
    public sealed class BossSkillVfx : MonoBehaviour
    {
        private const int PointCount = 32;
        private const int TrailCapacity = 8;

        private static int _activeVisualCount;

        private readonly Vector3[] _mainBuffer = new Vector3[PointCount];
        private readonly Vector3[] _coreBuffer = new Vector3[PointCount];
        private readonly Vector3[] _accentBuffer = new Vector3[PointCount];
        private readonly Vector3[] _trailHistory = new Vector3[TrailCapacity];
        private readonly Vector3[] _trailBuffer = new Vector3[TrailCapacity];

        private LineRenderer _mainLine;
        private LineRenderer _coreLine;
        private LineRenderer _accentLine;
        private BossHazard.Shape _shape;
        private Vector2 _from;
        private Vector2 _to;
        private Vector2 _lastCenter;
        private float _radius;
        private float _bodyHeight,_bodyScale;
        private Color _tint;
        private int _kind;
        private int _trailNext;
        private int _trailCount;
        private bool _initialized;
        private bool _visible;
        private bool _hasMoverSample;

        public static int ActiveVisualCount => _activeVisualCount;

        private void Awake()
        {
            BuildLines();
        }

        public void Init(int kind, BossHazard.Shape shape, Vector2 from, Vector2 to, float radius, Color tint, float bodyHeight=0)
        {
            BuildLines();
            Hide();

            _kind = Mathf.Clamp(kind, 0, 4);
            _shape = shape;
            _from = from;
            _to = to;
            _radius = Mathf.Max(0.04f, radius);
            _bodyHeight=Mathf.Max(0,bodyHeight);
            _bodyScale=Mathf.Clamp(bodyHeight*.18f,1f,2.6f);
            _tint = tint;
            _trailNext = 0;
            _trailCount = 0;
            _lastCenter = from;
            _hasMoverSample = false;
            _initialized = true;
        }

        public void Tick(float activeAge, float duration, Vector2 center, Vector2 currentFrom, Vector2 currentTo)
        {
            if (!_initialized || duration <= 0f || Time.timeScale <= 0f || Time.deltaTime <= 0f)
                return;

            if (activeAge >= duration)
            {
                Hide();
                return;
            }

            if (!_visible)
            {
                _visible = true;
                _activeVisualCount++;
            }

            BindMaterials();

            float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(activeAge / 0.07f));
            float fadeOut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((duration - activeAge) / 0.20f));
            float pulse = 0.90f + 0.10f * Mathf.Sin(activeAge * 7.5f + _kind * 0.6f);
            float opacity = fadeIn * fadeOut * pulse;

            SetPalette(opacity);

            switch (_shape)
            {
                case BossHazard.Shape.Line:
                    DrawLine(currentFrom, currentTo, activeAge);
                    break;
                case BossHazard.Shape.Bolt:
                case BossHazard.Shape.Orbit:
                    DrawMover(center, activeAge);
                    break;
                default:
                    DrawImpact(center, activeAge);
                    break;
            }
        }

        public void Hide()
        {
            if (_visible)
            {
                _activeVisualCount = Mathf.Max(0, _activeVisualCount - 1);
                _visible = false;
            }

            if (_mainLine != null) _mainLine.enabled = false;
            if (_coreLine != null) _coreLine.enabled = false;
            if (_accentLine != null) _accentLine.enabled = false;
            _trailCount = 0;
            _trailNext = 0;
            _hasMoverSample = false;
        }

        private void BuildLines()
        {
            if (_mainLine == null)
                _mainLine = BossVfxShared.CreateLine(transform, "Boss VFX Motif", 19982, 0.20f);
            if (_accentLine == null)
                _accentLine = BossVfxShared.CreateLine(transform, "Boss VFX Accent", 19985, 0.11f);
            if (_coreLine == null)
                _coreLine = BossVfxShared.CreateLine(transform, "Boss VFX Core", 19988, 0.045f);
            BindMaterials();
        }

        private void BindMaterials()
        {
            BossVfxShared.BindMaterial(_mainLine);
            BossVfxShared.BindMaterial(_accentLine);
            BossVfxShared.BindMaterial(_coreLine);
        }

        private void SetPalette(float opacity)
        {
            Color outer;
            Color core;
            Color accent;
            switch (_kind)
            {
                case 0: // Jade spirits
                    outer = new Color(0.14f, 0.72f, 0.48f);
                    core = new Color(0.88f, 1f, 0.78f);
                    accent = new Color(0.42f, 1f, 0.74f);
                    break;
                case 1: // Bark, roots, and gold leaves
                    outer = new Color(0.35f, 0.20f, 0.09f);
                    core = new Color(1f, 0.80f, 0.38f);
                    accent = new Color(0.38f, 0.72f, 0.29f);
                    break;
                case 2: // Cyan ice and glints
                    outer = new Color(0.10f, 0.54f, 0.78f);
                    core = new Color(0.82f, 1f, 1f);
                    accent = new Color(0.34f, 0.90f, 1f);
                    break;
                case 3: // Crimson and gold crescents
                    outer = new Color(0.72f, 0.12f, 0.19f);
                    core = new Color(1f, 0.83f, 0.37f);
                    accent = new Color(1f, 0.35f, 0.28f);
                    break;
                default: // Violet eyes and tendrils
                    outer = new Color(0.38f, 0.18f, 0.72f);
                    core = new Color(1f, 0.83f, 0.40f);
                    accent = new Color(0.73f, 0.50f, 1f);
                    break;
            }

            core = Color.Lerp(core, _tint, 0.24f);
            outer.a = 0.18f * opacity;
            core.a = 0.52f * opacity;
            accent.a = 0.32f * opacity;

            if (_mainLine != null)
            {
                _mainLine.startColor = outer;
                _mainLine.endColor = outer;
            }
            if (_coreLine != null)
            {
                _coreLine.startColor = core;
                _coreLine.endColor = core;
            }
            if (_accentLine != null)
            {
                Color trailHead = accent;
                Color trailTail = accent;
                trailTail.a *= 0.16f;
                _accentLine.startColor = trailTail;
                _accentLine.endColor = trailHead;
            }
        }

        private void DrawLine(Vector2 from, Vector2 to, float age)
        {
            Vector2 direction = (to - from).normalized;
            if (direction.sqrMagnitude < 0.001f)
                direction = (_to - _from).normalized;
            if (direction.sqrMagnitude < 0.001f)
                direction = Vector2.right;

            Vector2 normal = new Vector2(-direction.y, direction.x);
            float amplitude = Mathf.Max(0.035f, _radius * 0.34f);
            const int count = PointCount;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                float envelope = Mathf.Sin(t * Mathf.PI);
                float wave;
                switch (_kind)
                {
                    case 0:
                        wave = Mathf.Sin(t * Mathf.PI * 3f + age * 3.2f) * envelope;
                        break;
                    case 1:
                        wave = ((i & 1) == 0 ? 1f : -0.52f) * envelope;
                        break;
                    case 2:
                        wave = (((i / 2) & 1) == 0 ? 0.72f : -0.72f) * envelope;
                        break;
                    case 3:
                        wave = Mathf.Sin(t * Mathf.PI) * envelope;
                        break;
                    default:
                        wave = Mathf.Sin(t * Mathf.PI * 4f + age * 2.4f) * envelope;
                        break;
                }

                Vector2 point = Vector2.Lerp(from, to, t) + normal * (wave * amplitude);
                _mainBuffer[i] = ToPoint(point);

                float mirrored = _kind == 3
                    ? -Mathf.Sin(t * Mathf.PI) * envelope
                    : Mathf.Sin(t * Mathf.PI * 2f + age * 2.0f + 1.2f) * envelope * 0.22f;
                _accentBuffer[i] = ToPoint(Vector2.Lerp(from, to, t) + normal * (mirrored * amplitude));
                _coreBuffer[i] = ToPoint(Vector2.Lerp(from, to, t));
            }

            Apply(_mainLine, _mainBuffer, count, false, 0.20f);
            Apply(_accentLine, _accentBuffer, count, false, 0.11f);
            Apply(_coreLine, _coreBuffer, count, false, 0.045f);
        }

        private void DrawImpact(Vector2 center, float age)
        {
            float pop = 0.78f + 0.22f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / 0.12f));
            float baseRadius = _shape==BossHazard.Shape.Ring?_radius*pop*.96f:
                Mathf.Max(_radius*.84f,_bodyHeight*.16f)*pop;
            float rotation = age * (_kind == 3 ? -0.55f : 0.45f);
            int mainCount = PointCount;
            bool mainLoop = _kind != 3;

            for (int i = 0; i < mainCount; i++)
            {
                float t = (float)i / (mainCount - 1);
                float angle = (t * Mathf.PI * 2f) + rotation;
                float shapeScale = 1f;
                switch (_kind)
                {
                    case 0:
                        shapeScale += 0.07f * Mathf.Sin(angle * 3f + age * 2f);
                        break;
                    case 1:
                        shapeScale = (i % 4 == 0) ? 1.08f : 0.88f;
                        break;
                    case 2:
                        shapeScale = (i % 4 < 2) ? 0.86f : 1f;
                        break;
                    case 3:
                        angle = (t * Mathf.PI * 1.52f) + rotation + 0.35f;
                        break;
                    default:
                        break;
                }

                Vector2 point;
                if (_kind == 4)
                    point = center + new Vector2(Mathf.Cos(angle) * baseRadius, Mathf.Sin(angle) * baseRadius * 0.48f);
                else
                    point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (baseRadius * shapeScale);
                _mainBuffer[i] = ToPoint(point);
            }

            int coreCount = PointCount;
            float coreRadius = baseRadius * (_kind == 4 ? 0.28f : 0.58f);
            for (int i = 0; i < coreCount; i++)
            {
                float t = (float)i / (coreCount - 1);
                float angle = t * Mathf.PI * 2f - rotation * 1.6f;
                float shapeScale = _kind == 2 && (i % 2 == 0) ? 0.72f : 1f;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (coreRadius * shapeScale);
                if (_kind == 4)
                    point = center + new Vector2(Mathf.Cos(angle) * coreRadius, Mathf.Sin(angle) * coreRadius * 0.58f);
                _coreBuffer[i] = ToPoint(point);
            }

            int accentCount = PointCount;
            for (int i = 0; i < accentCount; i++)
            {
                float t = (float)i / (accentCount - 1);
                float angle = t * Mathf.PI * 2f + age * 1.8f + 1.1f;
                float scale;
                if (_kind == 1)
                    scale = (i % 3 == 0) ? 1.05f : 0.48f;
                else if (_kind == 2)
                    scale = (i % 4 < 2) ? 0.46f : 0.94f;
                else if (_kind == 3)
                {
                    angle = t * Mathf.PI * 1.34f - rotation - 0.5f;
                    scale = 0.67f;
                }
                else if (_kind == 4)
                    scale = 0.52f + 0.24f * Mathf.Sin(t * Mathf.PI * 3f + age * 2f);
                else
                    scale = 0.56f + 0.12f * Mathf.Sin(t * Mathf.PI * 4f + age * 2.8f);

                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (baseRadius * scale);
                if (_kind == 4)
                    point.y = center.y + Mathf.Sin(angle) * baseRadius * 0.48f * scale;
                _accentBuffer[i] = ToPoint(point);
            }

            Apply(_mainLine, _mainBuffer, mainCount, mainLoop, 0.20f);
            Apply(_coreLine, _coreBuffer, coreCount, true, 0.045f);
            Apply(_accentLine, _accentBuffer, accentCount, _kind != 3, 0.11f);
        }

        private void DrawMover(Vector2 center, float age)
        {
            RecordTrail(center);
            float r = Mathf.Max(0.08f, Mathf.Max(_radius * 0.68f * _bodyScale,_bodyHeight*.065f));
            float rotation = age * (_kind == 3 ? -2.1f : 1.7f);
            int count = PointCount;
            bool loop = _kind != 3;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                float angle = t * Mathf.PI * 2f + rotation;
                float scale = 1f;
                switch (_kind)
                {
                    case 0:
                        scale = 0.56f + 0.44f * t;
                        break;
                    case 1:
                        scale = (i % 4 == 0) ? 1.12f : 0.72f;
                        break;
                    case 2:
                        scale = (i % 4 < 2) ? 0.76f : 1f;
                        break;
                    case 3:
                        angle = t * Mathf.PI * 1.45f + rotation;
                        break;
                    default:
                        break;
                }

                Vector2 point;
                if (_kind == 4)
                    point = center + new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r * 0.50f);
                else
                    point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (r * scale);
                _mainBuffer[i] = ToPoint(point);
            }

            Vector2 movement = center - _lastCenter;
            bool hasDirection = _hasMoverSample && movement.sqrMagnitude >= 0.00000001f;
            _lastCenter = center;
            _hasMoverSample = true;

            Apply(_mainLine, _mainBuffer, count, loop, 0.20f);
            if (!hasDirection)
            {
                _coreLine.enabled = false;
                _accentLine.enabled = false;
                return;
            }

            Vector2 direction = movement.normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x);
            for (int i = 0; i < PointCount; i++)
            {
                float t = (float)i / (PointCount - 1);
                float side = ((i & 1) == 0 ? 1f : -1f) * Mathf.Sin(t * Mathf.PI);
                Vector2 point = center - direction * (r * 0.9f * (1f - t)) + normal * (side * r * 0.36f);
                _coreBuffer[i] = ToPoint(point);
            }

            int trailCount = FillTrailBuffer();
            Apply(_coreLine, _coreBuffer, PointCount, false, 0.045f);
            if (trailCount >= 2)
                Apply(_accentLine, _trailBuffer, TrailCapacity, false, 0.11f);
            else
                _accentLine.enabled = false;

        }

        private void RecordTrail(Vector2 center)
        {
            if (_trailCount > 0)
            {
                int previousIndex = (_trailNext - 1 + TrailCapacity) % TrailCapacity;
                Vector3 previous = _trailHistory[previousIndex];
                float dx = previous.x - center.x;
                float dy = previous.y - center.y;
                if (dx * dx + dy * dy < 0.0004f)
                    return;
            }

            _trailHistory[_trailNext] = ToPoint(center);
            _trailNext = (_trailNext + 1) % TrailCapacity;
            if (_trailCount < TrailCapacity)
                _trailCount++;
        }

        private int FillTrailBuffer()
        {
            int first = (_trailNext - _trailCount + TrailCapacity) % TrailCapacity;
            int newest = (_trailNext - 1 + TrailCapacity) % TrailCapacity;
            for (int i = 0; i < TrailCapacity; i++)
            {
                int trailIndex = i < _trailCount ? (first + i) % TrailCapacity : newest;
                _trailBuffer[i] = _trailHistory[trailIndex];
            }
            return _trailCount;
        }

        private void Apply(LineRenderer line, Vector3[] buffer, int count, bool loop, float width)
        {
            line.loop = loop;
            line.widthMultiplier = width*Mathf.Min(_bodyScale,1.65f);
            line.positionCount = count;
            line.SetPositions(buffer);
            line.enabled = true;
        }

        private static Vector3 ToPoint(Vector2 point) => new Vector3(point.x, point.y, 0f);

        private void OnDisable()
        {
            Hide();
        }
    }
}
