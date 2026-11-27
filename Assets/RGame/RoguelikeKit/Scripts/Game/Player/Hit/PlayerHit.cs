#region

using System;
using DG.Tweening;
using RGame.CommonStat;
using RGame.Framework;
using UnityEngine;
using UnityEngine.Events;

#endregion

namespace RGame.RoguelikeKit
{
    public class PlayerHit : MonoBehaviour
    {
        [SerializeField] private CommonStatRuntimeSO mStat;
        [SerializeField] private HitAnimationSO mHitAnimationSO;
        [SerializeField] private SpriteRenderer[] mSpriteRenderers;
        [SerializeField] private SpriteRenderer _shadowSpriteRenderer;
        [SerializeField] private VoidEventChannelSO _playerDieEventChannel;
        [SerializeField] private VoidEventChannelSO _onGameOverEventChannel;
        
        public UnityAction OnDie;

        private bool _isDie;
        internal SpriteRenderer ContactShadow => _shadowSpriteRenderer;
        internal Color RunRestingColor(SpriteRenderer renderer)=>mHitAnimationSO.RestingColor(renderer);
        private float _nextHitFlashAt;
        private static readonly Color HitTint = new Color(0.28f, 0.02f, 0.02f, 0f);
        
        public void OnHit(int damage)
        {
            if (_isDie) return;
            
            // Purchased and in-run armor reductions may stack past zero.
            // A hit must never turn into healing when that multiplier is negative.
            damage = Mathf.Max(1, (int)(Mathf.Max(0,damage) * Mathf.Max(0,mStat.GetValue("Armor")) * 0.01f));
            
            mStat.ModifyValue("HP",damage * -1);
            ShadowgroveAudio.Play(SoundCue.PlayerHit);

            if (mStat.GetValue("HP") <= 0)
            {
                OnDie?.Invoke();
                _isDie = true;
                
                foreach (var spriteRenderer in mSpriteRenderers)
                {
                    mHitAnimationSO.Stop(spriteRenderer);
                    var material = spriteRenderer.material;
                    Tween fade = material.HasProperty("_Alpha")
                        ? material.DOFloat(0f, "_Alpha", 0.2f)
                        : spriteRenderer.DOFade(0f, 0.2f);
                    fade.SetEase(Ease.Linear).SetUpdate(true).SetLink(gameObject);
                }

                _shadowSpriteRenderer.DOFade(0, 0.8f).SetUpdate(true).SetLink(gameObject).OnComplete(() =>
                {
                    _onGameOverEventChannel.RaiseEvent();
                });

                _playerDieEventChannel.RaiseEvent();
            }
            else if (Time.unscaledTime >= _nextHitFlashAt)
            {
                // Limit only the visual feedback; every hit still applies damage.
                _nextHitFlashAt = Time.unscaledTime + 0.35f;
                foreach (var spriteRenderer in mSpriteRenderers)
                {
                    mHitAnimationSO.Play(spriteRenderer, HitTint, 0.025f, 0.095f, unscaled: true);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var spriteRenderer in mSpriteRenderers)
            {
                mHitAnimationSO.Remove(spriteRenderer);
            }
        }
    }
}
