#region

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#endregion

namespace RGame.RoguelikeKit
{
    public class SettingItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;

        private int currentValue;
        private string[] displayTexts;
        private bool isBoolean;
        private int maxValue;
        private int minValue;
        private UnityAction<int> onValueChanged;
        private Slider forestSlider;
        private string displayLanguage;

        private void LateUpdate()
        {
            string language = DATN.Localization.LanguageManager.GetCurrentCode();
            if (displayLanguage == language) return;
            displayLanguage = language;
            SetValue(currentValue, false); // Presentation only; never reapply settings on locale changes.
        }

        public void AddForestSlider()
        {
            if (forestSlider != null || isBoolean || displayTexts != null) return;
            var go = new GameObject("Forest_VolumeSlider", typeof(RectTransform), typeof(Image), typeof(Slider));
            go.transform.SetParent(transform, false);
            DATN.UI.ForestUI.Place(go.transform, .55f, .15f, .88f, .40f);
            go.GetComponent<Image>().color = new Color(.22f, .31f, .23f);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)); fill.transform.SetParent(go.transform, false);
            DATN.UI.ForestUI.Place(fill.transform, 0, 0, 1, 1); fill.GetComponent<Image>().color = DATN.UI.ForestUI.Gold;
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)); handle.transform.SetParent(go.transform, false);
            var hr = handle.GetComponent<RectTransform>(); hr.sizeDelta = new Vector2(14, 25);
            handle.GetComponent<Image>().color = DATN.UI.ForestUI.Ink;
            forestSlider = go.GetComponent<Slider>(); forestSlider.fillRect = fill.GetComponent<RectTransform>();
            forestSlider.handleRect = hr; forestSlider.targetGraphic = handle.GetComponent<Image>();
            forestSlider.minValue = minValue; forestSlider.maxValue = maxValue; forestSlider.wholeNumbers = true;
            forestSlider.SetValueWithoutNotify(currentValue); forestSlider.onValueChanged.AddListener(value => SetValue(Mathf.RoundToInt(value)));
            DATN.UI.ForestUI.Place(valueText.transform, .55f, .48f, .88f, .95f);
        }

        public void Initialize(int defaultValue, int min, int max, bool isBool, UnityAction<int> callback)
        {
            minValue = min;
            maxValue = max;
            isBoolean = isBool;
            onValueChanged = callback;

            SetValue(defaultValue, false);

            leftButton.onClick.AddListener(OnLeftButtonClick);
            rightButton.onClick.AddListener(OnRightButtonClick);
        }

        public void Initialize(int defaultValue, string[] texts, UnityAction<int> callback)
        {
            displayTexts = texts;
            minValue = 0;
            maxValue = texts.Length - 1;
            isBoolean = false;
            onValueChanged = callback;

            SetValue(defaultValue, false);

            leftButton.onClick.AddListener(OnLeftButtonClick);
            rightButton.onClick.AddListener(OnRightButtonClick);
        }

        private void OnLeftButtonClick()
        {
            if (isBoolean)
            {
                SetValue(0);
            }
            else
            {
                var newValue = Mathf.Max(minValue, currentValue - 1);
                SetValue(newValue);
            }
        }

        private void OnRightButtonClick()
        {
            if (isBoolean)
            {
                SetValue(1);
            }
            else
            {
                var newValue = Mathf.Min(maxValue, currentValue + 1);
                SetValue(newValue);
            }
        }

        private void SetValue(int value, bool notify = true)
        {
            currentValue = Mathf.Clamp(value, minValue, Mathf.Max(minValue, maxValue));

            if (displayTexts != null && displayTexts.Length > 0)
            {
                string label = displayTexts[Mathf.Clamp(currentValue, 0, displayTexts.Length - 1)];
                if (label.StartsWith("Vietnamese", System.StringComparison.OrdinalIgnoreCase))
                    label = DATN.UI.ForestUI.Tr("Tiếng Việt", "Vietnamese");
                else if (label.StartsWith("English", System.StringComparison.OrdinalIgnoreCase))
                    label = DATN.UI.ForestUI.Tr("Tiếng Anh", "English");
                valueText.text = label;
            }
            else if (isBoolean)
                valueText.text = currentValue == 1 ? DATN.UI.ForestUI.Tr("Bật", "ON") : DATN.UI.ForestUI.Tr("Tắt", "OFF");
            else
                valueText.text = currentValue.ToString();

            if (notify) onValueChanged?.Invoke(currentValue);
            if (forestSlider != null) forestSlider.SetValueWithoutNotify(currentValue);
        }

        public void UpdateValue(int value)
        {
            SetValue(value);
        }

        public void SetValueWithoutNotify(int value) => SetValue(value, false);
    }
}
