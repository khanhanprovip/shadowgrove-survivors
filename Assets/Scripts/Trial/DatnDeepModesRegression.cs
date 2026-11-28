using System.Collections;
using DATN.Modes;
using RGame.RoguelikeKit;
using UnityEngine;

namespace DATN.Trial
{
    public sealed partial class DatnRegression
    {
        IEnumerator CheckDeepModes()
        {
            int survivalWins = 0;
            int killTargetWins = 0;

            for (int stageIndex = 0; stageIndex < 8; stageIndex++)
            {
                string hero = Heroes[stageIndex % Heroes.Length];
                yield return EnterRound(stageIndex, hero);

                GameModeId mode = _selection.SelectLevelSO.GameMode.ModeId;
                Require(mode == FullSmokeExpectedModes[stageIndex],
                    $"{Levels[stageIndex]} selects {FullSmokeExpectedModes[stageIndex]}");
                var config = _selection.SelectLevelSO.GameMode;
                bool hasCampaignBoss = stageIndex == 2 || stageIndex == 4 || stageIndex == 7;
                var director = FindFirstObjectByType<EndlessBossDirector>();
                Require((director != null && director.HasSurvivalBoss) == hasCampaignBoss,
                    $"{Levels[stageIndex]} campaign boss configuration matches its stage");

                if (hasCampaignBoss)
                    _round.GlobalConfig.GlobalPlayer.GetComponent<StarterWeaponProfile>().Range = 0f;

                // Runtime-only test bot padding keeps the real clock moving without
                // changing balance assets or skipping player/enemy damage paths.
                _round.PlayerStat.ModifyValue("HP", 100000, true);
                _round.PlayerStat.ModifyValue("HP", 100000);
                Time.timeScale = 3f;
                float deadline = Time.realtimeSinceStartup + 100f;

                if (mode == GameModeId.Survival)
                {
                    yield return CheckDeepSurvival(stageIndex, config.Duration, hasCampaignBoss, director, deadline);
                    survivalWins++;
                }
                else
                {
                    yield return CheckDeepKillTarget(stageIndex, config.TargetKills, deadline);
                    killTargetWins++;
                }

                Record("PASS", $"Deep outcome {Levels[stageIndex]} | {hero} | {mode} | " +
                    $"matchSeconds={_round.MatchSeconds:F2} | scaled real clock x3");
                yield return CaptureFrame("deep-outcome-" + Levels[stageIndex]);
                _round.ExitToMainMenu();
                yield return WaitForMenu();
            }

            Require(survivalWins == 5 && killTargetWins == 3,
                $"Deep outcomes include Survival={survivalWins} and KillTarget={killTargetWins}");
            Record("PASS", $"Deep outcome totals: matches=8; Survival={survivalWins}; KillTarget={killTargetWins}; " +
                "heroes cycle Rowan/Lyra/Kael; loss paths were not exercised");
        }

        private IEnumerator CheckDeepSurvival(int stageIndex, float duration, bool hasCampaignBoss,
            EndlessBossDirector director, float deadline)
        {
            while (_round.MatchSeconds < duration && !Visible("GameWin") && !Visible("GameOver") &&
                   Time.realtimeSinceStartup < deadline)
            {
                MaintainDeepModeRun();
                yield return null;
            }

            Require(_round.MatchSeconds >= duration - .25f,
                $"{Levels[stageIndex]} advances its real Survival clock to {duration:F0} game seconds");

            if (!hasCampaignBoss)
            {
                Require(Visible("GameWin") && !Visible("GameOver"),
                    $"{Levels[stageIndex]} wins after its full Survival duration");
                Require(Mathf.Abs(_round.MatchSeconds - duration) < .25f,
                    $"{Levels[stageIndex]} records the Survival deadline at {duration:F0} seconds");
                yield break;
            }

            while ((director.ActiveBoss == null || director.ActiveBoss.Entering) &&
                   !Visible("GameWin") && !Visible("GameOver") && Time.realtimeSinceStartup < deadline)
            {
                MaintainDeepModeRun();
                yield return null;
            }

            Require(director.ActiveBoss != null && !director.ActiveBoss.Entering,
                $"{Levels[stageIndex]} spawns and reveals its campaign boss after the full timer");
            Require(!_round.IsEnded && !Visible("GameWin") && !Visible("GameOver"),
                $"{Levels[stageIndex]} does not end at the timer while its campaign boss is alive");
            Require(director.ActiveBoss.GetComponent<BossBrain>().Kind ==
                    _selection.SelectLevelSO.GameMode.FinalBossKind,
                $"{Levels[stageIndex]} spawns its configured campaign boss");

            var boss = director.ActiveBoss;
            Require(boss.StatRuntime != null && boss.StatRuntime.GetValue("HP") > 0,
                $"{Levels[stageIndex]} campaign boss has live health before the real hit");
            boss.MyHit.Hit(1000000);

            while (!Visible("GameWin") && !Visible("GameOver") && Time.realtimeSinceStartup < deadline)
            {
                MaintainDeepModeRun();
                yield return null;
            }

            Require(Visible("GameWin") && !Visible("GameOver"),
                $"{Levels[stageIndex]} wins only after its campaign boss dies");
        }

        private IEnumerator CheckDeepKillTarget(int stageIndex, int targetKills, float deadline)
        {
            while (!Visible("GameWin") && !Visible("GameOver") && Time.realtimeSinceStartup < deadline)
            {
                MaintainDeepModeRun();
                yield return null;
            }

            Require(Visible("GameWin") && !Visible("GameOver"),
                $"{Levels[stageIndex]} reaches its KillTarget victory through normal combat");
            Require(_round.GlobalConfig.CurrentGetKill == targetKills,
                $"{Levels[stageIndex]} wins at its configured {targetKills} real enemy kills");
        }

        private void MaintainDeepModeRun()
        {
            if (_round.IsEnded) return;
            HealTestPlayer();
            if (Time.timeScale <= 0f)
            {
                PickUpgradeOrTreasure();
                if (!_round.IsEnded) Time.timeScale = 3f;
            }
        }
    }
}
