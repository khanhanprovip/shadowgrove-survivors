using System;
using System.Linq;
using DATN.Modes;
using RGame.RoguelikeKit;
using UnityEditor;
using UnityEngine;

public static class StageDesignAuthoring
{
    // All authored data is created with Unity APIs. No scene or prefab text edits.
    public static string Run()
    {
        const string directory="Assets/Resources/SGStageModes";
        if(!AssetDatabase.IsValidFolder(directory))AssetDatabase.CreateFolder("Assets/Resources","SGStageModes");
        var keys=DATN.UI.ForestUI.LevelKeys;
        int count=0;
        for(int i=0;i<keys.Length;i++)
        {
            var level=AssetDatabase.FindAssets("t:LevelConfigSO").Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<LevelConfigSO>).Single(x=>x.name==keys[i]);
            string path=directory+"/"+keys[i]+".asset";
            var mode=AssetDatabase.LoadAssetAtPath<GameModeSO>(path);
            if(mode==null){mode=ScriptableObject.CreateInstance<GameModeSO>();AssetDatabase.CreateAsset(mode,path);}
            mode.ModeId=i==9?GameModeId.BossRush:i==8?GameModeId.Endless:i==1||i==3||i==6?GameModeId.KillTarget:GameModeId.Survival;
            mode.Duration=i==0?90:120;mode.TargetKills=30;mode.DifficultyPerMinute=.1f;
            mode.FinalBossKind=i==2?1:i==4?2:i==7?3:-1;
            mode.BossRushCount=0;mode.BossBreakSeconds=5;
            mode.BossRushStartingChoices=10;mode.BossRushStartingChoicesGrantLevels=true;
            level.GameMode=mode;
            EditorUtility.SetDirty(mode);EditorUtility.SetDirty(level);count++;
        }
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        return "Configured "+count+" stages; #9 Endless; #10 infinite Boss Rush, level 11 + 10 choices, 5s intermissions.";
    }
}
