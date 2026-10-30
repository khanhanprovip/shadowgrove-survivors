#region

using System;
using RGame.Framework;
using UnityEngine;
using UnityEngine.Localization.Settings;

#endregion

namespace RGame.RoguelikeKit
{
    public class SettingsSystem : MonoBehaviour
    {
        [SerializeField] private VoidEventChannelSO mSaveSettingsEvent;
        [SerializeField] private VoidEventChannelSO mSave;
        
        [SerializeField] private FloatEventChannelSO mChangeMasterVolumeEventChannel;
        [SerializeField] private FloatEventChannelSO mChangeSfxVolumeEventChannel;
        [SerializeField] private FloatEventChannelSO mChangeMusicVolumeEventChannel;

        [SerializeField] private SettingsSO mCurrentSettings;

        private void Awake()
        {
            Application.targetFrameRate = 240;
            Time.fixedDeltaTime = 1.0f / 240.0f;
        }

        private void Start()
        {
            SetCurrentSettings();
        }

        private void OnEnable()
        {
            mSaveSettingsEvent.RegisterListener(SaveSettings);
        }

        private void OnDisable()
        {
            mSaveSettingsEvent.UnregisterListener(SaveSettings);
        }

        /// <summary>
        ///     Set the current settings.
        /// </summary>
        private void SetCurrentSettings()
        {
            mChangeMusicVolumeEventChannel.RaiseEvent(mCurrentSettings.MusicVolume); // Raise event for volume change
            mChangeSfxVolumeEventChannel.RaiseEvent(mCurrentSettings.SfxVolume); // Raise event for volume change
            mChangeMasterVolumeEventChannel.RaiseEvent(mCurrentSettings.MasterVolume); // Raise event for volume change

            // Unity applies -screen-width/-screen-height before Start. Respect those
            // explicit launch options (used by QA and desktop shortcuts) instead of
            // immediately replacing them with the last saved resolution.
            if (TryGetCommandLineResolution(out int launchWidth, out int launchHeight, out bool launchFullscreen))
            {
                Screen.SetResolution(launchWidth, launchHeight, launchFullscreen);
            }
            else
            {
                var currentResolution = Screen.currentResolution;
                if (mCurrentSettings.ResolutionsIndex < Screen.resolutions.Length)
                    currentResolution = Screen.resolutions[mCurrentSettings.ResolutionsIndex];
                Screen.SetResolution(currentResolution.width, currentResolution.height, mCurrentSettings.IsFullscreen);
            }

            LocalizationSettings.SelectedLocale = mCurrentSettings.LanguageLocale;
        }

        private static bool TryGetCommandLineResolution(out int width, out int height, out bool fullscreen)
        {
            width = Screen.width;
            height = Screen.height;
            fullscreen = false;
            bool found = false;
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (arguments[i].Equals("-screen-width", StringComparison.OrdinalIgnoreCase) &&
                    i + 1 < arguments.Length && int.TryParse(arguments[++i], out int parsedWidth))
                {
                    width = parsedWidth;
                    found = true;
                }
                else if (arguments[i].Equals("-screen-height", StringComparison.OrdinalIgnoreCase) &&
                         i + 1 < arguments.Length && int.TryParse(arguments[++i], out int parsedHeight))
                {
                    height = parsedHeight;
                    found = true;
                }
                else if (arguments[i].Equals("-screen-fullscreen", StringComparison.OrdinalIgnoreCase) &&
                         i + 1 < arguments.Length)
                {
                    fullscreen = arguments[++i] != "0";
                    found = true;
                }
            }
            return found;
        }

        private void SaveSettings()
        {
            SetCurrentSettings();
            mSave.RaiseEvent();
        }
    }
}
