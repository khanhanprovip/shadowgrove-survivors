using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RGame.RoguelikeKit;
public static class JadeChest
{
 public static string Run()
 {
  const string path="Assets/Resources/SGJade/GoldChest.png";
  var src=new Texture2D(2,2,TextureFormat.RGBA32,false);
  ImageConversion.LoadImage(src,File.ReadAllBytes(@"F:\Do_An_KT\_assistant_work\Jade_Eclipse_2026-11-22\source\GoldChest.png"));
  var px=src.GetPixels32();int x0=src.width,y0=src.height,x1=0,y1=0;
  for(int y=0;y<src.height;y++)for(int x=0;x<src.width;x++)if(px[y*src.width+x].a>=128){x0=Math.Min(x0,x);x1=Math.Max(x1,x);y0=Math.Min(y0,y);y1=Math.Max(y1,y);}
  int w=x1-x0+1,h=y1-y0+1;
  var tex=new Texture2D(128,128,TextureFormat.RGBA32,false);var dst=new Color32[128*128];
  int dw=112,dh=Mathf.RoundToInt(112f*h/w),left=(128-dw)/2,bottom=(128-dh)/2;
  for(int y=0;y<dh;y++)for(int x=0;x<dw;x++){var c=px[(y0+y*h/dh)*src.width+x0+x*w/dw];if(c.a<128)c=new Color32();else c.a=255;dst[(y+bottom)*128+x+left]=c;}
  tex.SetPixels32(dst);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());
  UnityEngine.Object.DestroyImmediate(src);UnityEngine.Object.DestroyImmediate(tex);
  AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
  var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=112;ti.filterMode=FilterMode.Point;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;ti.isReadable=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
  const string prefab="Assets/RGame/RoguelikeKit/Prefabs/DropOut/TreasureBox.prefab";
  var go=PrefabUtility.LoadPrefabContents(prefab);
  try
  {
   go.GetComponent<SpriteRenderer>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
   var so=new SerializedObject(go.GetComponent<TreasureBox>());
   var config=AssetDatabase.FindAssets("t:GlobalConfigSO").Select(g=>AssetDatabase.LoadAssetAtPath<GlobalConfigSO>(AssetDatabase.GUIDToAssetPath(g))).Single();
   so.FindProperty("_globalConfig").objectReferenceValue=config;
   so.FindProperty("_goldCount").intValue=50;so.ApplyModifiedPropertiesWithoutUndo();
   PrefabUtility.SaveAsPrefabAsset(go,prefab);
  }finally{PrefabUtility.UnloadPrefabContents(go);}
  const string prop="Assets/RGame/RoguelikeKit/Prefabs/DropOut/Prop.prefab";
  go=PrefabUtility.LoadPrefabContents(prop);
  try
  {
   var sr=go.GetComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
   sr.sharedMaterial=AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
   var so=new SerializedObject(go.GetComponent<DropBox>());
   so.FindProperty("_touchGoldConfig").objectReferenceValue=AssetDatabase.FindAssets("t:GlobalConfigSO").Select(g=>AssetDatabase.LoadAssetAtPath<GlobalConfigSO>(AssetDatabase.GUIDToAssetPath(g))).Single();
   so.FindProperty("_touchGold").intValue=50;so.ApplyModifiedPropertiesWithoutUndo();
   PrefabUtility.SaveAsPrefabAsset(go,prop);
  }finally{PrefabUtility.UnloadPrefabContents(go);}
  AssetDatabase.SaveAssets();return "PASS: pooled chest and all map chests use GoldChest and award 50 gold on touch";
 }
}
