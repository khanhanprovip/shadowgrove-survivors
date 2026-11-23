using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class JadePolish
{
 const string Root="Assets/Resources/SGJade";
 const string Work=@"F:\Do_An_KT\_assistant_work\Jade_Eclipse_2026-11-22";
 static Texture2D Read(string p){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(t,File.ReadAllBytes(p));return t;}
 static void Import(string path){AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var i=(TextureImporter)AssetImporter.GetAtPath(path);i.filterMode=FilterMode.Point;i.textureCompression=TextureImporterCompression.Uncompressed;i.mipmapEnabled=false;i.isReadable=true;i.SaveAndReimport();}
 public static string Run()
 {
  // Remove the neighbouring scythe fragment without stretching Lyra's portrait.
  var source=Read(@"F:\Do_An_KT\_assistant_work\Art_Direction_2026-11-22\assets\B-roster.png");
  var focus=new RectInt(550,544,474,474);var portrait=new Texture2D(384,384,TextureFormat.RGBA32,false);
  var pp=new Color32[384*384];var pixels=source.GetPixels32();
  for(int y=0;y<384;y++)for(int x=0;x<384;x++)pp[y*384+x]=pixels[(focus.y+y*focus.height/384)*source.width+focus.x+x*focus.width/384];
  portrait.SetPixels32(pp);portrait.Apply();File.WriteAllBytes(Root+"/Lyra.png",portrait.EncodeToPNG());Import(Root+"/Lyra.png");
  UnityEngine.Object.DestroyImmediate(portrait);UnityEngine.Object.DestroyImmediate(source);
  // Snap the single resampling edge case to the same sole baseline as the other 47 frames.
  string frame=Root+"/Heroes/Rowan/Rowan_06.png";var t=Read(frame);var before=t.GetPixels32();int bottom=t.height;
  for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++)if(before[y*t.width+x].a>=128)bottom=Math.Min(bottom,y);
  int shift=bottom-16;if(shift!=0){var after=new Color32[before.Length];for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++){int srcY=y+shift;if(srcY>=0&&srcY<t.height)after[y*t.width+x]=before[srcY*t.width+x];}t.SetPixels32(after);t.Apply();File.WriteAllBytes(frame,t.EncodeToPNG());Import(frame);}
  UnityEngine.Object.DestroyImmediate(t);
  // The forest joins the existing single-ground renderer; no map or collider changes.
  string terrain="Assets/Resources/SGGround/TrialForest.png";File.Copy(Work+"/source/TrialForest.png",terrain,true);AssetDatabase.ImportAsset(terrain,ImportAssetOptions.ForceSynchronousImport);
  var importer=(TextureImporter)AssetImporter.GetAtPath(terrain);importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Repeat;importer.mipmapEnabled=false;importer.isReadable=true;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
  source=Read(terrain);var preview=new Texture2D(384,216,TextureFormat.RGBA32,false);pixels=source.GetPixels32();pp=new Color32[384*216];
  for(int y=0;y<216;y++)for(int x=0;x<384;x++)pp[y*384+x]=pixels[(y*source.height/216)*source.width+x*source.width/384];
  preview.SetPixels32(pp);preview.Apply();File.WriteAllBytes(Root+"/Level0.png",preview.EncodeToPNG());Import(Root+"/Level0.png");UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(preview);
  string[] skills={"ToNearestSkill","BothSideSkill","BouncingSkill","ChainSkill","CircularAttackSkill","CircularMotionSkill","FourCornerSkill","InsideScopeSkill","RandomSkill"};
  string[] effects={"EclipseBolt","ThornFan","GloomSpore","RootLash","WardenHalo","ThornOrbit","CrossBolt","SpiritBloom","WildMagic"};
  foreach(var pair in skills.Select((key,i)=>new{key,index=i}))
  {
   string path="Assets/RGame/RoguelikeKit/Prefabs/Skill/"+pair.key+".prefab";var go=PrefabUtility.LoadPrefabContents(path);
   try
   {
    foreach(var r in go.GetComponentsInChildren<SpriteRenderer>(true))
    {
     if(r.sprite==null)continue;
     float extent=Mathf.Max(r.sprite.bounds.size.x,r.sprite.bounds.size.y);
     if(pair.key=="ToNearestSkill")extent=.55f;
     // Only the two collider-free visual leaves had anisotropic legacy scaling.
     // Their parent gameplay transform and every collider are retained.
     if((pair.key=="RandomSkill"||pair.key=="BothSideSkill")&&r.transform!=go.transform&&r.GetComponentsInChildren<Collider2D>(true).Length==0)
     {r.transform.localScale=Vector3.one;if(pair.key=="RandomSkill")extent=.75f;}
     string variant=Root+"/VFX/Crisp_"+pair.key+"_"+r.name.Replace(" ","_").Replace("(","").Replace(")","")+".png";
     File.Copy(Root+"/VFX/"+effects[pair.index]+".png",variant,true);AssetDatabase.ImportAsset(variant,ImportAssetOptions.ForceSynchronousImport);
     var ti=(TextureImporter)AssetImporter.GetAtPath(variant);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=128f/extent;ti.filterMode=FilterMode.Point;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.isReadable=true;ti.SaveAndReimport();
     r.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(variant);r.sortingOrder=Math.Min(r.sortingOrder,45);
    }
    PrefabUtility.SaveAsPrefabAsset(go,path);
   }finally{PrefabUtility.UnloadPrefabContents(go);}
  }
  AssetDatabase.SaveAssets();return "Lyra crop, sole alignment, forest terrain, nine undistorted VFX and actor foreground order saved";
 }
}
