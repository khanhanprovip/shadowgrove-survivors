using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using RGame.RoguelikeKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DATN.Modes;
using Object = UnityEngine.Object;
using TrialController = DATN.Trial.TrialController;

/// <summary>
///     Bo kiem thu runtime tu dong cho do an (chay headless qua -executeMethod).
///     Mo scene Initialization vao Play Mode, lai menu bang code (goi dung method
///     cua cac nut bam), vao man Trial Forest, ghi lai map/player/quai/kills/loi.
///     Chong mat state khi domain reload bang [InitializeOnLoad] + SessionState:
///     static ctor chi re-dang ky handler khi co phien test dang bat co.
///     Vi du: Unity.exe -batchmode -projectPath ... -executeMethod DATN.PlayTestRunner.Smoke45
/// </summary>
namespace DATN
{
    // [InitializeOnLoad] bat buoc: static ctor phai chay lai sau moi domain reload
    // (khi vao Play Mode) de re-dang ky handler; khong co attribute nay ctor chi
    // chay lazy khi co code truy cap class → test se bi treo mai trong play mode.
    [InitializeOnLoad]
    public static class PlayTestRunner
    {
        private const string InitScene = "Assets/RGame/RoguelikeKit/Scenes/Initialization.unity";
        private static string ReportDir => Environment.GetEnvironmentVariable("DATN_TEST_OUTPUT_DIR")
                                           ?? "F:/Do_An_KT/_assistant_work";
        private const string GlobalConfigPath =
            "Assets/RGame/RoguelikeKit/ScriptableObjects/RuntimeDataSO/Global/GlobalConfigSO.asset";

        private const string KeyActive = "DATN_PT_Active";
        private const string KeyDone = "DATN_PT_Done";
        private const string KeyErrorCount = "DATN_PT_Errors";
        private const string KeyReport = "DATN_PT_Report";
        private const string KeyDuration = "DATN_PT_Duration";
        private const string KeyStartWallClock = "DATN_PT_StartWall";
        private const string KeyMode = "DATN_PT_Mode";
        private const string KeyCharacter = "DATN_PT_Character";
        private const string KeyLevel = "DATN_PT_Level";

        // State runtime (chi ton tai trong 1 domain — duoc reset khoi tau moi lan reload)
        private static readonly List<string> Errors = new();
        private static readonly List<string> Events = new();
        private static readonly List<string> LogLines = new();
        private static float _playStart;
        private static float _lastSample = -1f;
        private static int _maxEnemies;
        private static int _lastKills = -1;
        private static bool _menuDriven;
        private static bool _finished;
        private static bool _subscribed;
        private static GlobalConfigSO _globalConfig;
        private static RGame.CommonStat.CommonStatRuntimeSO _playerStat;

        static PlayTestRunner()
        {
            // Duoc goi sau moi domain reload (ke ca reload khi vao Play Mode).
            // Chi lam viec khi co phien test dang chay — khong anh huong editor binh thuong.
            if (SessionState.GetBool(KeyActive, false))
            {
                SubscribeAll();
                Debug.Log("[PlayTest] Domain reloaded — da re-arming handlers cho phien test dang chay");
            }
        }

        // ---- Entry points (goi qua -executeMethod) ----

        public static void Smoke45()
        {
            SessionState.SetString(KeyMode, "smoke");
            Begin(45f, Path.Combine(ReportDir, "playtest_smoke45.txt"));
        }

        public static void Full100()
        {
            SessionState.SetString(KeyMode, "full");
            Begin(130f, Path.Combine(ReportDir, "playtest_full100.txt"));
        }

        public static void Full100Rowan()
        {
            SessionState.SetString(KeyMode, "full");
            Begin(130f, Path.Combine(ReportDir, "playtest_full100_rowan.txt"), "Rowan");
        }

        public static void Full100Lyra()
        {
            SessionState.SetString(KeyMode, "full");
            Begin(130f, Path.Combine(ReportDir, "playtest_full100_lyra.txt"), "Lyra");
        }

        public static void Full100Kael()
        {
            SessionState.SetString(KeyMode, "full");
            Begin(130f, Path.Combine(ReportDir, "playtest_full100_kael.txt"), "Kael");
        }

        /// <summary>Kiem thu man KillTarget moi (LV2): thang khi ha du 30 quai.</summary>
        public static void KillTargetLevel()
        {
            SessionState.SetString(KeyMode, "full");
            Begin(160f, Path.Combine(ReportDir, "playtest_killtarget_lv2.txt"), "Rowan", 1);
        }

        public static void Flow65()
        {
            SessionState.SetString(KeyMode, "flow");
            Begin(65f, Path.Combine(ReportDir, "playtest_flow65.txt"));
        }

        public static void Lose60()
        {
            SessionState.SetString(KeyMode, "lose");
            Begin(60f, Path.Combine(ReportDir, "playtest_lose60.txt"));
        }

        private static string Mode => SessionState.GetString(KeyMode, "smoke");

        private static void Begin(float duration, string reportFile, string character = "Rowan", int levelIndex = 0)
        {
            Directory.CreateDirectory(ReportDir);
            SessionState.SetBool(KeyActive, true);
            SessionState.SetString(KeyReport, reportFile);
            SessionState.SetFloat(KeyDuration, duration);
            SessionState.SetString(KeyCharacter, character);
            SessionState.SetInt(KeyLevel, levelIndex);
            SessionState.SetFloat(KeyStartWallClock, Time.realtimeSinceStartup);

            SubscribeAll();

            EditorSceneManager.OpenScene(InitScene, OpenSceneMode.Single);
            Debug.Log($"[PlayTest] Bat dau kiem thu {duration:F0}s — mo Initialization scene va vao Play Mode");
            EditorApplication.isPlaying = true;
        }

        private static void SubscribeAll()
        {
            if (_subscribed) return;
            _subscribed = true;
            Application.logMessageReceived += OnLog;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += Tick;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _playStart = Time.realtimeSinceStartup;
                Events.Add("PASS | Play Mode bat dau tu scene Initialization");
            }
            else if (state == PlayModeStateChange.ExitingPlayMode && !_finished)
            {
                Finish("Play Mode ket thuc truoc du kien (bi dong boi he thong)");
            }
        }

        private static void OnLog(string condition, string stacktrace, LogType type)
        {
            if (type is LogType.Exception or LogType.Error or LogType.Assert)
            {
                if (condition.Contains("Licensing") || condition.Contains("Entitlement")) return;
                // Loi mang cua telemetry Unity trong moi truong khong co internet
                if (condition.Contains("Curl error") || condition.Contains("cdp.cloud")) return;
                if (Errors.Count < 200) Errors.Add($"[{type}] {condition}");
            }
        }

        private static void Tick()
        {
            if (EditorApplication.isPlaying)
            {
                RunPlayLogic();
                return;
            }

            // Dang o edit mode (sau khi play ket thuc hoac sau domain reload thoat play)
            // — dung co SessionState vi bien static co the bi xoa boi reload
            if (SessionState.GetBool(KeyActive, false) && SessionState.GetBool(KeyDone, false))
            {
                WriteReportAndExit();
            }
        }

        private static void RunPlayLogic()
        {
            float elapsed = Time.realtimeSinceStartup - _playStart;
            float duration = SessionState.GetFloat(KeyDuration, 45f);

            // ---- Phan 1: doi MainMenu xuat hien roi lai menu qua cac nut that ----
            if (!_menuDriven && elapsed > 6f) DriveMenu();
            if (!_menuDriven && elapsed > 35f)
            {
                Events.Add("FAIL | Menu did not reach level confirmation");
                Finish("Menu timeout");
                return;
            }

            // ---- Phan 2: quan sat gameplay, mau 1 giay/lan ----
            if (_menuDriven && elapsed - _lastSample >= 1f)
            {
                _lastSample = elapsed;
                SampleGameplay(elapsed);
            }

            // ---- Cac hanh dong kiem thu rieng theo che do ----
            if (_menuDriven)
            {
                RunModeScriptedActions(elapsed);
            }

            // ---- Timeout bao ve: sau duration + 30s ma chua finish thi ep ket thuc ----
            if (elapsed >= duration + 30f && !_finished)
            {
                FinalChecks();
                Finish($"TIMEOUT: ep ket thuc sau {elapsed:F0}s");
                return;
            }

            if ((elapsed >= duration || (_winSeenAt >= 0f && elapsed >= _winSeenAt + 1f)) && _menuDriven && !_finished)
            {
                FinalChecks();
                Finish($"Ket thuc kiem thu dung han sau {elapsed:F0}s");
            }
        }

        private static int _menuStep;

        private static void DriveMenu()
        {
            if (_menuStep == 0)
            {
                var menu = Object.FindFirstObjectByType<UIMainMenu>();
                if (menu == null) return;
                menu.NewGameButton();
                _menuStep++;
                return;
            }

            string character = SessionState.GetString(KeyCharacter, "Rowan");
            if (_menuStep == 1)
            {
                var panel = Object.FindFirstObjectByType<CharacterSelectPanel>();
                if (panel == null) return;
                foreach (var item in panel.GetComponentsInChildren<CharacterItem>())
                {
                    if (!Array.Exists(item.GetComponentsInChildren<TMPro.TMP_Text>(),
                            label => label.text == character)) continue;
                    item.GetComponent<Button>().onClick.Invoke();
                    panel.ConfirmButton();
                    _menuStep++;
                    break;
                }
                return;
            }

            int levelIndex = SessionState.GetInt(KeyLevel, 0);
            var levels = Object.FindFirstObjectByType<LevelSelectPanel>();
            if (levels == null) return;
            var items = levels.GetComponentsInChildren<LevelItem>();
            if (items.Length <= levelIndex) return;
            items[levelIndex].GetComponent<Button>().onClick.Invoke();
            levels.ConfirmButton();
            _menuDriven = true;
            Events.Add($"PASS | New Game → {character} → level {levelIndex + 1} → Confirm");
        }

        private static void SampleGameplay(float elapsed)
        {
            var enemies = Object.FindObjectsByType<BaseEnemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            _maxEnemies = Mathf.Max(_maxEnemies, enemies.Length);

            var map = GameObject.Find("Map");
            var player = Object.FindFirstObjectByType<Player>(FindObjectsInactive.Exclude);

            if (_globalConfig == null)
            {
                _globalConfig = AssetDatabase.LoadAssetAtPath<GlobalConfigSO>(GlobalConfigPath);
            }

            // Che do "full" can song sot du 90s de kiem chien thang — bot dung yen
            // se chet nen bom HP (god mode) chi trong tam kiem thu tu dong.
            if (Mode == "full" && _playerStat == null)
            {
                _playerStat = AssetDatabase.LoadAssetAtPath<RGame.CommonStat.CommonStatRuntimeSO>(
                    "Assets/RGame/RoguelikeKit/ScriptableObjects/DataSO/Player/PlayerRuntimeStat.asset");
            }
            if (Mode == "full" && _playerStat != null)
            {
                _playerStat.ModifyValue("HP", 800);
            }

            int kills = _globalConfig != null ? _globalConfig.CurrentGetKill : -1;
            if (kills != _lastKills)
            {
                Events.Add($"INFO | t={elapsed:F0}s kills={kills} enemies_dang_song={enemies.Length}");
                _lastKills = kills;
            }

            if (Mathf.Approximately(elapsed % 5f, 0f))
            {
                LogLines.Add(
                    $"t={elapsed:F0}s scenes={SceneManager.sceneCount} mapTiles={(map != null ? map.transform.childCount : -1)} " +
                    $"player={(player != null ? "CO" : "KHONG")} enemies={enemies.Length} kills={kills}");
            }

            // Map phai duoc sinh som trong 10 giay dau tien sau khi vao game
            if (map != null && map.transform.childCount > 0 && !Events.Exists(e => e.Contains("Map duoc sinh")))
            {
                Events.Add($"PASS | Map duoc sinh tai t={elapsed:F0}s voi {map.transform.childCount} tile");
            }
        }

        /// <summary>
        ///     Cac hanh dong kiem thu dac thu tung che do:
        ///     - flow: Pause (25s) → Resume (30s) → Restart (40s) → kiem tra reset.
        ///     - full: kiem tra HUD dong ho va man chien thang hien sau 90 giay.
        /// </summary>
        private static bool _flowPaused, _flowResumed, _flowRestarted, _winChecked, _hudChecked;
        private static bool _postRestartChecked;
        private static Player _previousPlayer;
        private static int _killsAtPause = -1, _killsBeforeRestart = -1;

        private static void RunModeScriptedActions(float elapsed)
        {
            if (Mode == "flow")
            {
                if (!_flowPaused && elapsed >= 25f && Time.timeScale > 0f)
                {
                    _flowPaused = true;
                    var ctrl = Object.FindFirstObjectByType<TrialController>(FindObjectsInactive.Include);
                    if (ctrl == null)
                    {
                        Events.Add("FAIL | Khong tim thay TrialController de kiem tra Pause");
                        return;
                    }

                    ctrl.Pause();
                    bool paused = Mathf.Approximately(Time.timeScale, 0f);
                    var panel = GameObject.Find("TrialPausePanel");
                    _killsAtPause = _globalConfig != null ? _globalConfig.CurrentGetKill : -1;
                    Events.Add($"{(paused && panel != null && panel.activeSelf ? "PASS" : "FAIL")} | Pause(): timeScale={Time.timeScale}, panel hien={(panel != null && panel.activeSelf)}");
                }

                if (_flowPaused && !_flowResumed && elapsed >= 28f)
                {
                    _flowResumed = true;
                    int killsNow = _globalConfig != null ? _globalConfig.CurrentGetKill : -1;
                    bool stillFrozen = Mathf.Approximately(Time.timeScale, 0f) && killsNow == _killsAtPause;
                    Events.Add($"{(stillFrozen ? "PASS" : "FAIL")} | Giu pause 3s: timeScale={Time.timeScale}, kills giu nguyen ({_killsAtPause} → {killsNow})");

                    var ctrl = Object.FindFirstObjectByType<TrialController>(FindObjectsInactive.Include);
                    ctrl?.Resume();
                    bool resumed = Mathf.Approximately(Time.timeScale, 1f);
                    var panel = GameObject.Find("TrialPausePanel");
                    Events.Add($"{(resumed && panel == null ? "PASS" : "FAIL")} | Resume(): timeScale={Time.timeScale}, panel an={panel == null}");
                }

                if (!_flowRestarted && elapsed >= 40f)
                {
                    _flowRestarted = true;
                    _previousPlayer = Object.FindFirstObjectByType<Player>();
                    _killsBeforeRestart = _globalConfig != null ? _globalConfig.CurrentGetKill : -1;
                    Events.Add($"INFO | Truoc Restart: kills={_killsBeforeRestart} — goi Restart()");

                    var ctrl = Object.FindFirstObjectByType<TrialController>(FindObjectsInactive.Include);
                    ctrl?.Restart();
                }

                if (_flowRestarted && !_postRestartChecked)
                {
                    var player = Object.FindFirstObjectByType<Player>();
                    if (player == null || player == _previousPlayer) return;
                    var ctrl = Object.FindFirstObjectByType<TrialController>();
                    if (ctrl == null || ctrl.GlobalConfig.GlobalPlayer != player) return;
                    _postRestartChecked = true;
                    int killsAfter = _globalConfig != null ? _globalConfig.CurrentGetKill : -1;
                    bool reset = _killsBeforeRestart > 0 && killsAfter == 0 &&
                                 ctrl.MatchSeconds < 0.25f && ctrl.PlayerStat.GetValue("Level") == 1 &&
                                 Mathf.Approximately(Time.timeScale, 1f);
                    Events.Add($"{(reset ? "PASS" : "FAIL")} | Restart: kills {_killsBeforeRestart} → {killsAfter}, match={ctrl.MatchSeconds:F3}s, level={ctrl.PlayerStat.GetValue("Level")}, timeScale={Time.timeScale}");
                }
            }
            else if (Mode == "full")
            {
                if (!_hudChecked && elapsed >= 20f)
                {
                    _hudChecked = true;
                    var hud = GameObject.Find("TrialTimeHud");
                    var tmp = hud != null ? hud.GetComponent<TMPro.TextMeshProUGUI>() : null;
                    Events.Add($"{(tmp != null && !string.IsNullOrEmpty(tmp.text) ? "PASS" : "FAIL")} | HUD thoi gian: {(tmp != null ? tmp.text : "KHONG TON TAI")}");
                }

                // Headless khong co nguoi choi bam chon — panel nang cap hoac ruong
                // se dong bang thoi gian mai. Tu dong xac nhan de game chay tiep.
                // Item luon active (panel an bang CanvasGroup) nen phai doc alpha
                // cua panel cha truoc khi quyet dinh loai panel dang mo.
                if (Mathf.Approximately(Time.timeScale, 0f) && _winSeenAt < 0f &&
                    elapsed - _lastPickAttempt >= 1f)
                {
                    _lastPickAttempt = elapsed;
                    var upgradePanel = Object.FindFirstObjectByType<UpgradeSkillPanel>(FindObjectsInactive.Exclude);
                    if (upgradePanel != null && IsPanelVisible(upgradePanel))
                    {
                        var item = Object.FindFirstObjectByType<UpgradeSkillItem>(FindObjectsInactive.Exclude);
                        if (item != null)
                        {
                            item.UpgradeSkill();
                            _skillPicks++;
                        }
                    }
                    else
                    {
                        var treasure = Object.FindFirstObjectByType<TreasurePanel>(FindObjectsInactive.Exclude);
                        if (treasure != null && IsPanelVisible(treasure))
                        {
                            treasure.EnterSkill();
                            _treasurePicks++;
                        }
                    }
                }

            // Phat hien chien thang lien tuc (khong doi moc 92s cu the)
                if (_winSeenAt < 0f)
                {
                    var winPanel = GameObject.Find("GameWin");
                    var group = winPanel != null ? winPanel.GetComponent<CanvasGroup>() : null;
                    if (group != null && group.alpha > 0.9f)
                    {
                        _winSeenAt = elapsed;
                        var ctrl = Object.FindFirstObjectByType<TrialController>();
                        var selection = AssetDatabase.LoadAssetAtPath<LevelAndCharacterSO>(
                            "Assets/RGame/RoguelikeKit/ScriptableObjects/LogicSO/Level/LevelAndCharacterSO.asset");
                        var config = selection.SelectLevelSO.GameMode;
                        int kills = ctrl.GlobalConfig.CurrentGetKill;
                        bool objectiveMet = config.ModeId == GameModeId.KillTarget
                            ? kills == config.TargetKills
                            : Mathf.Abs(ctrl.MatchSeconds - config.Duration) < 0.25f;
                        objectiveMet &= Mathf.Approximately(Time.timeScale, 0f);
                        Events.Add($"{(objectiveMet ? "PASS" : "FAIL")} | {config.ModeId}: match={ctrl.MatchSeconds:F3}s, kills={kills}, timeScale={Time.timeScale}");
                        var restartBtn = winPanel.transform.Find("RestartBtn");
                        Events.Add($"{(restartBtn != null ? "PASS" : "FAIL")} | Nut CHOI LAI tren man thang: {(restartBtn != null ? "CO" : "KHONG")}");
                    }
                }
            }
            else if (Mode == "lose")
            {
                // Che do thua: bot dung yen. De ket qua xac dinh (tu nhien co the
                // song qua 60s tuy seed), tu dong ha HP ve 1 o t=35s → dot tan
                // cong ke tiep se giet nhan vat va phai hien GameOver.
                if (!_hpDrained && elapsed >= 35f)
                {
                    _hpDrained = true;
                    if (_playerStat == null)
                    {
                        _playerStat = AssetDatabase.LoadAssetAtPath<RGame.CommonStat.CommonStatRuntimeSO>(
                            "Assets/RGame/RoguelikeKit/ScriptableObjects/DataSO/Player/PlayerRuntimeStat.asset");
                    }
                    int hp = _playerStat != null ? _playerStat.GetValue("HP") : -1;
                    if (_playerStat != null && hp > 1)
                    {
                        _playerStat.ModifyValue("HP", -(hp - 1));
                    }
                    Events.Add($"INFO | t=35s: ep HP ve 1 (tu {hp}) de kich hoat tu chet co dinh");
                }

                if (_loseSeenAt < 0f)
                {
                    var losePanel = GameObject.Find("GameOver");
                    var group = losePanel != null ? losePanel.GetComponent<CanvasGroup>() : null;
                    if (group != null && group.alpha > 0.9f)
                    {
                        _loseSeenAt = elapsed;
                        Events.Add($"PASS | THUA xay ra tai t={elapsed:F0}s (alpha={group.alpha:F2}), timeScale={Time.timeScale}");
                        var restartBtn = losePanel.transform.Find("RestartBtn");
                        Events.Add($"{(restartBtn != null ? "PASS" : "FAIL")} | Nut CHOI LAI tren man thua: {(restartBtn != null ? "CO" : "KHONG")}");
                    }
                }
            }
        }

        private static float _winSeenAt = -1f;
        private static float _loseSeenAt = -1f;
        private static bool _hpDrained;
        private static int _skillPicks;
        private static int _treasurePicks;
        private static float _lastPickAttempt = -1f;

        /// <summary>
        ///     Panel dung CanvasGroup (khong SetActive) nen phai doc alpha qua
        ///     SerializedObject — component khong expose canvasGroup ra public.
        /// </summary>
        private static bool IsPanelVisible(UpgradeSkillPanel panel)
        {
            var so = new SerializedObject(panel);
            var groupRef = so.FindProperty("_canvasGroup").objectReferenceValue as CanvasGroup;
            return groupRef != null && groupRef.alpha > 0.5f;
        }

        private static bool IsPanelVisible(TreasurePanel panel)
        {
            var so = new SerializedObject(panel);
            var groupRef = so.FindProperty("canvasGroup").objectReferenceValue as CanvasGroup;
            return groupRef != null && groupRef.alpha > 0.5f;
        }

        private static void FinalChecks()
        {
            var map = GameObject.Find("Map");
            int tiles = map != null ? map.transform.childCount : 0;
            Events.Add($"{(tiles >= 1000 ? "PASS" : "FAIL")} | So tile map = {tiles} (ky vong >= 4096, ban do 64x64)");

            var player = Object.FindFirstObjectByType<Player>(FindObjectsInactive.Exclude);
            Events.Add($"{(player != null ? "PASS" : "FAIL")} | Player ton tai: {(player != null ? "CO" : "KHONG")}");

            var cam = Camera.main;
            Events.Add($"{(cam != null ? "PASS" : "FAIL")} | Camera chinh: {(cam != null ? cam.name + " pos=" + cam.transform.position : "KHONG")}");

            int sceneCount = SceneManager.sceneCount;
            var names = new StringBuilder();
            for (int i = 0; i < sceneCount; i++)
            {
                names.Append(SceneManager.GetSceneAt(i).name);
                if (i < sceneCount - 1) names.Append(", ");
            }
            Events.Add($"{(sceneCount >= 3 ? "PASS" : "FAIL")} | Scenes nap ({sceneCount}): {names}");

            var audioListeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Events.Add($"{(audioListeners.Length <= 1 ? "PASS" : "FAIL")} | AudioListener hoat dong: {audioListeners.Length} (<=1)");

            Events.Add($"{(_maxEnemies > 0 ? "PASS" : "FAIL")} | Quai duoc sinh (toi da dong thoi = {_maxEnemies})");

            int finalKills = _globalConfig != null ? _globalConfig.CurrentGetKill : -1;
            Events.Add($"{(finalKills > 0 ? "PASS" : "WARN")} | Tong kill tich luy = {finalKills} (tu dong tan cong cua nhan vat)");

            var gameWinPanel = GameObject.Find("GameWin");
            var gameOverPanel = GameObject.Find("GameOver");
            Events.Add($"INFO | GameWin panel: {(gameWinPanel != null ? "ton tai" : "KHONG")} | GameOver panel: {(gameOverPanel != null ? "ton tai" : "KHONG")}");

            if (Mode == "full")
            {
                Events.Add(_winSeenAt >= 0
                    ? $"PASS | TONG KET: chien thang xac nhan tai t={_winSeenAt:F0}s"
                    : "FAIL | TONG KET: khong thay man chien thang");
                Events.Add($"{(_skillPicks > 0 ? "PASS" : "FAIL")} | Upgrade selections: {_skillPicks}");
                Events.Add($"INFO | So lan tu chon ky nang nang cap: {_skillPicks} | so lan mo ruong tu xac nhan: {_treasurePicks}");

                var stat = AssetDatabase.LoadAssetAtPath<RGame.CommonStat.CommonStatRuntimeSO>(
                    "Assets/RGame/RoguelikeKit/ScriptableObjects/DataSO/Player/PlayerRuntimeStat.asset");
                Events.Add($"INFO | Cap do cuoi cung cua nhan vat: {(stat != null ? stat.GetValue("Level").ToString() : "?")}");
            }
            else if (Mode == "lose")
            {
                Events.Add(_loseSeenAt >= 0
                    ? $"PASS | TONG KET: THAT BAI xac nhan tai t={_loseSeenAt:F0}s khi nhan vat het mau (bot dung yen)"
                    : "FAIL | TONG KET: nhan vat khong chet trong 60s (khong kiem duoc luong thua)");
            }
            else if (Mode == "flow" && !_postRestartChecked)
            {
                Events.Add("FAIL | Restart did not spawn a new player");
            }
        }

        private static void Finish(string reason)
        {
            if (_finished) return;
            _finished = true;
            Events.Add($"INFO | {reason}");

            // Ghi report NGAY trong domain nay — domain reload khi thoat play
            // se xoa sach Events/Errors trong RAM.
            int failures = Events.FindAll(line => line.StartsWith("FAIL |", StringComparison.Ordinal)).Count;
            SessionState.SetInt(KeyErrorCount, Errors.Count + failures);
            WriteReport(reason);
            SessionState.SetBool(KeyDone, true);
            EditorApplication.isPlaying = false;
        }

        private static void WriteReport(string reason)
        {
            string reportFile = SessionState.GetString(KeyReport, Path.Combine(ReportDir, "playtest_last.txt"));

            var sb = new StringBuilder();
            sb.AppendLine($"BAO CAO PLAY TEST TU DONG — {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Thoi luong yeu cau: {SessionState.GetFloat(KeyDuration, 0f):F0}s | Menu da lai: {_menuDriven} | Ly do ket thuc: {reason}");
            sb.AppendLine();
            sb.AppendLine("== KET QUA CAC DIEM KIEM ==");
            foreach (string e in Events) sb.AppendLine(e);
            sb.AppendLine();
            sb.AppendLine("== LOG MAU 5s/lan ==");
            foreach (string l in LogLines) sb.AppendLine(l);
            sb.AppendLine();
            sb.AppendLine($"== SO LOI/EXCEPTION: {Errors.Count} ==");
            foreach (string err in Errors) sb.AppendLine(err);

            try
            {
                File.WriteAllText(reportFile, sb.ToString());
                Debug.Log($"[PlayTest] Bao cao da ghi: {reportFile} | Loi: {Errors.Count}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayTest] Khong ghi duoc bao cao: {ex.Message}");
            }
        }

        private static void WriteReportAndExit()
        {
            if (!SessionState.GetBool(KeyDone, false)) return;
            SessionState.SetBool(KeyActive, false);
            SessionState.SetBool(KeyDone, false);

            Application.logMessageReceived -= OnLog;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.update -= Tick;
            _subscribed = false;

            int errorCount = SessionState.GetInt(KeyErrorCount, 0);
            Debug.Log($"[PlayTest] Thoat Unity. Ma loi: {errorCount}");
            EditorApplication.Exit(errorCount > 0 ? 1 : 0);
        }
    }
}
