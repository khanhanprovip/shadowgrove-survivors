using System.Linq;
using UnityEditor;
using UnityEngine;
public static class InspectShadows
{
    public static string Run()
    {
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/RGame/RoguelikeKit/Scripts/Shader/Enemy.shader");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RGame/RoguelikeKit/Prefabs/Enemy/CompassMonster.prefab");
        return "Asset shader="+shader.name+"; Shader.Find="+(Shader.Find("RoguelikeKit/Enemy")?.name??"missing")+"; prefab="+
            string.Join(";",prefab.GetComponentsInChildren<SpriteRenderer>(true).Select(s=>s.name+":"+s.sharedMaterial?.name+":"+s.sharedMaterial?.shader.name));
    }
}
