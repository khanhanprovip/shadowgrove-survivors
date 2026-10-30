using System;
using DATN.Trial;
using RGame.CommonStat;
using RGame.Framework;
using RGame.RoguelikeKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;
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

        /// <summary>Priority 2: Khôi phục 10 màn + ScrollRect — thêm VerticalLayoutGroup + ContentSizeFitter vào Content.</summary>
        public static void RestoreLevelsAndScroll()
        {
            try
            {
                string path = "Assets/RGame/RoguelikeKit/Prefabs/UI/MainMenu/LevelSelect_Panel .prefab";
                var panelGo = PrefabUtility.LoadPrefabContents(path);
                var panel = panelGo.GetComponent<LevelSelectPanel>();

                // 1) Khôi phục 10 màn
                string[] levels = {
                    "TrialLevel", "LV2_MoonlitRuins", "LV3_SunkenMarsh", "LV4_AshenQuarry",
                    "LV5_FrostHollow", "LV6_CursedVillage", "LV7_ClockworkCitadel",
                    "LV8_CrimsonCathedral", "LV9_EndlessAbyss", "LV10_EclipseThrone"
                };
                var keepAssets = new System.Collections.Generic.List<LevelConfigSO>();
                foreach (var k in levels)
                {
                    var a = AssetDatabase.LoadAssetAtPath<LevelConfigSO>(
                        "Assets/RGame/RoguelikeKit/ScriptableObjects/DataSO/Level/SelectLevel/" + k + ".asset");
                    if (a != null) keepAssets.Add(a);
                }
                var so = new SerializedObject(panel);
                var list = so.FindProperty("mLevelSos");
                list.arraySize = keepAssets.Count;
                for (int i = 0; i < keepAssets.Count; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = keepAssets[i];
                so.ApplyModifiedPropertiesWithoutUndo();

                // 2) Tim ScrollRect + Content → them layout + fitter
                var scrollRect = panelGo.GetComponentInChildren<ScrollRect>(true);
                if (scrollRect != null)
                {
                    var content = scrollRect.content;
                    if (content != null)
                    {
                        // Them VerticalLayoutGroup neu chua co
                        var vlg = content.GetComponent<VerticalLayoutGroup>();
                        if (vlg == null) vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
                        vlg.spacing = 14f;
                        vlg.childForceExpandHeight = false;
                        vlg.childForceExpandWidth = true;
                        vlg.childControlHeight = false;
                        vlg.childControlWidth = false;
                        vlg.childAlignment = TextAnchor.UpperCenter;

                        // Them ContentSizeFitter
                        var csf = content.GetComponent<ContentSizeFitter>();
                        if (csf == null) csf = content.gameObject.AddComponent<ContentSizeFitter>();
                        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                        // ScrollRect vertical = true
                        scrollRect.vertical = true;
                        scrollRect.horizontal = false;
                        scrollRect.movementType = ScrollRect.MovementType.Clamped;
                        scrollRect.scrollSensitivity = 30f;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(panelGo, path);
                PrefabUtility.UnloadPrefabContents(panelGo);
                Debug.Log("[P2] RestoreLevelsAndScroll: " + keepAssets.Count + " man + ScrollRect da fix");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[P2] LOI: " + ex);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Hoan thien bang MainMenu va layout hai panel chon bang Unity API.
        /// Khong sua truc tiep YAML cua StringTable/prefab.
        /// </summary>
        public static void AddViTableEntryViaAPI()
        {
            try
            {
                // Load the authored tables through AssetDatabase, then mutate them through
                // StringTable.AddEntry/GetEntry. The collection cache can be incomplete in
                // batchmode when Addressables has not initialized yet.
                var enTable = AssetDatabase.LoadAssetAtPath<StringTable>(
                    "Assets/RGame/RoguelikeKit/LocalizationFiles/AssetTables/MainMenu_en.asset");
                var viTable = AssetDatabase.LoadAssetAtPath<StringTable>(
                    "Assets/RGame/RoguelikeKit/LocalizationFiles/AssetTables/MainMenu_vi.asset");
                if (enTable == null || viTable == null) throw new Exception("Khong tim thay bang EN hoac VI");

                var values = new (string key, string en, string vi)[]
                {
                    ("Start", "START", "BẮT ĐẦU"),
                    ("QUIT", "QUIT", "THOÁT"),
                    ("OPTIONS", "OPTIONS", "TÙY CHỌN"),
                    ("POWERUP", "POWER UP", "NÂNG CẤP"),
                    ("CharacterSelection", "CHARACTER SELECTION", "CHỌN NHÂN VẬT"),
                    ("Confirm", "CONFIRM", "XÁC NHẬN"),
                    ("Back", "BACK", "QUAY LẠI")
                };
                foreach (var value in values)
                {
                    UpsertString(enTable, value.key, value.en);
                    UpsertString(viTable, value.key, value.vi);
                }
                EditorUtility.SetDirty(enTable.SharedData);
                EditorUtility.SetDirty(enTable);
                EditorUtility.SetDirty(viTable);

                FixSelectionPanelLayout(
                    "Assets/RGame/RoguelikeKit/Prefabs/UI/MainMenu/CharacterSelect_Panel.prefab",
                    "CharacterList", false);
                FixSelectionPanelLayout(
                    "Assets/RGame/RoguelikeKit/Prefabs/UI/MainMenu/LevelSelect_Panel .prefab",
                    "LevelList", true);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[P1API] MainMenu EN/VI + responsive layout da duoc luu bang Unity API");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[P1API] LOI: " + ex);
                EditorApplication.Exit(1);
            }
        }

        private static void UpsertString(StringTable table, string key, string value)
        {
            var entry = table.GetEntry(key) ?? table.AddEntry(key, value);
            entry.Value = value;
        }

        private static void FixSelectionPanelLayout(string path, string listName, bool isLevelPanel)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var button in root.GetComponentsInChildren<Button>(true))
                {
                    string name = button.name.Trim();
                    if (name != "ButtonBack" && name != "ButtonConfirm") continue;
                    bool back = name == "ButtonBack";
                    var rect = button.GetComponent<RectTransform>();
                    rect.anchorMin = rect.anchorMax = back ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
                    rect.pivot = back ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
                    rect.anchoredPosition = back ? new Vector2(44f, -34f) : new Vector2(-44f, -34f);
                    rect.sizeDelta = new Vector2(250f, 72f);
                }

                var list = FindChild(root.transform, listName) as RectTransform;
                if (list != null)
                {
                    list.anchorMin = isLevelPanel ? new Vector2(0.08f, 0.10f) : new Vector2(0.08f, 0.22f);
                    list.anchorMax = isLevelPanel ? new Vector2(0.92f, 0.82f) : new Vector2(0.92f, 0.78f);
                    list.pivot = new Vector2(0.5f, 0.5f);
                    list.offsetMin = Vector2.zero;
                    list.offsetMax = Vector2.zero;
                }

                if (isLevelPanel)
                {
                    var scroll = root.GetComponentInChildren<ScrollRect>(true);
                    if (scroll != null && scroll.content != null)
                    {
                        var layout = scroll.content.GetComponent<VerticalLayoutGroup>();
                        if (layout == null) layout = scroll.content.gameObject.AddComponent<VerticalLayoutGroup>();
                        layout.padding = new RectOffset(20, 20, 18, 18);
                        layout.spacing = 14f;
                        layout.childAlignment = TextAnchor.UpperCenter;
                        layout.childControlWidth = false;
                        layout.childControlHeight = false;
                        layout.childForceExpandWidth = false;
                        layout.childForceExpandHeight = false;
                        var fitter = scroll.content.GetComponent<ContentSizeFitter>();
                        if (fitter == null) fitter = scroll.content.gameObject.AddComponent<ContentSizeFitter>();
                        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                        scroll.horizontal = false;
                        scroll.vertical = true;
                        scroll.movementType = ScrollRect.MovementType.Clamped;
                        scroll.scrollSensitivity = 36f;
                    }
                }
                else if (list != null)
                {
                    var grid = list.GetComponent<GridLayoutGroup>();
                    if (grid != null)
                    {
                        grid.cellSize = new Vector2(220f, 220f);
                        grid.spacing = new Vector2(24f, 24f);
                        grid.childAlignment = TextAnchor.MiddleCenter;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name.Trim() == name) return child;
                var nested = FindChild(child, name);
                if (nested != null) return nested;
            }
            return null;
        }

    }
}
