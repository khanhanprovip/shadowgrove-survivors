using System;
using DATN.Trial;
using RGame.CommonStat;
using RGame.Framework;
using RGame.RoguelikeKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
///     Cong cu cau hinh scene mot lan cho do an (chay qua -executeMethod).
///     AddTrialController: tao GameObject "TrialController" trong scene GamePlay
///     va gan day du cac tai nguyen serialize — tranh phai sua YAML scene bang tay.
/// </summary>
namespace DATN
{
    public static class DatnSceneSetup
    {
        private const string GamePlayScene =
            "Assets/RGame/RoguelikeKit/Scenes/Managers/GamePlay.unity";

        private const string ChRoot =
            "Assets/RGame/RoguelikeKit/ScriptableObjects";

        public static void AddTrialController()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(GamePlayScene, OpenSceneMode.Single);

                if (Object.FindFirstObjectByType<TrialController>(FindObjectsInactive.Include) != null)
                {
                    Debug.Log("[DatnSceneSetup] TrialController da ton tai — khong lam gi them");
                }
                else
                {
                    var go = new GameObject("TrialController");
                    var ctrl = go.AddComponent<TrialController>();

                    var so = new SerializedObject(ctrl);
                    so.FindProperty("mLevelDuration").floatValue = 90f;
                    so.FindProperty("mPlayerSpawnChannel").objectReferenceValue =
                        Load<PlayerSpawnChannelSO>($"{ChRoot}/LogicSO/EventChannels/Player/PlayerSpawnChannelSO.asset");
                    so.FindProperty("mGameWinChannel").objectReferenceValue =
                        Load<VoidEventChannelSO>($"{ChRoot}/LogicSO/EventChannels/UI/OnGameWin.asset");
                    so.FindProperty("mInputReader").objectReferenceValue =
                        Load<InputReader>($"{ChRoot}/LogicSO/Input/InputReader.asset");
                    so.FindProperty("mPlayerStat").objectReferenceValue =
                        Load<CommonStatRuntimeSO>($"{ChRoot}/DataSO/Player/PlayerRuntimeStat.asset");
                    so.FindProperty("mGlobalConfig").objectReferenceValue =
                        Load<GlobalConfigSO>($"{ChRoot}/RuntimeDataSO/Global/GlobalConfigSO.asset");
                    so.FindProperty("mPoolRuntime").objectReferenceValue =
                        Load<PoolRuntimeSO>($"{ChRoot}/RuntimeDataSO/Pool/Pool.asset");
                    so.FindProperty("mClearAllBuffsChannel").objectReferenceValue =
                        Load<VoidEventChannelSO>($"{ChRoot}/LogicSO/EventChannels/Buff/ClearAllBuffs.asset");
                    so.FindProperty("mLoadLevelChannel").objectReferenceValue =
                        Load<LoadEventChannelSO>($"{ChRoot}/LogicSO/EventChannels/SceneManagement/LoadLevel_Channel.asset");
                    so.FindProperty("mLoadMenuChannel").objectReferenceValue =
                        Load<LoadEventChannelSO>($"{ChRoot}/LogicSO/EventChannels/SceneManagement/LoadMenu_Channel.asset");
                    so.FindProperty("mGameScene").objectReferenceValue =
                        Load<GameSceneSO>($"{ChRoot}/LogicSO/SceneData/GameLevel/Game.asset");
                    so.FindProperty("mMainMenuScene").objectReferenceValue =
                        Load<GameSceneSO>($"{ChRoot}/LogicSO/SceneData/Menus/MainMenu.asset");
                    so.ApplyModifiedPropertiesWithoutUndo();

                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("[DatnSceneSetup] Da tao va luu TrialController trong scene GamePlay");
                }

                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatnSceneSetup] LOI: {ex}");
                EditorApplication.Exit(1);
            }
        }

        public static void WireTrialControllerMode()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(GamePlayScene, OpenSceneMode.Single);
                var ctrl = Object.FindFirstObjectByType<DATN.Trial.TrialController>(FindObjectsInactive.Include);
                if (ctrl == null) throw new Exception("Khong tim thay TrialController trong GamePlay");

                var so = new SerializedObject(ctrl);
                so.FindProperty("mLevelAndCharacterSO").objectReferenceValue =
                    Load<LevelAndCharacterSO>($"{ChRoot}/LogicSO/Level/LevelAndCharacterSO.asset");
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[DatnSceneSetup] Da gan mLevelAndCharacterSO cho TrialController (Moc 4)");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatnSceneSetup] LOI: {ex}");
                EditorApplication.Exit(1);
            }
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new Exception($"Khong tai duoc asset: {path} ({typeof(T).Name})");
            }
            return asset;
        }

        public static void AddAutoTestDriver()
        {
            try
            {
                const string initScene = "Assets/RGame/RoguelikeKit/Scenes/Initialization.unity";
                var scene = EditorSceneManager.OpenScene(initScene, OpenSceneMode.Single);

                if (Object.FindFirstObjectByType<DATN.Trial.DatnAutoTestDriver>(FindObjectsInactive.Include) != null)
                {
                    Debug.Log("[DatnSceneSetup] DatnAutoTestDriver da ton tai trong Initialization");
                }
                else
                {
                    var go = new GameObject("DatnAutoTestDriver");
                    go.AddComponent<DATN.Trial.DatnAutoTestDriver>();
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("[DatnSceneSetup] Da them DatnAutoTestDriver vao scene Initialization");
                }

                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatnSceneSetup] LOI: {ex}");
                EditorApplication.Exit(1);
            }
        }

        public static void WireGameOverChannel()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(GamePlayScene, OpenSceneMode.Single);
                var ctrl = Object.FindFirstObjectByType<DATN.Trial.TrialController>(FindObjectsInactive.Include);
                if (ctrl == null) throw new Exception("Khong tim thay TrialController");
                var so = new SerializedObject(ctrl);
                so.FindProperty("mGameOverChannel").objectReferenceValue =
                    Load<VoidEventChannelSO>($"{ChRoot}/LogicSO/EventChannels/UI/OnGameOver.asset");
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[DatnSceneSetup] Da gan OnGameOver cho TrialController (Moc 8)");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatnSceneSetup] LOI: {ex}");
                EditorApplication.Exit(1);
            }
        }


        /// <summary>Moc fix chớp: gỡ DynamicSortOrder khỏi 3 prefab nhân vật + order 50, bằng PrefabUtility.</summary>
        public static void RemoveDynamicSortOrderSafe()
        {
            try
            {
                foreach (var name in new[] { "Rowan", "Lyra", "Kael" })
                {
                    string path = "Assets/RGame/RoguelikeKit/Prefabs/Player/Shadowgrove/" + name + ".prefab";
                    var root = PrefabUtility.LoadPrefabContents(path);
                    foreach (var dso in root.GetComponentsInChildren<DynamicSortOrder>(true))
                        Object.DestroyImmediate(dso);
                    var sr = root.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sortingOrder = 50;
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    PrefabUtility.UnloadPrefabContents(root);
                    Debug.Log("[Fix] " + name + ": da go DynamicSortOrder, order=50");
                }
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[Fix] LOI: " + ex);
                EditorApplication.Exit(1);
            }
        }


        /// <summary>Rút menu chọn màn còn 6 màn đại diện (đúng cách, qua SerializedObject).</summary>
        public static void TrimLevelListSafe()
        {
            try
            {
                string path = "Assets/RGame/RoguelikeKit/Prefabs/UI/MainMenu/LevelSelect_Panel .prefab";
                var panelGo = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var panel = panelGo.GetComponent<LevelSelectPanel>();
                var so = new SerializedObject(panel);
                var list = so.FindProperty("mLevelSos");
                string[] keep = { "TrialLevel", "LV2_MoonlitRuins", "LV3_SunkenMarsh", "LV5_FrostHollow", "LV6_CursedVillage", "LV9_EndlessAbyss" };
                var keepAssets = new System.Collections.Generic.List<LevelConfigSO>();
                foreach (var k in keep)
                {
                    var a = AssetDatabase.LoadAssetAtPath<LevelConfigSO>(
                        "Assets/RGame/RoguelikeKit/ScriptableObjects/DataSO/Level/SelectLevel/" + k + ".asset");
                    if (a != null) keepAssets.Add(a);
                }
                list.arraySize = keepAssets.Count;
                for (int i = 0; i < keepAssets.Count; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = keepAssets[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(panel);
                AssetDatabase.SaveAssets();
                Debug.Log("[Fix] Menu man: " + keepAssets.Count + " man");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[Fix] LOI: " + ex);
                EditorApplication.Exit(1);
            }
        }

    }
}
