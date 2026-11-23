using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class JadeAnimationProbe
{
 public static string Run(){
 var report=new List<string>();
 foreach(var hero in new[]{"Rowan","Lyra","Kael"}){
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RGame/RoguelikeKit/Prefabs/Player/Shadowgrove/"+hero+".prefab");
 var go=UnityEngine.Object.Instantiate(prefab);
 try{
 foreach(var state in new[]{"Idle","Run","Attack"}){
 var clip=prefab.GetComponent<Animator>().runtimeAnimatorController.animationClips.First(c=>c.name.EndsWith("_"+state));
 report.Add(hero+" "+state+" length="+clip.length+" fps="+clip.frameRate);
 for(int n=0;n<=16;n++){float time=n*.05f;clip.SampleAnimation(go,time);report.Add(time.ToString("F2")+" "+go.GetComponent<SpriteRenderer>().sprite.name);}
 }}finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 string path=@"F:\Do_An_KT\_assistant_work\Jade_Eclipse_2026-11-22\clip-sampling-"+DateTime.Now.ToString("HHmmss")+".txt";File.WriteAllLines(path,report);return path;
 }
}
