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
        private static readonly Dictionary<string, string> ViFix = new()
        {
            { "Close", "Đóng" }, { "Confirm", "Xác nhận" }, { "Character Selection", "Chọn Nhân Vật" },
            { "Level Selection", "Chọn Màn" }, { "Level Select", "Chọn Màn" }, { "Level", "Màn" },
            { "Name", "Tên" }, { "Description", "Mô tả" }, { "Level:1", "Màn 1" },
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
            // Tre khoi dong 2 giay — cho UI/menu nap xong roi moi lan quet dau
            _nextScan = Time.unscaledTime + 2f;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _nextScan = Time.unscaledTime + 0.8f; // quet lai som khi vao man moi
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + 2.5f;
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

            // Chi sap lai luoi man sau khi da quet duoc lan thu 2 (UI da on dinh)
            if (_scanCount > 1) ArrangeLevelGrid();
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

            // Chu toi (den/xam dam) → mau trang xuong de doc tren nen moi
            var c = tmp.color;
            if (c.r < 0.4f && c.g < 0.4f && c.b < 0.4f)
            {
                tmp.color = TextColor;
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

            // Khung/nen lon cua panel: bo sprite cu (khung vang do cua kit) → nen toi
            if (rect.width > 360f && rect.height > 240f)
            {
                img.sprite = null;
                img.type = Image.Type.Simple;
                img.color = PanelColor;
            }
        }

        private static Sprite _buttonSpriteCache;

        /// <summary>Sap muc chon man thanh 2 cot khong tran man hinh.</summary>
        private static void ArrangeLevelGrid()
        {
            var panel = FindFirstObjectByType<RGame.RoguelikeKit.LevelSelectPanel>(FindObjectsInactive.Include);
            if (panel == null) return;
            var items = panel.GetComponentsInChildren<RGame.RoguelikeKit.LevelItem>(false);
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
