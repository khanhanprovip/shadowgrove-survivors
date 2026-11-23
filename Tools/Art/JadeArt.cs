using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RGame.RoguelikeKit;
public static class JadeArt {
 const string Work=@"F:\Do_An_KT\_assistant_work\Jade_Eclipse_2026-11-22";
 const string Root="Assets/Resources/SGJade";
 static List<string> log=new List<string>();
 static Texture2D Load(string name){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!ImageConversion.LoadImage(t,File.ReadAllBytes(name)))throw new Exception(name);return t;}
 static RectInt Cell(Texture2D t,int col,int row,int cols,int rows){int x=col*t.width/cols,x2=(col+1)*t.width/cols,y=t.height-(row+1)*t.height/rows,y2=t.height-row*t.height/rows;return new RectInt(x,y,x2-x,y2-y);}
 static RectInt Trim(Texture2D t,RectInt area){int minX=area.xMax,minY=area.yMax,maxX=-1,maxY=-1;var pixels=t.GetPixels32();for(int y=area.y;y<area.yMax;y++)for(int x=area.x;x<area.xMax;x++){if(pixels[y*t.width+x].a<90)continue;minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}if(maxX<0)throw new Exception("Empty source frame "+area);return new RectInt(minX,minY,maxX-minX+1,maxY-minY+1);}
 static Sprite Save(Texture2D t,string key,float ppu=100,Vector2? pivot=null,Vector4? border=null){
 string path=Root+"/"+key+".png";Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,t.EncodeToPNG());AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
 var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.alphaIsTransparency=true;imp.isReadable=true;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.maxTextureSize=4096;imp.spritePixelsPerUnit=ppu;imp.spritePivot=pivot??Vector2.one*.5f;imp.spriteBorder=border??Vector4.zero;var settings=new TextureImporterSettings();imp.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=pivot??Vector2.one*.5f;settings.spriteMeshType=SpriteMeshType.FullRect;imp.SetTextureSettings(settings);imp.SaveAndReimport();UnityEngine.Object.DestroyImmediate(t);return AssetDatabase.LoadAssetAtPath<Sprite>(path);
 }
 static Texture2D Crop(Texture2D source,RectInt r,int width,int height,bool alpha=true){var dest=new Texture2D(width,height,TextureFormat.RGBA32,false);var src=source.GetPixels32();var pix=new Color32[width*height];for(int y=0;y<height;y++)for(int x=0;x<width;x++){var c=src[Mathf.Clamp(r.y+y*r.height/height,0,source.height-1)*source.width+Mathf.Clamp(r.x+x*r.width/width,0,source.width-1)];if(alpha){if(c.a<90)c=new Color32(0,0,0,0);else c.a=255;}pix[y*width+x]=c;}dest.SetPixels32(pix);dest.Apply();return dest;}
 static Texture2D Fit(Texture2D source,RectInt r,int canvas,float scale,int baseline=16){
 int w=Mathf.RoundToInt(r.width*scale),h=Mathf.RoundToInt(r.height*scale);if(w>canvas-8||h>canvas-baseline-4)throw new Exception("Source frame exceeds canvas "+r+" "+w+"x"+h);
 var crop=Crop(source,r,Math.Max(1,w),Math.Max(1,h));var dest=new Texture2D(canvas,canvas,TextureFormat.RGBA32,false);dest.SetPixels32(new Color32[canvas*canvas]);dest.SetPixels((canvas-w)/2,baseline,w,h,crop.GetPixels());dest.Apply();UnityEngine.Object.DestroyImmediate(crop);return dest;
 }
 static string[] heroes={"Rowan","Lyra","Kael"};
 public static string Heroes(){
 Directory.CreateDirectory(Root);log.Clear();
 foreach(string hero in heroes){
 var src=Load(Work+"/source/"+hero+".png");var first=Trim(src,Cell(src,0,0,4,4));float scale=(hero=="Rowan"?122f:104f)/first.height;var frames=new List<Sprite>();
 for(int n=0;n<16;n++){var r=Trim(src,Cell(src,n%4,n/4,4,4));var t=Fit(src,r,160,scale);var sprite=Save(t,"Heroes/"+hero+"/"+hero+"_"+n.ToString("00"),60,new Vector2(.5f,.34f));frames.Add(sprite);log.Add(hero+" "+n+" source="+r+" scale="+scale);}
 foreach(string state in new[]{"Idle","Run","Attack"}){
 string path="Assets/RGame/RoguelikeKit/Art/Animations/Shadowgrove/"+hero+"_"+state+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);int start=state=="Idle"?0:state=="Run"?4:10,count=state=="Idle"?4:6;float duration=clip.length;var bindings=AnimationUtility.GetObjectReferenceCurveBindings(clip);var binding=bindings.First(b=>b.type==typeof(SpriteRenderer)&&b.propertyName=="m_Sprite");var curve=new ObjectReferenceKeyframe[count+1];for(int n=0;n<=count;n++)curve[n]=new ObjectReferenceKeyframe{time=n*duration/count,value=frames[start+Math.Min(n,count-1)]};AnimationUtility.SetObjectReferenceCurve(clip,binding,curve);EditorUtility.SetDirty(clip);
 }
 string prefab="Assets/RGame/RoguelikeKit/Prefabs/Player/Shadowgrove/"+hero+".prefab";var go=PrefabUtility.LoadPrefabContents(prefab);try{go.GetComponent<SpriteRenderer>().sprite=frames[0];PrefabUtility.SaveAsPrefabAsset(go,prefab);}finally{PrefabUtility.UnloadPrefabContents(go);}UnityEngine.Object.DestroyImmediate(src);
 }
 var board=Load(@"F:\Do_An_KT\_assistant_work\Art_Direction_2026-11-22\assets\B-roster.png");
 for(int i=0;i<3;i++)Save(Crop(board,Cell(board,i,0,3,2),384,384,false),heroes[i]);
 UnityEngine.Object.DestroyImmediate(board);AssetDatabase.SaveAssets();File.WriteAllLines(Work+"/hero-import.txt",log);return "48 hero frames, 9 clips, 3 prefabs and portraits saved";
 }
 public static string Enemies(){
 log.Clear();var src=Load(Work+"/source/enemies.png");
 string[] names={"Mossling","MosslingBloom","GloomBat","HollowEye","GraveWisp","PaleShade","RotWatcher","HollowWarden","GroveWarden","GroveColossus"};
 float[][] bounds={new[]{0f,.185f,.385f,.61f,.8f,1f},new[]{0f,.19f,.385f,.572f,.742f,1f}};var map=new Dictionary<string,Sprite>();
 for(int n=0;n<10;n++){int row=n/5,col=n%5;int x=Mathf.RoundToInt(bounds[row][col]*src.width),x2=Mathf.RoundToInt(bounds[row][col+1]*src.width);int y=row==0?Mathf.RoundToInt(src.height*.535f):0,y2=row==0?src.height:Mathf.RoundToInt(src.height*.535f);var rect=Trim(src,new RectInt(x,y,x2-x,y2-y));int canvas=n==9?256:160;float target=n==9?224:112;float scale=Mathf.Min(target/rect.height,(canvas-16f)/rect.width);map["SG_"+(n==9?"Boss_":"Enemy_")+names[n]]=Save(Fit(src,rect,canvas,scale,12),"Enemies/"+names[n]);log.Add(names[n]+" source="+rect);}
 int changed=0;
 foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/RGame/RoguelikeKit/Prefabs/Enemy"})){
 string path=AssetDatabase.GUIDToAssetPath(guid);var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
 if(!asset.GetComponentsInChildren<SpriteRenderer>(true).Any(r=>r.sprite!=null&&map.ContainsKey(r.sprite.texture.name)))continue;
 var go=PrefabUtility.LoadPrefabContents(path);try{foreach(var r in go.GetComponentsInChildren<SpriteRenderer>(true))if(r.sprite!=null&&map.TryGetValue(r.sprite.texture.name,out var sprite))r.sprite=sprite;PrefabUtility.SaveAsPrefabAsset(go,path);changed++;log.Add(path);}finally{PrefabUtility.UnloadPrefabContents(go);}
 }
 UnityEngine.Object.DestroyImmediate(src);AssetDatabase.SaveAssets();File.WriteAllLines(Work+"/enemy-import.txt",log);return changed+" enemy prefabs updated";
 }
 static string[] attackKeys={"ToNearestSkill","BothSideSkill","BouncingSkill","ChainSkill","CircularAttackSkill","CircularMotionSkill","FourCornerSkill","InsideScopeSkill","RandomSkill"};
 public static string Icons(){
 var src=Load(Work+"/source/icons.png");var sprites=new Sprite[25];for(int i=0;i<25;i++)sprites[i]=Save(Crop(src,Cell(src,i%5,i/5,5,5),96,96,false),"Icons/"+(i<9?attackKeys[i]:i==9?"EclipseEvolved":i<24?((AttributeType)(i-10)).ToString():"MoneyBag"));
 int count=0;
 foreach(var guid in AssetDatabase.FindAssets("t:SkillDataSO")){
 var path=AssetDatabase.GUIDToAssetPath(guid);var data=AssetDatabase.LoadAssetAtPath<SkillDataSO>(path);int i=data.SkillType==SkillType.AttributeSkill?10+(int)data.AttributeType:Array.IndexOf(attackKeys,data.Key);if(i<0)continue;data.SkillIcon=sprites[i];EditorUtility.SetDirty(data);count++;
 }
 foreach(var hero in heroes){
 string path="Assets/RGame/RoguelikeKit/ScriptableObjects/DataSO/Level/SelectCharacter/Shadowgrove/"+hero+".asset";var obj=AssetDatabase.LoadMainAssetAtPath(path);var so=new SerializedObject(obj);so.FindProperty("CharacterImage").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/"+hero+".png");so.FindProperty("BeginSkillImage").objectReferenceValue=sprites[0];so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(obj);
 }
 var previous=EditorSceneManager.GetSceneManagerSetup();var scene=EditorSceneManager.OpenScene("Assets/RGame/RoguelikeKit/Scenes/Managers/GamePlay.unity",OpenSceneMode.Single);
 foreach(var go in scene.GetRootGameObjects())foreach(var manager in go.GetComponentsInChildren<SkillManager>(true)){var so=new SerializedObject(manager);so.FindProperty("_moneyBagIcon").objectReferenceValue=sprites[24];so.ApplyModifiedPropertiesWithoutUndo();}
 EditorSceneManager.SaveScene(scene);if(previous.Any(s=>s.isLoaded && s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(previous); else EditorSceneManager.OpenScene("Assets/RGame/RoguelikeKit/Scenes/Initialization.unity",OpenSceneMode.Single);
 UnityEngine.Object.DestroyImmediate(src);AssetDatabase.SaveAssets();return count+" skill icons and 3 selection assets updated";
 }
 public static string Effects(){
 string[] keys={"EclipseBolt","GloomSpore","ThornFan","RootLash","WardenHalo","ThornOrbit","CrossBolt","SpiritBloom","WildMagic","Impact","Contact","Dust"};
 var src=Load(Work+"/source/vfx.png");var sprites=new Sprite[12];
 for(int i=0;i<12;i++){var r=Trim(src,Cell(src,i%4,i/4,4,3));float scale=Mathf.Min(112f/r.width,112f/r.height);sprites[i]=Save(Fit(src,r,128,scale,8),"VFX/"+keys[i]);}
 var index=new[]{0,2,1,3,4,5,6,7,8};var changed=new List<string>();
 for(int i=0;i<attackKeys.Length;i++){
 string path="Assets/RGame/RoguelikeKit/Prefabs/Skill/"+attackKeys[i]+".prefab";var go=PrefabUtility.LoadPrefabContents(path);
 try{foreach(var r in go.GetComponentsInChildren<SpriteRenderer>(true)){
 // Keep transforms, colliders, motion and hit shapes untouched. Draw a dedicated
 // child at the previous renderer's local bounds; the original remains the tint owner.
 var old=r.sprite;if(old==null)continue;var so=new SerializedObject(r);var sp=sprites[index[i]];
 // Preserve the original sprite's world-space box by creating a sprite-specific PPU
 // through a separate imported image, never by scaling the physics transform.
 string variant="VFX/Bound_"+attackKeys[i]+"_"+r.name.Replace(" ","_").Replace("(","").Replace(")","");
 var oldSize=old.bounds.size;var pixels=Load(Root+"/VFX/"+keys[index[i]]+".png");float max=Mathf.Max(.08f,Mathf.Max(oldSize.x,oldSize.y)); var shaped=Crop(pixels,new RectInt(0,0,pixels.width,pixels.height),Mathf.Max(1,Mathf.RoundToInt(128*oldSize.x/max)),Mathf.Max(1,Mathf.RoundToInt(128*oldSize.y/max))); UnityEngine.Object.DestroyImmediate(pixels); var fitted=Save(shaped,variant,128f/max);
 r.sprite=fitted;EditorUtility.SetDirty(r);
 }PrefabUtility.SaveAsPrefabAsset(go,path);changed.Add(path);}finally{PrefabUtility.UnloadPrefabContents(go);}
 }
 UnityEngine.Object.DestroyImmediate(src);AssetDatabase.SaveAssets();File.WriteAllLines(Work+"/vfx-import.txt",changed);return changed.Count+" skill prefabs updated";
 }
 static Color Hex(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}
 static Texture2D Plate(int w,int h,bool primary=false){
 var t=new Texture2D(w,h,TextureFormat.RGBA32,false);var p=new Color32[w*h];int bevel=10;
 for(int y=0;y<h;y++)for(int x=0;x<w;x++){int d=Math.Min(Math.Min(x,w-1-x),Math.Min(y,h-1-y));bool cut=x+y<bevel||w-1-x+h-1-y<bevel;if(cut)continue;Color c=d<2?Hex(primary?"#B8E3D1":"#6EAF9D"):d<4?Hex("#183D37"):d<6?Hex(primary?"#97CBBB":"#335950"):Hex(primary?"#8CD3BD":"#0D2326");p[y*w+x]=c;}t.SetPixels32(p);t.Apply();return t;
 }
 public static string Shell(){
 Save(Plate(160,160),"Panel",100,null,new Vector4(12,12,12,12));Save(Plate(160,160),"Card",100,null,new Vector4(12,12,12,12));Save(Plate(256,80),"Button",100,null,new Vector4(12,12,12,12));Save(Plate(256,80,true),"Primary",100,null,new Vector4(12,12,12,12));
 var forest=Load(@"F:\Do_An_KT\_assistant_work\Art_Direction_2026-11-22\assets\B-menu.png");Save(forest,"Forest");
 string[] maps={"MoonlitRuins","SunkenMarsh","AshenQuarry","FrostHollow","CursedVillage","ClockworkCitadel","CrimsonCathedral","EndlessAbyss","EclipseThrone"};
 for(int i=0;i<10;i++){var path=i==0?Root+"/Forest.png":"Assets/Resources/SGGround/"+maps[i-1]+".png";var tex=Load(path);Save(Crop(tex,new RectInt(0,0,tex.width,tex.height),384,216,false),"Level"+i);UnityEngine.Object.DestroyImmediate(tex);}
 foreach(var key in new[]{"sigil","shrine","dawn","fallen","camp"}){
 var t=new Texture2D(256,256,TextureFormat.RGBA32,false);var p=new Color32[256*256];Color bright=Hex(key=="fallen"?"#C58380":key=="dawn"?"#DACD9C":"#8CD3BD");
 for(int y=0;y<256;y++)for(int x=0;x<256;x++){float dx=x-127.5f,dy=y-127.5f,r=Mathf.Sqrt(dx*dx+dy*dy),a=Mathf.Atan2(dy,dx);bool ring=Mathf.Abs(r-82)<2||Mathf.Abs(r-94)<1;bool rune=Mathf.Abs(Mathf.Abs(dx)+Mathf.Abs(dy)-42)<2;bool rays=r>96&&r<113&&Mathf.Abs(Mathf.Sin(a*6))<.07f;if(ring||rune||rays)p[y*256+x]=bright;}
 t.SetPixels32(p);t.Apply();Save(t,key);}
 AssetDatabase.SaveAssets();return "Jade shell, ten previews and five seals created";
 }
}
