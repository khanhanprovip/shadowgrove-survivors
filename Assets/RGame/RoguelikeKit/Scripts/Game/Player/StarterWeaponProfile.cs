using System.Collections.Generic;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    public enum StarterWeaponKind { Legacy, ThornSlash, DarkOrb, Arrow }

    // Owned by the actual survivor prefab. The shared upgrade key remains stable
    // for the kit's timers/evolution; only this round's copied data is configured.
    public sealed class StarterWeaponProfile : MonoBehaviour
    {
        public StarterWeaponKind Kind;
        public float Range;
        public Sprite Icon;
        public Sprite Effect;

        public void Configure(SkillDataSO data)
        {
            data.StarterWeapon = Kind;
            data.AttackRange = Range;
            data.SkillIcon = Icon;
            data.Velocity = Kind == StarterWeaponKind.Arrow ? 16f : Kind == StarterWeaponKind.DarkOrb ? 9f : 0f;
        }

        public List<BaseEnemy> Targets(EnemySystem system)
        {
            var targets = system.GetNearestEnemies(transform.position, Range, Range);
            targets.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeInHierarchy ||
                enemy.StatRuntime == null || enemy.StatRuntime.GetValue("HP") <= 0 ||
                ((Vector2)(enemy.transform.position - transform.position)).sqrMagnitude > Range * Range);
            return targets;
        }

        public static string Name(StarterWeaponKind kind, bool vi) => kind switch
        {
            StarterWeaponKind.ThornSlash => vi ? "Nhát Chém Gai" : "Thorn Cleave",
            StarterWeaponKind.DarkOrb => vi ? "Cầu Bóng Đêm" : "Dark Orb",
            StarterWeaponKind.Arrow => vi ? "Tên Tàn Lửa" : "Ember Arrow",
            _ => ""
        };

        public static string Description(StarterWeaponKind kind, bool vi) => kind switch
        {
            StarterWeaponKind.ThornSlash => vi ? "Chém hình quạt vào quái gần nhất; trúng nhiều quái phía trước." : "Cleave toward the nearest enemy, striking enemies in front.",
            StarterWeaponKind.DarkOrb => vi ? "Bắn cầu bóng đêm về quái trong tầm." : "Fire a dark orb at an enemy within range.",
            StarterWeaponKind.Arrow => vi ? "Bắn tên nhanh về quái trong tầm xa." : "Fire a swift arrow at a distant enemy.",
            _ => ""
        };
    }
}
