using System;
using DATN.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RGame.RoguelikeKit
{
    /// <summary>
    ///     The nang cap ky nang (Moc 3 - giai doan hoan thien):
    ///     - Ten + mo ta lay tu bang song ngu Shadowgrove theo Key ky nang.
    ///     - Dong cap: "Cap X → Y / toi da" hoac "MAX", nhan KY NANG MOI khi cap 0.
    ///     - Chi so truoc → sau lay truc tiep tu SkillDataSO (UpgradeAttribute /
    ///       UpgradeAttributeModValue) — khong mo ta viet cung lech voi code.
    /// </summary>
    public class UpgradeSkillItem : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private TextMeshProUGUI _description;
        [SerializeField] private TextMeshProUGUI _level;
        private SkillDataSO _skillData;
        private string _language;
        private TMP_Text _newBadge;
        private TMP_Text _category;

        private void Update()
        {
            if (_skillData != null && _language != LanguageManager.GetCurrentCode())
                BuildCard(_skillData);
        }

        public UnityAction<SkillDataSO> UpgradeSkillAction;
        private void Awake()
        {
            _description.rectTransform.anchorMin = new Vector2(0.075f, 0.19f);
            _description.rectTransform.anchorMax = new Vector2(0.925f, 0.60f);
            _description.rectTransform.offsetMin = Vector2.zero;
            _description.rectTransform.offsetMax = Vector2.zero;
            _description.enableAutoSizing = true;
            _description.fontSizeMin = 22f;
            _description.fontSizeMax = 26f;
            _description.alignment = TextAlignmentOptions.TopLeft;
            _description.overflowMode = TextOverflowModes.Ellipsis;
            _name.rectTransform.anchorMin = new Vector2(0.06f, 0.70f);
            _name.rectTransform.anchorMax = new Vector2(0.94f, 0.80f);
            _name.rectTransform.offsetMin = Vector2.zero;
            _name.rectTransform.offsetMax = Vector2.zero;
            _name.enableAutoSizing = true;
            _name.fontSizeMin = 22f;
            _name.fontSizeMax = 32f;
            _name.fontStyle = FontStyles.Bold;
            _name.alignment = TextAlignmentOptions.Center;
            _level.rectTransform.anchorMin = new Vector2(0.06f, 0.625f);
            _level.rectTransform.anchorMax = new Vector2(0.94f, 0.68f);
            _level.rectTransform.offsetMin = Vector2.zero;
            _level.rectTransform.offsetMax = Vector2.zero;
            _level.enableAutoSizing = true;
            _level.fontSizeMin = 22f;
            _level.fontSizeMax = 26f;
            _level.color = DATN.UI.ForestUI.Gold;
            _level.alignment = TextAlignmentOptions.Center;
            _icon.rectTransform.anchorMin = _icon.rectTransform.anchorMax = new Vector2(0.5f, 0.87f);
            _icon.rectTransform.anchoredPosition = Vector2.zero;
            _icon.rectTransform.sizeDelta = new Vector2(88f, 88f);
            _icon.preserveAspect = true;
            _newBadge = DATN.UI.ForestUI.Label(transform, "Jade_NewBadge", "", "", 19f,
                .42f, .955f, .94f, .99f, TextAlignmentOptions.MidlineRight, DATN.UI.ForestUI.Gold);
            _category = DATN.UI.ForestUI.Label(transform, "Jade_Category", "", "", 19f,
                .06f, .955f, .41f, .99f, color: DATN.UI.ForestUI.Gold);
            _newBadge.GetComponent<DATN.UI.ForestText>().enabled = false;
            _category.GetComponent<DATN.UI.ForestText>().enabled = false;

        }


        public void SetUI(SkillDataSO skillData)
        {
            _skillData = skillData;

            _icon.sprite = skillData.SkillIcon;
            BuildCard(skillData);
        }

        private void BuildCard(SkillDataSO skillData)
        {
            _language = LanguageManager.GetCurrentCode();
            string key = skillData.Key;
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                foreach (var label in button.GetComponentsInChildren<Text>(true))
                {
                    label.text = _language == "vi" ? "CHỌN" : "SELECT";
                    label.color = new Color(0.95f, 0.94f, 0.86f);
                    label.resizeTextForBestFit = true;
                    label.resizeTextMinSize = 20;
                    label.resizeTextMaxSize = 30;
                }
                foreach (var label in button.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    label.text = _language == "vi" ? "CHỌN" : "SELECT";
                    label.color = new Color(0.95f, 0.94f, 0.86f);
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 20f;
                    label.fontSizeMax = 30f;
                }
            }


            // Ten + nhan (ky nang moi khi cap 0)
            string displayName = LanguageManager.Get(skillData.SkillType == SkillType.AttributeSkill
                ? "SkillAttr_" + skillData.AttributeType : "Skill_" + key);
            if (displayName == "Skill_" + key) displayName = skillData.SkillType == SkillType.AttributeSkill
                ? LanguageManager.Get("SkillAttr_" + skillData.AttributeType) : key; // chua co dich → dung Key goc
            if (skillData.StarterWeapon != StarterWeaponKind.Legacy)
                displayName = StarterWeaponProfile.Name(skillData.StarterWeapon, _language == "vi");
            bool isNew = skillData.Level == 0 && skillData.SkillType != SkillType.MoneyBag;
            _name.text = displayName;
            _newBadge.text = isNew ? LanguageManager.Get("CardNew") : "";
            _category.text = skillData.SkillType == SkillType.AttributeSkill
                ? (_language == "vi" ? "THUỘC TÍNH" : "ATTRIBUTE")
                : skillData.SkillType == SkillType.MoneyBag
                    ? (_language == "vi" ? "PHẦN THƯỞNG" : "REWARD")
                    : (_language == "vi" ? "TẤN CÔNG" : "ATTACK");

            // Mo ta
            string desc = LanguageManager.Get("SkillDesc_" + key);
            if (skillData.StarterWeapon != StarterWeaponKind.Legacy)
                desc = StarterWeaponProfile.Description(skillData.StarterWeapon, _language == "vi");
            _description.text = skillData.SkillType == SkillType.AttributeSkill || desc == "SkillDesc_" + key ? "" : desc;

            // Dong chi tiet theo loai ky nang
            switch (skillData.SkillType)
            {
                case SkillType.AttackSkill:
                    BuildAttackDetail(skillData);
                    break;
                case SkillType.AttributeSkill:
                    BuildAttributeDetail(skillData);
                    break;
                case SkillType.MoneyBag:
                    _level.text = "";
                    _description.text += (_description.text.Length > 0 ? "\n" : "")
                                         + LanguageManager.Get("Stat_Money") + ": +" + skillData.MoneyAmount;
                    break;
            }
        }

        private void BuildAttackDetail(SkillDataSO data)
        {
            int maxLevel = data.UpgradeAttribute.Count + 1;
            _level.text = data.Level >= maxLevel
                ? (_language == "vi" ? "Cấp tối đa " : "MAX Lv. ") + maxLevel
                : LanguageManager.GetFormatted("CardLevel", data.Level, data.Level + 1, maxLevel);

            AttackSkillAttributeMod next = data.Level > 0 && data.Level <= data.UpgradeAttribute.Count
                ? data.UpgradeAttribute[data.Level - 1] : null;
            string detail = _description.text.Length > 0 ? _description.text + "\n" : "";
            detail += StatLine(LanguageManager.Get("Stat_Damage"), data.Damage,
                next != null && next.skillAttributeType == UpgradeSkillAttributeType.DamageAdd
                    ? data.Damage + next.IntValue : data.Damage);
            detail += StatLine(LanguageManager.Get("SkillAttr_Cooldown"), data.CD,
                next != null && next.skillAttributeType == UpgradeSkillAttributeType.CDLessPercent
                    ? data.CD * (1f - next.IntValue * 0.01f) : data.CD, " s");
            if (data.StarterWeapon != StarterWeaponKind.Legacy)
                detail += StatLine(_language == "vi" ? "Tầm đánh" : "Range", data.AttackRange, data.AttackRange);
            if (data.StarterWeapon == StarterWeaponKind.ThornSlash)
            {
                detail += StatLine(_language == "vi" ? "Số mục tiêu" : "Targets", 2 + data.Amount,
                    2 + data.Amount + (next != null && next.skillAttributeType == UpgradeSkillAttributeType.ProjectilesCountAdd ? next.IntValue : 0));
                _description.text = detail.TrimEnd('\n');
                return;
            }
            detail += StatLine(LanguageManager.Get("Stat_Amount"), data.Amount,
                next != null && next.skillAttributeType == UpgradeSkillAttributeType.ProjectilesCountAdd
                    ? data.Amount + next.IntValue : data.Amount);
            detail += StatLine(LanguageManager.Get("Stat_Velocity"), data.Velocity,
                next != null && next.skillAttributeType == UpgradeSkillAttributeType.VelocityAddPercent
                    ? data.Velocity * (1f + next.IntValue * 0.01f) : data.Velocity);
            detail += StatLine(LanguageManager.Get("Stat_Area"), data.Area,
                next != null && next.skillAttributeType == UpgradeSkillAttributeType.AreaAddPercent
                    ? data.Area + next.IntValue : data.Area);
            _description.text = detail.TrimEnd('\n');
        }

        private static string StatLine(string label, float current, float after, string unit = "")
        {
            string line = label + ": " + current.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + unit;
            if (!Mathf.Approximately(current, after))
                line += " <color=#88C9A1>→ " + after.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + unit + "</color>";
            return line + "\n";
        }

        /// <summary>Ky nang thuoc tinh: ten attribute + gia tri tang o cap ke tiep.</summary>
        private void BuildAttributeDetail(SkillDataSO skillData)
        {
            int level = skillData.Level;
            int maxLevel = skillData.UpgradeAttributeModValue.Count;
            _level.text = level >= maxLevel
                ? (_language == "vi" ? "Cấp tối đa " : "MAX Lv. ") + maxLevel
                : LanguageManager.GetFormatted("CardLevel", level, level + 1, maxLevel);

            string attrName = LanguageManager.Get("SkillAttr_" + skillData.AttributeType);
            string detail = _description.text.Length > 0 ? _description.text + "\n" : "";

            if (level < skillData.UpgradeAttributeModValue.Count)
            {
                int mod = skillData.UpgradeAttributeModValue[level];
                detail += attrName + " <color=#8CD3BD>" + mod.ToString("+0;-0;0") + "</color>";
            }
            else
            {
                detail += attrName;
            }
            _description.text = detail;
        }

        public void UpgradeSkill()
        {
            UpgradeSkillAction?.Invoke(_skillData);
        }
    }
}
