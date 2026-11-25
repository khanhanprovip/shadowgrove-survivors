using System;
using RGame.CommonStat;
using RGame.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace RGame.RoguelikeKit
{
    public class EnemyHit : MonoBehaviour
    {
        [SerializeField] private HitAnimationSO _hitAnimationSO;
        [SerializeField] private DissolveAnimationSO _dissolveAnimationSO;
        [SerializeField] private SpriteRenderer _spriteRenderers;
        [SerializeField] private PoolRuntimeSO poolRuntimeSo;
        private CommonStatRuntimeSO _selfStat;
        
        public UnityAction OnDeath { get; set; }
        public UnityAction OnDeathEnd { get; set; }
        
        private bool _isDead;
        private EndlessBossEnemy _boss;
        private void Awake(){_boss=GetComponentInParent<EndlessBossEnemy>();}
        public void SetPool(PoolRuntimeSO pool) { poolRuntimeSo=pool; }

        private void OnEnable()
        {
            _dissolveAnimationSO.ResetMaterial(_spriteRenderers);
            _hitAnimationSO.ResetMaterial(_spriteRenderers.material);
        }

        public void SetStat(CommonStatRuntimeSO _stat)
        {
            _selfStat = _stat;
        }

        private void OnDisable()
        {
            _isDead = false;
            _hitAnimationSO.Stop(_spriteRenderers);
            _dissolveAnimationSO.Stop(_spriteRenderers);
        }

        public void Hit(int damage)
        {
            if(_isDead||(_boss!=null&&_boss.Entering)) return;
            
            _selfStat.ModifyValue("HP",damage * -1);
            ShadowgroveAudio.Play(SoundCue.EnemyHit);
            DATN.World.JadeImpact.Show(_spriteRenderers);

            var go = poolRuntimeSo.Request("DamagePopup");
            go.GetComponent<DamagePopup>().Show(transform.position,damage);
            
            if (_selfStat.GetValue("HP") <= 0)
            {
                _isDead = true;
                OnDeath?.Invoke();
                
                _dissolveAnimationSO.Play(_spriteRenderers, OnDeathEnd);
            }
            else
            {
                _hitAnimationSO.Play(_spriteRenderers);
            }
        }

    }
}
