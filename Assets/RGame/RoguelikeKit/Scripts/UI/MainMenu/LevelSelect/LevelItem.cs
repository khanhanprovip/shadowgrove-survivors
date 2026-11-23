#region

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

#endregion

namespace RGame.RoguelikeKit
{
    public class LevelItem : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerExitHandler
    {
        [SerializeField] private LevelAndCharacterSO mLevelAndCharacterSO;

        [SerializeField] private TextMeshProUGUI mDescription;
        [SerializeField] private Image mPreviewImage;
        public Image BackgroundImage;
        public UnityAction<LevelItem> OnPointerEnterAction;
        public UnityAction<LevelItem> OnPointerDownAction;
        public UnityAction<LevelItem> OnPointerExitAction;
        public bool IsSelected;
        private LevelConfigSO _config;
        public LevelConfigSO Config => _config;
        public Image PreviewImage => mPreviewImage;
        public TextMeshProUGUI Description => mDescription;
        private string _lastLocale;

        public void Initialization(LevelConfigSO _configSo)
        {
            _config = _configSo;
            // Each card uses its own level configuration. The inherited kit
            // LocalizeStringEvent otherwise overwrites it after a locale switch.
            if (mDescription.TryGetComponent<LocalizeStringEvent>(out var defaultDescription))
                defaultDescription.enabled = false;
            RefreshDescription();
            mPreviewImage.sprite = _configSo.PreviewImage;
            mPreviewImage.preserveAspect = true;
            mPreviewImage.color = _configSo.PreviewImage == null
                ? new Color(0.055f, 0.105f, 0.075f, 1f)
                : Color.white;

            GetComponent<Button>().onClick.AddListener(() => { mLevelAndCharacterSO.SelectLevelSO = _configSo; });
        }

        private void Update()
        {
            string locale = DATN.Localization.LanguageManager.Instance != null
                ? DATN.Localization.LanguageManager.Instance.CurrentCode
                : PlayerPrefs.GetString(DATN.Localization.LanguageManager.PrefsKey, "");
            if (locale != _lastLocale) RefreshDescription();
        }

        private void RefreshDescription()
        {
            if (_config == null || mDescription == null) return;
            _lastLocale = DATN.Localization.LanguageManager.Instance != null
                ? DATN.Localization.LanguageManager.Instance.CurrentCode
                : PlayerPrefs.GetString(DATN.Localization.LanguageManager.PrefsKey, "");
            mDescription.text = DATN.Localization.LanguageManager.GetLevelDescription(
                _config.name, _config.LevelDescription);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnPointerEnterAction?.Invoke(this);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnPointerDownAction?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnPointerExitAction?.Invoke(this);
        }
    }
}
