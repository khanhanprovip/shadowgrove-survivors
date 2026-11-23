using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public static class JadeRebuildAnimations
{
 public static string Run(){
 const string root="Assets/RGame/RoguelikeKit/Art/Animations/Jade";
 Directory.CreateDirectory(root);
 int clips=0;
 foreach(string hero in new[]{"Rowan","Lyra","Kael"}){
 var targets=new Dictionary<string,AnimationClip>();
 foreach(string state in new[]{"Idle","Run","Attack"}){
 int first=state=="Idle"?0:state=="Run"?4:10,count=state=="Idle"?4:6;float fps=state=="Idle"?6:state=="Run"?10:18;
 var clip=new AnimationClip{name=hero+"_Jade_"+state,frameRate=fps,wrapMode=state=="Attack"?WrapMode.ClampForever:WrapMode.Loop};
 var binding=EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite");
 var keys=new ObjectReferenceKeyframe[count+1];
 for(int n=0;n<=count;n++){var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/SGJade/Heroes/"+hero+"/"+hero+"_"+(first+Math.Min(n,count-1)).ToString("00")+".png");if(sprite==null)throw new Exception("Missing frame");keys[n]=new ObjectReferenceKeyframe{time=n/fps,value=sprite};}
 AnimationUtility.SetObjectReferenceCurve(clip,binding,keys);
 var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.startTime=0;settings.stopTime=count/fps;settings.loopTime=state!="Attack";AnimationUtility.SetAnimationClipSettings(clip,settings);
 string path=root+"/"+clip.name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
 if(existing!=null){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);clip=existing;EditorUtility.SetDirty(clip);}else AssetDatabase.CreateAsset(clip,path);
 targets[state]=clip;clips++;
 // Restore the previous modified-on-entry artwork through the Unity serializer,
 // importing each backup under a disposable Editor-only path.
 string previous="Assets/RGame/RoguelikeKit/Art/Animations/Shadowgrove/"+hero+"_"+state+".anim";
 string backup=@"F:\Do_An_KT\_assistant_work\Jade_Eclipse_2026-11-22\backup\"+previous;
 string temp="Assets/Editor/JadeRestore_"+hero+"_"+state+".anim";
 if(!File.Exists(temp)){File.Copy(backup,temp);AssetDatabase.ImportAsset(temp,ImportAssetOptions.ForceSynchronousImport);var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(temp);var destination=AssetDatabase.LoadAssetAtPath<AnimationClip>(previous);EditorUtility.CopySerialized(old,destination);EditorUtility.SetDirty(destination);AssetDatabase.DeleteAsset(temp);}
 }
 var baseController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/RGame/RoguelikeKit/Art/Animations/Shadowgrove/"+hero+"_Controller.controller");
 string overridePath=root+"/"+hero+"_Jade.overrideController";
 var overrides=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);
 if(overrides==null){overrides=new AnimatorOverrideController(baseController){name=hero+"_Jade"};AssetDatabase.CreateAsset(overrides,overridePath);}
 var pairs=new List<KeyValuePair<AnimationClip,AnimationClip>>();overrides.GetOverrides(pairs);
 for(int i=0;i<pairs.Count;i++){string state=targets.Keys.First(s=>pairs[i].Key.name.EndsWith("_"+s));pairs[i]=new KeyValuePair<AnimationClip,AnimationClip>(pairs[i].Key,targets[state]);}
 overrides.ApplyOverrides(pairs);EditorUtility.SetDirty(overrides);
 string prefab="Assets/RGame/RoguelikeKit/Prefabs/Player/Shadowgrove/"+hero+".prefab";var go=PrefabUtility.LoadPrefabContents(prefab);
 try{go.GetComponent<Animator>().runtimeAnimatorController=overrides;PrefabUtility.SaveAsPrefabAsset(go,prefab);}finally{PrefabUtility.UnloadPrefabContents(go);}
 }
 AssetDatabase.SaveAssets();return clips+" frame-aligned Jade clips and 3 overrides saved; old clips restored through Unity API";
 }
}
