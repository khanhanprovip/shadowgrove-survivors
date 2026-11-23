#region

using UnityEngine;
using UnityEngine.Events;

#endregion

namespace RGame.RoguelikeKit
{
    public class UIMainMenu : MonoBehaviour
    {
        public UnityAction ExitButtonAction;
        public UnityAction NewGameButtonAction;
        public UnityAction PowerUpButtonAction;
        public UnityAction SettingsButtonAction;

        public void NewGameButton()
        {
            ShadowgroveAudio.Play(SoundCue.UiSelect);
            NewGameButtonAction.Invoke();
        }

        public void SettingsButton()
        {
            ShadowgroveAudio.Play(SoundCue.UiSelect);
            SettingsButtonAction.Invoke();
        }

        public void PowerUpButton()
        {
            ShadowgroveAudio.Play(SoundCue.UiSelect);
            PowerUpButtonAction.Invoke();
        }

        public void ExitButton()
        {
            ExitButtonAction.Invoke();
        }
    }
}