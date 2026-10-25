using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace DATN.Localization
{
    /// <summary>
    ///     Quan ly song ngu Viet/Anh cua do an (Moc 2 - giai doan hoan thien).
    ///     - Tu khoi tao qua RuntimeInitializeOnLoadMethod, khong can sua scene.
    ///     - Nut "VI/EN" noi phai tren cung, bam de doi ngon ngu ngay lap tuc.
    ///     - Luu lua chon bang PlayerPrefs; lan dau theo ngon ngu he thong
    ///       (SystemLocaleSelector cua Unity Localization da chay truoc).
    ///     - Cap ham lay chuoi da dich cho UI tao runtime (TrialController...).
    /// </summary>
    public class LanguageManager : MonoBehaviour
    {
        public const string PrefsKey = "DATN_LocaleCode";
        public const string Table = "Shadowgrove";

        public static LanguageManager Instance { get; private set; }

        private Locale _vi;
        private Locale _en;
        private Button _toggleButton;
        private TMPro.TextMeshProUGUI _toggleLabel;

        private string CurrentCode => LocalizationSettings.SelectedLocale != null
            ? LocalizationSettings.SelectedLocale.Identifier.Code
            : "en";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance == null)
            {
                var go = new GameObject("DATN_LanguageManager");
                go.AddComponent<LanguageManager>();
                DontDestroyOnLoad(go);
            }
        }

        private int _buttonAttempts;

        private void Update()
        {
            // Tao nut o Update thay vi Awake: TMP can tai nguyen san trong Editor,
            // that bai thi thu lai toi da 3 lan, khong bao gio lam game loi.
            if (_toggleButton == null && _buttonAttempts < 3)
            {
                _buttonAttempts++;
                try { BuildToggleButton(); }
                catch (System.Exception ex) { Debug.LogWarning("[Lang] Thu tao nut ngon ngu that bai: " + ex.Message); }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            StartCoroutine(InitWhenLocalizationReady());
        }

        private IEnumerator InitWhenLocalizationReady()
        {
            yield return LocalizationSettings.InitializationOperation;

            foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
            {
                string code = locale.Identifier.Code;
                if (code == "vi") _vi = locale;
                else if (code == "en") _en = locale;
            }

            // Ap dung ngon ngu da luu (uu tien cao hon ngon ngu he thong)
            string saved = PlayerPrefs.GetString(PrefsKey, "");
            if (!string.IsNullOrEmpty(saved))
            {
                var target = saved == "vi" ? _vi : _en;
                if (target != null && CurrentCode != saved)
                {
                    LocalizationSettings.SelectedLocale = target;
                    yield return null;
                }
            }

            RefreshToggleLabel();
        }

        private void BuildToggleButton()
        {
            var canvasGo = new GameObject("DATN_LanguageCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760; // tren moi UI khac

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;

            var btnGo = new GameObject("LangButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TMPro.TextMeshProUGUI));
            btnGo.transform.SetParent(canvasGo.transform, false);
            var rect = btnGo.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -20f);
            rect.sizeDelta = new Vector2(130f, 54f);

            var image = btnGo.GetComponent<Image>();
            image.color = new Color(0.1f, 0.16f, 0.12f, 0.92f);

            _toggleLabel = btnGo.GetComponent<TMPro.TextMeshProUGUI>();
            _toggleLabel.alignment = TMPro.TextAlignmentOptions.Center;
            _toggleLabel.fontSize = 30f;
            _toggleLabel.fontStyle = TMPro.FontStyles.Bold;
            _toggleLabel.color = new Color(0.91f, 0.89f, 0.82f);
            _toggleLabel.raycastTarget = true;

            _toggleButton = btnGo.GetComponent<Button>();
            _toggleButton.onClick.AddListener(Toggle);
        }

        public void Toggle()
        {
            if (LocalizationSettings.SelectedLocale == null || _vi == null || _en == null) return;
            var target = CurrentCode == "vi" ? _en : _vi;
            if (target == null) return;
            PlayerPrefs.SetString(PrefsKey, target.Identifier.Code);
            PlayerPrefs.Save();
            StartCoroutine(SetLocale(target));
        }

        private IEnumerator SetLocale(Locale target)
        {
            // Localization 1.5.x: gan qua property — su kien ap dung khap UI
            LocalizationSettings.SelectedLocale = target;
            yield return null;
            RefreshToggleLabel();
        }

        private void RefreshToggleLabel()
        {
            if (_toggleLabel != null)
            {
                // Hien thi ngon ngu SE CHUYEN TOI khi bam
                _toggleLabel.text = CurrentCode == "vi" ? "EN" : "VI";
            }
        }

        /// <summary>Lay chuoi da dich tu bang Shadowgrove (dung cho UI runtime).</summary>
        public static string Get(string key)
        {
            var op = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(Table, key);
            if (op.IsDone)
            {
                var result = op.Result;
                if (!string.IsNullOrEmpty(result) && result != key) return result;
                // fallback: thu bang MainMenu (UI kit)
                var op2 = LocalizationSettings.StringDatabase.GetLocalizedStringAsync("MainMenu", key);
                return op2.IsDone && !string.IsNullOrEmpty(op2.Result) ? op2.Result : key;
            }
            return key;
        }

        public static string GetFormatted(string key, params object[] args)
        {
            var op = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(Table, key, args);
            return op.IsDone && !string.IsNullOrEmpty(op.Result) ? op.Result : key;
        }
    }
}
