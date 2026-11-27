using System.Collections.Generic;
using RGame.CommonStat;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    public enum StarterWeaponKind { Legacy, ThornSlash, DarkOrb, Arrow }

    public struct StarterWeaponSnapshot
    {
        public int Damage;
        public float Cooldown;
        public float Range;
        public float Velocity;
        public int Amount;

        public StarterWeaponSnapshot(int damage, float cooldown, float range, float velocity, int amount)
        {
            Damage = damage;
            Cooldown = cooldown;
            Range = range;
            Velocity = velocity;
            Amount = amount;
        }
    }

    // Each survivor owns one automatic signature. The run uses the shared skill
    // cast/timer key, while this profile supplies hero-specific levels and visuals.
    public sealed class StarterWeaponProfile : MonoBehaviour
    {
        public StarterWeaponKind Kind;
        public float Range;
        public Sprite Icon;
        public Sprite Effect;
        public bool UsesLegacyStarter { get; private set; }

        // Captured by RunCheckpointWorld with the hero component. Resume configures
        // the starter first, then restores stats and this flag, so the passive is
        // never stacked on the saved run.
        private bool _passiveApplied;
        private int _passiveStatInstanceId;
        private int _passiveBaseline;

        public float SignatureProjectileSpeedMultiplier =>
            Kind == StarterWeaponKind.DarkOrb && !UsesLegacyStarter ? 1.12f : 1f;

        public void Configure(SkillDataSO data) => Configure(data, null);

        public void Configure(SkillDataSO data, CommonStatRuntimeSO playerStat)
        {
            if (data == null || Kind == StarterWeaponKind.Legacy) return;

            UsesLegacyStarter = false;
            var firstLevel = GetSnapshot(Kind, 1);
            data.StarterWeapon = Kind;
            data.AttackRange = firstLevel.Range;
            data.SkillIcon = Icon;
            data.Damage = firstLevel.Damage;
            data.CD = firstLevel.Cooldown;
            data.Velocity = firstLevel.Velocity;
            data.Amount = firstLevel.Amount;
            data.MixAttributeSkillKey = string.Empty;
            data.UpgradeAttribute = CreateFiveLevelTrack();
            Range = firstLevel.Range;

            ApplyFixedStatPassive(playerStat);
        }

        /// <summary>
        /// Restores the authored ten-step ToNearest track for a legacy checkpoint.
        /// Saved skill fields and the selected hero's current combat profile remain
        /// available for the checkpoint restore to apply afterwards.
        /// </summary>
        public bool ConfigureLegacyStarter(SkillDataSO runtimeData, SkillDataSO legacyDefinition)
        {
            if (runtimeData == null || legacyDefinition == null ||
                runtimeData.Key != "ToNearestSkill" || legacyDefinition.Key != "ToNearestSkill" ||
                legacyDefinition.UpgradeAttribute == null || legacyDefinition.UpgradeAttribute.Count < 10)
                return false;

            runtimeData.MixAttributeSkillKey = legacyDefinition.MixAttributeSkillKey;
            runtimeData.UpgradeAttribute = new List<AttackSkillAttributeMod>(legacyDefinition.UpgradeAttribute);
            UsesLegacyStarter = true;
            return runtimeData.UpgradeAttribute.Count == legacyDefinition.UpgradeAttribute.Count;
        }

        public bool ApplySignatureUpgrade(SkillDataSO data, int targetLevel)
        {
            if (data == null || UsesLegacyStarter || Kind == StarterWeaponKind.Legacy || data.StarterWeapon != Kind ||
                targetLevel != data.Level + 1 || targetLevel < 2 || targetLevel > 5)
                return false;

            var next = GetSnapshot(Kind, targetLevel);
            if (next.Range <= 0f) return false;

            data.Damage = next.Damage;
            data.CD = next.Cooldown;
            data.AttackRange = next.Range;
            data.Velocity = next.Velocity;
            data.Amount = next.Amount;
            Range = next.Range;
            return true;
        }

        public List<BaseEnemy> Targets(EnemySystem system) => Targets(system, Range);

        public List<BaseEnemy> Targets(EnemySystem system, float attackRange)
        {
            if (system == null || attackRange <= 0f) return new List<BaseEnemy>();

            var targets = system.GetNearestEnemies(transform.position, attackRange, attackRange);
            targets.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeInHierarchy ||
                enemy.StatRuntime == null || enemy.StatRuntime.GetValue("HP") <= 0 ||
                ((Vector2)(enemy.transform.position - transform.position)).sqrMagnitude > attackRange * attackRange);
            return targets;
        }

        public static StarterWeaponSnapshot GetSnapshot(StarterWeaponKind kind, int level)
        {
            level = Mathf.Clamp(level, 1, 5);
            return kind switch
            {
                StarterWeaponKind.ThornSlash => level switch
                {
                    1 => new StarterWeaponSnapshot(12, .90f, 2.2f, 0f, 1),
                    2 => new StarterWeaponSnapshot(14, .90f, 2.2f, 0f, 1),
                    3 => new StarterWeaponSnapshot(14, .90f, 2.2f, 0f, 2),
                    4 => new StarterWeaponSnapshot(14, .90f, 2.4f, 0f, 2),
                    5 => new StarterWeaponSnapshot(16, .81f, 2.4f, 0f, 2),
                    _ => new StarterWeaponSnapshot(0, 0f, 0f, 0f, 0)
                },
                StarterWeaponKind.DarkOrb => level switch
                {
                    1 => new StarterWeaponSnapshot(10, 1.15f, 7.0f, 9.0f, 1),
                    2 => new StarterWeaponSnapshot(12, 1.15f, 7.0f, 9.0f, 1),
                    3 => new StarterWeaponSnapshot(12, 1.15f, 7.0f, 10.8f, 1),
                    4 => new StarterWeaponSnapshot(12, 1.08f, 7.5f, 10.8f, 1),
                    5 => new StarterWeaponSnapshot(13, 1.08f, 7.5f, 10.8f, 2),
                    _ => new StarterWeaponSnapshot(0, 0f, 0f, 0f, 0)
                },
                StarterWeaponKind.Arrow => level switch
                {
                    1 => new StarterWeaponSnapshot(8, .85f, 10.5f, 16.0f, 1),
                    2 => new StarterWeaponSnapshot(10, .85f, 10.5f, 16.0f, 1),
                    3 => new StarterWeaponSnapshot(10, .85f, 10.5f, 18.4f, 1),
                    4 => new StarterWeaponSnapshot(10, .85f, 12.0f, 18.4f, 1),
                    5 => new StarterWeaponSnapshot(12, .78f, 12.0f, 18.4f, 2),
                    _ => new StarterWeaponSnapshot(0, 0f, 0f, 0f, 0)
                },
                _ => new StarterWeaponSnapshot(0, 0f, 0f, 0f, 0)
            };
        }

        public static string Name(StarterWeaponKind kind, bool vi) => HeroLocalization.SkillName(kind, vi);
        public static string Description(StarterWeaponKind kind, bool vi) => HeroLocalization.SkillShort(kind, vi);
        public static string LongDescription(StarterWeaponKind kind, bool vi) => HeroLocalization.SkillLong(kind, vi);
        public static string PassiveName(StarterWeaponKind kind, bool vi) => HeroLocalization.PassiveName(kind, vi);
        public static string PassiveDescription(StarterWeaponKind kind, bool vi) => HeroLocalization.PassiveLong(kind, vi);
        public static string UpgradeTitle(StarterWeaponKind kind, int level, bool vi) => HeroLocalization.UpgradeTitle(kind, level, vi);
        public static string UpgradeDescription(StarterWeaponKind kind, int level, bool vi) => HeroLocalization.UpgradeDescription(kind, level, vi);
        public static string MaxLevelDescription(StarterWeaponKind kind, bool vi) => HeroLocalization.MaxLevel(kind, vi);

        private void ApplyFixedStatPassive(CommonStatRuntimeSO playerStat)
        {
            if (playerStat == null) return;

            string statKey;
            int amount;
            switch (Kind)
            {
                case StarterWeaponKind.ThornSlash:
                    statKey = "Recovery";
                    amount = 1;
                    break;
                case StarterWeaponKind.Arrow:
                    statKey = "MoveSpeed";
                    amount = 8;
                    break;
                default:
                    return;
            }

            int current = playerStat.GetValue(statKey);
            int instanceId = playerStat.GetInstanceID();
            if (_passiveApplied && _passiveStatInstanceId == instanceId && current > _passiveBaseline)
                return;

            // A reset returns this stat to its pre-passive value. Detect that case
            // so a reused profile can grant the fixed passive once in the new run.
            _passiveBaseline = current;
            playerStat.ModifyValue(statKey, amount);
            _passiveStatInstanceId = instanceId;
            _passiveApplied = true;
        }

        private static List<AttackSkillAttributeMod> CreateFiveLevelTrack() => new()
        {
            new AttackSkillAttributeMod { skillAttributeType = UpgradeSkillAttributeType.SignatureLevel, IntValue = 2 },
            new AttackSkillAttributeMod { skillAttributeType = UpgradeSkillAttributeType.SignatureLevel, IntValue = 3 },
            new AttackSkillAttributeMod { skillAttributeType = UpgradeSkillAttributeType.SignatureLevel, IntValue = 4 },
            new AttackSkillAttributeMod { skillAttributeType = UpgradeSkillAttributeType.SignatureLevel, IntValue = 5 }
        };
    }
}
