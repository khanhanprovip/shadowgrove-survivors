using System;
using System.Linq;
using RGame.RoguelikeKit;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

internal static class ShadowgroveAudioAuthoring
{
    private const string AudioFolder = "Assets/Resources/ShadowgroveAudio";
    private const string MixerPath = "Assets/RGame/RoguelikeKit/Settings/Audio/DefaultAudioMixer.mixer";
    private const string RoutesPath = AudioFolder + "/ShadowgroveAudioMixerRoutes.asset";

    [MenuItem("Tools/Shadowgrove/Configure Audio Prototype")]
    private static void ConfigureAudioPrototype()
    {
        ConfigurePrototypeClipImporters();
        ConfigureMixerRoutes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Shadowgrove prototype SFX import and mixer routing are configured.");
    }

    private static void ConfigurePrototypeClipImporters()
    {
        string[] prototypeNames =
        {
            "Hero_Rowan_ThornSlash_SyntheticSketch.wav",
            "Hero_Lyra_DarkOrb_SyntheticSketch.wav",
            "Hero_Kael_Arrow_SyntheticSketch.wav",
            "Boss_EclipseWraith_SyntheticSketch.wav",
            "Boss_ThornRegent_SyntheticSketch.wav",
            "Boss_FrostMourner_SyntheticSketch.wav",
            "Boss_CrimsonHarvester_SyntheticSketch.wav",
            "Boss_AbyssOracle_SyntheticSketch.wav"
        };

        foreach (string name in prototypeNames)
        {
            string path = AudioFolder + "/" + name;
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
                throw new InvalidOperationException("Missing imported prototype WAV: " + path);

            importer.forceToMono = true;
            importer.loadInBackground = false;
            var sampleSettings = importer.defaultSampleSettings;
            sampleSettings.preloadAudioData = true;
            sampleSettings.loadType = AudioClipLoadType.DecompressOnLoad;
            sampleSettings.compressionFormat = AudioCompressionFormat.PCM;
            sampleSettings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = sampleSettings;
            importer.SaveAndReimport();
        }
    }

    private static void ConfigureMixerRoutes()
    {
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer == null)
            throw new InvalidOperationException("Could not load the existing audio mixer: " + MixerPath);

        var routes = AssetDatabase.LoadAssetAtPath<ShadowgroveAudioMixerRoutes>(RoutesPath);
        if (routes == null)
        {
            routes = ScriptableObject.CreateInstance<ShadowgroveAudioMixerRoutes>();
            AssetDatabase.CreateAsset(routes, RoutesPath);
        }

        routes.Music = FindGroup(mixer, "Music");
        routes.Sfx = FindGroup(mixer, "SFX");
        routes.Ui = FindGroup(mixer, "UI_SFX");
        EditorUtility.SetDirty(routes);
    }

    private static AudioMixerGroup FindGroup(AudioMixer mixer, string name)
    {
        var group = mixer.FindMatchingGroups("Master/" + name).FirstOrDefault(x => x.name == name)
            ?? mixer.FindMatchingGroups(name).FirstOrDefault(x => x.name == name);
        if (group == null)
            throw new InvalidOperationException("Could not find mixer group '" + name + "'.");
        return group;
    }
}
