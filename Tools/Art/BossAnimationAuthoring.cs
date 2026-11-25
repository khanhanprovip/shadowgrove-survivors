using System;
using UnityEditor;
using UnityEngine;
using RGame.RoguelikeKit;

public static class BossAnimationAuthoring
{
    const string BossRoot = "Assets/Resources/SGBosses";
    const string MaterialFolder = BossRoot + "/Materials";
    const string MaterialPath = MaterialFolder + "/BossAnimatedSprite.mat";
    const string DissolvePath = BossRoot + "/BossDissolve.asset";
    const string ShaderPath = "Assets/RGame/RoguelikeKit/Scripts/Shader/BossAnimatedSprite.shader";
    const string LegacyBossMaterialPath = "Assets/RGame/RoguelikeKit/Art/Matrials/Enemy/Boss.mat";
    const string SourceDissolvePath = "Assets/RGame/RoguelikeKit/ScriptableObjects/LogicSO/Animation/DissolveAnimation.asset";
    const float BossDissolveDuration = 0.68f;

    static readonly string[] BossIds =
    {
        "EclipseWraith",
        "ThornRegent",
        "FrostMourner",
        "CrimsonHarvester",
        "AbyssOracle"
    };

    public static string Run()
    {
        AssetDatabase.ImportAsset(ShaderPath, ImportAssetOptions.ForceSynchronousImport);
        Shader shader = Shader.Find("RoguelikeKit/BossAnimatedSprite");
        if (shader == null)
            throw new InvalidOperationException("Boss animated sprite shader failed to import or compile.");

        EnsureFolder(MaterialFolder);
        Material material = LoadOrCreateMaterial(shader);
        DissolveAnimationSO dissolve = LoadOrCreateBossDissolve();
        int authored = 0;

        foreach (string id in BossIds)
        {
            string prefabPath = BossRoot + "/Prefabs/" + id + ".prefab";
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                SpriteRenderer renderer = FindBossRenderer(contents);
                if (renderer == null)
                    throw new InvalidOperationException("No boss SpriteRenderer found in " + prefabPath);

                BossVisualAnimation visual = contents.GetComponent<BossVisualAnimation>();
                if (visual == null)
                    visual = contents.AddComponent<BossVisualAnimation>();
                var visualObject = new SerializedObject(visual);
                SerializedProperty grid = visualObject.FindProperty("_gridSubdivisions");
                if (grid == null)
                    throw new InvalidOperationException("BossVisualAnimation grid field was not found.");
                grid.intValue = 24;
                visualObject.ApplyModifiedPropertiesWithoutUndo();

                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(renderer);
                EditorUtility.SetDirty(visual);

                EnemyHit hit = contents.GetComponentInChildren<EnemyHit>(true);
                if (hit == null)
                    throw new InvalidOperationException("No EnemyHit component found in " + prefabPath);
                var hitObject = new SerializedObject(hit);
                SerializedProperty spriteReference = hitObject.FindProperty("_spriteRenderers");
                SerializedProperty dissolveReference = hitObject.FindProperty("_dissolveAnimationSO");
                if (spriteReference == null || dissolveReference == null)
                    throw new InvalidOperationException("EnemyHit references were not found in " + prefabPath);
                spriteReference.objectReferenceValue = renderer;
                dissolveReference.objectReferenceValue = dissolve;
                hitObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(hit);

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                authored++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        AssetDatabase.SaveAssets();
        return "Authored " + authored + " boss prefabs with 24x24 runtime sprite meshes, " +
               "BossAnimatedSprite material, and " + BossDissolveDuration + "s boss dissolve.";
    }

    static Material LoadOrCreateMaterial(Shader shader)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "Boss Animated Sprite" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        Material source = AssetDatabase.LoadAssetAtPath<Material>(LegacyBossMaterialPath);
        if (source != null && source.HasProperty("_NoiseTex") && material.HasProperty("_NoiseTex"))
            material.SetTexture("_NoiseTex", source.GetTexture("_NoiseTex"));
        if (material.HasProperty("_BlendColor"))
            material.SetColor("_BlendColor", Color.clear);
        if (material.HasProperty("_Cutoff"))
            material.SetFloat("_Cutoff", 0f);
        if (material.HasProperty("_EdgeWidth"))
            material.SetFloat("_EdgeWidth", 0f);
        if (material.HasProperty("_ShadowColor"))
            material.SetColor("_ShadowColor", new Color(0f, 0f, 0f, 0.7f));

        EditorUtility.SetDirty(material);
        return material;
    }

    static DissolveAnimationSO LoadOrCreateBossDissolve()
    {
        DissolveAnimationSO dissolve = AssetDatabase.LoadAssetAtPath<DissolveAnimationSO>(DissolvePath);
        if (dissolve == null)
        {
            DissolveAnimationSO source = AssetDatabase.LoadAssetAtPath<DissolveAnimationSO>(SourceDissolvePath);
            if (source == null)
                throw new InvalidOperationException("The shared dissolve configuration was not found.");
            dissolve = UnityEngine.Object.Instantiate(source);
            dissolve.name = "Boss Dissolve Animation";
            AssetDatabase.CreateAsset(dissolve, DissolvePath);
        }

        var dissolveObject = new SerializedObject(dissolve);
        SerializedProperty duration = dissolveObject.FindProperty("_animationDuration");
        if (duration == null)
            throw new InvalidOperationException("Dissolve duration field was not found.");
        duration.floatValue = BossDissolveDuration;
        dissolveObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(dissolve);
        return dissolve;
    }

    static SpriteRenderer FindBossRenderer(GameObject root)
    {
        SpriteRenderer selected = null;
        float largestArea = -1f;
        foreach (SpriteRenderer candidate in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (candidate.sprite == null || candidate.name.IndexOf("shadow", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;
            Vector2 size = candidate.sprite.bounds.size;
            float area = size.x * size.y;
            if (area > largestArea)
            {
                selected = candidate;
                largestArea = area;
            }
        }
        return selected;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
