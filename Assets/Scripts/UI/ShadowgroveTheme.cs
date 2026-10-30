using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace DATN.UI
{
    /// <summary>
    ///     Skin Shadowgrove v2 — ve lai toan bo giao dien cua kit luc runtime.
    ///     Rut kinh nghiem phien truoc (crash native + NRE preload):
    ///     - KHONG dung bat ky API Unity Localization o day (doc ngon ngu tu PlayerPrefs).
    ///     - KHONG doi font TMP (fallback ArialVN trong TMP Settings da phu dau Viet).
    ///     - Khong sua prefab — chi restyle Image/TMP da tai runtime.
    ///     Phong cach (Art Bible): nen toi xanh ruong, vien vang dat, chu trang xuong.
    /// </summary>
    public class ShadowgroveTheme : MonoBehaviour
    {
        private static readonly Color TextColor = new(0.91f, 0.89f, 0.82f);
        private static readonly Color GoldColor = new(0.77f, 0.64f, 0.35f);
        private static readonly Color BtnColor = new(0.11f, 0.19f, 0.14f, 0.97f);
        private static readonly Color PanelColor = new(0.07f, 0.12f, 0.09f, 0.94f);

        // Nhan English cung con sot tren panel kit → tieng Viet (khi dang chon VI)
        // Bao gom ca cac key trong bang MainMenu (backup khi table lookup cham)
        private static readonly Dictionary<string, string> ViFix = new()
        {
            { "Close", "Đóng" }, { "Confirm", "Xác nhận" }, { "Character Selection", "Chọn Nhân Vật" },
            { "Level Selection", "Chọn Màn" }, { "Level Select", "Chọn Màn" }, { "Level", "Màn" },
            { "Name", "Tên" }, { "Description", "Mô tả" },
            { "Back", "Quay lại" }, { "Save", "Lưu" }, { "Start", "BẮT ĐẦU" }, { "QUIT", "THOÁT" },
            { "OPTIONS", "TÙY CHỌN" }, { "POWER UP", "NÂNG CẤP" },
            { "MasterVolume", "Âm lượng chung" }, { "MusicVolume", "Âm lượng nhạc" },
            { "SFXVolume", "Âm lượng hiệu ứng" }, { "Resolution", "Độ phân giải" },
            { "FullScreen", "Toàn màn hình" }, { "ShowDamageText", "Hiện số sát thương" },
            { "Language", "Ngôn ngữ" },
        };

        private Sprite _buttonSprite;
        private float _nextScan;
        private int _scanCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<ShadowgroveTheme>(FindObjectsInactive.Include) == null)
            {
                var go = new GameObject("DATN_ShadowgroveTheme");
                DontDestroyOnLoad(go);
                go.AddComponent<ShadowgroveTheme>();
            }
        }

        private void Awake()
        {
            _buttonSprite = Resources.Load<Sprite>("SGUI/SG_UI_Button");
            _nextScan = Time.unscaledTime + 0.2f;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _nextScan = Time.unscaledTime + 0.2f;
        }

        private void Update()
        {
            // Selection panels are instantiated/enabled between regular scans. Pull the
            // next pass forward immediately so the kit's red defaults and localization
            // diagnostics are never visible for a full scan interval.
            if (IsSelectionPanelOpen() && _nextScan > Time.unscaledTime + 0.2f)
                _nextScan = Time.unscaledTime;
            if (Time.unscaledTime < _nextScan) return;
            bool selectionOpen = IsSelectionPanelOpen();
            _nextScan = Time.unscaledTime + (selectionOpen ? 0.2f : 2.5f);
            try
            {
                ApplyAll();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[Theme] Bo qua loi restyle: " + ex.Message);
            }
        }

        private static bool IsVi
        {
            get
            {
                string saved = PlayerPrefs.GetString(DATN.Localization.LanguageManager.PrefsKey, "");
                return saved == "vi"
                       || (saved.Length == 0 && Application.systemLanguage == SystemLanguage.Vietnamese);
            }
        }

        private void ApplyAll()
        {
            bool vi = IsVi;
            _scanCount++;

            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (canvas.name.StartsWith("DATN_")) continue; // UI cua minh da style san

                foreach (var tmp in canvas.GetComponentsInChildren<TMP_Text>(false))
                {
                    try { StyleText(tmp, vi); } catch { /* bo qua 1 label */ }
                }

                foreach (var img in canvas.GetComponentsInChildren<Image>(false))
                {
                    try { StyleImage(img); } catch { /* bo qua 1 image */ }
                }
            }

            ApplyMainMenuPanelChrome(vi);
            StyleSelectionItems();

            // Chi sap lai luoi man sau khi da quet duoc lan thu 2 (UI da on dinh)
            if (_scanCount > 1) ArrangeLevelGrid();
        }

        private static bool IsSelectionPanelOpen()
        {
            var charPanel = FindFirstObjectByType<RGame.RoguelikeKit.CharacterSelectPanel>(FindObjectsInactive.Include);
            if (charPanel != null && charPanel.gameObject.activeInHierarchy) return true;
            var levelPanel = FindFirstObjectByType<RGame.RoguelikeKit.LevelSelectPanel>(FindObjectsInactive.Include);
            return levelPanel != null && levelPanel.gameObject.activeInHierarchy;
        }

        private static void StyleSelectionItems()
        {
            foreach (var item in FindObjectsByType<RGame.RoguelikeKit.LevelItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (item.BackgroundImage != null)
                    item.BackgroundImage.color = item.IsSelected ? GoldColor : BtnColor;
            }
            foreach (var item in FindObjectsByType<RGame.RoguelikeKit.CharacterItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (item.BackgroundImage != null)
                    item.BackgroundImage.color = item.IsSelected ? GoldColor : BtnColor;

                var grid = item.transform.parent.GetComponent<GridLayoutGroup>();
                if (grid != null)
                {
                    grid.cellSize = new Vector2(220f, 220f);
                    grid.spacing = new Vector2(24f, 24f);
                    grid.childAlignment = TextAnchor.MiddleCenter;
                }
            }
        }

        private static void StyleText(TMP_Text tmp, bool vi)
        {
            if (tmp == null) return;
            var goName = tmp.gameObject.name;
            if (goName.StartsWith("DATN_")) return;

            if (vi)
            {
                string key = tmp.text == null ? "" : tmp.text.Trim();
                if (key.Length > 0 && key.Length < 30 && ViFix.TryGetValue(key, out var viText)
                    && tmp.text != viText)
                {
                    tmp.text = viText;
                }
            }


            // Unity Localization prints this diagnostic into the label when a table entry
            // is temporarily unavailable during Addressables initialization. Never expose
            // that implementation detail to the player; use the local, deterministic fallback.
            string current = tmp.text == null ? "" : tmp.text.Trim();
            if (current.StartsWith("No translation found for '", System.StringComparison.Ordinal))
            {
                int start = current.IndexOf('\'') + 1;
                int end = current.IndexOf('\'', start);
                if (start > 0 && end > start)
                    tmp.text = DATN.Localization.LanguageManager.Get(current.Substring(start, end - start));
            }

            // Kit labels use dark red/brown colors which fail contrast on the new dark
            // background. Normalize all kit text to bone-white; dedicated titles use gold.
            tmp.color = TextColor;
        }

        private static void ApplyMainMenuPanelChrome(bool vi)
        {
            var characterPanel = FindFirstObjectByType<RGame.RoguelikeKit.CharacterSelectPanel>(FindObjectsInactive.Include);
            if (characterPanel != null && characterPanel.gameObject.activeInHierarchy)
            {
                EnsurePanelTitle(characterPanel.transform, "CharacterSelection");
                FixPanelButtons(characterPanel.transform);
            }

            var levelPanel = FindFirstObjectByType<RGame.RoguelikeKit.LevelSelectPanel>(FindObjectsInactive.Include);
            if (levelPanel != null && levelPanel.gameObject.activeInHierarchy)
            {
                EnsurePanelTitle(levelPanel.transform, "LevelSelection");
                FixPanelButtons(levelPanel.transform);
            }
        }

        private static void EnsurePanelTitle(Transform panel, string key)
        {
            var titleTransform = panel.Find("DATN_Title");
            TMPro.TextMeshProUGUI title;
            if (titleTransform == null)
            {
                var go = new GameObject("DATN_Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
                go.transform.SetParent(panel, false);
                title = go.GetComponent<TMPro.TextMeshProUGUI>();
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -28f);
                rect.sizeDelta = new Vector2(620f, 74f);
                title.alignment = TextAlignmentOptions.Center;
                title.fontSize = 42f;
                title.fontStyle = FontStyles.Bold;
                title.raycastTarget = false;
            }
            else
            {
                title = titleTransform.GetComponent<TMPro.TextMeshProUGUI>();
            }

            if (title != null)
            {
                title.text = DATN.Localization.LanguageManager.Get(key);
                title.color = GoldColor;
            }
        }

        private static void FixPanelButtons(Transform panel)
        {
            foreach (var button in panel.GetComponentsInChildren<Button>(true))
            {
                string trimmed = button.gameObject.name.Trim();
                string key = trimmed == "ButtonBack" ? "Back" : trimmed == "ButtonConfirm" ? "Confirm" : null;
                if (key == null) continue;

                var rect = button.GetComponent<RectTransform>();
                bool isBack = key == "Back";
                rect.anchorMin = rect.anchorMax = isBack ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
                rect.pivot = isBack ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
                rect.anchoredPosition = isBack ? new Vector2(44f, -34f) : new Vector2(-44f, -34f);
                rect.sizeDelta = new Vector2(250f, 72f);

                foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                {
                    foreach (var behaviour in label.GetComponents<MonoBehaviour>())
                    {
                        if (behaviour != null && behaviour.GetType().FullName == "UnityEngine.Localization.Components.LocalizeStringEvent")
                            behaviour.enabled = false;
                    }
                    label.text = DATN.Localization.LanguageManager.Get(key);
                    label.color = TextColor;
                    label.fontSize = 30f;
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 20f;
                    label.fontSizeMax = 30f;
                    label.alignment = TextAlignmentOptions.Center;
                }
            }
        }

        private static void StyleImage(Image img)
        {
            if (img == null) return;
            var go = img.gameObject;
            if (go.name.StartsWith("DATN_") || go.name == "BG") return;
            if (go.GetComponent<UiCoverImage>() != null) return;

            var rect = img.rectTransform.rect;
            var btn = img.GetComponent<Button>();

            if (btn != null)
            {
                // Nut: sprite moi + nen toi. DUNG Sliced (9-slice border > nho nho
                // gay crash native renderer tren build) — dang Simple an toan.
                if (rect.width > 64f && rect.height > 30f && _buttonSpriteCache != null)
                {
                    img.sprite = _buttonSpriteCache;
                    img.type = Image.Type.Simple;
                    img.color = BtnColor;
                    var colors = btn.colors;
                    colors.normalColor = Color.white;
                    colors.highlightedColor = new Color(1.18f, 1.32f, 1.22f, 1f);
                    colors.pressedColor = new Color(0.7f, 0.82f, 0.74f, 1f);
                    colors.selectedColor = Color.white;
                    colors.fadeDuration = 0.08f;
                    btn.colors = colors;
                }
                return;
            }

            // Do not recolor arbitrary large Images: gameplay contains full-screen
            // fade/effect graphics which would otherwise become an opaque dark veil.
        }

        private static Sprite _buttonSpriteCache;

        /// <summary>Sap muc chon man thanh 2 cot khong tran man hinh.</summary>
        private static void ArrangeLevelGrid()
        {
            var panel = FindFirstObjectByType<RGame.RoguelikeKit.LevelSelectPanel>(FindObjectsInactive.Include);
            if (panel == null) return;
            var items = panel.GetComponentsInChildren<RGame.RoguelikeKit.LevelItem>(false);
            if (items.Length > 0 && items[0].transform.parent.GetComponent<VerticalLayoutGroup>() != null)
                return;
            for (int i = 0; i < items.Length; i++)
            {
                var r = items[i].GetComponent<RectTransform>();
                if (r == null) continue;
                int col = i % 2, row = i / 2;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
                r.pivot = new Vector2(0.5f, 1f);
                r.anchoredPosition = new Vector2(col == 0 ? -250f : 250f, -34f - row * 215f);
            }
        }

        private void Start()
        {
            _buttonSpriteCache = _buttonSprite;
        }
    }
}
