using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RGame.RoguelikeKit;
public static class StarterWeapons
{
 const string Root="Assets/Resources/SGJade";
 const string Work=@"F:\Do_An_KT\_assistant_work\Starter_Weapons_2026-11-23";
 static void Import(string path,float ppu)
 {
  AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
  var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=ppu;ti.mipmapEnabled=false;ti.filterMode=FilterMode.Point;ti.alphaIsTransparency=true;ti.isReadable=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=2048;ti.SaveAndReimport();
 }
 public static string Run()
 {
  File.Copy(Work+"/source/Logo.png",Root+"/Logo.png",true);Import(Root+"/Logo.png",100);
  Directory.CreateDirectory(Root+"/Starters");
  var source=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(source,File.ReadAllBytes(Work+"/source/StarterAtlas.png"));var px=source.GetPixels32();
  string[] names={"Rowan","Lyra","Kael"};float[] ranges={2.2f,7f,10.5f};
  for(int i=0;i<3;i++)
  {
   int left=i*source.width/3,right=(i+1)*source.width/3,x0=right,x1=left,y0=source.height,y1=0;
   for(int y=0;y<source.height;y++)for(int x=left;x<right;x++)if(px[y*source.width+x].a>=128){x0=Math.Min(x0,x);x1=Math.Max(x1,x);y0=Math.Min(y0,y);y1=Math.Max(y1,y);}
   int w=x1-x0+1,h=y1-y0+1;float factor=112f/Math.Max(w,h);int dw=Mathf.RoundToInt(w*factor),dh=Mathf.RoundToInt(h*factor);
   var image=new Texture2D(128,128,TextureFormat.RGBA32,false);var output=new Color32[128*128];
   for(int y=0;y<dh;y++)for(int x=0;x<dw;x++)output[(y+(128-dh)/2)*128+x+(128-dw)/2]=px[(y0+y*h/dh)*source.width+x0+x*w/dw];
   image.SetPixels32(output);image.Apply();string icon=Root+"/Starters/"+names[i]+"Icon.png",effect=Root+"/Starters/"+names[i]+"Effect.png";
   var png=image.EncodeToPNG();File.WriteAllBytes(icon,png);File.WriteAllBytes(effect,png);UnityEngine.Object.DestroyImmediate(image);
   Import(icon,128);Import(effect,i==1?160:i==2?112:128);
   string path="Assets/RGame/RoguelikeKit/Prefabs/Player/Shadowgrove/"+names[i]+".prefab";var go=PrefabUtility.LoadPrefabContents(path);
   try
   {
    var profile=go.GetComponent<StarterWeaponProfile>()??go.AddComponent<StarterWeaponProfile>();profile.Kind=(StarterWeaponKind)(i+1);profile.Range=ranges[i];profile.Icon=AssetDatabase.LoadAssetAtPath<Sprite>(icon);profile.Effect=AssetDatabase.LoadAssetAtPath<Sprite>(effect);PrefabUtility.SaveAsPrefabAsset(go,path);
   }finally{PrefabUtility.UnloadPrefabContents(go);}
   var selection=AssetDatabase.LoadAssetAtPath<CharacterSelectConfigSO>("Assets/RGame/RoguelikeKit/ScriptableObjects/DataSO/Level/SelectCharacter/Shadowgrove/"+names[i]+".asset");selection.BeginSkillImage=AssetDatabase.LoadAssetAtPath<Sprite>(icon);EditorUtility.SetDirty(selection);
  }
  UnityEngine.Object.DestroyImmediate(source);AssetDatabase.SaveAssets();return "PASS: logo, three starter icons/effects and actual hero profiles saved (2.2 < 7 < 10.5)";
 }
}
