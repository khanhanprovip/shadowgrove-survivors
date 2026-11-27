using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
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
        public static string PrefsKey { get; } = ResolvePrefsKey();

        public static LanguageManager Instance { get; private set; }

        private Locale _vi;
        private Locale _en;
        private Button _toggleButton;
        private TMPro.TextMeshProUGUI _toggleLabel;
        private int _buttonAttempts;

        private static string ResolvePrefsKey()
        {
            string profile = Environment.GetEnvironmentVariable("DATN_TEST_PROFILE_DIR");
            return string.IsNullOrEmpty(profile) ? "DATN_LocaleCode" : "DATN_LocaleCode_" + Path.GetFileName(profile);
        }

        public string CurrentCode => GetCurrentCode();

        public static string GetCurrentCode()
        {
            string saved = PlayerPrefs.GetString(PrefsKey, "");
            if (saved == "vi" || saved == "en") return saved;
            return Application.systemLanguage == SystemLanguage.Vietnamese ? "vi" : "en";
        }

        // ---------------- Tu dien chuoi noi tai: key → [en, vi] ----------------
        private static readonly Dictionary<string, string[]> Strings = new()
        {
            { "Pause", new[] { "PAUSE", "TẠM DỪNG" } },
            { "Resume", new[] { "RESUME", "TIẾP TỤC" } },
            { "Restart", new[] { "RESTART", "CHƠI LẠI" } },
            { "BackToMenu", new[] { "MAIN MENU", "VỀ MENU" } },
            { "Close", new[] { "CLOSE", "ĐÓNG" } },
            { "Confirm", new[] { "CONFIRM", "XÁC NHẬN" } },
            { "Back", new[] { "BACK", "QUAY LẠI" } },
            { "Save", new[] { "SAVE", "LƯU" } },
            { "CharacterSelection", new[] { "CHARACTER SELECTION", "CHỌN NHÂN VẬT" } },
            { "LevelSelection", new[] { "LEVEL SELECTION", "CHỌN MÀN" } },
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
            { "Objective_BossRush", new[] { "Boss Rush", "Liên chiến boss" } },
            { "Objective_BossProgress", new[] { "Boss Rush: {0}/{1}", "Liên chiến boss: {0}/{1}" } },
            { "Objective_BossRushCount", new[] { "Boss Rush · Defeated: {0}", "Liên chiến boss · Đã hạ: {0}" } },
            { "Objective_BossCountdown", new[] { "Next boss in: {0}s", "Boss tiếp theo sau: {0}s" } },
            { "Objective_DefeatBoss", new[] { "Defeat the final boss", "Hạ boss cuối màn" } },
            { "Objective_EnterArena", new[] { "Return to the central boss arena", "Đến sân boss ở trung tâm" } },
            { "CardLevel", new[] { "Lv. {0} → {1} / {2}", "Cấp {0} → {1} / {2}" } },
            { "CardNew", new[] { "NEW SKILL!", "KỸ NĂNG MỚI!" } },
            { "Kills", new[] { "Kills: {0}", "Hạ: {0}" } },
            { "TimeSurvived", new[] { "Time: {0}", "Thời gian: {0}" } },
            { "LevelReached", new[] { "Level: {0}", "Cấp độ: {0}" } },
            { "Skill_BothSideSkill", new[] { "Thorn Fan", "Quạt Gai" } },
            { "SkillDesc_BothSideSkill", new[] { "Fires thorn blades to both sides.", "Phóng lưỡi gai sang hai bên." } },
            { "Skill_BouncingSkill", new[] { "Gloom Spore", "Bào Tử U Ám" } },
            { "SkillDesc_BouncingSkill", new[] { "Spores bounce off screen edges and damage enemies on contact.", "Bào tử nảy ở mép màn hình, gây sát thương khi chạm quái." } },
            { "Skill_ChainSkill", new[] { "Root Lash", "Roi Rễ Cây" } },
            { "SkillDesc_ChainSkill", new[] { "Root whip lashes through nearby foes.", "Roi rễ quật qua quái gần đó." } },
            { "Skill_CircularAttackSkill", new[] { "Warden Halo", "Vầng Vệ Binh" } },
            { "SkillDesc_CircularAttackSkill", new[] { "Halo strikes all enemies around you.", "Vầng sáng đánh mọi quái xung quanh." } },
            { "Skill_CircularMotionSkill", new[] { "Thorn Orbit", "Gai Xoay" } },
            { "SkillDesc_CircularMotionSkill", new[] { "Thorns orbit and shred on contact.", "Lưỡi gai xoay quanh, cắt quái khi chạm." } },
            { "Skill_FourCornerSkill", new[] { "Cross Bolt", "Cắt Chéo" } },
            { "SkillDesc_FourCornerSkill", new[] { "Fires piercing bolts along four diagonals.", "Bắn đạn xuyên quái theo bốn hướng chéo." } },
            { "Skill_InsideScopeSkill", new[] { "Spirit Bloom", "Hoa Linh Hồn" } },
            { "SkillDesc_InsideScopeSkill", new[] { "A field follows you and deals four damage pulses.", "Vùng phép đi theo bạn, gây bốn nhịp sát thương." } },
            { "Skill_RandomSkill", new[] { "Wild Magic", "Ma Thuật Loạn" } },
            { "SkillDesc_RandomSkill", new[] { "Instantly damages random nearby enemies.", "Gây sát thương tức thì lên các quái gần ngẫu nhiên." } },
            { "Skill_ToNearestSkill", new[] { "Eclipse Bolt", "Cầu Nhật Thực" } },
            { "SkillDesc_ToNearestSkill", new[] { "Aims a bolt at the nearest enemy.", "Ngắm bắn vào quái gần nhất." } },
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

        private static readonly Dictionary<string, string[]> LevelDescriptions = new()
        {
            { "TrialLevel", new[] { "Trial Forest — Survive 90 seconds", "Rừng Thử Thách — Sống sót 90 giây" } },
            { "LV2_MoonlitRuins", new[] { "Moonlit Ruins — Defeat 30 enemies", "Tàn Tích Ánh Trăng — Hạ 30 quái" } },
            { "LV3_SunkenMarsh", new[] { "Sunken Marsh — 120s, then defeat Thorn Regent", "Đầm Lầy Chìm — 120 giây, rồi hạ Cổ Vương Gai" } },
            { "LV4_AshenQuarry", new[] { "Ashen Quarry — Defeat 30 enemies", "Mỏ Đá Tro Tàn — Hạ 30 quái" } },
            { "LV5_FrostHollow", new[] { "Frost Hollow — 120s, then defeat Frost Mourner", "Khe Băng Giá — 120 giây, rồi hạ Ai Nữ Băng Sương" } },
            { "LV6_CursedVillage", new[] { "Cursed Village — Survive 120 seconds", "Ngôi Làng Bị Nguyền — Sống sót 120 giây" } },
            { "LV7_ClockworkCitadel", new[] { "Clockwork Citadel — Defeat 30 enemies", "Thành Trì Cơ Giới — Hạ 30 quái" } },
            { "LV8_CrimsonCathedral", new[] { "Crimson Cathedral — 120s, then defeat Crimson Harvester", "Thánh Đường Đỏ Thẫm — 120 giây, rồi hạ Huyết Nguyệt Đao Phủ" } },
            { "LV9_EndlessAbyss", new[] { "Endless Abyss — Endless", "Vực Thẳm Vô Tận — Vô tận" } },
            { "LV10_EclipseThrone", new[] { "Eclipse Throne — Boss Rush; bosses only", "Ngai Nhật Thực — Liên chiến boss; chỉ đánh boss" } },
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

            // A GameObject may only contain one Graphic. Keep Image/Button on the
            // button object and put the TMP label on a stretched child.
            var btnGo = new GameObject("LangButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(canvasGo.transform, false);
            var rect = btnGo.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            // Keep the language switch clear of the selection panels' Confirm button.
            rect.anchoredPosition = new Vector2(-24f, -104f);
            rect.sizeDelta = new Vector2(130f, 54f);

            var image = btnGo.GetComponent<Image>();
            image.color = new Color(0.1f, 0.16f, 0.12f, 0.92f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            _toggleLabel = labelGo.GetComponent<TMPro.TextMeshProUGUI>();
            _toggleLabel.alignment = TMPro.TextAlignmentOptions.Center;
            _toggleLabel.fontSize = 30f;
            _toggleLabel.fontStyle = TMPro.FontStyles.Bold;
            _toggleLabel.color = new Color(0.91f, 0.89f, 0.82f);
            _toggleLabel.raycastTarget = false;

            _toggleButton = btnGo.GetComponent<Button>();
            _toggleButton.onClick.AddListener(Toggle);
            RefreshToggleLabel();
        }

        public void Toggle()
        {
            SetLanguage(CurrentCode == "vi" ? _en : _vi);
        }

        public void SetLanguage(Locale target)
        {
            if (target == null) return;
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
                bool vi = GetCurrentCode() == "vi";
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

        public static string GetLevelDescription(string levelAssetName, string fallback)
        {
            if (!LevelDescriptions.TryGetValue(levelAssetName, out var pair)) return fallback;
            bool vi = GetCurrentCode() == "vi";
            return vi ? pair[1] : pair[0];
        }
    }
}
