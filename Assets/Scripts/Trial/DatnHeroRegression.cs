using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RGame.RoguelikeKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DATN.Trial
{
    public sealed partial class DatnRegression
    {
        private static readonly BindingFlags HeroRegressionFlags =
            BindingFlags.Instance | BindingFlags.NonPublic;

        private IEnumerator CheckHeroKits()
        {
            yield return EnterHeroRunWithCopyReview();
            yield return CheckHeroKitRuntime("Rowan");
            _round.ExitToMainMenu();
            yield return WaitForMenu();

            foreach (string hero in new[] { "Lyra", "Kael" })
            {
                yield return EnterRound(0, hero);
                yield return CheckHeroKitRuntime(hero);
                _round.ExitToMainMenu();
                yield return WaitForMenu();
            }
        }

        private IEnumerator EnterHeroRunWithCopyReview()
        {
            Click(ButtonIn(FindFirstObjectByType<UIMainMenu>(), "ButtonNewGame"));
            yield return Wait(() => FindFirstObjectByType<CharacterSelectPanel>()?.GetComponentsInChildren<CharacterItem>().Length == 3,
                "Three hero cards with signature copy");

            var characterPanel = FindFirstObjectByType<CharacterSelectPanel>();
            var cards = characterPanel.GetComponentsInChildren<CharacterItem>();
            yield return SelectLanguage("vi");
            yield return null;
            foreach (string hero in Heroes)
            {
                var card = cards.Single(item => item.GetComponentsInChildren<TMP_Text>().Any(label => label.text == hero));
                Require(HasText(card, "Nội tại") && HasText(card, "Tự động"),
                    hero + " selection card shows fixed passive and automatic signature in Vietnamese");
            }
            yield return CaptureFrame("hero-selection-vi");

            yield return SelectLanguage("en");
            yield return null;
            foreach (string hero in Heroes)
            {
                var card = cards.Single(item => item.GetComponentsInChildren<TMP_Text>().Any(label => label.text == hero));
                Require(HasText(card, "Passive") && HasText(card, "Auto"),
                    hero + " selection card shows fixed passive and automatic signature in English");
            }
            yield return CaptureFrame("hero-selection-en");

            var rowanCard = cards.Single(item => item.GetComponentsInChildren<TMP_Text>().Any(label => label.text == "Rowan"));
            Click(rowanCard.GetComponent<Button>());
            Click(ButtonIn(characterPanel, "ButtonConfirm"));
            yield return Wait(() => FindFirstObjectByType<LevelSelectPanel>()?.GetComponentsInChildren<LevelItem>().Length == Levels.Length,
                "Level selection after choosing Rowan");

            var levelPanel = FindFirstObjectByType<LevelSelectPanel>();
            var scroll = levelPanel.GetComponentInChildren<ScrollRect>();
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
            Canvas.ForceUpdateCanvases();
            var trial = levelPanel.GetComponentsInChildren<LevelItem>().Single(item => item.Config.name == "TrialLevel");
            yield return Wait(() => CanClick(trial.GetComponent<Button>()), "Trial Forest receives clicks");
            Click(trial.GetComponent<Button>());
            Click(ButtonIn(levelPanel, "ButtonConfirm"));
            yield return Wait(() => GameObject.Find("Forest_Lobby") != null, "Hero lobby opens");
            yield return null;

            var lobby = GameObject.Find("Forest_Lobby").transform;
            yield return SelectLanguage("vi");
            yield return null;
            Require(HasText(lobby, "Nội tại") && HasText(lobby, "Tự động"),
                "Hero lobby shows passive and automatic signature in Vietnamese");
            yield return CaptureFrame("hero-lobby-vi");
            yield return SelectLanguage("en");
            yield return null;
            Require(HasText(lobby, "Passive") && HasText(lobby, "Auto"),
                "Hero lobby shows passive and automatic signature in English");
            yield return CaptureFrame("hero-lobby-en");

            var start = ButtonIn(lobby, "StartBattleBtn");
            yield return Wait(() => CanClick(start), "Lobby start button receives clicks");
            Click(start);
            yield return Wait(() =>
            {
                _round = FindFirstObjectByType<TrialController>();
                return _round != null && _round.GlobalConfig.GlobalPlayer != null;
            }, "Rowan and the combat systems are ready");
            Require(_round.GlobalConfig.GlobalPlayer.name.Contains("Rowan"), "Hero copy review enters Rowan's run");
            yield return Wait(() => FindFirstObjectByType<BaseEnemy>() != null, "First enemy spawns for Rowan");
        }

        private IEnumerator CheckHeroKitRuntime(string hero)
        {
            _round.Pause();
            yield return Wait(() => _round.IsPaused, hero + " combat pauses for deterministic checks");

            var player = _round.GlobalConfig.GlobalPlayer;
            var profile = player.GetComponent<StarterWeaponProfile>();
            var manager = FindFirstObjectByType<SkillManager>();
            var caster = manager.GetComponentInChildren<ToNearestSkillCast>();
            var runtimeSkills = (Dictionary<string, SkillDataSO>)typeof(SkillManager)
                .GetField("_skillDataDictionary", HeroRegressionFlags).GetValue(manager);
            var skill = runtimeSkills[caster.Key];
            var expectedKind = HeroLocalization.KindForHeroId(hero);
            Require(profile != null && profile.Kind == expectedKind, hero + " receives its own weapon profile");
            Require(skill.Level == 1 && skill.StarterWeapon == expectedKind && skill.UpgradeAttribute.Count == 4,
                hero + " starts at signature rank 1 with four upgrades and a five-rank cap");
            Require(skill.MixAttributeSkillKey == string.Empty,
                hero + " signature has no unapproved evolution path");

            int[] baseline = { skill.Damage, Mathf.RoundToInt(skill.CD * 1000f), Mathf.RoundToInt(skill.AttackRange * 100f),
                Mathf.RoundToInt(skill.Velocity * 100f), skill.Amount };
            string passiveStat = hero == "Rowan" ? "Recovery" : hero == "Kael" ? "MoveSpeed" : null;
            if (passiveStat != null)
            {
                var baselineField = typeof(StarterWeaponProfile).GetField("_passiveBaseline", HeroRegressionFlags);
                int passiveBaseline = (int)baselineField.GetValue(profile);
                int passiveAmount = hero == "Rowan" ? 1 : 8;
                profile.Configure(skill, _round.PlayerStat);
                int once = _round.PlayerStat.GetValue(passiveStat);
                profile.Configure(skill, _round.PlayerStat);
                int twice = _round.PlayerStat.GetValue(passiveStat);
                Require(once == passiveBaseline + passiveAmount && twice == once,
                    hero + " fixed passive remains applied exactly once after repeated Configure");
            }
            else
            {
                Require(Mathf.Approximately(profile.SignatureProjectileSpeedMultiplier, 1.12f),
                    "Lyra's fixed passive increases Dark Orb speed by 12% without changing another stat");
                profile.Configure(skill, _round.PlayerStat);
                profile.Configure(skill, _round.PlayerStat);
            }

            var filter = typeof(SkillManager).GetMethod("IsSignatureAvailableForSelectedHero", HeroRegressionFlags);
            var ownedSkills = (HashSet<string>)typeof(SkillManager).GetField("_activeSkills", HeroRegressionFlags).GetValue(manager);
            Require(ownedSkills.Contains(skill.Key), hero + " owns its automatic signature at run start");
            var ownKind = skill.StarterWeapon;
            skill.StarterWeapon = ownKind == StarterWeaponKind.ThornSlash
                ? StarterWeaponKind.DarkOrb : StarterWeaponKind.ThornSlash;
            bool mismatchedAllowed = (bool)filter.Invoke(manager, new object[] { skill, false });
            int levelBeforeRejected = skill.Level;
            manager.UpgradeSkill(skill.Key);
            Require(!mismatchedAllowed && skill.Level == levelBeforeRejected,
                hero + " signature is filtered and cannot be upgraded by another hero");
            skill.StarterWeapon = ownKind;

            var enemySystem = (EnemySystem)typeof(SkillCast)
                .GetField("_enemySystem", HeroRegressionFlags).GetValue(caster);
            var existingEnemies = enemySystem.GetEnemies();
            enemySystem.ClearEnemies();
            int projectileCount = FindObjectsByType<ToNearestSkill>(FindObjectsSortMode.None).Length;
            Require(!caster.CanCast(), hero + " does not cast when no valid enemy is available");
            typeof(SkillManager).GetMethod("CreateSkill", HeroRegressionFlags)
                .Invoke(manager, new object[] { skill.Key });
            Require(FindObjectsByType<ToNearestSkill>(FindObjectsSortMode.None).Length == projectileCount,
                hero + " creates no projectile when the arena has no enemies");

            var pool = Resources.FindObjectsOfTypeAll<RGame.Framework.PoolRuntimeSO>().Single(asset => asset.name == "Pool");
            var definitions = (List<RGame.Framework.PoolDefinition>)typeof(RGame.Framework.PoolRuntimeSO)
                .GetField("_poolDefinitions", HeroRegressionFlags).GetValue(pool);
            var enemyDefinition = definitions.First(item => item.Pool.Prefab.GetComponent<BaseEnemy>() != null);
            var targetObject = pool.Request(enemyDefinition.Key);
            Require(targetObject != null, hero + " range test obtains a pooled enemy");
            var target = targetObject.GetComponent<BaseEnemy>();
            foreach (var behaviour in targetObject.GetComponentsInChildren<MonoBehaviour>())
                if (behaviour is not BaseEnemy) behaviour.enabled = false;
            foreach (var collider in targetObject.GetComponentsInChildren<Collider2D>()) collider.enabled = false;
            target.transform.position = player.transform.position + Vector3.right;
            enemySystem.AddEnemy(target);
            yield return null;
            Require(target.StatRuntime != null, hero + " pooled enemy initializes before range checks");
            target.StatRuntime.ModifyValue("HP", 10000, true);
            target.StatRuntime.ModifyValue("HP", 10000);
            target.enabled = false;
            if (target.MyRigidbody2D != null) target.MyRigidbody2D.linearVelocity = Vector2.zero;
            target.transform.position = player.transform.position + Vector3.right * (profile.Range * .6f);
            Require(caster.CanCast(), hero + " detects an enemy inside its configured range");
            target.transform.position = player.transform.position + Vector3.right * (profile.Range + .1f);
            Require(!caster.CanCast(), hero + " rejects an enemy just outside its configured range");
            target.StatRuntime.ModifyValue("HP", -target.StatRuntime.GetValue("HP"));
            Require(!caster.CanCast(), hero + " rejects a defeated enemy");
            enemySystem.ClearEnemies();
            foreach (var enemy in existingEnemies) if (enemy != null) enemySystem.AddEnemy(enemy);
            pool.Return(targetObject);

            var upgrade = typeof(SkillManager).GetMethod("UpgradeSkill", BindingFlags.Instance | BindingFlags.Public);
            for (int targetLevel = 2; targetLevel <= 5; targetLevel++)
            {
                manager.UpgradeSkill(skill.Key);
                var expected = ExpectedHeroSnapshot(expectedKind, targetLevel);
                Require(skill.Level == targetLevel && skill.Damage == expected.Damage &&
                        Mathf.Abs(skill.CD - expected.Cooldown) < .001f &&
                        Mathf.Abs(skill.AttackRange - expected.Range) < .001f &&
                        Mathf.Abs(skill.Velocity - expected.Velocity) < .001f &&
                        skill.Amount == expected.Amount && Mathf.Approximately(profile.Range, expected.Range),
                    hero + " rank " + targetLevel + " applies its proposed profile values");
            }
            manager.UpgradeSkill(skill.Key);
            Require(skill.Level == 5 && skill.CheckLevelToMax(), hero + " cannot exceed rank 5");

            bool restoredLegacyTrack = manager.ConfigureLegacyStarter();
            int savedLevel = skill.Level;
            skill.Level = 6;
            Require(restoredLegacyTrack && skill.UpgradeAttribute.Count == 10 &&
                    skill.UpgradeAttribute.Count + 1 == 11 && !skill.CheckLevelToMax(),
                hero + " legacy checkpoint keeps a level 6 signature valid on its original 11-level track");
            skill.Level = savedLevel;
            profile.Configure(skill, _round.PlayerStat);
            Require(skill.UpgradeAttribute.Count == 4 && skill.Level == savedLevel,
                hero + " normal hero profile remains five ranks after the legacy compatibility check");
        }

        private static StarterWeaponSnapshot ExpectedHeroSnapshot(StarterWeaponKind kind, int level)
        {
            return kind switch
            {
                StarterWeaponKind.ThornSlash => level switch
                {
                    2 => new StarterWeaponSnapshot(14, .90f, 2.2f, 0f, 1),
                    3 => new StarterWeaponSnapshot(14, .90f, 2.2f, 0f, 2),
                    4 => new StarterWeaponSnapshot(14, .90f, 2.4f, 0f, 2),
                    5 => new StarterWeaponSnapshot(16, .81f, 2.4f, 0f, 2),
                    _ => throw new ArgumentOutOfRangeException(nameof(level))
                },
                StarterWeaponKind.DarkOrb => level switch
                {
                    2 => new StarterWeaponSnapshot(12, 1.15f, 7.0f, 9.0f, 1),
                    3 => new StarterWeaponSnapshot(12, 1.15f, 7.0f, 10.8f, 1),
                    4 => new StarterWeaponSnapshot(12, 1.08f, 7.5f, 10.8f, 1),
                    5 => new StarterWeaponSnapshot(13, 1.08f, 7.5f, 10.8f, 2),
                    _ => throw new ArgumentOutOfRangeException(nameof(level))
                },
                StarterWeaponKind.Arrow => level switch
                {
                    2 => new StarterWeaponSnapshot(10, .85f, 10.5f, 16.0f, 1),
                    3 => new StarterWeaponSnapshot(10, .85f, 10.5f, 18.4f, 1),
                    4 => new StarterWeaponSnapshot(10, .85f, 12.0f, 18.4f, 1),
                    5 => new StarterWeaponSnapshot(12, .78f, 12.0f, 18.4f, 2),
                    _ => throw new ArgumentOutOfRangeException(nameof(level))
                },
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }
    }
}
