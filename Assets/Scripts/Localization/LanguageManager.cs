using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace DATN.Localization
{
    /// <summary>
    ///     Quan ly song ngu Viet/Anh. Phien ban v3 — rut kinh nghiem crash native:
    ///     khong goi API lay chuoi bang (GetLocalizedStringAsync) luc runtime vi no
    ///     kich hoat PreloadLocaleOperation gay NRE/crash tren ban build. Chuoi cua
    ///     do an doc TRUC TIEP tu tu dien noi tai; menu kit van dung
    ///     LocalizeStringEvent san co tren prefab.
    /// </summary>
    public class LanguageManager : MonoBehaviour
    {
        public const string PrefsKey = "DATN_LocaleCode";

        public static LanguageManager Instance { get; private set; }

        private Locale _vi;
        private Locale _en;
        private Button _toggleButton;
        private TMPro.TextMeshProUGUI _toggleLabel;
        private int _buttonAttempts;

        public string CurrentCode
        {
            get
            {
                string saved = PlayerPrefs.GetString(PrefsKey, "");
                if (saved == "vi" || saved == "en") return saved;
                return Application.systemLanguage == SystemLanguage.Vietnamese ? "vi" : "en";
            }
        }

        // ---------------- Tu dien chuoi noi tai: key → [en, vi] ----------------
        private static readonly Dictionary<string, string[]> Strings = new()
        {
            { "Pause", new[] { "PAUSE", "TẠM DỪNG" } },
            { "Resume", new[] { "RESUME", "TIẾP TỤC" } },
            { "Restart", new[] { "RESTART", "CHƠI LẠI" } },
            { "BackToMenu", new[] { "MAIN MENU", "VỀ MENU" } },
            { "Victory", new[] { "VICTORY!", "CHIẾN THẮNG!" } },
            { "Defeat", new[] { "DEFEAT", "THẤT BẠI" } },
            { "CharName_Rowan", new[] { "Rowan", "Rowan" } },
            { "CharDesc_Rowan", new[] { "Warden of Thorns. Balanced fighter with a wide-swing thorn scythe.", "Vệ Binh Gai. Chiến binh cân bằng với lưỡi liềm gai quét rộng." } },
            { "CharName_Lyra", new[] { "Lyra", "Lyra" } },
            { "CharDesc_Lyra", new[] { "Eclipse Arcanist. Long-range mage firing eclipse bolts.", "Pháp Sư Nhật Thực. Đánh xa bằng cầu ma thuật nhật thực." } },
            { "CharName_Kael", new[] { "Kael", "Kael" } },
            { "CharDesc_Kael", new[] { "Ember Hunter. Fast hunter with ember crossbow.", "Thợ Săn Tàn Lửa. Di chuyển nhanh với nỏ than hồng." } },
            { "Objective_Survive", new[] { "Survive: {0}s", "Sống sót: {0}s" } },
            { "Objective_Kills", new[] { "Kills: {0}/{1}", "Hạ: {0}/{1}" } },
            { "Objective_Endless", new[] { "Endless", "Vô tận" } },
            { "CardLevel", new[] { "Lv. {0} → {1} / {2}", "Cấp {0} → {1} / {2}" } },
            { "CardNew", new[] { "NEW SKILL!", "KỸ NĂNG MỚI!" } },
            { "Kills", new[] { "Kills: {0}", "Hạ: {0}" } },
            { "TimeSurvived", new[] { "Time: {0}", "Thời gian: {0}" } },
            { "LevelReached", new[] { "Level: {0}", "Cấp độ: {0}" } },
            { "Skill_BothSideSkill", new[] { "Thorn Fan", "Quạt Gai" } },
            { "SkillDesc_BothSideSkill", new[] { "Fires thorn blades to both sides.", "Phóng lưỡi gai sang hai bên." } },
            { "Skill_BouncingSkill", new[] { "Gloom Spore", "Bào Tử U Ám" } },
            { "SkillDesc_BouncingSkill", new[] { "Throws a spore that bounces between enemies.", "Ném bào tử nảy giữa các quái." } },
            { "Skill_ChainSkill", new[] { "Root Lash", "Roi Rễ Cây" } },
            { "SkillDesc_ChainSkill", new[] { "Root whip lashes through nearby foes.", "Roi rễ quật qua quái gần đó." } },
            { "Skill_CircularAttackSkill", new[] { "Warden Halo", "Vầng Vệ Binh" } },
            { "SkillDesc_CircularAttackSkill", new[] { "Halo strikes all enemies around you.", "Vầng sáng đánh mọi quái xung quanh." } },
            { "Skill_CircularMotionSkill", new[] { "Thorn Orbit", "Gai Xoay" } },
            { "SkillDesc_CircularMotionSkill", new[] { "Thorns orbit and shred on contact.", "Lưỡi gai xoay quanh, cắt quái khi chạm." } },
            { "Skill_FourCornerSkill", new[] { "Cross Bolt", "Cắt Chéo" } },
            { "SkillDesc_FourCornerSkill", new[] { "Bolts fire in four directions.", "Bắn đạn bốn hướng." } },
            { "Skill_InsideScopeSkill", new[] { "Spirit Bloom", "Hoa Linh Hồn" } },
            { "SkillDesc_InsideScopeSkill", new[] { "Bursts bloom around you.", "Hoa nổ nở quanh bạn." } },
            { "Skill_RandomSkill", new[] { "Wild Magic", "Ma Thuật Loạn" } },
            { "SkillDesc_RandomSkill", new[] { "Unpredictable bolts seek enemies.", "Đạn loạn tự tìm quái." } },
            { "Skill_ToNearestSkill", new[] { "Eclipse Bolt", "Cầu Nhật Thực" } },
            { "SkillDesc_ToNearestSkill", new[] { "Homing bolt strikes the nearest foe.", "Đạn tự tìm đánh quái gần nhất." } },
            { "Skill_MoneyBag", new[] { "Money Bag", "Túi Vàng" } },
            { "SkillDesc_MoneyBag", new[] { "Contains coins. Instant wealth!", "Chứa vàng — của cải tức thì!" } },
            { "SkillAttr_Might", new[] { "Might", "Sức mạnh" } },
            { "SkillAttr_Armor", new[] { "Armor", "Giáp" } },
            { "SkillAttr_HPMax", new[] { "Max HP", "Máu tối đa" } },
            { "SkillAttr_Recovery", new[] { "Recovery", "Hồi máu" } },
            { "SkillAttr_Cooldown", new[] { "Cooldown", "Hồi chiêu" } },
            { "SkillAttr_Area", new[] { "Area", "Phạm vi" } },
            { "SkillAttr_SkillSpeed", new[] { "Skill Speed", "Tốc kỹ năng" } },
            { "SkillAttr_Duration", new[] { "Duration", "Thời gian" } },
            { "SkillAttr_Amount", new[] { "Projectiles", "Số đạn" } },
            { "SkillAttr_MoveSpeed", new[] { "Move Speed", "Tốc chạy" } },
            { "SkillAttr_Magnet", new[] { "Magnet", "Hút EXP" } },
            { "SkillAttr_Growth", new[] { "Growth", "Tăng trưởng" } },
            { "SkillAttr_Greed", new[] { "Greed", "Tham lam" } },
            { "SkillAttr_Curse", new[] { "Curse", "Nguyền rủa" } },
            { "Stat_Damage", new[] { "Damage", "Sát thương" } },
            { "Stat_Amount", new[] { "Projectiles", "Số đạn" } },
            { "Stat_Velocity", new[] { "Speed", "Tốc độ" } },
            { "Stat_Area", new[] { "Area", "Diện tích" } },
            { "Stat_Money", new[] { "Coins", "Vàng" } },
        };

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

        private void Update()
        {
            // Tao nut o Update thay vi Awake: TMP can tai nguyen san trong Editor,
            // that bai thi thu lai toi da 3 lan, khong bao gio lam game loi.
            if (_toggleButton == null && _buttonAttempts < 3)
            {
                _buttonAttempts++;
                try { BuildToggleButton(); }
                catch (System.Exception ex) { Debug.LogWarning("[Lang] Thu tao nut that bai: " + ex.Message); }
            }
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

            string targetCode = CurrentCode;
            var target = targetCode == "vi" ? _vi : _en;
            if (target != null && LocalizationSettings.SelectedLocale != target)
            {
                // Menu kit tu cap nhat nhan qua LocalizeStringEvent
                LocalizationSettings.SelectedLocale = target;
                yield return null;
            }
            RefreshToggleLabel();
        }

        private void BuildToggleButton()
        {
            var canvasGo = new GameObject("DATN_LanguageCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;

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
            RefreshToggleLabel();
        }

        public void Toggle()
        {
            if (_vi == null || _en == null) return;
            var target = CurrentCode == "vi" ? _en : _vi;
            PlayerPrefs.SetString(PrefsKey, target.Identifier.Code);
            PlayerPrefs.Save();
            StartCoroutine(SetLocale(target));
        }

        private IEnumerator SetLocale(Locale target)
        {
            LocalizationSettings.SelectedLocale = target;
            yield return null;
            RefreshToggleLabel();
        }

        private void RefreshToggleLabel()
        {
            if (_toggleLabel != null)
            {
                _toggleLabel.text = CurrentCode == "vi" ? "EN" : "VI";
            }
        }

        /// <summary>Lay chuoi da dich tu tu dien noi tai — khong qua Unity Localization.</summary>
        public static string Get(string key)
        {
            if (Strings.TryGetValue(key, out var pair))
            {
                bool vi = Instance != null && Instance.CurrentCode == "vi";
                return vi ? pair[1] : pair[0];
            }
            return key;
        }

        public static string GetFormatted(string key, params object[] args)
        {
            string format = Get(key);
            try
            {
                return string.Format(format, args);
            }
            catch
            {
                return format;
            }
        }
    }
}
