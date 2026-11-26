using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DATN.Localization;
using DATN.Modes;
using DG.Tweening;
using RGame.RoguelikeKit;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace DATN.Trial
{
    // Opt-in integration tests for a built player. Clicks are sent only to the
    // top hit from the live EventSystem, including CanvasGroup and viewport checks.
    public sealed partial class DatnRegression : MonoBehaviour
    {
        private static readonly string[] Levels =
        {
            "TrialLevel", "LV2_MoonlitRuins", "LV3_SunkenMarsh", "LV4_AshenQuarry",
            "LV5_FrostHollow", "LV6_CursedVillage", "LV7_ClockworkCitadel",
            "LV8_CrimsonCathedral", "LV9_EndlessAbyss", "LV10_EclipseThrone"
        };

        private static readonly string[] Heroes = { "Rowan", "Lyra", "Kael" };

        private readonly List<string> _report = new();
        private readonly List<string> _errors = new();
        private Keyboard _keyboard;
        private string _output;
        private int _errorCount;
        private int _upgradeCount;
        private bool _capturedCombat;
        private TrialController _round;
        private LevelAndCharacterSO _selection;

        public void Begin(string suite, string output)
        {
            _output = output;
            Directory.CreateDirectory(_output);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            Application.logMessageReceived += CollectError;
            StartCoroutine(Run(suite));
        }

        private IEnumerator Run(string suite)
        {
            _report.Add($"Shadowgrove Windows QA | {Application.unityVersion} | {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            var tests = new Stack<IEnumerator>();
            tests.Push(SelectSuite(suite));
            bool passed = true;

            // Drive nested coroutines here so a failed assertion still writes a
            // report and exits the player with a nonzero status.
            while (tests.Count > 0)
            {
                var iterator = tests.Peek();
                bool moved;
                object step = null;
                try
                {
                    moved = iterator.MoveNext();
                    if (moved) step = iterator.Current;
                }
                catch (Exception exception)
                {
                    Record("FAIL", exception.ToString());
                    Capture("failure");
                    passed = false;
                    break;
                }

                if (!moved) tests.Pop();
                else if (step is IEnumerator child) tests.Push(child);
                else yield return step;
            }

            if (!passed) yield return new WaitForSecondsRealtime(0.3f);
            _report.Add($"Runtime errors: {_errorCount}");
            _report.AddRange(_errors);
            File.WriteAllLines(Path.Combine(_output, $"report-{suite}.txt"), _report);
            Application.Quit(passed && _errorCount == 0 ? 0 : 1);
        }

        private IEnumerator SelectSuite(string suite)
        {
            yield return Wait(() => FindFirstObjectByType<UIMainMenu>() != null &&
                                   EventSystem.current != null &&
                                   GameObject.Find("LangButton") != null, "Initialization → MainMenu");
            yield return new WaitForSecondsRealtime(2.8f);
            Record("INFO", $"Screen {Screen.width}x{Screen.height} | Graphics: {SystemInfo.graphicsDeviceName}");
            _selection = Resources.FindObjectsOfTypeAll<LevelAndCharacterSO>().Single(
                asset => asset.name == "LevelAndCharacterSO");

            switch (suite)
            {
                case "run-save-write":yield return CheckRunSaveWrite();break;
                case "run-save-modes":yield return CheckRunSaveModes();break;
                case "run-save-bosses":yield return CheckRunSaveBosses();break;
                case "run-save-read":yield return CheckRunSaveRead();break;
                case "bosses": yield return CheckBosses(); break;
                case "balance": yield return CheckBalance(); break;
                case "pause-skills": yield return CheckPauseSkills(); break;
                case "spawn-pacing": yield return CheckSpawnPacing(); break;
                case "enemy-cap": yield return CheckEnemyCap(); break;
                case "menus":
                    yield return CaptureFrame("menu");
                    for (int i = 0; i < Levels.Length; i++)
                    {
                        yield return EnterRound(i, Heroes[i % Heroes.Length], checkMenu: i == 0);
                        if (i == 0) yield return CheckMovement();
                        yield return LeaveFromPause();
                    }
                    break;
                case "backgrounds":
                    for (int i = 0; i < Levels.Length; i++)
                    {
                        yield return EnterRound(i, Heroes[i % Heroes.Length]);
                        yield return CheckLevelBackground(i);
                        yield return LeaveFromPause();
                    }
                    break;
                case "survival":
                    yield return EnterRound(2, "Lyra");
                    yield return CheckObjective(GameModeId.Survival);
                    break;
                case "survival-boundary":
                    yield return EnterRound(0, "Rowan");
                    _round.Pause();
                    var boundaryFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                    var mode = typeof(TrialController).GetField("mMode", boundaryFlags).GetValue(_round);
                    float duration = _selection.SelectLevelSO.GameMode.Duration;
                    typeof(TrialController).GetField("_matchSeconds", boundaryFlags).SetValue(_round, (double)duration + .31);
                    typeof(SurvivalMode).GetField("_remaining", boundaryFlags).SetValue(mode, -.31d);
                    typeof(TrialController).GetMethod("TryWin", boundaryFlags).Invoke(_round, null);
                    yield return Wait(() => Visible("GameWin"), "Survival boundary result becomes visible");
                    Require(Visible("GameWin"), "A frame crossing the Survival deadline still wins");
                    Require(Mathf.Abs(_round.MatchSeconds - duration) < .001f, "Survival clock clamps exactly to its deadline after a 310ms overshoot");
                    yield return CaptureFrame("survival-boundary");
                    break;
                case "killtarget":
                    yield return EnterRound(1, "Rowan");
                    yield return CheckObjective(GameModeId.KillTarget);
                    break;
                case "killtarget-burst":
                    yield return EnterRound(1, "Rowan");
                    yield return CheckObjective(GameModeId.KillTarget, testSimultaneousDeaths: true);
                    break;
                case "endless":
                    yield return EnterRound(8, "Kael");
                    yield return CheckObjective(GameModeId.Endless);
                    break;
                case "animations":
                    yield return CheckPlayerAnimations();
                    break;
                case "art":
                    yield return CheckJadeArt();
                    break;
                case "combat-pickups":
                    yield return CheckCombatPickups();
                    break;
                case "hit-feedback":
                    yield return CheckPlayerHitFeedback();
                    break;
                case "ui":
                    yield return CheckForestUi();
                    break;
                case "skill-lifetime":
                    yield return CheckSkillOwnerLifetime();
                    break;
                case "skills":
                    yield return CheckSkillCards();
                    break;
                case "audio":
                    yield return CheckAudio();
                    break;
                case "settings-write":
                    yield return CheckSettings(write: true);
                    break;
                case "settings-read":
                    yield return CheckSettings(write: false);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown suite: {suite}");
            }
        }

        private IEnumerator EnterRound(int index, string hero, bool checkMenu = false)
        {
            Click(ButtonIn(FindFirstObjectByType<UIMainMenu>(), "ButtonNewGame"));
            yield return Wait(() => FindFirstObjectByType<CharacterSelectPanel>()?.GetComponentsInChildren<CharacterItem>().Length == 3,
                "Three characters in selection");
            var characters = FindFirstObjectByType<CharacterSelectPanel>();
            var character = characters.GetComponentsInChildren<CharacterItem>().Single(
                item => item.GetComponentsInChildren<TMP_Text>().Any(label => label.text == hero));
            yield return null;
            yield return Wait(() => CanClick(character.GetComponent<Button>()), "Character card receives clicks");
            Click(character.GetComponent<Button>());
            Click(ButtonIn(characters, "ButtonConfirm"));

            yield return Wait(() => FindFirstObjectByType<LevelSelectPanel>()?.GetComponentsInChildren<LevelItem>().Length == Levels.Length,
                "Ten levels in selection");
            var levelPanel = FindFirstObjectByType<LevelSelectPanel>();
            var levels = levelPanel.GetComponentsInChildren<LevelItem>();
            var scroll = levelPanel.GetComponentInChildren<ScrollRect>();
            Require(scroll != null && scroll.vertical, "Level list ScrollRect");
            var confirm = ButtonIn(levelPanel, "ButtonConfirm");

            if (checkMenu)
            {
                yield return SelectLanguage("vi");
                yield return Wait(() => HasText(confirm, "XÁC NHẬN") && HasText(levels[0], "Rừng Thử Thách"),
                    "Vietnamese confirmation and stage description");
                Record("PASS", "Vietnamese confirmation and stage description");
                yield return CaptureFrame("level-select-vi");
                yield return null;
                yield return SelectLanguage("en");
                yield return Wait(() => HasText(confirm, "CONFIRM") && HasText(levels[0], "Trial Forest"),
                    "English confirmation and stage description");
                Record("PASS", "English confirmation and stage description");
                yield return CaptureFrame("level-select-en");
                yield return null;

                Canvas.ForceUpdateCanvases();
                var pointer = PointerAt(levels[0].GetComponent<Button>());
                float original = scroll.verticalNormalizedPosition;
                var hits = Raycast(pointer);
                Require(hits.Count > 0, "Level list receives scroll");
                pointer.scrollDelta = new Vector2(0f, -7f);
                var receiver = ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.scrollHandler);
                yield return null;
                Require(receiver == scroll.gameObject && scroll.verticalNormalizedPosition < original - 0.01f,
                    $"Level list scrolls: {original:F2} → {scroll.verticalNormalizedPosition:F2}");
            }

            Canvas.ForceUpdateCanvases();
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f - index / (Levels.Length - 1f);
            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return Wait(() => CanClick(levels[index].GetComponent<Button>()), "Selected level receives clicks after layout");
            Click(levels[index].GetComponent<Button>());
            Require(_selection.SelectCharacterSo.CharacterName == hero && _selection.SelectLevelSO.name == Levels[index],
                $"Selected {hero} → {Levels[index]}");
            Click(confirm);

            yield return Wait(() => GameObject.Find("Forest_Lobby") != null, "Forest lobby opens");
            yield return null; // New graphics acquire their raycast depth on the next canvas frame.
            Require(HasText(GameObject.Find("Forest_Lobby").transform, hero), "Lobby shows the chosen survivor");
            yield return Wait(() => CanClick(ButtonIn(GameObject.Find("Forest_Lobby").transform, "StartBattleBtn")), "Lobby enter button receives clicks");
            Click(ButtonIn(GameObject.Find("Forest_Lobby").transform, "StartBattleBtn"));

            yield return Wait(() =>
            {
                _round = FindFirstObjectByType<TrialController>();
                return _round != null && _round.GlobalConfig.GlobalPlayer != null &&
                       GameObject.Find("Map")?.transform.childCount > 0;
            }, "Gameplay, map and player ready");
            Require(_round.GlobalConfig.GlobalPlayer.name.Contains(hero), $"Spawned {hero} prefab");
            int tiles = GameObject.Find("Map").transform.childCount;
            yield return Wait(() => FindFirstObjectByType<BaseEnemy>() != null, "First enemy spawns");
            yield return new WaitForSecondsRealtime(1.1f);
            Record("PASS", $"{Levels[index]} | {hero} | tiles={tiles} | {_selection.SelectLevelSO.GameMode.ModeId}");
        }

        private IEnumerator CheckLevelBackground(int index)
        {
            var map = GameObject.Find("Map");
            var background = map.GetComponentInChildren<DATN.World.LevelBackground>();
            {
                string expected = index == 0 ? "TrialForest" : Levels[index].Substring(Levels[index].IndexOf('_') + 1);
                Require(background != null && background.ThemeKey == expected, Levels[index] + " loads its own terrain");
                var renderer = background.GroundRenderer;
                var config = _selection.SelectLevelSO.MapConfig;
                Require(renderer.sprite != null && renderer.sprite.name == "Ground_" + expected &&
                        renderer.drawMode == SpriteDrawMode.Tiled && renderer.enabled, expected + " terrain renders as repeating ground");
                Require(Mathf.Abs(renderer.bounds.size.x - config.Width) < 0.01f &&
                        Mathf.Abs(renderer.bounds.size.y - config.Height) < 0.01f &&
                        Vector2.Distance(renderer.bounds.center, new Vector2(-0.5f, -0.5f)) < 0.01f,
                    expected + " covers the full map without changing its bounds");
                Require(renderer.sortingLayerName == "Map" && renderer.sortingOrder == short.MinValue &&
                        background.GetComponentsInChildren<Collider2D>().Length == 0,
                    expected + " stays below combat and adds no collision");
                Require(map.GetComponentsInChildren<SpriteRenderer>().Count(r => r.enabled) == 1,
                    expected + " has no forest tiles showing through");
            }
            yield return CaptureFrame("background-" + index.ToString("00") + "-" + Levels[index]);
            yield return new WaitForSecondsRealtime(0.2f);
            yield return CheckMovement();
        }

        private IEnumerator CheckMovement()
        {
            var player = _round.GlobalConfig.GlobalPlayer;
            Vector3 before = player.transform.position;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D));
            yield return new WaitForSecondsRealtime(0.55f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
            float dx = player.transform.position.x - before.x;
            Require(dx > 0.25f, $"D moves player right: {dx:F2} world units");

            yield return PressEscape();
            var pausePanel = GameObject.Find("TrialPausePanel");
            Require(_round.IsPaused && Time.timeScale == 0f && pausePanel != null, "Escape pauses");
            float match = _round.MatchSeconds;
            yield return new WaitForSecondsRealtime(0.45f);
            Require(Mathf.Approximately(_round.MatchSeconds, match), "Match clock stops while paused");
            yield return PressEscape();
            Require(!_round.IsPaused && Time.timeScale == 1f && GameObject.Find("TrialPausePanel") == null, "Escape resumes");
        }

        private IEnumerator LeaveFromPause()
        {
            yield return PressEscape();
            var pausePanel = GameObject.Find("TrialPausePanel");
            Require(pausePanel != null, "Pause menu is visible");
            Click(ButtonIn(pausePanel.transform, "VeMenuBtn"));
            yield return WaitForMenu();
        }

        private IEnumerator WaitForMenu()
        {
            yield return Wait(() => _round == null && FindFirstObjectByType<UIMainMenu>() != null,
                "Return to Main Menu");
            yield return new WaitForSecondsRealtime(2.7f);
        }

        private IEnumerator CheckObjective(GameModeId mode, bool testSimultaneousDeaths = false)
        {
            int peakEnemies=0;
            var config = _selection.SelectLevelSO.GameMode;
            int initialCurse = _round.PlayerStat.GetValue("Curse");
            int earlySpawnCount = -1;
            bool checkedEndless = false;
            bool burstStarted = false;
            float timeLimit = Time.realtimeSinceStartup + 200f;
            Player player = _round.GlobalConfig.GlobalPlayer;

            while (!Visible("GameWin") && !Visible("GameOver") && Time.realtimeSinceStartup < timeLimit)
            {
                peakEnemies=Math.Max(peakEnemies,ActiveEnemies());
                if (!checkedEndless) HealTestPlayer();
                if (Time.timeScale == 0f) PickUpgradeOrTreasure();
                if (testSimultaneousDeaths && !burstStarted && Time.timeScale > 0f &&
                    _round.GlobalConfig.CurrentGetKill >= config.TargetKills - 3)
                {
                    var enemies = FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None)
                        .Where(enemy => enemy.MyHit != null && enemy.StatRuntime != null &&
                                        enemy.StatRuntime.GetValue("HP") > 0)
                        .Take(6).ToArray();
                    if (enemies.Length == 6)
                    {
                        burstStarted = true;
                        Record("INFO", $"Lethal hits delivered together to 6 enemies at {_round.GlobalConfig.CurrentGetKill} kills");
                        foreach (var enemy in enemies) enemy.MyHit.Hit(100000);
                    }
                }

                if (!_capturedCombat && _round.GlobalConfig.CurrentGetKill >= 4)
                {
                    yield return CaptureFrame("combat");
                    _capturedCombat = true;
                    var samples = new float[1024];
                    AudioListener.GetOutputData(samples, 0);
                    Record("INFO", $"Audio output peak: {samples.Max(sample => Mathf.Abs(sample)):F6}");
                }
                if (mode == GameModeId.Endless)
                {
                    if (_round.MatchSeconds >= 58f && earlySpawnCount < 0)
                        earlySpawnCount = ActiveEnemies() + _round.GlobalConfig.CurrentGetKill;
                    if (!checkedEndless && _round.MatchSeconds >= 74f)
                    {
                        int curse = _round.PlayerStat.GetValue("Curse");
                        int spawns = ActiveEnemies() + _round.GlobalConfig.CurrentGetKill;
                        Require(curse > initialCurse && spawns > earlySpawnCount,
                            $"Endless after 60s: Curse {initialCurse} → {curse}, active + kills {earlySpawnCount} → {spawns}");
                        checkedEndless = true;
                        player.GetComponentInChildren<PlayerHit>().OnHit(100000);
                    }
                }

                yield return null;
            }

            Require(peakEnemies<=FindFirstObjectByType<StageManager>().EnemyLimit,$"{mode}: peak active enemies {peakEnemies} stays inside shared performance cap");
            Require(_round.GlobalConfig.CurrentGetKill > 0, "Automatic attack killed an enemy");
            Require(_upgradeCount > 0 && _round.PlayerStat.GetValue("Level") > 1,
                $"Character level-up and upgrade selection: {_upgradeCount}");
            Require(GameObject.Find("TrialResultSummary") != null, "Result summary exists");
            Require(Mathf.Approximately(Time.timeScale, 0f) &&
                    FindFirstObjectByType<UpgradeSkillPanel>() == null && FindFirstObjectByType<TreasurePanel>() == null,
                "Result freezes gameplay and closes upgrade overlays");
            if (testSimultaneousDeaths) Require(burstStarted, "The simultaneous-death case ran");

            if (mode == GameModeId.Endless)
            {
                Require(checkedEndless && Visible("GameOver") && !Visible("GameWin") &&
                        _round.PlayerStat.GetValue("HP") <= 0, "Endless ends in defeat at 0 HP");
                bool faded = player.GetComponentsInChildren<SpriteRenderer>().Any(renderer =>
                    renderer.color.a < 0.05f || (renderer.material.HasProperty("_Alpha") &&
                                                 renderer.material.GetFloat("_Alpha") < 0.05f));
                Require(faded, "Death fade reached transparent alpha");
                yield return CaptureFrame("defeat");
                yield return null;
                Click(ButtonIn(GameObject.Find("GameOver").transform, "ExitBtn"));
                yield return WaitForMenu();
                yield break;
            }

            Require(Visible("GameWin") && !Visible("GameOver"), $"{mode} shows victory");
            if (mode == GameModeId.Survival)
                Require(Mathf.Abs(_round.MatchSeconds - config.Duration) < 0.25f,
                    $"Survival wins at {_round.MatchSeconds:F3}/{config.Duration:F0} match seconds");
            else
                Require(_round.GlobalConfig.CurrentGetKill == config.TargetKills,
                    $"KillTarget wins at {_round.GlobalConfig.CurrentGetKill}/{config.TargetKills} kills");

            int killsBefore = _round.GlobalConfig.CurrentGetKill;
            yield return CaptureFrame("victory");
            yield return null;
            Click(ButtonIn(GameObject.Find("GameWin").transform, "RestartBtn"));
            yield return Wait(() =>
            {
                _round = FindFirstObjectByType<TrialController>();
                if (_round == null) return false;
                var current = _round.GlobalConfig.GlobalPlayer;
                return current != null && current != player;
            }, "Result Restart spawns a new player");
            Require(_round.GlobalConfig.CurrentGetKill == 0 && _round.MatchSeconds < 0.25f &&
                    _round.PlayerStat.GetValue("Level") == 1 && _round.PlayerStat.GetValue("Exp") == 0 &&
                    !Visible("GameWin") && !Visible("GameOver") && GameObject.Find("TrialResultSummary") == null,
                $"New attempt: kills {killsBefore} → 0, clock={_round.MatchSeconds:F3}s, XP 0, level 1, clear result UI");
            yield return new WaitForSecondsRealtime(2.6f);
            yield return LeaveFromPause();
        }

        private void HealTestPlayer()
        {
            // Keep the test bot alive without changing the round's clock, enemy
            // damage or any combat stats. The defeat test uses the real hit path.
            int hp = _round.PlayerStat.GetValue("HP");
            if (hp > 0 && hp < 80) _round.PlayerStat.ModifyValue("HP", 80 - hp);
        }

        private void PickUpgradeOrTreasure()
        {
            var upgrades = GameObject.Find("UpgradePanel");
            if (upgrades != null && upgrades.GetComponent<CanvasGroup>()?.alpha > 0.9f)
            {
                var button = upgrades.GetComponentsInChildren<Button>().FirstOrDefault(CanClick);
                if (button != null)
                {
                    Click(button);
                    _upgradeCount++;
                }
                return;
            }

            var treasure = FindFirstObjectByType<TreasurePanel>();
            var treasureButton = treasure?.GetComponentsInChildren<Button>().FirstOrDefault(CanClick);
            if (treasureButton != null) Click(treasureButton);
        }

        private IEnumerator CheckPlayerHitFeedback()
        {
            foreach (string hero in Heroes)
            {
                yield return EnterRound(0, hero);
                yield return Wait(() =>
                {
                    if (Time.timeScale <= 0f) PickUpgradeOrTreasure();
                    return Time.timeScale > 0f;
                }, "Combat ready for hit feedback");
                HealTestPlayer();
                _round.Pause();
                yield return Wait(() => _round.IsPaused, "Pause isolates visual feedback from enemy damage");
                yield return new WaitForSecondsRealtime(0.4f);
                var player = _round.GlobalConfig.GlobalPlayer;
                var hit = player.GetComponentInChildren<PlayerHit>();
                var renderer = player.GetComponentInChildren<PlayerAnim>().MyAnimator.GetComponent<SpriteRenderer>();
                var material = renderer.material;
                var feedbackTargets=(SpriteRenderer[])typeof(PlayerHit).GetField("mSpriteRenderers",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(hit);
                Record("INFO",$"{hero} feedback actual renderer={renderer.GetInstanceID()}, configured={string.Join(",",feedbackTargets.Select(x=>x==null?"null":x.GetInstanceID().ToString()))}, nextFlash={typeof(PlayerHit).GetField("_nextHitFlashAt",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(hit)}, unscaled={Time.unscaledTime}, clock={Time.realtimeSinceStartup}");
                yield return null; // Keep setup/logging costs outside the measured feedback frame.
                bool blendShader = material.HasProperty("_BlendColor");
                Color normal = renderer.color;
                Require(normal.a > 0.95f, hero + " starts opaque with " + material.shader.name);
                int before = _round.PlayerStat.GetValue("HP");
                int hits = 5, pulses = 0;
                float peak = 0, minAlpha = 1, maxOther = 0;
                bool wasRed = false;
                float start = Time.realtimeSinceStartup;
                // Deliver the intended burst synchronously. Scheduling only one
                // hit per frame inside a fixed wall-clock window could silently
                // deliver fewer than five when a Windows frame stalls.
                for (int i = 0; i < hits; i++) hit.OnHit(1);
                while (Time.realtimeSinceStartup - start < 0.6f)
                {
                    float elapsed = Time.realtimeSinceStartup - start;
                    Color tint = blendShader ? material.GetColor("_BlendColor") :
                        new Color(1f - renderer.color.g / normal.g,
                            Mathf.Abs(renderer.color.r - normal.r),
                            Mathf.Abs(renderer.color.g - renderer.color.b), 0f);
                    bool red = tint.r > 0.02f;
                    if (red && !wasRed) pulses++;
                    wasRed = red;
                    peak = Mathf.Max(peak, tint.r);
                    maxOther = Mathf.Max(maxOther, tint.g, tint.b);
                    minAlpha = Mathf.Min(minAlpha, renderer.color.a,
                        material.HasProperty("_Alpha") ? material.GetFloat("_Alpha") : 1f);
                    if(!renderer.enabled||renderer.sprite==null)Require(false,hero+" disappeared during damage feedback");
                    yield return null;
                }
                Require(hits == 5 && before - _round.PlayerStat.GetValue("HP") == hits, hero + " all five hits still apply damage");
                Require(peak > 0.02f && peak <= 0.29f && maxOther <= 0.025f, $"{hero} uses a subtle red tint instead of a white flash: red={peak:F3}, other={maxOther:F3}, pulses={pulses}, timeScale={Time.timeScale}, tweenScale={DOTween.timeScale}, tweenComponents={FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Count(x=>x.GetType().Name=="DOTweenComponent"&&x.isActiveAndEnabled)}");
                Require(pulses == 1, hero + " five close hits produce one visual pulse");
                Require(minAlpha >= 0.95f, hero + " stays opaque throughout the feedback");
                Require(blendShader ? material.GetColor("_BlendColor").maxColorComponent < 0.001f :
                        Vector4.Distance(renderer.color, normal) < 0.001f,
                    hero + " tint returns to normal while paused");
                Record("INFO", $"{hero}: hits={hits}, pulses={pulses}, redPeak={peak:F3}, minAlpha={minAlpha:F3}");
                _round.Resume();
                _round.ExitToMainMenu();
                yield return WaitForMenu();
            }
        }

        private IEnumerator CheckPlayerAnimations()
        {
            foreach (string hero in Heroes)
            {
                yield return EnterRound(0, hero);
                _round.Pause();
                var animation = _round.GlobalConfig.GlobalPlayer.GetComponentInChildren<PlayerAnim>();
                var animator = animation.MyAnimator;
                var renderer = animator.GetComponent<SpriteRenderer>();
                Require(renderer != null, hero + " sprite renderer exists");
                using var visibility = new SpriteVisibilityProbe();
                animation.enabled = false;
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                foreach (string state in new[] { "Idle", "Run", "Attack" })
                {
                    // Verify every pose before the timing window. A synchronous
                    // first GPU readback must not consume most of a short clip.
                    int count=state=="Idle"?4:6;
                    for(int frame=0;frame<count;frame++)
                    {
                        animator.Play(state,0,(frame+.05f)/count);animator.Update(0);
                        Require(renderer.sprite!=null&&visibility.VisiblePixels(renderer)>=200,
                            $"{hero} {state} frame {frame} has visible pixels on the real animated renderer: {renderer.sprite?.name}");
                    }
                    yield return null;
                    animator.Play(state, 0, 0);
                    animator.Update(0f); // Apply the requested state before sampling its rendered frames.
                    var frames = new HashSet<string>();
                    string previousSprite = null;
                    float end = Time.realtimeSinceStartup + 0.8f;
                    float hardEnd=Time.realtimeSinceStartup+6;
                    int processed=0;
                    while (Time.realtimeSinceStartup<hardEnd && (Time.realtimeSinceStartup<end||frames.Count<3))
                    {
                        yield return null;
                        if(!renderer.enabled||renderer.sprite==null||renderer.color.a<=.95f)
                            Require(false,hero+" "+state+" lost its visible sprite");
                        if(renderer.sortingOrder<50)Require(false,hero+" dropped below the ground");
                        if(visibility.VisiblePixels(renderer)<200)Require(false,hero+" "+state+" rendered a transparent frame: "+renderer.sprite.name);
                        processed++;
                        frames.Add(renderer.sprite.name);
                        if (previousSprite != renderer.sprite.name)
                        {
                            var info = animator.GetCurrentAnimatorStateInfo(0);
                            Record("INFO", $"{hero} {state}: {renderer.sprite.name}, normalized={info.normalizedTime:F3}, unscaledDelta={Time.unscaledDeltaTime:F4}");
                            previousSprite = renderer.sprite.name;
                        }
                        if(state=="Attack"&&frames.Count<3&&animator.GetCurrentAnimatorStateInfo(0).normalizedTime>1.05f)
                        {animator.Play(state,0,0);animator.Update(0);}
                    }
                    Require(frames.Count >= 3, $"{hero} {state}: {frames.Count} distinct poses advance; every one of {processed} sampled frames passed pixel/opacity/sorting checks");
                }
                animator.updateMode = AnimatorUpdateMode.Normal;
                animation.enabled = true;
                _round.Resume();
                var colliders = _round.GlobalConfig.GlobalPlayer.GetComponentsInChildren<Collider2D>();
                var colliderStates = colliders.Select(c => c.enabled).ToArray();
                foreach (var collider in colliders) collider.enabled = false;
                int hpBefore = _round.PlayerStat.GetValue("HP");
                int attacks = 0;
                float nextAttack = Time.realtimeSinceStartup;
                float until = Time.realtimeSinceStartup + 4f;
                int normalFrames=0;
                while (Time.realtimeSinceStartup < until)
                {
                    if (Time.timeScale == 0f) PickUpgradeOrTreasure();
                    if (Time.realtimeSinceStartup >= nextAttack)
                    {
                        animation.PlayAttack();
                        attacks++;
                        nextAttack = Time.realtimeSinceStartup + 0.55f;
                    }
                    yield return null;
                    if(renderer.sprite==null||!renderer.enabled||renderer.color.a<.95f||visibility.VisiblePixels(renderer)<200)
                        Require(false,hero+" disappeared while auto-attacking without taking damage");
                    normalFrames++;
                }
                Require(attacks >= 4 && _round.PlayerStat.GetValue("HP") >= hpBefore,
                    hero + " no-damage visibility window: " + attacks + " attacks across "+normalFrames+" checked frames, HP " + hpBefore + " → " + _round.PlayerStat.GetValue("HP"));
                for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = colliderStates[i];
                visibility.Dispose();
                _round.ExitToMainMenu();
                yield return WaitForMenu();
            }
        }


        private IEnumerator CheckSkillOwnerLifetime()
        {
            var owner = new GameObject("QA_SkillOwner");
            var inside = new GameObject("QA_InsideScope").AddComponent<InsideScopeSkill>();
            var orbit = new GameObject("QA_Orbit").AddComponent<CircularMotionSkill>();
            int released = 0;
            inside.OnDeath += skill => { released++; skill.gameObject.SetActive(false); };
            orbit.OnDeath += skill => { released++; skill.gameObject.SetActive(false); };
            inside.Init(5, owner.transform);
            orbit.Init(1, 90, 5, 0, 5, owner.transform);
            Destroy(owner);
            yield return null;
            yield return new WaitForFixedUpdate();
            Require(released == 2 && !inside.gameObject.activeSelf && !orbit.gameObject.activeSelf,
                "Area and orbit skills release safely when their owner is destroyed");
            yield return new WaitForFixedUpdate();
            Require(released == 2, "Destroyed-owner skills do not release repeatedly");
            Destroy(inside.gameObject); Destroy(orbit.gameObject);

            var mainCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Where(camera => camera.CompareTag("MainCamera")).ToArray();
            var spore = new GameObject("QA_SporeCameraLifetime").AddComponent<BouncingSkill>();
            int sporeReturns = 0;
            spore.OnDeath += skill => { sporeReturns++; skill.gameObject.SetActive(false); };
            try
            {
                foreach (var camera in mainCameras) camera.tag = "Untagged";
                spore.Init(4, 1, 5);
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                Require(sporeReturns == 1 && !spore.gameObject.activeSelf,
                    "Bouncing spore returns once when its arena camera disappears");
                spore.gameObject.SetActive(true);
                spore.Init(4, 1, 0);
                var position = spore.transform.position;
                yield return new WaitForFixedUpdate();
                Require(sporeReturns == 2 && spore.transform.position == position,
                    "Expired bouncing spore stops before movement or camera access");
            }
            finally
            {
                foreach (var camera in mainCameras) if (camera != null) camera.tag = "MainCamera";
                Destroy(spore.gameObject);
            }
        }

        private IEnumerator CheckForestUi()
        {
            foreach (string language in new[] { "vi", "en" })
            {
                yield return SelectLanguage(language);
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    Click(ButtonIn(FindFirstObjectByType<UIMainMenu>(), "ButtonPowerUp"));
                    yield return new WaitForSecondsRealtime(.5f);
                    var panel = FindFirstObjectByType<PowerUpPanel>();
                    Require(panel != null && panel.GetComponentsInChildren<PowerUpAttributeUpgrade>().Length == 14,
                        $"{language}: all 14 permanent upgrades remain accessible");
                    Require(!panel.GetComponentsInChildren<TMP_Text>().Any(text => text.text == "New Text"),
                        "Upgrade description initialized, including a fully upgraded attribute");
                    var coin = panel.transform.Find("Gold/GoldIcon").GetComponent<Image>();
                    Require(coin.preserveAspect && coin.rectTransform.rect.width / coin.rectTransform.rect.height < 1.5f,
                        "Power-up gold icon keeps its proportions");
                    foreach (var attribute in panel.GetComponentsInChildren<PowerUpAttributeUpgrade>())
                    {
                        var rect = (RectTransform)attribute.transform;
                        Require(rect.rect.width >= 250 && rect.rect.height >= 100, "Permanent-upgrade tile keeps usable dimensions");
                        var pointer = PointerAt(attribute.transform);
                        var hits = Raycast(pointer);
                        Require(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerDownHandler>(hits[0].gameObject) == attribute.gameObject,
                            language + " " + attribute.name + " full tile receives pointer selection");
                        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerDownHandler);
                        var description = panel.GetComponentInChildren<PowerUpDescription>();
                        var title = (TMP_Text)typeof(PowerUpDescription).GetField("mTitle",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(description);
                        Require(title.text == DATN.UI.ForestUI.AttributeName(attribute.name.Trim()),
                            language + " " + attribute.name + " click displays its actual description");
                    }
                    yield return CaptureFrame("powers-" + language);
                    Click(ButtonIn(panel, "Button Close"));
                    yield return null;
                    Require(FindFirstObjectByType<PowerUpPanel>() == null, "Power-up Close returns to the menu on repeat opens");
                }
                Click(ButtonIn(FindFirstObjectByType<UIMainMenu>(), "ButtonSetting"));
                yield return new WaitForSecondsRealtime(.3f);
                var settings = FindFirstObjectByType<SettingPanel>();
                Require(settings != null && settings.GetComponentsInChildren<Slider>().Length == 3, "Three real volume sliders exist");
                var optionLabels = settings.GetComponentsInChildren<TMP_Text>().Select(t => t.text).ToArray();
                Require(optionLabels.Contains(language == "vi" ? "Tiếng Việt" : "English"),
                    language + ": language option uses a localized display name");
                Require(language != "vi" || (!optionLabels.Contains("OFF") && !optionLabels.Contains("ON") &&
                        optionLabels.Any(t => t == "Bật" || t == "Tắt")),
                    language + ": boolean setting labels follow the selected language");
                foreach (var arrow in settings.GetComponentsInChildren<Button>().Where(button => button.name.StartsWith("ButtonP") || button.name == "ButtonNext"))
                {
                    var image = arrow.GetComponent<Image>();
                    Require(image != null && !image.preserveAspect && ((RectTransform)arrow.transform).rect.height > 35,
                        "Settings arrow keeps a full-height click target");
                }
                yield return CaptureFrame("settings-" + language);
                Click(ButtonIn(settings, "Button Close"));
                yield return null;
                Require(FindFirstObjectByType<SettingPanel>() == null, "Settings Close returns to menu");
            }
            yield return EnterRound(0, "Rowan");
            var renderer = _round.GlobalConfig.GlobalPlayer.GetComponent<SpriteRenderer>();
            Require(renderer != null && Mathf.Approximately(renderer.sprite.pixelsPerUnit, 60), "Hero sprite enlarged with 60 PPU, without scaling the movement root");
            var hud = FindFirstObjectByType<MainPanel>();
            foreach (string name in new[] { "GoldIcon", "KillCount" })
            {
                var icon = hud.transform.Find(name).GetComponent<Image>();
                Require(icon.preserveAspect && icon.rectTransform.rect.width / icon.rectTransform.rect.height < 1.5f,
                    name + " keeps a compact, undistorted icon");
            }
            Require(hud.transform.Find("SkillIcons").GetComponentsInChildren<Image>().Any(image => image.name == "Forest_SkillIcon" && image.sprite != null && image.color.a > .9f),
                "Equipped skill remains visible above its slot background");
            var xp = hud.transform.Find("Level/LevelValue").GetComponent<Image>();
            Require(xp.sprite != null && xp.type == Image.Type.Filled, "Experience bar retains its filled sprite");
            yield return CaptureFrame("hud");
            int pauseFrame = Time.frameCount;
            float pauseRequestedAt = Time.realtimeSinceStartup;
            _round.Pause();
            yield return Wait(() => _round.IsPaused, "Pause request accepted");
            // Forest styling is applied by LateUpdate on an unscaled timer.
            // A fixed delay can resume this coroutine before that frame's LateUpdate.
            yield return Wait(() => GameObject.Find("TrialPausePanel")?.transform.Find("Forest_PausePlate") != null,
                "Forest pause presentation ready");
            Record("INFO", $"Pause presentation ready after {Time.frameCount - pauseFrame} frames, " +
                           $"{Time.realtimeSinceStartup - pauseRequestedAt:F3}s");
            var pause = GameObject.Find("TrialPausePanel");
            Require(pause != null && pause.transform.Find("Forest_PausePlate") != null, "Pause uses the forest panel");
            Click(ButtonIn(pause.transform, "TiepTucBtn"));
            yield return null;
            Require(!_round.IsPaused, "Forest pause Resume works");
            _round.ExitToMainMenu();
            yield return WaitForMenu();
        }

        private IEnumerator CheckSkillCards()
        {
            yield return EnterRound(0, "Rowan");
            _round.Pause();
            var manager = FindFirstObjectByType<SkillManager>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var skills = (Dictionary<string, SkillDataSO>)typeof(SkillManager)
                .GetField("_skillDataDictionary", flags).GetValue(manager);
            typeof(SkillManager).GetMethod("OnCharacterUpgrade", flags).Invoke(manager, null);
            yield return null;
            yield return new WaitForSecondsRealtime(0.2f);
            Canvas.ForceUpdateCanvases();
            var panelRect = (RectTransform)FindFirstObjectByType<UpgradeSkillPanel>().transform;
            foreach (var card in FindObjectsByType<UpgradeSkillItem>(FindObjectsSortMode.None))
            {
                var cardRect = (RectTransform)card.transform;
                Require(cardRect.rect.width >= panelRect.rect.width * 0.20f &&
                        cardRect.rect.height >= panelRect.rect.height * 0.50f,
                    $"Skill card has usable dimensions: {cardRect.rect.width:F0}x{cardRect.rect.height:F0}");
            }
            var heading = panelRect.Find("Forest_UpgradeTitle") as RectTransform;
            Require(heading != null && heading.rect.width >= panelRect.rect.width * 0.65f,
                "Upgrade heading keeps a wide horizontal text area");
            if (Environment.GetCommandLineArgs().Contains("-datn-review-cards"))
                yield return new WaitForSecondsRealtime(90f);
            var item = FindObjectsByType<UpgradeSkillItem>(FindObjectsSortMode.None)
                .First(card => card.gameObject.activeInHierarchy);
            var name = (TMP_Text)typeof(UpgradeSkillItem).GetField("_name", flags).GetValue(item);
            var detail = (TMP_Text)typeof(UpgradeSkillItem).GetField("_description", flags).GetValue(item);
            var level = (TMP_Text)typeof(UpgradeSkillItem).GetField("_level", flags).GetValue(item);
            var log = new List<string>();
            foreach (string language in new[] { "vi", "en" })
            {
                LanguageManager.Instance.SetLanguage(LocalizationSettings.AvailableLocales.Locales.Single(
                    locale => locale.Identifier.Code == language));
                yield return Wait(() => LanguageManager.GetCurrentCode() == language, "Skill card language");
                foreach (var original in skills.Values.ToArray())
                {
                    // Exercise the real manager upgrade path on an isolated copy.
                    var data = original.Copy();
                    data.Level = 0;
                    skills[data.Key] = data;
                    item.SetUI(data);
                    Require(!name.text.Contains("Skill_") && !name.text.Contains("SkillAttr_") &&
                            item.transform.Find("Jade_NewBadge").GetComponent<TMP_Text>().text.Contains(LanguageManager.Get("CardNew")), $"{language} {data.Key} localized name and separate new-skill badge");
                    while (!data.CheckLevelToMax())
                    {
                        item.SetUI(data);
                        string beforeText = System.Text.RegularExpressions.Regex.Replace(detail.text, "<.*?>", "");
                        int previousLevel = data.Level;
                        float damage = data.Damage, cd = data.CD, amount = data.Amount, speed = data.Velocity, area = data.Area;
                        manager.UpgradeSkill(data.Key);
                        Require(data.Level == previousLevel + 1, $"{language} {data.Key} advances one level");
                        if (data.SkillType == SkillType.AttackSkill && previousLevel > 0)
                        {
                            foreach (var pair in new[]
                            {
                                (LanguageManager.Get("Stat_Damage"), damage, (float)data.Damage, ""),
                                (LanguageManager.Get("SkillAttr_Cooldown"), cd, data.CD, " s"),
                                (data.StarterWeapon == StarterWeaponKind.ThornSlash ? (language == "vi" ? "Số mục tiêu" : "Targets") : LanguageManager.Get("Stat_Amount"),
                                    amount + (data.StarterWeapon == StarterWeaponKind.ThornSlash ? 2 : 0),
                                    (float)data.Amount + (data.StarterWeapon == StarterWeaponKind.ThornSlash ? 2 : 0), ""),
                                (LanguageManager.Get("Stat_Velocity"), speed, data.Velocity, ""),
                                (LanguageManager.Get("Stat_Area"), area, (float)data.Area, "")
                            })
                            {
                                if (Mathf.Approximately(pair.Item2, pair.Item3)) continue;
                                string value = pair.Item3.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                                Require(beforeText.Split('\n').Any(line => line.StartsWith(pair.Item1 + ": ") &&
                                    line.EndsWith("→ " + value + pair.Item4)),
                                    $"{language} {data.Key} level {previousLevel}: preview matches applied {pair.Item1} {value}");
                            }
                        }
                        Canvas.ForceUpdateCanvases();
                        detail.ForceMeshUpdate();
                        name.ForceMeshUpdate();
                        var descriptionCorners = new Vector3[4];
                        detail.rectTransform.GetWorldCorners(descriptionCorners);
                        foreach (var button in item.GetComponentsInChildren<Button>())
                        {
                            var buttonCorners = new Vector3[4];
                            ((RectTransform)button.transform).GetWorldCorners(buttonCorners);
                            Require(descriptionCorners[0].y > buttonCorners[2].y,
                                $"{language} {data.Key} description stays above the selection button");
                            string expectedLabel = language == "vi" ? "CHỌN" : "SELECT";
                            Require(button.GetComponentsInChildren<TMP_Text>().Any(label =>
                                label.text == expectedLabel && label.color.r > 0.9f) ||
                                button.GetComponentsInChildren<Text>().Any(label =>
                                label.text == expectedLabel && label.color.r > 0.9f),
                                $"{language} selection button has a readable localized label");
                        }

                        Require(!detail.isTextOverflowing && !name.isTextOverflowing,
                            $"{language} {data.Key} card text fits its bounds");

                        log.Add($"{language} | {data.Key} | before level {previousLevel} | {beforeText.Replace('\n', ';')}");
                    }
                    item.SetUI(data);
                    Require(level.text.Contains(language == "vi" ? "Cấp tối đa" : "MAX"), $"{language} {data.Key} maximum level label");

                }
                item.SetUI(SkillDataSO.CreateMoneyBag(Resources.Load<Sprite>("SGJade/Icons/MoneyBag")));
                Require(detail.text.Contains("50") && level.text == "", $"{language} money bag reward and no skill level");
                yield return CaptureFrame("skills-" + language);
                yield return null;
            }
            File.WriteAllLines(Path.Combine(_output, "skill-card-values.txt"), log);
        }


        private IEnumerator CheckAudio()
        {
            var audio = FindFirstObjectByType<ShadowgroveAudio>();
            Require(audio != null, "Persistent audio player exists");
            var music = audio.GetComponents<AudioSource>().Single(source => source.loop);
            Require(music.isPlaying && music.clip.name == "MenuLoop", "Menu music is playing");
            float peak = 0;
            yield return ReadAudioPeak(value => peak = value);
            Require(peak > 0.001f, $"Menu music reaches the listener (peak {peak:F5})");

            Click(ButtonIn(FindFirstObjectByType<UIMainMenu>(), "ButtonSetting"));
            yield return Wait(() => FindFirstObjectByType<SettingPanel>() != null, "Settings opens");
            yield return null;
            var panel = FindFirstObjectByType<SettingPanel>();
            var settings = Resources.FindObjectsOfTypeAll<SettingsSO>().Single(asset => asset.name == "Settings");
            var items = panel.GetComponentsInChildren<SettingItem>();
            var master = items.Single(item => item.name == "Setting_Item");
            var musicItem = items.Single(item => item.name == "Setting_Item (1)");
            var sfx = items.Single(item => item.name == "Setting_Item (2)");
            yield return ChangeVolume(musicItem, () => settings.MusicVolume, 0);
            yield return new WaitForSecondsRealtime(0.25f);
            yield return ReadAudioPeak(value => peak = value);
            Require(peak < 0.0001f, $"Music 0/10 silences the background (peak {peak:F5})");

            foreach (SoundCue cue in Enum.GetValues(typeof(SoundCue)))
            {
                yield return new WaitForSecondsRealtime(1.35f);
                ShadowgroveAudio.Play(cue);
                yield return ReadAudioPeak(value => peak = value);
                Require(peak > 0.0005f && peak < 0.98f, $"Cue {cue} produces unclipped output (peak {peak:F5})");
            }
            Click(ButtonIn(sfx, "ButtonPrevious"));
            yield return ReadAudioPeak(value => peak = value);
            Require(peak > 0.001f, $"SFX preview remains audible with music muted (peak {peak:F5})");

            yield return ChangeVolume(sfx, () => settings.SfxVolume, 0);
            yield return new WaitForSecondsRealtime(0.25f);
            ShadowgroveAudio.Play(SoundCue.UiConfirm);
            yield return ReadAudioPeak(value => peak = value);
            Require(peak < 0.0001f, $"SFX 0/10 silences the cue (peak {peak:F5})");

            yield return ChangeVolume(musicItem, () => settings.MusicVolume, 6);
            yield return ChangeVolume(sfx, () => settings.SfxVolume, 7);
            yield return ChangeVolume(master, () => settings.MasterVolume, 0);
            yield return new WaitForSecondsRealtime(0.25f);
            ShadowgroveAudio.Play(SoundCue.UiConfirm);
            yield return ReadAudioPeak(value => peak = value);
            Require(peak < 0.0001f, $"Master 0/10 silences both channels (peak {peak:F5})");

            yield return ChangeVolume(master, () => settings.MasterVolume, 4);
            Require(FileManager.LoadFromFile("RoguelikeKit.RGame", out var json), "Volume settings saved to JSON");
            var save = JsonUtility.FromJson<SaveGame>(json);
            Require(Mathf.Approximately(save.MasterVolume, 0.4f) &&
                    Mathf.Approximately(save.MusicVolume, 0.6f) &&
                    Mathf.Approximately(save.SfxVolume, 0.7f), "Master, music and SFX persist in the save");

            Click(ButtonIn(panel, "Button Close"));
            yield return Wait(() => FindFirstObjectByType<SettingPanel>() == null, "Settings closes");
            yield return EnterRound(0, "Rowan");
            Require(music.isPlaying && music.clip.name == "BattleLoop" &&
                    FindObjectsByType<ShadowgroveAudio>(FindObjectsSortMode.None).Length == 1,
                "Battle music switches on the same audio player");
            float untilReady = Time.realtimeSinceStartup + 3f;
            while (Time.timeScale <= 0f && Time.realtimeSinceStartup < untilReady)
            {
                PickUpgradeOrTreasure();
                yield return null;
            }
            HealTestPlayer();
            yield return null;
            float battleVolume = music.volume;
            _round.Pause();
            yield return Wait(() => _round.IsPaused, "Pause");
            yield return null;
            Require(music.volume < battleVolume && music.volume > 0f, "Battle music ducks during pause");
            _round.Resume();
            yield return Wait(() => !_round.IsPaused, "Resume");
            yield return null;
            Require(Mathf.Approximately(music.volume, battleVolume), "Battle music recovers on resume");
            _round.ExitToMainMenu();
            yield return WaitForMenu();
            Require(music.clip.name == "MenuLoop" && music.isPlaying &&
                    FindObjectsByType<ShadowgroveAudio>(FindObjectsSortMode.None).Length == 1,
                "Return to menu restores one music player");
        }

        private static IEnumerator ChangeVolume(SettingItem item, Func<float> read, int target)
        {
            for (int step = 0; step < 10 && Mathf.RoundToInt(read() * 10) != target; step++)
            {
                Click(ButtonIn(item, read() * 10 > target ? "ButtonPrevious" : "ButtonNext"));
                yield return null;
            }

            if (Mathf.RoundToInt(read() * 10) != target)
                throw new InvalidOperationException($"Could not set {item.name} to {target}/10");
        }

        private static IEnumerator ReadAudioPeak(Action<float> result)
        {
            var samples = new float[512];
            float peak = 0;
            float end = Time.realtimeSinceStartup + 0.35f;
            while (Time.realtimeSinceStartup < end)
            {
                AudioListener.GetOutputData(samples, 0);
                foreach (float sample in samples)
                    peak = Mathf.Max(peak, Mathf.Abs(sample));
                yield return null;
            }

            result(peak);
        }


        private IEnumerator CheckSettings(bool write)
        {
            if (write) yield return SelectLanguage("en");
            Click(ButtonIn(FindFirstObjectByType<UIMainMenu>(), "ButtonSetting"));
            yield return Wait(() => FindFirstObjectByType<SettingPanel>() != null, "Settings opens");
            yield return null;
            var panel = FindFirstObjectByType<SettingPanel>();
            var settings = Resources.FindObjectsOfTypeAll<SettingsSO>().Single(asset => asset.name == "Settings");
            var master = panel.GetComponentsInChildren<SettingItem>().Single(item => item.name == "Setting_Item");
            if (write)
            {
                var language = panel.GetComponentsInChildren<SettingItem>().Single(item => item.name == "Setting_Item (5)");
                Click(ButtonIn(language, "ButtonNext"));
                yield return Wait(() => LanguageManager.GetCurrentCode() == "vi",
                    "Language option in settings updates the game");
                float before = settings.MasterVolume;
                for (int n = 0; n < 10 && !Mathf.Approximately(settings.MasterVolume, 0.4f); n++)
                {
                    Click(ButtonIn(master, settings.MasterVolume > 0.4f ? "ButtonPrevious" : "ButtonNext"));
                    yield return null;
                }
                Require(Mathf.Approximately(settings.MasterVolume, 0.4f), $"Master volume {before:F1} → 0.4 by UI");
                Require(FileManager.LoadFromFile("RoguelikeKit.RGame", out var save) &&
                        Mathf.Approximately(JsonUtility.FromJson<SaveGame>(save).MasterVolume, 0.4f),
                    "Settings JSON written to disk");
            }

            Require(Mathf.Approximately(settings.MasterVolume, 0.4f) && HasText(master, "4"), "Master volume displays 4/10");
            Require(LanguageManager.GetCurrentCode() == "vi" &&
                    LocalizationSettings.SelectedLocale.Identifier.Code == "vi" &&
                    PlayerPrefs.GetString(LanguageManager.PrefsKey) == "vi", "Vietnamese preference applied and stored");
            yield return CaptureFrame(write ? "settings-saved" : "settings-reopened");
            yield return new WaitForSecondsRealtime(0.4f);
        }

        private IEnumerator SelectLanguage(string language)
        {
            yield return Wait(() => LocalizationSettings.InitializationOperation.IsDone, "Locales initialized");
            if (LanguageManager.GetCurrentCode() != language)
                Click(GameObject.Find("LangButton").GetComponent<Button>());
            yield return Wait(() => LanguageManager.GetCurrentCode() == language &&
                                   LocalizationSettings.SelectedLocale?.Identifier.Code == language, $"Locale {language}");
        }

        private IEnumerator PressEscape()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Escape));
            yield return new WaitForSecondsRealtime(0.08f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
        }

        private static IEnumerator Wait(Func<bool> done, string step)
        {
            float until = Time.realtimeSinceStartup + 20f;
            while (!done())
            {
                if (Time.realtimeSinceStartup >= until) throw new InvalidOperationException($"Timed out: {step}");
                yield return null;
            }
        }

        private static int ActiveEnemies() => FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None).Length;

        private static bool Visible(string name) => GameObject.Find(name)?.GetComponent<CanvasGroup>()?.alpha > 0.9f;

        private static bool HasText(Component component, string expected) =>
            component.GetComponentsInChildren<TMP_Text>().Any(label =>
                label.text.Contains(expected, StringComparison.OrdinalIgnoreCase));

        private static Button ButtonIn(Component parent, string name) =>
            parent.GetComponentsInChildren<Button>().Single(button => button.name.Trim() == name);

        private static PointerEventData PointerAt(Button button)
            => PointerAt(button.transform);

        private static PointerEventData PointerAt(Transform target)
        {
            var rect = (RectTransform)target;
            var canvas = target.GetComponentInParent<Canvas>().rootCanvas;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)),
                button = PointerEventData.InputButton.Left,
                eligibleForClick = true
            };
        }

        private static List<RaycastResult> Raycast(PointerEventData pointer)
        {
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            return hits;
        }

        private static bool CanClick(Button button)
        {
            if (!button.isActiveAndEnabled || !button.IsInteractable()) return false;
            var hits = Raycast(PointerAt(button));
            return hits.Count != 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == button.gameObject;
        }

        private static void Click(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var pointer = PointerAt(button);
            var hits = Raycast(pointer);
            if (!button.isActiveAndEnabled || !button.IsInteractable() || hits.Count == 0 ||
                ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) != button.gameObject)
                throw new InvalidOperationException($"Button {button.name} at {pointer.position} is not clickable. Top hit: {(hits.Count > 0 ? hits[0].gameObject.name : "<none>")}.");

            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        private IEnumerator CheckCombatPickups()
        {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            foreach (string hero in Heroes)
            {
                yield return EnterRound(0, hero);
                _round.Pause();
                var player = _round.GlobalConfig.GlobalPlayer;
                var manager = FindFirstObjectByType<SkillManager>();
                var caster = manager.GetComponentInChildren<ToNearestSkillCast>();
                var profile = player.GetComponent<StarterWeaponProfile>();
                float expectedRange = hero == "Rowan" ? 2.2f : hero == "Lyra" ? 7f : 10.5f;
                Require(profile != null && Mathf.Approximately(profile.Range, expectedRange), hero + " has the intended radial range: " + expectedRange);
                Require(profile.Icon != null && profile.Effect != null, hero + " distinct starter icon and effect are assigned");
                var runtimeSkills = (Dictionary<string, SkillDataSO>)typeof(SkillManager).GetField("_skillDataDictionary", flags).GetValue(manager);
                Require(runtimeSkills[caster.Key].StarterWeapon == profile.Kind && runtimeSkills[caster.Key].SkillIcon == profile.Icon,
                    hero + " runtime upgrade data and HUD use its own starter weapon");
                var enemies = (EnemySystem)typeof(SkillCast).GetField("_enemySystem", flags).GetValue(caster);
                var existing = enemies.GetEnemies();
                enemies.ClearEnemies();
                Require(manager.GetComponentInChildren<RandomSkillCast>().CanCast() == false &&
                        manager.GetComponentInChildren<ChainSkillCast>().CanCast() == false,
                    hero + " other targeted weapons also reject an empty arena");
                var anim = player.GetComponent<PlayerAnim>();
                typeof(PlayerAnim).GetField("_attackLockUntil", flags).SetValue(anim, 0f);
                anim.MyAnimator.Play("Idle", 0, 0); anim.MyAnimator.Update(0);
                var fire = typeof(SkillManager).GetMethod("CreateSkill", flags);
                int bolts = FindObjectsByType<ToNearestSkill>(FindObjectsSortMode.None).Length;
                fire.Invoke(manager, new object[] { caster.Key }); anim.MyAnimator.Update(0);
                Require(!caster.CanCast() && anim.MyAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), hero + " empty arena does not play Attack");
                Require(FindObjectsByType<ToNearestSkill>(FindObjectsSortMode.None).Length == bolts, hero + " empty arena creates no projectile");
                var pool = Resources.FindObjectsOfTypeAll<RGame.Framework.PoolRuntimeSO>().Single(a => a.name == "Pool");
                var defs = (List<RGame.Framework.PoolDefinition>)typeof(RGame.Framework.PoolRuntimeSO).GetField("_poolDefinitions", flags).GetValue(pool);
                var enemyDef = defs.First(d => d.Pool.Prefab.GetComponent<BaseEnemy>() != null);
                var targetObject = pool.Request(enemyDef.Key);
                var target = targetObject.GetComponent<BaseEnemy>();
                foreach (var b in targetObject.GetComponentsInChildren<MonoBehaviour>()) if (b is not BaseEnemy) b.enabled = false;
                foreach (var c in targetObject.GetComponentsInChildren<Collider2D>()) c.enabled = false;
                if (target.StatRuntime == null)
                {
                    enemies.AddEnemy(target);
                    target.transform.position = player.transform.position + Vector3.right;
                    Require(!caster.CanCast(), hero + " waits for a newly spawned enemy to initialize its HP");
                }
                yield return null; // The real pooled enemy must initialize its HP/hit receiver.
                target.StatRuntime.ModifyValue("HP", 10000, true);
                target.StatRuntime.ModifyValue("HP", 10000);
                target.enabled = false; // Keep this real hit receiver stationary for range/collision measurements.
                target.MyRigidbody2D.linearVelocity = Vector2.zero;
                enemies.ClearEnemies(); enemies.AddEnemy(target);
                target.transform.position = player.transform.position + new Vector3(profile.Range + .1f, 0);
                fire.Invoke(manager, new object[] { caster.Key }); anim.MyAnimator.Update(0);
                Require(!caster.CanCast() && anim.MyAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), hero + " outside horizontal range does not attack");
                target.transform.position = player.transform.position + new Vector3(0, profile.Range + .1f);
                Require(!caster.CanCast(), hero + " outside vertical range does not attack");
                target.transform.position = player.transform.position + new Vector3(profile.Range * .8f, profile.Range * .8f);
                Require(!caster.CanCast(), hero + " rejects diagonal targets outside the circular range");
                target.transform.position = player.transform.position + new Vector3(profile.Range * .6f, 0);
                Require(caster.CanCast(), hero + " detects an enemy within its own range");
                Require(manager.GetComponentInChildren<RandomSkillCast>().CanCast() &&
                        manager.GetComponentInChildren<ChainSkillCast>().CanCast(),
                    hero + " other targeted weapons detect a valid target");
                int hpBefore = target.StatRuntime.GetValue("HP");
                fire.Invoke(manager, new object[] { caster.Key }); anim.MyAnimator.Update(0);
                Require(anim.MyAnimator.GetCurrentAnimatorStateInfo(0).IsName("Attack"), hero + " real target triggers Attack");
                if (profile.Kind == StarterWeaponKind.ThornSlash)
                {
                    Require(FindObjectsByType<ToNearestSkill>(FindObjectsSortMode.None).Length == bolts && target.StatRuntime.GetValue("HP") < hpBefore,
                        "Rowan cleaves in melee and deals real damage without a projectile");
                    var slash = FindFirstObjectByType<DATN.World.ThornSlashEffect>().GetComponent<SpriteRenderer>();
                    using var probe = new SpriteVisibilityProbe();
                    Require(probe.VisiblePixels(slash) >= 200, "Rowan slash renders actual visible pixels");
                }
                else
                {
                    Require(FindObjectsByType<ToNearestSkill>(FindObjectsSortMode.None).Length > bolts, hero + " real target creates projectile");
                    Require(FindObjectsByType<ToNearestSkill>(FindObjectsSortMode.None).Any(p => p.GetComponent<SpriteRenderer>().sprite == profile.Effect),
                        hero + " projectile uses its distinct orb/arrow sprite");
                }
                var combatPause = GameObject.Find("TrialPausePanel"); combatPause.SetActive(false);
                yield return CaptureFrame("starter-" + hero);
                combatPause.SetActive(true);
                if (profile.Kind != StarterWeaponKind.ThornSlash)
                {
                    var shot = FindObjectsByType<ToNearestSkill>(FindObjectsSortMode.None)
                        .Where(p => p.GetComponent<SpriteRenderer>().sprite == profile.Effect)
                        .OrderBy(p => Vector2.Distance(p.transform.position, player.transform.position)).First();
                    var enemyCollider = target.GetComponentsInChildren<Collider2D>().First(c => c.CompareTag("EnemyHit"));
                    bool hitReturned = false;
                    UnityEngine.Events.UnityAction<SkillBase> hitReturn = s => hitReturned = true;
                    shot.OnDeath += hitReturn;
                    enemyCollider.enabled = true;
                    enemies.ClearEnemies();
                    _round.Resume();
                    yield return Wait(() => target.StatRuntime.GetValue("HP") < hpBefore, hero + " projectile hits the actual enemy through physics");
                    Require(hitReturned, hero + " hit projectile returns to its real pool");
                    shot.OnDeath -= hitReturn;
                    _round.Pause();
                    enemyCollider.enabled = false;
                    enemies.ClearEnemies(); enemies.AddEnemy(target);
                    fire.Invoke(manager, new object[] { caster.Key });
                    shot = FindObjectsByType<ToNearestSkill>(FindObjectsSortMode.None)
                        .Where(p => p.GetComponent<SpriteRenderer>().sprite == profile.Effect)
                        .OrderBy(p => Vector2.Distance(p.transform.position, player.transform.position)).First();
                    Vector3 origin = shot.transform.position;
                    bool rangeReturned = false;
                    UnityEngine.Events.UnityAction<SkillBase> rangeReturn = s => rangeReturned = true;
                    shot.OnDeath += rangeReturn;
                    target.transform.position = player.transform.position + Vector3.right * 20;
                    var enabledColliders = existing.Where(e => e != null).SelectMany(e => e.GetComponentsInChildren<Collider2D>()).Where(c => c.enabled).ToArray();
                    foreach (var c in enabledColliders) c.enabled = false;
                    _round.Resume();
                    float maximumDistance = 0, deadline = Time.realtimeSinceStartup + 2.2f;
                    while (!rangeReturned && Time.realtimeSinceStartup < deadline)
                    {
                        maximumDistance = Mathf.Max(maximumDistance, Vector2.Distance(origin, shot.transform.position));
                        yield return null;
                    }
                    shot.OnDeath -= rangeReturn;
                    Require(rangeReturned && maximumDistance <= profile.Range + .01f && maximumDistance >= profile.Range * .8f,
                        hero + " projectile expires at its own range; measured " + maximumDistance.ToString("F2"));
                    _round.Pause();
                    foreach (var c in enabledColliders) if (c != null) c.enabled = true;
                }
                target.transform.position = player.transform.position + new Vector3(20, 0);
                typeof(PlayerAnim).GetField("_attackLockUntil", flags).SetValue(anim, 0f);
                anim.MyAnimator.Play("Idle", 0, 0); anim.MyAnimator.Update(0);
                fire.Invoke(manager, new object[] { caster.Key }); anim.MyAnimator.Update(0);
                Require(anim.MyAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), hero + " stops attacking when enemy leaves range");
                enemies.ClearEnemies(); foreach (var enemy in existing) if (enemy != null) enemies.AddEnemy(enemy);
                pool.Return(targetObject);

                var chestDef = defs.Single(d => d.Pool.Prefab.GetComponent<TreasureBox>() != null);
                var chestObject = pool.Request(chestDef.Key);
                var chest = chestObject.GetComponent<TreasureBox>();
                chest.transform.position = player.transform.position + new Vector3(4, 1, 0);
                Require(chest.GetComponent<SpriteRenderer>().sprite.name == "GoldChest", "Pooled chest uses the new Jade sprite");
                using (var probe = new SpriteVisibilityProbe()) Require(probe.VisiblePixels(chest.GetComponent<SpriteRenderer>()) >= 200, "Chest has actual visible rendered pixels");
                var pausePanel = GameObject.Find("TrialPausePanel");
                pausePanel.SetActive(false);
                yield return CaptureFrame("gold-chest-" + hero);
                pausePanel.SetActive(true);
                var collider = player.GetComponentsInChildren<Collider2D>().First(c => c.CompareTag("PlayerHit"));
                int gold = _round.GlobalConfig.CurrentGetGold;
                var collect = typeof(TreasureBox).GetMethod("OnTriggerEnter2D", flags);
                collect.Invoke(chest, new object[] { chest.GetComponent<Collider2D>() });
                Require(_round.GlobalConfig.CurrentGetGold == gold, "Non-player cannot collect chest");
                collect.Invoke(chest, new object[] { collider });
                Require(_round.GlobalConfig.CurrentGetGold == gold + 50 && !chestObject.activeSelf, hero + " chest awards exactly 50 round gold and disappears");
                collect.Invoke(chest, new object[] { collider });
                Require(_round.GlobalConfig.CurrentGetGold == gold + 50, "Duplicate collider callback cannot award twice");
                chestObject = pool.Request(chestDef.Key);
                chestObject.transform.position = player.transform.position;
                _round.Resume();
                yield return Wait(() => _round.GlobalConfig.CurrentGetGold >= gold + 100, "Real physics overlap collects a reused chest");
                Require(_round.GlobalConfig.CurrentGetGold == gold + 100 && !chestObject.activeSelf, "Reused chest awards exactly once through physics");
                Require(Time.timeScale > 0 && !_round.IsPaused, "Gold pickup keeps combat running without upgrade panel");
                var props = FindObjectsByType<DropBox>(FindObjectsSortMode.None);
                Require(props.Length > 0 && props.All(p => p.GetComponent<SpriteRenderer>().sprite.name == "GoldChest"), "Every chest placed on the map uses the new sprite");
                int beforeProp = _round.GlobalConfig.CurrentGetGold;
                var mapChest = props[0];
                player.transform.position = mapChest.transform.position;
                player.GetComponent<Rigidbody2D>().position = mapChest.transform.position;
                Physics2D.SyncTransforms();
                yield return Wait(() => mapChest == null || !mapChest.gameObject.activeSelf, "Walking onto a placed map chest collects it");
                Require(_round.GlobalConfig.CurrentGetGold == beforeProp + 50, hero + " placed map chest awards 50 gold once");
                yield return CaptureFrame("gold-collected-" + hero);
                _round.ExitToMainMenu(); yield return WaitForMenu();
            }
        }

        private IEnumerator CheckJadeArt()
        {
            yield return EnterRound(0, "Rowan");
            _round.Pause();
            yield return null;
            var pools = Resources.FindObjectsOfTypeAll<RGame.Framework.PoolRuntimeSO>()
                .First(asset => asset.name == "Pool");
            var definitions = (List<RGame.Framework.PoolDefinition>)typeof(RGame.Framework.PoolRuntimeSO)
                .GetField("_poolDefinitions", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(pools);
            var prefabs = definitions.Select(definition => definition.Pool.Prefab)
                .Where(prefab => prefab != null && prefab.GetComponent<BaseEnemy>() != null)
                .Where(prefab => prefab.GetComponent<SpriteRenderer>() != null)
                .GroupBy(prefab => prefab.GetComponent<SpriteRenderer>().sprite.name)
                .Select(group => group.First()).ToArray();
            string[] expected = { "Mossling", "MosslingBloom", "GloomBat", "HollowEye", "GraveWisp", "PaleShade", "RotWatcher", "HollowWarden", "GroveWarden", "GroveColossus" };
            Require(expected.All(name => prefabs.Any(prefab => prefab.GetComponent<SpriteRenderer>().sprite.name == name)),
                "All nine Jade enemies and the boss are wired in the real runtime pool");
            using var visibility = new SpriteVisibilityProbe();
            foreach (var name in expected)
            {
                var prefab = prefabs.First(candidate => candidate.GetComponent<SpriteRenderer>().sprite.name == name);
                var instance = Instantiate(prefab);
                foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
                foreach (var collider in instance.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
                var renderer = instance.GetComponent<SpriteRenderer>();
                Require(renderer.sprite != null && renderer.color.a > .95f && visibility.VisiblePixels(renderer) >= 200,
                    name + " has opaque rendered pixels on its actual prefab material");
                Destroy(instance);
            }
            var gallery = new GameObject("Jade_EnemyGallery", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = gallery.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = gallery.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var bg = DATN.UI.ForestUI.Picture(gallery.transform, "Backdrop", "Panel", 0, 0, 1, 1);
            bg.color = Color.white;
            DATN.UI.ForestUI.Label(gallery.transform, "Title", "NHẬT THỰC NGỌC BÍCH · QUÁI VÀ BOSS", "JADE ECLIPSE · ENEMIES AND BOSS", 42,
                .04f, .85f, .96f, .96f, TextAlignmentOptions.Center, DATN.UI.ForestUI.Gold);
            for (int i = 0; i < expected.Length; i++)
            {
                float x = .04f + (i % 5) * .19f, top = .79f - (i / 5) * .34f;
                var image = DATN.UI.ForestUI.Picture(gallery.transform, "Enemy" + i, null, x, top - .22f, x + .17f, top, true);
                image.sprite = prefabs.First(candidate => candidate.GetComponent<SpriteRenderer>().sprite.name == expected[i]).GetComponent<SpriteRenderer>().sprite;
                DATN.UI.ForestUI.Label(gallery.transform, "Name" + i, expected[i], expected[i], 24,
                    x, top - .28f, x + .17f, top - .22f, TextAlignmentOptions.Center);
            }
            yield return CaptureFrame("jade-enemies");
            Destroy(gallery);
            Require(Resources.Load<Sprite>("SGJade/Icons/MoneyBag") != null, "Money bag has its real Jade icon");
            Require(Resources.Load<Sprite>("SGJade/Icons/EclipseEvolved") != null, "Evolution has a distinct diamond icon");
            Require(Resources.Load<Sprite>("SGJade/VFX/Impact") != null, "Jade impact effect is included");
            _round.ExitToMainMenu();
            yield return WaitForMenu();
        }

        private IEnumerator CaptureFrame(string name)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            bool completed = false;
            Exception captureError = null;
            IEnumerator CaptureRenderedFrame()
            {
                yield return new WaitForEndOfFrame();
                Texture2D texture = null;
                try
                {
                    texture = ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(Path.Combine(_output, name + ".png"), texture.EncodeToPNG());
                }
                catch (Exception error) { captureError = error; }
                finally
                {
                    if (texture != null) Destroy(texture);
                    completed = true;
                }
            }
            var pending = StartCoroutine(CaptureRenderedFrame());
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!completed && Time.realtimeSinceStartup < deadline) yield return null;
            if (!completed)
            {
                StopCoroutine(pending);
                throw new TimeoutException("No rendered end-of-frame for screenshot '" + name +
                    "'. Keep the QA Player visible; minimized rendering cannot prove a visual result.");
            }
            if (captureError != null) throw new InvalidOperationException("Screenshot failed: " + name, captureError);
        }

        private void Capture(string name) => ScreenCapture.CaptureScreenshot(Path.Combine(_output, name + ".png"));

        private void Require(bool condition, string detail)
        {
            if (!condition) throw new InvalidOperationException(detail);
            Record("PASS", detail);
        }

        private void Record(string status, string detail)
        {
            string message = $"{status} | {detail}";
            _report.Add(message);
            Debug.Log("[Regression] " + message);
        }

        private void CollectError(string condition, string stack, LogType type)
        {
            if (type is not (LogType.Error or LogType.Exception or LogType.Assert) ||
                condition.StartsWith("Curl error", StringComparison.Ordinal)) return;
            _errorCount++;
            if (_errors.Count < 60) _errors.Add($"{type}: {condition}\n{stack}");
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= CollectError;
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
        }
    }
}
