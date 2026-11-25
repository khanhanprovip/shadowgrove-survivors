using System;
using System.IO;
using UnityEngine;
using RGame.RoguelikeKit;
public static class BossAnimationPreview
{
 public static string Run()
 {
  if(!Application.isPlaying)throw new InvalidOperationException("Run BossRuntimeInspect.Setup in Play Mode first.");
  string output="F:/Do_An_KT/_assistant_work/Boss_Rework_2026-11-23/smooth-vfx-preview";Directory.CreateDirectory(output);
  var root=new GameObject("Native Boss Preview QA");root.transform.position=new Vector3(20000,20000,0);
  var cameraObject=new GameObject("Preview Camera",typeof(Camera));cameraObject.transform.SetParent(root.transform,false);cameraObject.transform.localPosition=new Vector3(0,0,-20);
  var camera=cameraObject.GetComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.045f,.055f,1);
  var image=new GameObject("Preview Sprite",typeof(SpriteRenderer));image.layer=31;image.transform.SetParent(root.transform,false);var renderer=image.GetComponent<SpriteRenderer>();
  var target=RenderTexture.GetTemporary(384,384,16,RenderTextureFormat.ARGB32);camera.targetTexture=target;var readback=new Texture2D(384,384,TextureFormat.RGBA32,false);
  var old=RenderTexture.active;
  try
  {
   for(int kind=0;kind<5;kind++)
   {
    var rig=GameObject.Find("QA_Preview_"+kind).GetComponent<BossVisualAnimation>();var source=rig.AnimatedRenderer;
    if(rig.Frames.Length!=36||rig.StateClips.Length!=6)throw new InvalidOperationException("Native clips not authored for boss"+kind);
    var material=new Material(source.sharedMaterial);renderer.sharedMaterial=material;
    var bounds=rig.StandingSprite.bounds;foreach(var sprite in rig.Frames)bounds.Encapsulate(sprite.bounds);image.transform.localPosition=-bounds.center;
    camera.orthographicSize=Mathf.Max(bounds.extents.x,bounds.extents.y)*1.2f;
    var initial=rig.NativeAnimator.GetCurrentAnimatorStateInfo(0);
    try
    {
     for(int frame=0;frame<385;frame++)
     {
      float t=frame/50f;BossMotion motion;float age;
      if(t<1.6f){motion=BossMotion.Idle;age=t;}
      else if(t<4f){motion=BossMotion.Move;age=t-1.6f;}
      else if(t<5.1f){motion=BossMotion.Windup;age=t-4f;}
      else if(t<6f){motion=BossMotion.Attack;age=t-5.1f;}
      else if(t<6.36f){motion=BossMotion.Hit;age=(t-6f)*.5f;}
      else{motion=BossMotion.Death;age=(t-6.36f)*.75f;}
      var clip=rig.ClipFor(motion);bool loop=motion==BossMotion.Idle||motion==BossMotion.Move;
      float normalized=loop?age/clip.length:Mathf.Min(.999f,age/Mathf.Max(.01f,clip.length));
      rig.NativeAnimator.Play("Base Layer."+motion,0,normalized);rig.NativeAnimator.Update(0);
      renderer.sprite=source.sprite;
      float death=motion==BossMotion.Death?Mathf.Clamp01(age/.68f):0;
      material.SetFloat("_Cutoff",death);material.SetColor("_ShadowColor",new Color(0,0,0,.7f*(1-death)));
      material.SetColor("_BlendColor",motion==BossMotion.Hit?new Color(.12f,.015f,.015f,0):Color.clear);
      camera.Render();RenderTexture.active=target;readback.ReadPixels(new Rect(0,0,384,384),0,0,false);readback.Apply(false);File.WriteAllBytes(Path.Combine(output,BossCatalog.Ids[kind]+"-"+frame.ToString("D3")+".png"),readback.EncodeToPNG());
     }
    }
    finally{rig.NativeAnimator.Play(initial.fullPathHash,0,initial.normalizedTime);rig.NativeAnimator.Update(0);UnityEngine.Object.DestroyImmediate(material);}
   }
  }
  finally{RenderTexture.active=old;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(readback);UnityEngine.Object.DestroyImmediate(root);}
  return output;
 }
}
