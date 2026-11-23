using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace RGame.RoguelikeKit
{
    public class PowerUpDescription : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI mTitle;
        [SerializeField] private TextMeshProUGUI mDescription;
        [SerializeField] private TextMeshProUGUI mGoldCount;
        
        private UnityAction mUpgradeAction;
        private string currentTitle, currentDescription, language;
        private int currentCost;
        
        public void SelectPowerUp(string _title, string _description, int _requireGold, UnityAction _upgrade)
        {
            mUpgradeAction = null;
            mUpgradeAction += _upgrade;

            currentTitle = _title; currentDescription = _description; currentCost = _requireGold;
            RefreshText();
            var button = GetComponentInChildren<UnityEngine.UI.Button>();
            if (button != null) button.interactable = _upgrade != null;
        }

        private void Update()
        {
            if (currentTitle != null && language != DATN.Localization.LanguageManager.GetCurrentCode()) RefreshText();
        }

        private void RefreshText()
        {
            language = DATN.Localization.LanguageManager.GetCurrentCode();
            mTitle.text = DATN.UI.ForestUI.AttributeName(currentTitle);
            mTitle.color = DATN.UI.ForestUI.Gold;
            mDescription.color = DATN.UI.ForestUI.Ink;
            mGoldCount.color = DATN.UI.ForestUI.Ink;
            mDescription.text = language == "vi" ? VietnameseDescription(currentTitle) : currentDescription;
            mGoldCount.text = currentCost < 0 ? DATN.UI.ForestUI.Tr("TỐI ĐA", "MAX") : currentCost.ToString();
        }

        private static string VietnameseDescription(string key) => key switch
        {
            "Growth" => "Mỗi bậc nhận thêm 10% kinh nghiệm.",
            "Area" => "Mỗi bậc tăng 10% phạm vi tấn công.",
            "Armor" => "Mỗi bậc giảm 10% sát thương nhận vào.",
            "Might" => "Mỗi bậc tăng 5% sát thương gây ra.",
            "HPMax" => "Mỗi bậc tăng 10% máu tối đa.",
            "Amount" => "Bắn thêm 1 viên đạn.",
            "MoveSpeed" => "Mỗi bậc tăng 10% tốc độ di chuyển.",
            "Greed" => "Mỗi bậc nhận thêm 10% vàng.",
            "Cooldown" => "Mỗi bậc sử dụng vũ khí nhanh hơn 10%.",
            "SkillSpeed" => "Mỗi bậc tăng 10% tốc độ đạn.",
            "Magnet" => "Mỗi bậc tăng 25% phạm vi nhặt vật phẩm.",
            "Curse" => "Mỗi bậc tăng 10% tốc độ, máu, số lượng và tần suất xuất hiện của quái.",
            "Duration" => "Mỗi bậc tăng 15% thời gian hiệu lực vũ khí.",
            "Recovery" => "Mỗi bậc hồi thêm 1 HP mỗi giây.",
            _ => DATN.UI.ForestUI.AttributeName(key)
        };

        public void OnBuyButtonOnclick()
        {
            mUpgradeAction?.Invoke();
        }
    }
}
