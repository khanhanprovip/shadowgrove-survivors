using System.Collections.Generic;
using DG.Tweening;
using RGame.Framework;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    [CreateAssetMenu(fileName = "HitAnimationConfig", menuName = "RGame/RoguelikeKit/Animation/HitAnimation")]
    public class HitAnimationSO : DescriptionBaseSO
    {
        [Header("Animation Timing")]
        [SerializeField] private float mHitDuration = 0.1f;
        [SerializeField] private float mFadeDuration = 0.1f;

        [Header("Colors")]
        [SerializeField] private Color mHitColor = Color.white;
        [SerializeField] private Color mNormalColor = new Color(0, 0, 0, 0);

        [Header("Easing")]
        [SerializeField] private Ease mHitEase = Ease.Linear;
        [SerializeField] private Ease mFadeEase = Ease.Linear;

        private readonly int _blendColorId = Shader.PropertyToID("_BlendColor");

        private readonly Dictionary<SpriteRenderer, Sequence> _seqMap = new();
        private readonly Dictionary<SpriteRenderer, Color> _baseColors = new();
        internal Color RestingColor(SpriteRenderer renderer)=>_baseColors.TryGetValue(renderer,out var color)?color:renderer.color;
        internal Color NormalBlend=>mNormalColor;

        public void OnEnable()
        {
            _seqMap.Clear();
            _baseColors.Clear();
        }

        public void OnDisable()
        {
            foreach (var seq in _seqMap.Values) seq.Kill();
            foreach (var pair in _baseColors)
                if (pair.Key) pair.Key.color = pair.Value;
            _seqMap.Clear();
            _baseColors.Clear();
        }

        public void Play(SpriteRenderer sr)
        {
            Play(sr, mHitColor, mHitDuration, mFadeDuration);
        }

        public void Play(SpriteRenderer sr, Color hitColor, float hitDuration, float fadeDuration,
            bool unscaled = false)
        {
            if (!sr) return;

            // if already animating, stop first
            Stop(sr);

            Sequence seq = DOTween.Sequence().SetUpdate(unscaled);

            if (!sr.material.HasProperty(_blendColorId))
            {
                // Built-in sprite shaders tint through vertex color, not _BlendColor.
                // Preserve alpha so damage feedback never hides the character.
                Color normal = sr.color;
                float strength = Mathf.Clamp01(Mathf.Max(hitColor.r, hitColor.g, hitColor.b));
                Color hue = strength > 0f
                    ? new Color(hitColor.r / strength, hitColor.g / strength, hitColor.b / strength, normal.a)
                    : normal;
                Color target = Color.Lerp(normal, hue, strength);
                target.a = normal.a;
                _baseColors[sr] = normal;
                seq.Append(sr.DOColor(target, hitDuration).SetEase(mHitEase));
                seq.Append(sr.DOColor(normal, fadeDuration).SetEase(mFadeEase));
                _seqMap[sr] = seq;
                return;
            }

            seq.Append(
                DOTween.To(
                        () => sr.material.GetColor(_blendColorId),
                        c => sr.material.SetColor(_blendColorId, c),
                        hitColor,
                        hitDuration)
                    .SetEase(mHitEase)
            );

            seq.Append(
                DOTween.To(
                        () => sr.material.GetColor(_blendColorId),
                        c => sr.material.SetColor(_blendColorId, c),
                        mNormalColor,
                        fadeDuration)
                    .SetEase(mFadeEase)
            );

            _seqMap[sr] = seq;
        }

        public void Stop(SpriteRenderer sr)
        {
            if (_seqMap.TryGetValue(sr, out var seq))
            {
                seq.Kill();
                _seqMap.Remove(sr);
            }

            if (sr)
            {
                if (_baseColors.TryGetValue(sr, out var normal)) sr.color = normal;
                ResetMaterial(sr.material);
            }
            _baseColors.Remove(sr);
        }

        public void Remove(SpriteRenderer sr) => Stop(sr);

        public void ResetMaterial(Material mat)
        {
            if (mat && mat.HasProperty(_blendColorId)) mat.SetColor(_blendColorId, mNormalColor);
        }
    }
}
