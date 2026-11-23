#region

using UnityEngine;

#endregion

namespace RGame.RoguelikeKit
{
    public class UIMenuManager : MonoBehaviour
    {
        [SerializeField] private UIMainMenu mMainMenuPanel;
        [SerializeField] private CharacterSelectPanel mCharacterSelectPanel;
        [SerializeField] private LevelSelectPanel mLevelSelectPanel;
        [SerializeField] private SettingPanel mSettingPanel;
        [SerializeField] private PowerUpPanel mPowerUpPanel;
        [SerializeField] private GameSceneSO mGameScene;
        [SerializeField] private LoadEventChannelSO mGameLoad;

        [SerializeField] private InputReader mInputReader;

        private void Start()
        {
            mInputReader.EnableMenuInput();
            SetMenuScreen();
        }

        private void OnDestroy()
        {
            mMainMenuPanel.SettingsButtonAction -= OpenSettingsScreen;
            mMainMenuPanel.NewGameButtonAction -= OpenCharacterSelectScreen;
            mMainMenuPanel.PowerUpButtonAction -= OpenPowerUpScreen;
        }

        private void SetMenuScreen()
        {
            mMainMenuPanel.SettingsButtonAction += OpenSettingsScreen;
            mMainMenuPanel.NewGameButtonAction += OpenCharacterSelectScreen;
            mMainMenuPanel.PowerUpButtonAction += OpenPowerUpScreen;
        }

        #region ChracterSelect

        private void OpenCharacterSelectScreen()
        {
            mCharacterSelectPanel.gameObject.SetActive(true);
            mCharacterSelectPanel.transform.SetAsLastSibling();
            mCharacterSelectPanel.Confirm += OpenLevelSelectScreen;
            mCharacterSelectPanel.Closed += CloseCharacterSelectScreen;
        }

        private void CloseCharacterSelectScreen()
        {
            mCharacterSelectPanel.Confirm -= OpenLevelSelectScreen;
            mCharacterSelectPanel.Closed -= CloseCharacterSelectScreen;
            mCharacterSelectPanel.gameObject.SetActive(false);
        }

        #endregion

        #region LevelSelect

        private void OpenLevelSelectScreen()
        {
            mLevelSelectPanel.gameObject.SetActive(true);
            mLevelSelectPanel.transform.SetAsLastSibling();
            mLevelSelectPanel.Closed += CloseLevelSelectScreen;
            mLevelSelectPanel.Confirm += OpenLobby;
        }

        private void CloseLevelSelectScreen()
        {
            mLevelSelectPanel.Closed -= CloseLevelSelectScreen;
            mLevelSelectPanel.Confirm -= OpenLobby;
            mLevelSelectPanel.gameObject.SetActive(false);
        }

        private void LoadGame()
        {
            mGameLoad.RaiseEvent(mGameScene);
        }

        private void OpenLobby() => DATN.UI.ForestLobby.Open(mLevelSelectPanel, LoadGame);

        #endregion

        #region Setting

        private void OpenSettingsScreen()
        {
            mSettingPanel.gameObject.SetActive(true);
            mSettingPanel.transform.SetAsLastSibling();
            mSettingPanel.Closed += CloseSettingsScreen;
        }

        private void CloseSettingsScreen()
        {
            mSettingPanel.Closed -= CloseSettingsScreen;
            mSettingPanel.gameObject.SetActive(false);
        }

        #endregion
        
        #region PowerUp

        private void OpenPowerUpScreen()
        {
            mPowerUpPanel.gameObject.SetActive(true);
            mPowerUpPanel.transform.SetAsLastSibling();
            mPowerUpPanel.Closed += ClosePowerUpScreen;
        }

        private void ClosePowerUpScreen()
        {
            mPowerUpPanel.Closed -= ClosePowerUpScreen;
            mPowerUpPanel.gameObject.SetActive(false);
        }

        #endregion
    }
}
