using RGame.CommonStat;
using RGame.Framework;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    /// <summary>
    /// Ranged weapon that spawns and launches a projectile at the player.
    /// </summary>
    public class EnemyRangeWeapon : BaseEnemyWeapon
    {
        [SerializeField] private PoolRuntimeSO _poolRuntime;
        [SerializeField] private string _magicKey;

        protected override void FixedUpdate()
        {
            var player = _globalConfig.GlobalPlayer;
            _isPlayerInRange = player != null && _stat != null &&
                               Vector3.Distance(transform.position, player.transform.position) <=
                               _stat.GetValue("AttackRange");
            base.FixedUpdate();
        }

        protected override void PerformAttack()
        {
            if (_poolRuntime == null || string.IsNullOrEmpty(_magicKey))
                return;

            GameObject projectile = _poolRuntime.Request(_magicKey);
            if (projectile == null) return;
            projectile.transform.position = transform.position;
            EnemyMagic magic = projectile.GetComponent<EnemyMagic>();
           // magic?.Init(_stat.GetValue("Attack"));
        }
    }
}
