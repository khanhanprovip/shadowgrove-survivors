using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using RGame.RoguelikeKit;

public static class BossFrameAnimationAuthoring
{
    const string BossRoot = "Assets/Resources/SGBosses";
    const string SheetFolder = BossRoot + "/AnimationSheets";
    const string LocomotionFolder = SheetFolder + "/Locomotion";
    const string CombatFolder = SheetFolder + "/Combat";
    const string ClipFolder = BossRoot + "/AnimationClips";
    const string ControllerFolder = BossRoot + "/AnimationControllers";
    const string ConfigFolder = BossRoot + "/AnimationConfigs";
    const string MaterialFolder = BossRoot + "/Materials";
    const string FrameMaterialPath = MaterialFolder + "/BossFrameSprite.mat";
    const string FrameShaderPath = "Assets/RGame/RoguelikeKit/Scripts/Shader/BossFrameSprite.shader";
    const string LegacySpriteMaterialPath = "Assets/RGame/RoguelikeKit/Art/Matrials/Enemy/Boss.mat";
    const string BossHitPath = ConfigFolder + "/BossHitAnimation.asset";
    const string BossDissolvePath = BossRoot + "/BossDissolve.asset";
    const string DefaultDissolvePath = "Assets/RGame/RoguelikeKit/ScriptableObjects/LogicSO/Animation/DissolveAnimation.asset";
    const string PrefabFolder = BossRoot + "/Prefabs";
    const int GridColumns = 4;
    const int GridRows = 4;
    const byte VisibleAlphaThreshold = 128;
    const float TargetBossWorldHeight = 122f / 60f * 5f;
    const float BossDissolveDuration = 0.68f;

    static readonly string[] BossIds =
    {
        "EclipseWraith",
        "ThornRegent",
        "FrostMourner",
        "CrimsonHarvester",
        "AbyssOracle"
    };

    sealed class SheetPlan
    {
        public string Id;
        public string AssetPath;
        public string SpriteNamePrefix;
        public int Width;
        public int Height;
        public RectInt[] Cells;
        public float PixelsPerUnit;
        public float PivotPixels;
    }

    sealed class ClipSpec
    {
        public BossMotion Motion;
        public int FirstFrame;
        public int FrameCount;
        public float Duration;
        public bool Loop;
        public float[] KeyFractions;
    }

    public static string Run()
    {
        EnsureFolder(LocomotionFolder);
        EnsureFolder(CombatFolder);
        EnsureFolder(ClipFolder);
        EnsureFolder(ControllerFolder);
        EnsureFolder(ConfigFolder);
        EnsureFolder(MaterialFolder);

        Material legacyBossMaterial = AssetDatabase.LoadAssetAtPath<Material>(LegacySpriteMaterialPath);
        if (legacyBossMaterial == null || legacyBossMaterial.shader == null || legacyBossMaterial.shader.name != "RoguelikeKit/Enemy")
            throw new InvalidOperationException("Expected the existing RoguelikeKit/Enemy Boss.mat asset.");

        // Preflight all ten generated sheets and the retained old hit/death frames
        // before changing animation clips, controllers, or prefabs.
        var locomotionPlans = new SheetPlan[BossIds.Length];
        var combatPlans = new SheetPlan[BossIds.Length];
        var legacyHitDeathFrames = new Dictionary<string, Sprite[]>(StringComparer.Ordinal);
        var providers = new SpriteDataProviderFactories();
        providers.Init();
        foreach (string id in BossIds)
        {
            RequireSheetFile(LocomotionFolder + "/" + id + ".png");
            RequireSheetFile(CombatFolder + "/" + id + ".png");
            legacyHitDeathFrames.Add(id, LoadLegacyHitDeathFrames(id));
        }
        for (int i = 0; i < BossIds.Length; i++)
        {
            string id = BossIds[i];
            locomotionPlans[i] = PreflightSheet(id, LocomotionFolder + "/" + id + ".png",
                id + "_Locomotion", providers, null);
        }
        for (int i = 0; i < BossIds.Length; i++)
        {
            string id = BossIds[i];
            combatPlans[i] = PreflightSheet(id, CombatFolder + "/" + id + ".png",
                id + "_Combat", providers, locomotionPlans[i]);
        }

        var locomotionSprites = new Dictionary<string, Sprite[]>(StringComparer.Ordinal);
        var combatSprites = new Dictionary<string, Sprite[]>(StringComparer.Ordinal);
        foreach (SheetPlan plan in locomotionPlans)
            locomotionSprites.Add(plan.Id, ImportSheet(plan, providers));
        foreach (SheetPlan plan in combatPlans)
            combatSprites.Add(plan.Id, ImportSheet(plan, providers));

        var frameBanks = new Dictionary<string, Sprite[]>(StringComparer.Ordinal);
        foreach (string id in BossIds)
            frameBanks.Add(id, AssembleFrameBank(locomotionSprites[id], combatSprites[id], legacyHitDeathFrames[id]));

        Shader frameShader = LoadFrameShader();
        Material bossMaterial = LoadOrCreateFrameMaterial(legacyBossMaterial, frameShader);
        HitAnimationSO bossHit = LoadOrCreateBossHit();
        DissolveAnimationSO bossDissolve = LoadOrCreateBossDissolve();

        int clipCount = 0;
        int prefabCount = 0;
        foreach (string id in BossIds)
        {
            Sprite[] sprites = frameBanks[id];
            ClipSpec[] clipSpecs = BuildClipSpecs(id);
            AnimationClip[] clips = CreateClips(id, sprites, clipSpecs);
            AnimatorController controller = CreateController(id, clips, clipSpecs);
            clipCount += clips.Length;
            AuthorPrefab(id, sprites, clips, controller, bossMaterial, bossHit, bossDissolve);
            prefabCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return "Authored " + prefabCount + " boss prefabs, " + clipCount +
               " state clips, 5 six-state Animator Controllers, and 10 sliced locomotion/combat sheets.";
    }

    static Shader LoadFrameShader()
    {
        AssetDatabase.ImportAsset(FrameShaderPath, ImportAssetOptions.ForceSynchronousImport);
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(FrameShaderPath);
        if (shader == null)
            throw new InvalidOperationException("Boss frame sprite shader is missing or failed to import: " + FrameShaderPath);
        if (ShaderUtil.ShaderHasError(shader))
            throw new InvalidOperationException("Boss frame sprite shader has compiler errors: " + FrameShaderPath);
        return shader;
    }

    static Material LoadOrCreateFrameMaterial(Material legacy, Shader shader)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(FrameMaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "Boss Frame Sprite" };
            AssetDatabase.CreateAsset(material, FrameMaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        CopyProperty(legacy, material, "_Color");
        CopyProperty(legacy, material, "_BlendColor");
        CopyProperty(legacy, material, "_Cutoff");
        CopyProperty(legacy, material, "_EdgeWidth");
        CopyProperty(legacy, material, "_EdgeColor");
        CopyTexture(legacy, material, "_NoiseTex");

        Vector4 shadowOffset = legacy.HasProperty("_ShadowOffset")
            ? legacy.GetVector("_ShadowOffset")
            : new Vector4(0f, -1.06f, 0f, 0f);
        material.SetVector("_ShadowOffset", shadowOffset);
        material.SetColor("_ShadowColor", new Color(0f, 0f, 0f, 0.32f));

        Vector4 shadowScale = legacy.HasProperty("_ShadowScale")
            ? legacy.GetVector("_ShadowScale")
            : new Vector4(1f, 0.12f, 1f, 1f);
        shadowScale.y = 0.12f;
        material.SetVector("_ShadowScale", shadowScale);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void CopyProperty(Material source, Material destination, string property)
    {
        if (source.HasProperty(property) && destination.HasProperty(property))
        {
            if (property == "_Color" || property == "_BlendColor" || property == "_EdgeColor")
                destination.SetColor(property, source.GetColor(property));
            else
                destination.SetFloat(property, source.GetFloat(property));
        }
    }

    static void CopyTexture(Material source, Material destination, string property)
    {
        if (!source.HasProperty(property) || !destination.HasProperty(property))
            throw new InvalidOperationException("Required boss dissolve texture property is missing: " + property);
        Texture texture = source.GetTexture(property);
        if (texture == null)
            throw new InvalidOperationException("Legacy Boss.mat has no dissolve noise texture assigned.");
        destination.SetTexture(property, texture);
        destination.SetTextureScale(property, source.GetTextureScale(property));
        destination.SetTextureOffset(property, source.GetTextureOffset(property));
    }

    static void RequireSheetFile(string path)
    {
        if (!File.Exists(FullAssetPath(path)))
            throw new FileNotFoundException("Boss animation sheet is not ready: " + path, path);
    }

    static SheetPlan PreflightSheet(string id, string path, string spriteNamePrefix,
        SpriteDataProviderFactories factory, SheetPlan referencePlan)
    {
        if (!File.Exists(FullAssetPath(path)))
            throw new FileNotFoundException("Boss animation sheet is not ready: " + path, path);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("Animation sheet is not using TextureImporter: " + path);

        // Sprite sheets must be imported as Sprites before the package exposes its data provider.
        importer.textureType = TextureImporterType.Sprite;
        importer.isReadable = true;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        int sourceWidth;
        int sourceHeight;
        var initialProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (initialProvider == null)
            throw new InvalidOperationException("Sprite data provider is unavailable for " + path);
        initialProvider.InitSpriteEditorDataProvider();
        var initialTextureData = initialProvider.GetDataProvider<ITextureDataProvider>();
        if (initialTextureData == null)
            throw new InvalidOperationException("Texture data provider is unavailable for " + path);
        initialTextureData.GetTextureActualWidthAndHeight(out sourceWidth, out sourceHeight);

        importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, Mathf.NextPowerOfTwo(Mathf.Max(sourceWidth, sourceHeight)));
        importer.SaveAndReimport();

        importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("Texture importer disappeared after reimport: " + path);
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (dataProvider == null)
            throw new InvalidOperationException("Sprite data provider is unavailable for " + path);
        dataProvider.InitSpriteEditorDataProvider();
        CheckCapabilities(dataProvider, path);

        var textureData = dataProvider.GetDataProvider<ITextureDataProvider>();
        if (textureData == null)
            throw new InvalidOperationException("Texture data provider is unavailable for " + path);
        textureData.GetTextureActualWidthAndHeight(out sourceWidth, out sourceHeight);
        if (sourceWidth < GridColumns || sourceHeight < GridRows)
            throw new InvalidOperationException("Animation sheet is too small for a 4x4 grid: " + path);

        Texture2D readable = textureData.GetReadableTexture2D();
        if (readable == null || readable.width != sourceWidth || readable.height != sourceHeight)
            throw new InvalidOperationException("Readable texture does not match original sheet dimensions: " + path);

        Color32[] pixels = readable.GetPixels32();
        RectInt[] cells = ComputeCellRects(sourceWidth, sourceHeight);
        int pivotMinY = int.MaxValue;
        int pivotMaxY = -1;
        for (int frame = 0; frame < GridColumns * GridRows; frame++)
        {
            RectInt cell = cells[frame];
            int visiblePixels = 0;
            for (int y = cell.y; y < cell.yMax; y++)
            {
                for (int x = cell.x; x < cell.xMax; x++)
                {
                    if (pixels[y * sourceWidth + x].a < VisibleAlphaThreshold)
                        continue;
                    visiblePixels++;
                    if (frame == 0)
                    {
                        pivotMinY = Mathf.Min(pivotMinY, y);
                        pivotMaxY = Mathf.Max(pivotMaxY, y);
                    }
                }
            }
            if (visiblePixels == 0)
                throw new InvalidOperationException("Animation frame " + frame + " is fully transparent in " + path);
        }

        if (pivotMaxY < pivotMinY)
            throw new InvalidOperationException("First pose has no visible alpha in " + path);
        float pixelsPerUnit;
        if (referencePlan == null)
        {
            int alphaHeight = pivotMaxY - pivotMinY + 1;
            pixelsPerUnit = alphaHeight / TargetBossWorldHeight;
        }
        else
        {
            pixelsPerUnit = referencePlan.PixelsPerUnit * cells[0].height / referencePlan.Cells[0].height;
        }
        if (pixelsPerUnit < 1f || float.IsNaN(pixelsPerUnit) || float.IsInfinity(pixelsPerUnit))
            throw new InvalidOperationException("Invalid calibrated pixels-per-unit for " + path);

        return new SheetPlan
        {
            Id = id,
            AssetPath = path,
            SpriteNamePrefix = spriteNamePrefix,
            Width = sourceWidth,
            Height = sourceHeight,
            Cells = cells,
            PixelsPerUnit = pixelsPerUnit,
            PivotPixels = pivotMinY - cells[0].y
        };
    }

    static RectInt[] ComputeCellRects(int width, int height)
    {
        var cells = new RectInt[GridColumns * GridRows];
        for (int frame = 0; frame < cells.Length; frame++)
        {
            int topRow = frame / GridColumns;
            int column = frame % GridColumns;
            int rowFromBottom = GridRows - topRow - 1;
            int x0 = Mathf.RoundToInt(column * width / (float)GridColumns);
            int x1 = Mathf.RoundToInt((column + 1) * width / (float)GridColumns);
            int y0 = Mathf.RoundToInt(rowFromBottom * height / (float)GridRows);
            int y1 = Mathf.RoundToInt((rowFromBottom + 1) * height / (float)GridRows);
            cells[frame] = new RectInt(x0, y0, x1 - x0, y1 - y0);
        }
        return cells;
    }

    static Sprite[] LoadLegacyHitDeathFrames(string id)
    {
        string path = SheetFolder + "/" + id + ".png";
        if (!File.Exists(FullAssetPath(path)))
            throw new FileNotFoundException("Legacy boss sheet for Hit/Death is missing: " + path, path);

        var spritesByName = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .ToDictionary(sprite => sprite.name, sprite => sprite, StringComparer.Ordinal);
        var frames = new Sprite[4];
        for (int i = 0; i < frames.Length; i++)
        {
            int oldFrame = 12 + i;
            string name = FrameName(id, oldFrame);
            if (!spritesByName.TryGetValue(name, out frames[i]))
                throw new InvalidOperationException("Retained legacy Hit/Death sprite is missing: " + name + " in " + path);
        }
        return frames;
    }

    static Sprite[] AssembleFrameBank(Sprite[] locomotion, Sprite[] combat, Sprite[] legacyHitDeath)
    {
        if (locomotion == null || locomotion.Length != 16 || combat == null || combat.Length != 16 ||
            legacyHitDeath == null || legacyHitDeath.Length != 4)
            throw new InvalidOperationException("Each boss requires 16 locomotion, 16 combat, and 4 retained Hit/Death sprites.");

        var frames = new Sprite[36];
        Array.Copy(locomotion, 0, frames, 0, locomotion.Length);
        Array.Copy(combat, 0, frames, 16, combat.Length);
        Array.Copy(legacyHitDeath, 0, frames, 32, legacyHitDeath.Length);
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] == null)
                throw new InvalidOperationException("Boss frame bank contains a missing sprite at index " + i + ".");
        }
        return frames;
    }

    static void CheckCapabilities(ISpriteEditorDataProvider dataProvider, string path)
    {
        var editProvider = dataProvider.GetDataProvider<ISpriteFrameEditCapability>();
        if (editProvider == null)
            throw new InvalidOperationException("Sprite frame edit capability is unavailable; aborted: " + path);
        EditCapability capability = editProvider.GetEditCapability();
        EEditCapability[] required =
        {
            EEditCapability.CreateAndDeleteSprite,
            EEditCapability.EditSpriteName,
            EEditCapability.EditSpriteRect,
            EEditCapability.EditPivot
        };
        foreach (EEditCapability item in required)
        {
            if (!capability.HasCapability(item))
                throw new InvalidOperationException("Missing sprite editing capability " + item + "; aborted: " + path);
        }

        if (dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>() == null)
            throw new InvalidOperationException("Sprite name/file ID provider is unavailable; aborted: " + path);
    }

    static Sprite[] ImportSheet(SheetPlan plan, SpriteDataProviderFactories factory)
    {
        var importer = AssetImporter.GetAtPath(plan.AssetPath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("Texture importer is unavailable for " + plan.AssetPath);
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = plan.PixelsPerUnit;
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (dataProvider == null)
            throw new InvalidOperationException("Sprite data provider is unavailable for " + plan.AssetPath);
        dataProvider.InitSpriteEditorDataProvider();
        CheckCapabilities(dataProvider, plan.AssetPath);

        SpriteRect[] currentRects = dataProvider.GetSpriteRects();
        var idsByName = new Dictionary<string, GUID>(StringComparer.Ordinal);
        foreach (SpriteRect current in currentRects)
        {
            if (!idsByName.ContainsKey(current.name))
                idsByName.Add(current.name, current.spriteID);
        }

        var rects = new SpriteRect[GridColumns * GridRows];
        var nameFileIds = new List<SpriteNameFileIdPair>(rects.Length);
        for (int frame = 0; frame < rects.Length; frame++)
        {
            RectInt cell = plan.Cells[frame];
            string name = FrameName(plan.SpriteNamePrefix, frame);
            GUID spriteId = idsByName.TryGetValue(name, out GUID existingId) ? existingId : GUID.Generate();
            rects[frame] = new SpriteRect
            {
                name = name,
                spriteID = spriteId,
                rect = new Rect(cell.x, cell.y, cell.width, cell.height),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, plan.PivotPixels / cell.height),
                border = Vector4.zero
            };
            nameFileIds.Add(new SpriteNameFileIdPair(name, spriteId));
        }

        dataProvider.SetSpriteRects(rects);
        var nameProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nameProvider.SetNameFileIdPairs(nameFileIds);
        dataProvider.Apply();
        importer.SaveAndReimport();
        AssetDatabase.ImportAsset(plan.AssetPath, ImportAssetOptions.ForceSynchronousImport);

        var spritesByName = AssetDatabase.LoadAllAssetsAtPath(plan.AssetPath)
            .OfType<Sprite>()
            .ToDictionary(sprite => sprite.name, sprite => sprite, StringComparer.Ordinal);
        var sprites = new Sprite[GridColumns * GridRows];
        for (int frame = 0; frame < sprites.Length; frame++)
        {
            string name = FrameName(plan.SpriteNamePrefix, frame);
            if (!spritesByName.TryGetValue(name, out sprites[frame]))
                throw new InvalidOperationException("Imported sprite is missing: " + name + " in " + plan.AssetPath);
        }
        return sprites;
    }

    static ClipSpec[] BuildClipSpecs(string id)
    {
        float idleCycle;
        float moveCycle;
        float idleEase;
        float moveEase;
        float windupEase;
        float attackEase;
        float hitSecondFrame;
        float deathSecondFrame;

        // Distinct timing gives each pose sequence a different cadence: a drifting
        // wisp, weighty roots, an airy queen, a quick executioner, and a pulsing oracle.
        switch (id)
        {
            case "EclipseWraith":
                idleCycle = 1.40f;
                moveCycle = 0.96f;
                idleEase = 0.12f;
                moveEase = 0.05f;
                windupEase = 0.18f;
                attackEase = 0.28f;
                hitSecondFrame = 0.30f;
                deathSecondFrame = 0.60f;
                break;
            case "ThornRegent":
                idleCycle = 1.80f;
                moveCycle = 1.20f;
                idleEase = 0.25f;
                moveEase = 0.32f;
                windupEase = 0.35f;
                attackEase = 0.30f;
                hitSecondFrame = 0.48f;
                deathSecondFrame = 0.78f;
                break;
            case "FrostMourner":
                idleCycle = 1.10f;
                moveCycle = 0.80f;
                idleEase = 0.05f;
                moveEase = 0.12f;
                windupEase = 0.10f;
                attackEase = 0.18f;
                hitSecondFrame = 0.40f;
                deathSecondFrame = 0.55f;
                break;
            case "CrimsonHarvester":
                idleCycle = 1.00f;
                moveCycle = 0.64f;
                idleEase = 0f;
                moveEase = 0f;
                windupEase = 0f;
                attackEase = 0.12f;
                hitSecondFrame = 0.25f;
                deathSecondFrame = 0.40f;
                break;
            case "AbyssOracle":
                idleCycle = 1.50f;
                moveCycle = 1.04f;
                idleEase = 0.18f;
                moveEase = 0.15f;
                windupEase = 0.22f;
                attackEase = 0.24f;
                hitSecondFrame = 0.52f;
                deathSecondFrame = 0.69f;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown boss animation profile.");
        }

        return new[]
        {
            new ClipSpec { Motion = BossMotion.Idle, FirstFrame = 0, FrameCount = 8, Duration = idleCycle, Loop = true, KeyFractions = BuildEightFrameFractions(idleEase) },
            new ClipSpec { Motion = BossMotion.Move, FirstFrame = 8, FrameCount = 8, Duration = moveCycle, Loop = true, KeyFractions = BuildEightFrameFractions(moveEase) },
            new ClipSpec { Motion = BossMotion.Windup, FirstFrame = 16, FrameCount = 8, Duration = 1.1f, Loop = false, KeyFractions = BuildEightFrameFractions(windupEase) },
            new ClipSpec { Motion = BossMotion.Attack, FirstFrame = 24, FrameCount = 8, Duration = 0.9f, Loop = false, KeyFractions = BuildEightFrameFractions(attackEase) },
            new ClipSpec { Motion = BossMotion.Hit, FirstFrame = 32, FrameCount = 2, Duration = 0.18f, Loop = false, KeyFractions = new[] { 0f, hitSecondFrame } },
            new ClipSpec { Motion = BossMotion.Death, FirstFrame = 34, FrameCount = 2, Duration = BossDissolveDuration, Loop = false, KeyFractions = new[] { 0f, deathSecondFrame } }
        };
    }

    static float[] BuildEightFrameFractions(float ease)
    {
        const int frameCount = 8;
        const float lastFrameStart = 0.875f;
        var fractions = new float[frameCount];
        for (int frame = 0; frame < frameCount; frame++)
        {
            float linear = frame / (float)(frameCount - 1);
            float smoothed = Mathf.SmoothStep(0f, 1f, linear);
            fractions[frame] = Mathf.Lerp(linear, smoothed, ease) * lastFrameStart;
        }
        return fractions;
    }

    static AnimationClip[] CreateClips(string id, Sprite[] sprites, ClipSpec[] clipSpecs)
    {
        var clips = new AnimationClip[clipSpecs.Length];
        for (int i = 0; i < clipSpecs.Length; i++)
        {
            ClipSpec spec = clipSpecs[i];
            ValidateClipSpec(spec);
            string name = id + "_" + spec.Motion;
            string path = ClipFolder + "/" + name + ".anim";
            var clip = new AnimationClip
            {
                name = name,
                frameRate = spec.FrameCount / spec.Duration,
                wrapMode = spec.Loop ? WrapMode.Loop : WrapMode.ClampForever
            };

            var frames = new ObjectReferenceKeyframe[spec.FrameCount + (spec.Loop ? 1 : 0)];
            for (int frame = 0; frame < spec.FrameCount; frame++)
            {
                frames[frame] = new ObjectReferenceKeyframe
                {
                    time = spec.KeyFractions[frame] * spec.Duration,
                    value = sprites[spec.FirstFrame + frame]
                };
            }
            if (spec.Loop)
            {
                frames[spec.FrameCount] = new ObjectReferenceKeyframe
                {
                    time = spec.Duration,
                    value = sprites[spec.FirstFrame]
                };
            }

            string rendererPath = string.Empty;
            // The authoring prefab places its SpriteRenderer at the root. Empty path is the
            // serialized m_Sprite binding and keeps the controller independent of object names.
            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(rendererPath, typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.startTime = 0f;
            settings.stopTime = spec.Duration;
            settings.loopTime = spec.Loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(clip, existing);
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(clip);
                clips[i] = existing;
            }
            else
            {
                AssetDatabase.CreateAsset(clip, path);
                clips[i] = clip;
            }
        }
        return clips;
    }

    static void ValidateClipSpec(ClipSpec spec)
    {
        if (spec == null || spec.FrameCount <= 0 || spec.Duration <= 0f ||
            spec.KeyFractions == null || spec.KeyFractions.Length != spec.FrameCount)
            throw new InvalidOperationException("Invalid boss animation clip specification.");

        float previous = -1f;
        foreach (float fraction in spec.KeyFractions)
        {
            if (fraction < 0f || fraction >= 1f || fraction <= previous)
                throw new InvalidOperationException("Clip key fractions must increase within [0, 1): " + spec.Motion);
            previous = fraction;
        }
        if (spec.KeyFractions[0] != 0f)
            throw new InvalidOperationException("Every boss animation clip must begin at frame zero: " + spec.Motion);
    }

    static AnimatorController CreateController(string id, AnimationClip[] clips, ClipSpec[] clipSpecs)
    {
        string path = ControllerFolder + "/" + id + ".controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        if (controller == null)
            throw new InvalidOperationException("Could not create AnimatorController: " + path);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        var stateNames = new HashSet<string>(clipSpecs.Select(spec => spec.Motion.ToString()), StringComparer.Ordinal);
        foreach (ChildAnimatorState child in machine.states)
        {
            if (!stateNames.Contains(child.state.name))
                machine.RemoveState(child.state);
        }

        AnimatorState idle = null;
        for (int i = 0; i < clipSpecs.Length; i++)
        {
            string stateName = clipSpecs[i].Motion.ToString();
            AnimatorState state = machine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == stateName);
            if (state == null)
                state = machine.AddState(stateName, new Vector3(250f + i * 240f, 100f, 0f));
            state.motion = clips[i];
            state.speed = 1f;
            if (clipSpecs[i].Motion == BossMotion.Idle)
                idle = state;
        }
        machine.defaultState = idle;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    static HitAnimationSO LoadOrCreateBossHit()
    {
        HitAnimationSO hit = AssetDatabase.LoadAssetAtPath<HitAnimationSO>(BossHitPath);
        if (hit == null)
        {
            HitAnimationSO source = ReadBossHitSource();
            hit = UnityEngine.Object.Instantiate(source);
            hit.name = "Boss Hit Animation";
            AssetDatabase.CreateAsset(hit, BossHitPath);
        }

        var serialized = new SerializedObject(hit);
        RequireProperty(serialized, "mHitDuration").floatValue = 0.05f;
        RequireProperty(serialized, "mFadeDuration").floatValue = 0.12f;
        RequireProperty(serialized, "mHitColor").colorValue = new Color(0.12f, 0.015f, 0.015f, 0f);
        RequireProperty(serialized, "mNormalColor").colorValue = Color.clear;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(hit);
        return hit;
    }

    static DissolveAnimationSO LoadOrCreateBossDissolve()
    {
        DissolveAnimationSO dissolve = AssetDatabase.LoadAssetAtPath<DissolveAnimationSO>(BossDissolvePath);
        if (dissolve == null)
        {
            DissolveAnimationSO source = ReadBossDissolveSource();
            dissolve = UnityEngine.Object.Instantiate(source);
            dissolve.name = "Boss Dissolve Animation";
            AssetDatabase.CreateAsset(dissolve, BossDissolvePath);
        }

        var serialized = new SerializedObject(dissolve);
        RequireProperty(serialized, "_animationDuration").floatValue = BossDissolveDuration;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(dissolve);
        return dissolve;
    }

    static HitAnimationSO ReadBossHitSource()
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(PrefabFolder + "/EclipseWraith.prefab");
        try
        {
            EnemyHit enemyHit = contents.GetComponentInChildren<EnemyHit>(true);
            if (enemyHit == null)
                throw new InvalidOperationException("EclipseWraith has no EnemyHit component.");
            var serialized = new SerializedObject(enemyHit);
            HitAnimationSO source = RequireProperty(serialized, "_hitAnimationSO").objectReferenceValue as HitAnimationSO;
            if (source == null)
                throw new InvalidOperationException("Boss hit animation source is missing.");
            return source;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    static DissolveAnimationSO ReadBossDissolveSource()
    {
        DissolveAnimationSO existing = AssetDatabase.LoadAssetAtPath<DissolveAnimationSO>(DefaultDissolvePath);
        if (existing != null)
            return existing;

        GameObject contents = PrefabUtility.LoadPrefabContents(PrefabFolder + "/EclipseWraith.prefab");
        try
        {
            EnemyHit enemyHit = contents.GetComponentInChildren<EnemyHit>(true);
            if (enemyHit == null)
                throw new InvalidOperationException("EclipseWraith has no EnemyHit component.");
            var serialized = new SerializedObject(enemyHit);
            DissolveAnimationSO source = RequireProperty(serialized, "_dissolveAnimationSO").objectReferenceValue as DissolveAnimationSO;
            if (source == null)
                throw new InvalidOperationException("Boss dissolve animation source is missing.");
            return source;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    static void AuthorPrefab(string id, Sprite[] sprites, AnimationClip[] clips,
        AnimatorController controller, Material bossMaterial, HitAnimationSO bossHit,
        DissolveAnimationSO bossDissolve)
    {
        string prefabPath = PrefabFolder + "/" + id + ".prefab";
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            SpriteRenderer renderer = FindBossRenderer(contents);
            if (renderer == null)
                throw new InvalidOperationException("No boss SpriteRenderer found in " + prefabPath);
            if (renderer.transform != contents.transform)
                throw new InvalidOperationException("Boss SpriteRenderer must remain on the prefab root for frame clips: " + prefabPath);

            Animator animator = contents.GetComponent<Animator>();
            if (animator == null)
                animator = contents.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;
            animator.speed = 1f;
            EditorUtility.SetDirty(animator);

            renderer.sprite = sprites[0];
            renderer.sharedMaterial = bossMaterial;
            EditorUtility.SetDirty(renderer);

            BossVisualAnimation visual = contents.GetComponent<BossVisualAnimation>();
            if (visual == null)
                visual = contents.AddComponent<BossVisualAnimation>();
            var visualObject = new SerializedObject(visual);
            RequireProperty(visualObject, "_animatedRenderer").objectReferenceValue = renderer;
            RequireProperty(visualObject, "_animator").objectReferenceValue = animator;
            RequireProperty(visualObject, "_standingSprite").objectReferenceValue = sprites[0];
            SetObjectArray(RequireProperty(visualObject, "_frames"), sprites);
            SetObjectArray(RequireProperty(visualObject, "_stateClips"), clips);
            visualObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(visual);

            EnemyHit hit = contents.GetComponentInChildren<EnemyHit>(true);
            if (hit == null)
                throw new InvalidOperationException("No EnemyHit component found in " + prefabPath);
            var hitObject = new SerializedObject(hit);
            RequireProperty(hitObject, "_spriteRenderers").objectReferenceValue = renderer;
            RequireProperty(hitObject, "_hitAnimationSO").objectReferenceValue = bossHit;
            RequireProperty(hitObject, "_dissolveAnimationSO").objectReferenceValue = bossDissolve;
            hitObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hit);

            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
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

    static string FrameName(string id, int index)
    {
        return id + "_" + index.ToString("00");
    }

    static string FullAssetPath(string assetPath)
    {
        const string assetPrefix = "Assets/";
        if (!assetPath.StartsWith(assetPrefix, StringComparison.Ordinal))
            throw new ArgumentException("Expected an Assets/ path: " + assetPath, nameof(assetPath));
        return Path.Combine(Application.dataPath, assetPath.Substring(assetPrefix.Length));
    }

    static SerializedProperty RequireProperty(SerializedObject serialized, string name)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null)
            throw new InvalidOperationException("Serialized property was not found: " + name);
        return property;
    }

    static void SetObjectArray(SerializedProperty array, UnityEngine.Object[] values)
    {
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
