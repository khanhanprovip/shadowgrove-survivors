#region

using System;
using System.Linq;
using DATN.Localization;
using RGame.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

#endregion

namespace RGame.RoguelikeKit
{
    public class SettingPanel : MonoBehaviour
    {
        [SerializeField] private SettingsSO _currentSettings;
        [SerializeField] private VoidEventChannelSO SaveSettingsEvent;

        [Header("Setting Items")] [SerializeField]
        private SettingItem masterVolumeItem;

        [SerializeField] private SettingItem musicVolumeItem;
        [SerializeField] private SettingItem sfxVolumeItem;
        [SerializeField] private SettingItem resolutionItem;
        [SerializeField] private SettingItem fullscreenItem;
        [SerializeField] private SettingItem languageItem;
        [SerializeField] private SettingItem showDamageItem;

        private Locale[] availableLocales;

        public UnityAction Closed;
        private string[] localeDisplayNames;
        private string[] resolutionDisplayTexts;

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += RefreshLanguage;
            RefreshLanguage(LocalizationSettings.SelectedLocale);
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= RefreshLanguage;
        }

        private void RefreshLanguage(Locale locale)
        {
            if (availableLocales == null || locale == null) return;
            int index = Array.IndexOf(availableLocales, locale);
            if (index >= 0) languageItem.SetValueWithoutNotify(index);
        }

        private void Start()
        {
            InitializeLocales();
            InitializeResolutions();
            InitializeSettingItems();
        }

        public void CloseButton()
        {
            ShadowgroveAudio.Play(SoundCue.UiSelect);
            Closed.Invoke();
        }

        private void InitializeLocales()
        {
            // 获取所有可用的语言
            availableLocales = LocalizationSettings.AvailableLocales.Locales.ToArray();
            localeDisplayNames = availableLocales.Select(l => l.LocaleName).ToArray();
        }

        private void InitializeResolutions()
        {
            var resolutions = Screen.resolutions;
            resolutionDisplayTexts = resolutions
                .Select(r => $"{r.width}x{r.height} @{Math.Round(r.refreshRateRatio.value)}Hz")
                .ToArray();
        }


        private void InitializeSettingItems()
        {
            masterVolumeItem.Initialize(
                Mathf.RoundToInt(_currentSettings.MasterVolume * 10),
                0, 10, false,
                value => SettingMasterVolume(value));

            musicVolumeItem.Initialize(
                Mathf.RoundToInt(_currentSettings.MusicVolume * 10),
                0, 10, false,
                value => SettingMusicVolume(value));

            sfxVolumeItem.Initialize(
                Mathf.RoundToInt(_currentSettings.SfxVolume * 10),
                0, 10, false,
                value => SettingSFXVolume(value));

            resolutionItem.Initialize(
                _currentSettings.ResolutionsIndex,
                resolutionDisplayTexts,
                value => SettingResolution(value));

            fullscreenItem.Initialize(
                _currentSettings.IsFullscreen ? 1 : 0,
                0, 1, true,
                value => SettingFullscreen(value == 1));

            var currentLocaleIndex = Array.FindIndex(availableLocales,
                locale => locale.Identifier.Code == LanguageManager.GetCurrentCode());
            languageItem.Initialize(
                currentLocaleIndex,
                localeDisplayNames,
                value => SettingLanguage(value));

            showDamageItem.Initialize(
                _currentSettings.IsShowDamage ? 1 : 0,
                0, 1, true,
                value => SettingShowDamage(value == 1));
        }

        private void SettingMasterVolume(int value)
        {
            var normalizedValue = value / 10f;
            SaveSettings(
                _currentSettings.MusicVolume,
                _currentSettings.SfxVolume,
                normalizedValue,
                _currentSettings.ResolutionsIndex,
                _currentSettings.IsFullscreen,
                _currentSettings.IsShowDamage
            );
        }

        private void SettingMusicVolume(int value)
        {
            var normalizedValue = value / 10f;
            SaveSettings(
                normalizedValue,
                _currentSettings.SfxVolume,
                _currentSettings.MasterVolume,
                _currentSettings.ResolutionsIndex,
                _currentSettings.IsFullscreen,
                _currentSettings.IsShowDamage
            );
        }

        private void SettingSFXVolume(int value)
        {
            var normalizedValue = value / 10f;
            SaveSettings(
                _currentSettings.MusicVolume,
                normalizedValue,
                _currentSettings.MasterVolume,
                _currentSettings.ResolutionsIndex,
                _currentSettings.IsFullscreen,
                _currentSettings.IsShowDamage
            );
            ShadowgroveAudio.Play(SoundCue.UiSelect);
        }

        private void SettingResolution(int value)
        {
            SaveSettings(
                _currentSettings.MusicVolume,
                _currentSettings.SfxVolume,
                _currentSettings.MasterVolume,
                value,
                _currentSettings.IsFullscreen,
                _currentSettings.IsShowDamage
            );
        }

        private void SettingFullscreen(bool isFullscreen)
        {
            SaveSettings(
                _currentSettings.MusicVolume,
                _currentSettings.SfxVolume,
                _currentSettings.MasterVolume,
                _currentSettings.ResolutionsIndex,
                isFullscreen,
                _currentSettings.IsShowDamage
            );
        }

        private void SettingLanguage(int localeIndex)
        {
            if (localeIndex >= 0 && localeIndex < availableLocales.Length)
                LanguageManager.Instance?.SetLanguage(availableLocales[localeIndex]);
        }

        private void SettingShowDamage(bool showDamage)
        {
            SaveSettings(
                _currentSettings.MusicVolume,
                _currentSettings.SfxVolume,
                _currentSettings.MasterVolume,
                _currentSettings.ResolutionsIndex,
                _currentSettings.IsFullscreen,
                showDamage
            );
        }

        private void SaveSettings(float _musicVolume, float _sfxVolume, float _masterVolume, int _resolutionsIndex, bool _isFullscreen, bool _damageShow)
        {
            _currentSettings.SaveSettings(_musicVolume, _sfxVolume, _masterVolume, _resolutionsIndex, _isFullscreen, _damageShow);
            SaveSettingsEvent.RaiseEvent();
        }
    }
}
