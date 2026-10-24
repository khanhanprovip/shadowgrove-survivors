using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using RGame.RoguelikeKit;
using UnityEngine;

namespace DATN.Trial
{
    /// <summary>
    ///     Trinh kiem thu tu dong tren BAN BUILD (khong phai Editor).
    ///     Chi hoat dong khi chay exe voi tham so "-datn-autotest" —
    ///     khi choi binh thuong script tu huy ngay, khong anh huong game.
    ///     Luong: doi MainMenu → NewGame → Confirm nhan vat → Confirm man →
    ///     kiem tra map/player/quai/FPS → bom HP (god mode) → tu xac nhan
    ///     panel nang cap → den 100s ghi bao cao ra file va thoat.
    ///     Bao cao: F:/Do_An_KT/_assistant_work/build_test_report.txt
    /// </summary>
    public class DatnAutoTestDriver : MonoBehaviour
    {
        private const string ReportPath = "F:/Do_An_KT/_assistant_work/build_test_report.txt";
        private const float Duration = 100f;

        private readonly List<string> _events = new();
        private readonly List<string> _errors = new();
        private float _start = -1f;
        private bool _menuDriven;
        private float _lastSample = -1f;
        private float _lastPick = -1f;
        private int _maxEnemies;
        private float _fpsAccum;
        private int _fpsFrames;
        private bool _winSeen;
        private float _winAt = -1f;
        private int _skillPicks;
        private bool _done;

        private void Start()
        {
            bool autoTest = false;
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg == "-datn-autotest") autoTest = true;
            }

            if (!autoTest)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            Application.logMessageReceived += OnLog;
            _start = Time.unscaledTime;
            _events.Add($"AUTO TEST BUILD — {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}");
            _events.Add($"Unity {Application.unityVersion} | CPU {SystemInfo.processorType} | RAM {SystemInfo.systemMemorySize} MB | GPU {SystemInfo.graphicsDeviceName}");
            _events.Add($"Do phan giai man hinh: {Screen.currentResolution.width}x{Screen.currentResolution.height}");
        }

        private void OnLog(string condition, string stacktrace, LogType type)
        {
            if (type is LogType.Exception or LogType.Error or LogType.Assert)
            {
                if (condition.Contains("Licensing") || condition.Contains("Curl error")) return;
                if (_errors.Count < 100) _errors.Add($"[{type}] {condition}");
            }
        }

        private void Update()
        {
            if (_done) return;

            float elapsed = Time.unscaledTime - _start;

            // Thong ke FPS theo cua so 5 giay
            _fpsAccum += Time.unscaledDeltaTime;
            _fpsFrames++;
            if (_fpsAccum >= 5f)
            {
                float avg = _fpsFrames / _fpsAccum;
                _events.Add($"INFO | t={elapsed:F0}s FPS(5s)={avg:F0} quai_dang_song={CountEnemies()}");
                _fpsAccum = 0f;
                _fpsFrames = 0;
            }

            // Lan menu bang dung cac method nut bam
            // (panel co the dang inactive — phai bao gom ca inactive khi tim)
            if (!_menuDriven && elapsed > 6f)
            {
                var menu = FindFirstObjectByType<UIMainMenu>(FindObjectsInactive.Include);
                var charPanel = FindFirstObjectByType<CharacterSelectPanel>(FindObjectsInactive.Include);
                var levelPanel = FindFirstObjectByType<LevelSelectPanel>(FindObjectsInactive.Include);
                if (menu != null && charPanel != null && levelPanel != null)
                {
                    menu.NewGameButton();
                    charPanel.ConfirmButton();
                    levelPanel.ConfirmButton();
                    _menuDriven = true;
                    _events.Add("PASS | Da lai menu: NewGame → Confirm nhan vat → Confirm man");
                }
                else if (elapsed > 30f)
                {
                    Finish("FAIL: Khong thay MainMenu sau 30s");
                    return;
                }
            }

            if (!_menuDriven) return;

            if (elapsed - _lastSample >= 1f)
            {
                _lastSample = elapsed;
                Sample(elapsed);
            }

            // Game bi dong bang → co panel mo (nang cap/ruong) → tu xac nhan
            if (Time.timeScale == 0f && !_winSeen && elapsed - _lastPick >= 1f)
            {
                _lastPick = elapsed;
                AutoConfirmPanels();
            }

            // Phat hien chien thang
            if (!_winSeen && elapsed >= 85f)
            {
                var winPanel = GameObject.Find("GameWin");
                var group = winPanel != null ? winPanel.GetComponent<CanvasGroup>() : null;
                if (group != null && group.alpha > 0.9f)
                {
                    _winSeen = true;
                    _winAt = elapsed;
                    _events.Add($"PASS | CHIEN THANG tren ban build tai t={elapsed:F0}s (alpha={group.alpha:F2})");
                }
            }

            if (elapsed >= Duration)
            {
                Finish(_winSeen
                    ? $"Ket thuc tu dong sau {Duration:F0}s — da ghi nhan chien thang tai t={_winAt:F0}s"
                    : $"Ket thuc tu dong sau {Duration:F0}s — KHONG ghi nhan chien thang");
            }
        }

        private static int CountEnemies()
        {
            return FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None).Length;
        }

        private void Sample(float elapsed)
        {
            int enemies = CountEnemies();
            _maxEnemies = Mathf.Max(_maxEnemies, enemies);

            var ctrl = FindFirstObjectByType<TrialController>();
            if (ctrl != null && ctrl.PlayerStat != null)
            {
                // God mode: bot dung yen se chet — bom HP de song sot du 90s
                ctrl.PlayerStat.ModifyValue("HP", 800);
            }

            var map = GameObject.Find("Map");
            if (map != null && map.transform.childCount > 0 &&
                !_events.Exists(e => e.Contains("Map duoc sinh")))
            {
                _events.Add($"PASS | Map duoc sinh {map.transform.childCount} tile tai t={elapsed:F0}s");
            }

            // Chup anh minh chung tai cac moc thoi gian quan trong
            if (elapsed >= 14.5f && elapsed < 15.5f && !_shot15)
            {
                _shot15 = Capture("screenshot_15s_gameplay.png", elapsed);
            }
            else if (elapsed >= 49.5f && elapsed < 50.5f && !_shot50)
            {
                _shot50 = Capture("screenshot_50s_ap_suat.png", elapsed);
            }
            else if (elapsed >= 96.5f && elapsed < 99.5f && !_shot96)
            {
                _shot96 = Capture("screenshot_96s_chien_thang.png", elapsed);
            }
        }

        private bool _shot15, _shot50, _shot96;

        private bool Capture(string fileName, float elapsed)
        {
            try
            {
                string path = Path.Combine("F:/Do_An_KT/_assistant_work", fileName);
                ScreenCapture.CaptureScreenshot(path); // ghi PNG cuoi frame
                _events.Add($"INFO | Da yeu cau chup anh: {fileName} (t={elapsed:F0}s)");
                return true;
            }
            catch (Exception ex)
            {
                _events.Add($"INFO | Chup anh that bai: {ex.Message}");
                return true; // khong thu lai
            }
        }

        private void AutoConfirmPanels()
        {
            var upgrade = FindFirstObjectByType<UpgradeSkillPanel>();
            if (upgrade != null && upgrade.IsVisible)
            {
                var item = FindFirstObjectByType<UpgradeSkillItem>();
                if (item != null)
                {
                    item.UpgradeSkill();
                    _skillPicks++;
                }
                return;
            }

            var treasure = FindFirstObjectByType<TreasurePanel>();
            if (treasure != null && treasure.IsVisible)
            {
                treasure.EnterSkill();
            }
        }

        private void Finish(string reason)
        {
            _done = true;

            var map = GameObject.Find("Map");
            var player = FindFirstObjectByType<Player>();
            var ctrl = FindFirstObjectByType<TrialController>();

            _events.Add($"{(map != null && map.transform.childCount >= 1000 ? "PASS" : "FAIL")} | Tong ket Map tiles: {(map != null ? map.transform.childCount : 0)}");
            _events.Add($"{(player != null ? "PASS" : "FAIL")} | Tong ket Player: {(player != null ? "CO" : "KHONG")}");
            _events.Add($"{(_maxEnemies > 0 ? "PASS" : "FAIL")} | Quai toi da dong thoi: {_maxEnemies}");
            _events.Add($"INFO | Kills: {(ctrl != null && ctrl.GlobalConfig != null ? ctrl.GlobalConfig.CurrentGetKill : -1)} | So lan tu chon ky nang: {_skillPicks}");
            _events.Add($"INFO | {reason}");
            _events.Add($"TONG_SO_LOI: {_errors.Count}");

            try
            {
                File.WriteAllLines(ReportPath, _events);
                File.AppendAllText(ReportPath, "\n== CHI TIET LOI ==\n" + string.Join("\n", _errors));
                Debug.Log($"[AutoTest] Report da ghi: {ReportPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AutoTest] Khong ghi duoc report: {ex.Message}");
            }

            Application.Quit(_errors.Count > 0 ? 1 : 0);
        }
    }
}
