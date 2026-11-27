using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using RGame.RoguelikeKit;

// Run from the Unity Editor after opening the project. The runtime dictionary
// remains a fallback; this authors the same keys into Shadowgrove's tables.
public static class HeroLocalizationAuthoring
{
    [MenuItem("Tools/Shadowgrove/Author Hero Localization")]
    public static void Run()
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection("Shadowgrove");
        const string folder="Assets/RGame/RoguelikeKit/LocalizationFiles/AssetTables/";
        var english = collection!=null?collection.GetTable(new LocaleIdentifier("en")) as StringTable:AssetDatabase.LoadAssetAtPath<StringTable>(folder+"Shadowgrove_en.asset");
        var vietnamese = collection!=null?collection.GetTable(new LocaleIdentifier("vi")) as StringTable:AssetDatabase.LoadAssetAtPath<StringTable>(folder+"Shadowgrove_vi.asset");
        if (english == null || vietnamese == null)
            throw new InvalidOperationException("Shadowgrove must have both en and vi string tables");

        var sharedData=english.SharedData;
        if(sharedData==null)
        {
            // Preserve the table GUID and every existing key ID while replacing
            // the legacy asset's missing MonoScript through the Unity asset API.
            string path=folder+"Shadowgrove Shared Data.asset";
            string source=File.ReadAllText(path);
            string guid=AssetDatabase.AssetPathToGUID(path);
            sharedData=ScriptableObject.CreateInstance<SharedTableData>();
            sharedData.TableCollectionName="Shadowgrove";
            foreach(Match match in Regex.Matches(source,@"- m_Id: (\d+)\s+m_Key: ([^\r\n]+)"))
                sharedData.AddKey(match.Groups[2].Value.Trim(),long.Parse(match.Groups[1].Value));
            if(sharedData.Entries.Count<10)throw new InvalidOperationException("Refusing to replace an incomplete key list");
            AssetDatabase.CreateAsset(sharedData,path);
            if(AssetDatabase.AssetPathToGUID(path)!=guid)throw new InvalidOperationException("Shared table GUID changed");
            english.SharedData=sharedData;vietnamese.SharedData=sharedData;
        }
        if(sharedData==null||vietnamese.SharedData!=sharedData)throw new InvalidOperationException("Hero tables must share key data");
        Undo.RecordObject(sharedData, "Author hero localization keys");
        Undo.RecordObject(english, "Author hero English strings");
        Undo.RecordObject(vietnamese, "Author hero Vietnamese strings");

        int authored = 0;
        foreach (var item in HeroLocalization.TableEntries)
        {
            var shared = sharedData.GetEntry(item.Key) ?? sharedData.AddKey(item.Key);
            if (shared == null) throw new InvalidOperationException("Could not create shared localization key: " + item.Key);

            Set(english, shared.Id, item.English);
            Set(vietnamese, shared.Id, item.Vietnamese);
            authored++;
        }

        EditorUtility.SetDirty(sharedData);
        EditorUtility.SetDirty(english);
        EditorUtility.SetDirty(vietnamese);
        if(collection!=null)EditorUtility.SetDirty(collection);
        AssetDatabase.SaveAssets();
        Debug.Log("[HeroLocalization] Authored " + authored + " Shadowgrove keys in Shared/en/vi.");
    }

    private static void Set(StringTable table, long keyId, string value)
    {
        var entry = table.GetEntry(keyId);
        if (entry == null) entry = table.AddEntry(keyId, value);
        else entry.Value = value;
    }
}
