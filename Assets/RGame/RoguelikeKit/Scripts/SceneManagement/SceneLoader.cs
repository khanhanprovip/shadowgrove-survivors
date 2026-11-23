using System.Collections;
using RGame.Framework;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace RGame.RoguelikeKit
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private GameSceneSO gameplaySceneSO;
        [SerializeField] private InputReader inputReader;
        [SerializeField] private LoadEventChannelSO loadLevelChannel;
        [SerializeField] private LoadEventChannelSO loadMenuChannel;
        [SerializeField] private BoolEventChannelSO toggleLoadingScreenChannel;
        [SerializeField] private FadeChannelSO fadeRequestChannel;
        [SerializeField] private VoidEventChannelSO sceneReadyChannel;

        private const float FadeDuration = 1.5f;
        private bool isLoading;
        private GameSceneSO currentLoadedScene;

        private void OnEnable()
        {
            loadLevelChannel.OnLoadingRequested += HandleLevelLoadRequest;
            loadMenuChannel.OnLoadingRequested += HandleMenuLoadRequest;
        }

        private void OnDisable()
        {
            loadLevelChannel.OnLoadingRequested -= HandleLevelLoadRequest;
            loadMenuChannel.OnLoadingRequested -= HandleMenuLoadRequest;
        }

        private void HandleLevelLoadRequest(GameSceneSO scene, bool showLoadingScreen, bool fadeScreen)
        {
            if (!isLoading) StartCoroutine(TransitionTo(scene, true));
        }

        private void HandleMenuLoadRequest(GameSceneSO scene, bool showLoadingScreen, bool fadeScreen)
        {
            if (!isLoading) StartCoroutine(TransitionTo(scene, false));
        }

        private IEnumerator TransitionTo(GameSceneSO nextScene, bool gameplay)
        {
            isLoading = true;
            inputReader.DisableAllInput();
            fadeRequestChannel.FadeOut(FadeDuration);
            yield return new WaitForSecondsRealtime(FadeDuration);

            toggleLoadingScreenChannel.RaiseEvent(true);
            yield return UnloadScene(currentLoadedScene);
            currentLoadedScene = null;
            yield return UnloadScene(gameplaySceneSO);

            // Recreate the manager scene when starting a new attempt. It owns
            // the skills, timers and result UI for the previous attempt.
            if (gameplay)
            {
                var managers = gameplaySceneSO.sceneReference.LoadSceneAsync(LoadSceneMode.Additive);
                yield return managers;
                if (managers.Status != AsyncOperationStatus.Succeeded)
                {
                    FinishFailedLoad(gameplaySceneSO);
                    yield break;
                }
            }

            var load = nextScene.sceneReference.LoadSceneAsync(LoadSceneMode.Additive);
            yield return load;
            if (load.Status != AsyncOperationStatus.Succeeded)
            {
                FinishFailedLoad(nextScene);
                yield break;
            }

            currentLoadedScene = nextScene;
            SceneManager.SetActiveScene(load.Result.Scene);
            ShadowgroveAudio.SetScene(gameplay);
            LightProbes.TetrahedralizeAsync();
            isLoading = false;
            toggleLoadingScreenChannel.RaiseEvent(false);
            fadeRequestChannel.FadeIn(FadeDuration);
            sceneReadyChannel.RaiseEvent();
        }

        private static IEnumerator UnloadScene(GameSceneSO scene)
        {
            if (scene == null || !scene.sceneReference.OperationHandle.IsValid()) yield break;
            var unload = scene.sceneReference.UnLoadScene();
            while (unload.IsValid() && !unload.IsDone)
                yield return null;
        }

        private void FinishFailedLoad(GameSceneSO scene)
        {
            Debug.LogError($"Unable to load scene {scene.name}.");
            isLoading = false;
            toggleLoadingScreenChannel.RaiseEvent(false);
            fadeRequestChannel.FadeIn(FadeDuration);
        }
    }
}
