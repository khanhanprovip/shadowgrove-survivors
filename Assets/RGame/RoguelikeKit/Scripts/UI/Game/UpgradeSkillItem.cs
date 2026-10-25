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

        public UnityAction<SkillDataSO> UpgradeSkillAction;

        public void SetUI(SkillDataSO skillData)
        {
            _skillData = skillData;

            _icon.sprite = skillData.SkillIcon;
            BuildCard(skillData);
        }

        private void BuildCard(SkillDataSO skillData)
        {
            string key = skillData.Key;

            // Ten + nhan (ky nang moi khi cap 0)
            string displayName = LanguageManager.Get("Skill_" + key);
            if (displayName == "Skill_" + key) displayName = key; // chua co dich → dung Key goc
            bool isNew = skillData.Level == 0 && skillData.SkillType != SkillType.MoneyBag;
            _name.text = isNew
                ? displayName + "  <color=#E8C35A>[" + LanguageManager.Get("CardNew") + "]</color>"
                : displayName;

            // Mo ta
            string desc = LanguageManager.Get("SkillDesc_" + key);
            _description.text = desc == "SkillDesc_" + key ? "" : desc;

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

        /// <summary>Chi so ky nang tanh cong: sat thuong/hoi chieu/so dan... + muc nang cap.</summary>
        private void BuildAttackDetail(SkillDataSO skillData)
        {
            int level = skillData.Level;
            int maxLevel = skillData.UpgradeAttribute.Count + 1;
            _level.text = level >= maxLevel
                ? LanguageManager.GetFormatted("CardLevel", level, level, maxLevel)
                : LanguageManager.GetFormatted("CardLevel", level, level + 1, maxLevel);

            string detail = _description.text.Length > 0 ? _description.text + "\n" : "";
            detail += StatLine(LanguageManager.Get("Stat_Damage"), skillData.Damage, skillData, UpgradeSkillAttributeType.DamageAdd, false);
            detail += StatLine(LanguageManager.Get("Stat_Amount"), skillData.Amount, skillData, UpgradeSkillAttributeType.ProjectilesCountAdd, true);
            detail += StatLine(LanguageManager.Get("Stat_Velocity"), skillData.Velocity, skillData, UpgradeSkillAttributeType.VelocityAddPercent, false);
            detail += StatLine(LanguageManager.Get("Stat_Area"), skillData.Area, skillData, UpgradeSkillAttributeType.AreaAddPercent, true);
            _description.text = detail.TrimEnd('\n');
        }

        /// <summary>Mot dong chi so: "Ten: gia tri → +X" neu nang cap lam thay doi.</summary>
        private string StatLine(string label, float current, SkillDataSO data,
            UpgradeSkillAttributeType modType, bool isInt)
        {
            int level = data.Level;
            string value = isInt ? ((int)current).ToString() : current.ToString("0.#");
            string line = label + ": " + value;

            // Mod cua cap ke tiep nam o index = level (list bat dau tu nang cap dau tien)
            if (level < data.UpgradeAttribute.Count && level < data.UpgradeAttributeModValue.Count)
            {
                if (data.UpgradeAttribute[level].skillAttributeType == modType)
                {
                    int mod = data.UpgradeAttributeModValue[level];
                    string suffix = modType == UpgradeSkillAttributeType.DamageAdd ? "%" : "";
                    line += " <color=#88C9A1>→ +" + mod + suffix + "</color>";
                }
            }
            return line + "\n";
        }

        /// <summary>Ky nang thuoc tinh: ten attribute + gia tri tang o cap ke tiep.</summary>
        private void BuildAttributeDetail(SkillDataSO skillData)
        {
            int level = skillData.Level;
            int maxLevel = skillData.UpgradeAttributeModValue.Count;
            _level.text = level >= maxLevel
                ? LanguageManager.GetFormatted("CardLevel", level, level, maxLevel)
                : LanguageManager.GetFormatted("CardLevel", level, level + 1, maxLevel);

            string attrName = LanguageManager.Get("SkillAttr_" + skillData.AttributeType);
            string detail = _description.text.Length > 0 ? _description.text + "\n" : "";

            if (level < skillData.UpgradeAttributeModValue.Count)
            {
                int mod = skillData.UpgradeAttributeModValue[level];
                detail += attrName + " <color=#88C9A1>+" + mod + "</color>";
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
