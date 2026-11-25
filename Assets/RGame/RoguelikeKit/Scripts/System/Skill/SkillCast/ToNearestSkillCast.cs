using RGame.CommonStat;
using RGame.Framework;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    public class ToNearestSkillCast : SkillCast
    {
        public override bool CanCast()
        {
            if (!base.CanCast()) return false;
            var profile = _globalConfig.GlobalPlayer.GetComponent<StarterWeaponProfile>();
            return profile != null ? profile.Targets(_enemySystem).Count > 0 :
                _enemySystem.GetNearestEnemies(_globalConfig.GlobalPlayer.transform.position, 12, 7).Count > 0;
        }

        public override void Cast(SkillDataSO skillData)
        {
            base.Cast(skillData);
            
            int amount = _stat.GetValue("Amount") + skillData.Amount;
            int damage = (int)(_stat.GetValue("Might") * skillData.Damage * 0.01f) + 1;
            float velocity = skillData.Velocity * _stat.GetValue("SkillSpeed") * 0.01f;
            
            var profile = _globalConfig.GlobalPlayer.GetComponent<StarterWeaponProfile>();
            var enemies = profile != null ? profile.Targets(_enemySystem) :
                _enemySystem.GetNearestEnemies(_globalConfig.GlobalPlayer.transform.position, 12, 7);

            if (profile != null && profile.Kind == StarterWeaponKind.ThornSlash)
            {
                if (enemies.Count == 0) return;
                Vector2 origin = _globalConfig.GlobalPlayer.transform.position;
                Vector2 direction = ((Vector2)enemies[0].transform.position - origin).normalized;
                float halfAngle = Mathf.Clamp(55f + (_stat.GetValue("Area") + skillData.Area - 100) * .2f, 55f, 85f);
                DATN.World.ThornSlashEffect.Show(profile, direction);
                int hitCount = 0;
                foreach (var enemy in enemies)
                {
                    if (Vector2.Angle(direction, (Vector2)enemy.transform.position - origin) > halfAngle) continue;
                    enemy.MyHit.Hit(damage);
                    if (++hitCount >= 2 + amount) break;
                }
                return;
            }
            
            for (int i = 0; i < amount && i< enemies.Count; i++)
            {
                var go = _pool.Request(Key);
                var skill = go.GetComponent<ToNearestSkill>();

                if (skillData.CurrentState is SkillCurrentState.Mixed)
                {
                    skill.GetComponent<SpriteRenderer>().color = Color.red;
                }
                
                skill.transform.localScale = skill.transform.localScale * (_stat.GetValue("Area") + skillData.Area) * 0.01f;                skill.transform.position = _globalConfig.GlobalPlayer.transform.position;
                skill.OnDeath += HandleSkillDeath;
                skill.Init(velocity,damage,enemies[i].transform.position);
                if (profile != null) skill.ConfigureStarter(profile);
            }
        }

        public override void MixSkill()
        {
            base.MixSkill();
            _mySkillData.CD = Mathf.Max(0.3f, _mySkillData.CD * 0.8f);
        }
    }
}
