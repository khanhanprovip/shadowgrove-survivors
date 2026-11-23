using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Executed in-memory through Pipeline; never compiled into the game.
public static class VerifyAuthoring
{
    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring verification.");
        string folder = "Assets/Editor/HarnessProbe_" + Guid.NewGuid().ToString("N");
        string prefabPath = folder + "/Probe.prefab";
        string scenePath = folder + "/Probe.unity";
        Scene original = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(original.path) || original.isDirty)
            throw new InvalidOperationException("Open a saved scene with no unsaved edits before this probe.");
        Scene probe = default;
        try
        {
            AssetDatabase.CreateFolder("Assets/Editor", folder.Substring("Assets/Editor/".Length));
            probe = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var go = new GameObject("Harness2DProbe", typeof(SpriteRenderer), typeof(BoxCollider2D));
            SceneManager.MoveGameObjectToScene(go, probe);
            go.transform.localScale = new Vector3(2, 3, 1);
            go.GetComponent<BoxCollider2D>().size = new Vector2(1.25f, 2.5f);
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                root.GetComponent<BoxCollider2D>().size = new Vector2(2.5f, 4);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceSynchronousImport);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab.GetComponent<BoxCollider2D>().size != new Vector2(2.5f, 4))
                throw new Exception("Prefab change did not survive reload.");
            PrefabUtility.InstantiatePrefab(prefab, probe);
            if (!EditorSceneManager.SaveScene(probe, scenePath)) throw new Exception("Scene save failed.");
            EditorSceneManager.CloseScene(probe, true);
            probe = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            var restored = probe.GetRootGameObjects();
            if (restored.Length != 1 || restored[0].transform.localScale != new Vector3(2, 3, 1) ||
                restored[0].GetComponent<BoxCollider2D>().size != new Vector2(2.5f, 4))
                throw new Exception("Scene/prefab state did not survive reopen.");
            return "PASS: created 2D prefab; edited collider; saved and reloaded prefab and additive scene; verified scale and collider.";
        }
        finally
        {
            if (probe.IsValid() && probe.isLoaded) EditorSceneManager.CloseScene(probe, true);
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            AssetDatabase.DeleteAsset(folder);
        }
    }
}
