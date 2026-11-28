using System.Collections;
using DATN.Localization;
using DATN.Modes;
using DATN.World;
using RGame.RoguelikeKit;
using UnityEngine;

namespace DATN.Trial
{
    public sealed partial class DatnRegression
    {
        private static readonly GameModeId[] FullSmokeExpectedModes =
        {
            GameModeId.Survival,
            GameModeId.KillTarget,
            GameModeId.Survival,
            GameModeId.KillTarget,
            GameModeId.Survival,
            GameModeId.Survival,
            GameModeId.KillTarget,
            GameModeId.Survival,
            GameModeId.Endless,
            GameModeId.BossRush
        };

        IEnumerator CheckFullSmoke30()
        {
            var stageCounts = new int[Levels.Length];
            var heroCounts = new int[Heroes.Length];
            var modeCounts = new int[4];
            var languageCounts = new int[2];
            int combinations = 0;

            for (int stageIndex = 0; stageIndex < Levels.Length; stageIndex++)
            {
                for (int heroIndex = 0; heroIndex < Heroes.Length; heroIndex++)
                {
                    // The first menu inspection checks both locales and ends in English.
                    // Start in Vietnamese so the resulting run language is explicit too.
                    string expectedLanguage = combinations % 2 == 0 ? "en" : "vi";
                    yield return SelectLanguage(combinations == 0 ? "vi" : expectedLanguage);

                    string hero = Heroes[heroIndex];
                    yield return EnterRound(stageIndex, hero, checkMenu: combinations == 0);
                    Require(LanguageManager.GetCurrentCode() == expectedLanguage,
                        $"{Levels[stageIndex]} runs in {expectedLanguage}");

                    GameModeId mode = _selection.SelectLevelSO.GameMode.ModeId;
                    Require(mode == FullSmokeExpectedModes[stageIndex],
                        $"{Levels[stageIndex]} uses {FullSmokeExpectedModes[stageIndex]}, got {mode}");
                    Require(StageLayout.Current != null && StageLayout.Current.StageKey == Levels[stageIndex],
                        $"{Levels[stageIndex]} builds its stage geometry");

                    var player = _round.GlobalConfig.GlobalPlayer;
                    var firstEnemy = FindFirstObjectByType<BaseEnemy>();
                    Require(StageLayout.IsOpen(player.transform.position, .45f),
                        $"{Levels[stageIndex]} keeps the {hero} spawn on open ground");
                    Require(firstEnemy != null && StageLayout.IsOpen(firstEnemy.transform.position, .45f),
                        $"{Levels[stageIndex]} spawns its first enemy on open ground");
                    Require(!_round.IsEnded && !Visible("GameWin") && !Visible("GameOver"),
                        $"{Levels[stageIndex]} remains active before its {mode} objective resolves");

                    stageCounts[stageIndex]++;
                    heroCounts[heroIndex]++;
                    modeCounts[(int)mode]++;
                    languageCounts[LanguageManager.GetCurrentCode() == "vi" ? 0 : 1]++;
                    Record("PASS", $"Full smoke {Levels[stageIndex]} | {hero} | {mode} | " +
                        $"language={LanguageManager.GetCurrentCode()} | live spawn bounds checked");

                    yield return CheckLevelBackground(stageIndex);
                    yield return LeaveFromPause();
                    combinations++;
                }
            }

            Require(combinations == 30, $"Full smoke visits {combinations}/30 stage/hero combinations");
            for (int stageIndex = 0; stageIndex < stageCounts.Length; stageIndex++)
                Require(stageCounts[stageIndex] == Heroes.Length,
                    $"{Levels[stageIndex]} covered by all {Heroes.Length} heroes");
            for (int heroIndex = 0; heroIndex < heroCounts.Length; heroIndex++)
                Require(heroCounts[heroIndex] == Levels.Length,
                    $"{Heroes[heroIndex]} covered by all {Levels.Length} stages");

            Require(modeCounts[(int)GameModeId.Survival] == 15 &&
                    modeCounts[(int)GameModeId.KillTarget] == 9 &&
                    modeCounts[(int)GameModeId.Endless] == 3 &&
                    modeCounts[(int)GameModeId.BossRush] == 3,
                "Mode distribution is Survival=15, KillTarget=9, Endless=3, BossRush=3");
            Require(languageCounts[0] == 15 && languageCounts[1] == 15,
                $"Alternating locales cover VI={languageCounts[0]} and EN={languageCounts[1]} runs");
            Record("PASS", $"Full smoke totals: combinations={combinations}; stages={stageCounts.Length}; " +
                $"heroes={heroCounts.Length}; modes Survival={modeCounts[0]}, KillTarget={modeCounts[1]}, " +
                $"Endless={modeCounts[2]}, BossRush={modeCounts[3]}; " +
                $"locales VI={languageCounts[0]}, EN={languageCounts[1]}");
        }
    }
}
