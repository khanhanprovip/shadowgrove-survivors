using UnityEditor;
using UnityEngine;

public static class ConfigureLevelBackgrounds
{
    public static string Run()
    {
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/SGGround" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var spriteSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(spriteSettings);
            spriteSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(spriteSettings);
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.alphaIsTransparency = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            count++;
        }
        return "Configured terrain textures: " + count;
    }
}
