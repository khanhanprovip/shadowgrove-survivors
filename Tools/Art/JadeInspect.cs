using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RGame.RoguelikeKit;
public static class JadeInspect {
 public static string Run(){
 var lines=new List<string>();
 foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/RGame/RoguelikeKit/Prefabs"})){
 var path=AssetDatabase.GUIDToAssetPath(guid);var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);
 var sr=go.GetComponentsInChildren<SpriteRenderer>(true);
 if(path.Contains("Player/Shadowgrove")||sr.Any(s=>s.sprite!=null&&s.sprite.texture.name.StartsWith("SG_"))){
 lines.Add("PREFAB "+path);
 foreach(var s in sr)lines.Add("  RENDER "+s.name+" sprite="+(s.sprite?.name??"null")+" ppu="+s.sprite?.pixelsPerUnit+" bounds="+s.sprite?.bounds.size+" scale="+s.transform.localScale);
 foreach(var a in go.GetComponentsInChildren<Animator>(true))lines.Add("  ANIM "+a.name+" "+a.runtimeAnimatorController?.name);
 }}
 foreach(var guid in AssetDatabase.FindAssets("t:SkillDataSO")){
 var path=AssetDatabase.GUIDToAssetPath(guid);var s=AssetDatabase.LoadAssetAtPath<SkillDataSO>(path);lines.Add("SKILL "+path+" key="+s.Key+" type="+s.SkillType+" attr="+s.AttributeType+" prefab="+AssetDatabase.GetAssetPath(s.SkillPrefab));
 if(s.SkillPrefab!=null)foreach(var r in s.SkillPrefab.GetComponentsInChildren<SpriteRenderer>(true))lines.Add("  VISUAL "+r.name+" sprite="+r.sprite?.name+" size="+r.sprite?.bounds.size+" scale="+r.transform.localScale+" anim="+(r.GetComponent<Animator>() != null ? r.GetComponent<Animator>().runtimeAnimatorController.name : "none"));
 }
 var factory=new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();factory.Init();lines.Add("CAPABILITY_TYPE "+factory.GetType().Assembly.GetType("UnityEditor.U2D.Sprites.ISpriteFrameEditCapability"));
 lines.Add("FONT "+AssetDatabase.GetAssetPath(TMPro.TMP_Settings.defaultFontAsset));
 File.WriteAllLines(@"F:\Do_An_KT\_assistant_work\Jade_Eclipse_2026-11-22\inventory.txt",lines);return "Inventory written, "+lines.Count+" lines";
 }
}
