using RGame.Framework;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    public class GameEndPanel : MonoBehaviour
    {
        [SerializeField] private VoidEventChannelSO _openGameOverPanel;
        [SerializeField] private GameSceneSO _mainMenuScene;
        [SerializeField] private LoadEventChannelSO _sceneLoader;
        [SerializeField] private VoidEventChannelSO _clearAllBuffs;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private GlobalConfigSO _globalConfig;
        [SerializeField] private VoidEventChannelSO _saveGame;
        [SerializeField] private AttributeSO _attributeSO;
        [SerializeField] private PoolRuntimeSO poolRuntime;
        
        private void OnEnable()
        {
            _openGameOverPanel.RegisterListener(Show);
        }

        private void OnDisable()
        {
            _openGameOverPanel.UnregisterListener(Show);
        }

        private void Show()
        {
            _globalConfig.BankGold();
            Time.timeScale = 0;
            poolRuntime.RecycleAll();
            _clearAllBuffs.RaiseEvent();
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;

            _canvasGroup.interactable = true;
        }
        
        public void Exit()
        {
            ShadowgroveAudio.Play(SoundCue.UiSelect);
            Time.timeScale = 1;
            _globalConfig.BankGold();
            _saveGame.RaiseEvent();
            _sceneLoader.RaiseEvent(_mainMenuScene);
        }

    }
}
