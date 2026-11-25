using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RGame.CommonStat;
using RGame.RoguelikeKit;
public static class BossAuthoring
{
 const string Root="Assets/Resources/SGBosses";
 static string Work=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../_assistant_work/Boss_Rework_2026-11-23"));
 static void Set(UnityEngine.Object obj,string key,UnityEngine.Object value){var so=new SerializedObject(obj);so.FindProperty(key).objectReferenceValue=value;so.ApplyModifiedProperties();EditorUtility.SetDirty(obj);if(PrefabUtility.IsPartOfPrefabInstance(obj))PrefabUtility.RecordPrefabInstancePropertyModifications(obj);}
 static void Stat(ValueConfigSO cfg,string key,int value){var s=cfg.ValueDefinitions.Find(v=>v.ValueName==key);if(s!=null)s.DefaultValue=value;EditorUtility.SetDirty(cfg);}
 static void Import(string path,float ppu)
 {
  AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var ti=(TextureImporter)AssetImporter.GetAtPath(path);
  ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=ppu;ti.mipmapEnabled=false;ti.filterMode=FilterMode.Point;ti.alphaIsTransparency=true;ti.isReadable=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=2048;
  var settings=new TextureImporterSettings();ti.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,12f/512);ti.SetTextureSettings(settings);ti.SaveAndReimport();
 }
 static float HeroHeight()
 {
  var hero=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RGame/RoguelikeKit/Prefabs/Player/Shadowgrove/Rowan.prefab");
  var sr=hero.GetComponentsInChildren<SpriteRenderer>(true).Where(s=>s.sprite!=null&&!s.name.ToLowerInvariant().Contains("shadow")).OrderByDescending(s=>s.sprite.bounds.size.y).First();
  var sprite=sr.sprite;var pixels=sprite.texture.GetPixels32();var rect=sprite.textureRect;int ymin=9999,ymax=0;
  for(int y=(int)rect.y;y<rect.yMax;y++)for(int x=(int)rect.x;x<rect.xMax;x++)if(pixels[y*sprite.texture.width+x].a>=128){ymin=Math.Min(ymin,y);ymax=Math.Max(ymax,y);}
  return (ymax-ymin+1)/sprite.pixelsPerUnit*sr.transform.lossyScale.y;
 }
 public static string Run()
 {
  Directory.CreateDirectory(Work);
  Directory.CreateDirectory(Root+"/Sprites");Directory.CreateDirectory(Root+"/Prefabs");Directory.CreateDirectory(Root+"/Stats");AssetDatabase.Refresh();
  var owned=AssetDatabase.LoadAssetAtPath<AttributeSO>("Assets/RGame/RoguelikeKit/ScriptableObjects/LogicSO/PowerUp/AttributeSO.asset");
  if(owned==null)throw new Exception("Purchased attribute source missing");
  int normal=0,elite=0,legacyBoss=0,wired=0;var log=new List<string>();
  string[] prefabs=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/RGame/RoguelikeKit/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
  foreach(string path in prefabs)
  {
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);bool hasEnemy=prefab.GetComponent<BaseEnemy>()!=null;
   bool hasAttr=prefab.GetComponentsInChildren<AttributeManager>(true).Length>0;
   if(!hasEnemy&&!hasAttr)continue;
   var go=PrefabUtility.LoadPrefabContents(path);
   try
   {
    foreach(var am in go.GetComponentsInChildren<AttributeManager>(true)){Set(am,"_ownedUpgrades",owned);wired++;}
    var enemy=go.GetComponent<BaseEnemy>();
    if(enemy!=null)
    {
     var old=(ValueConfigSO)new SerializedObject(enemy).FindProperty("_valueConfig").objectReferenceValue;
     string oldPath=AssetDatabase.GetAssetPath(old);
     bool boss=oldPath.Contains("/Boss/")||path.Contains("GroveColossus")||go.GetComponent<EnemyRank>()?.Tier==EnemyTier.Boss||
        new[]{"Crow","CrystalHealerFairy","Fairy","FlameDancerFairy","FrostCrystalFairy","PinkFairy"}.Contains(Path.GetFileNameWithoutExtension(path));
     bool tough=!boss&&(Path.GetFileNameWithoutExtension(path).EndsWith(" 1")||path.ToLowerInvariant().Contains("elite"));
     var rank=go.GetComponent<EnemyRank>()??go.AddComponent<EnemyRank>();rank.Tier=boss?EnemyTier.Boss:tough?EnemyTier.Elite:EnemyTier.Normal;
     if(boss)legacyBoss++;else if(tough)elite++;else normal++;
     string configPath=Root+"/Stats/"+AssetDatabase.AssetPathToGUID(path)+".asset";
     var cfg=AssetDatabase.LoadAssetAtPath<ValueConfigSO>(configPath);
     if(cfg==null){cfg=UnityEngine.Object.Instantiate(old);cfg.name=Path.GetFileNameWithoutExtension(path)+" Balanced";AssetDatabase.CreateAsset(cfg,configPath);}
     Stat(cfg,"HP",boss?300:tough?90:30);Stat(cfg,"Attack",boss?6:tough?4:2);Stat(cfg,"AttackCD",100);
     Set(enemy,"_valueConfig",cfg);log.Add(path+" "+rank.Tier+" HP="+(boss?300:tough?90:30));
    }
    PrefabUtility.SaveAsPrefabAsset(go,path);
   }finally{PrefabUtility.UnloadPrefabContents(go);}
  }
  foreach(string scenePath in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/RGame/RoguelikeKit/Scenes"}).Select(AssetDatabase.GUIDToAssetPath))
  {
   var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);bool dirty=false;
   foreach(var root in scene.GetRootGameObjects())foreach(var am in root.GetComponentsInChildren<AttributeManager>(true)){Set(am,"_ownedUpgrades",owned);wired++;dirty=true;}
   if(dirty){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
  }
  float heroHeight=HeroHeight();var source=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(source,File.ReadAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,"../Tools/Art/Sources/BossAtlas.png"))));var pixels=source.GetPixels32();
  string template="Assets/RGame/RoguelikeKit/Prefabs/Enemy/NewMonster/Slime/Slime.prefab";
  for(int i=0;i<5;i++)
  {
   int left=i%3*512,right=left+512,bottom=i<3?512:0,top=bottom+512,x0=right,x1=left,y0=top,y1=bottom;
   for(int y=bottom;y<top;y++)for(int x=left;x<right;x++)if(pixels[y*source.width+x].a>=128){x0=Math.Min(x0,x);x1=Math.Max(x1,x);y0=Math.Min(y0,y);y1=Math.Max(y1,y);}
   int w=x1-x0+1,h=y1-y0+1;float factor=448f/h;int dw=Mathf.RoundToInt(w*factor),width=Mathf.Max(512,dw+40);
   var texture=new Texture2D(width,512,TextureFormat.RGBA32,false);var output=new Color32[width*512];
   for(int y=0;y<448;y++)for(int x=0;x<dw;x++)output[(y+12)*width+x+(width-dw)/2]=pixels[(y0+y*h/448)*source.width+x0+x*w/dw];
   texture.SetPixels32(output);texture.Apply();string spritePath=Root+"/Sprites/"+BossCatalog.Ids[i]+".png";File.WriteAllBytes(spritePath,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);Import(spritePath,448f/(heroHeight*5));
   var go=PrefabUtility.LoadPrefabContents(template);
   try
   {
    go.name=BossCatalog.Ids[i];var old=go.GetComponent<BaseEnemy>();var oldSo=new SerializedObject(old);
    var global=oldSo.FindProperty("_globalConfig").objectReferenceValue;var pool=oldSo.FindProperty("_poolRuntime").objectReferenceValue;
    var cfg=UnityEngine.Object.Instantiate((ValueConfigSO)oldSo.FindProperty("_valueConfig").objectReferenceValue);string cp=Root+"/Stats/"+BossCatalog.Ids[i]+".asset";
    var existing=AssetDatabase.LoadAssetAtPath<ValueConfigSO>(cp);if(existing!=null){UnityEngine.Object.DestroyImmediate(cfg);cfg=existing;}else AssetDatabase.CreateAsset(cfg,cp);
    Stat(cfg,"HP",300);Stat(cfg,"Attack",6);Stat(cfg,"AttackCD",100);
    UnityEngine.Object.DestroyImmediate(old);var enemy=go.AddComponent<EndlessBossEnemy>();Set(enemy,"_globalConfig",null);Set(enemy,"_poolRuntime",null);Set(enemy,"_valueConfig",cfg);
    Set(go.GetComponentInChildren<EnemyHit>(true),"poolRuntimeSo",null);
    foreach(var weapon in go.GetComponentsInChildren<BaseEnemyWeapon>(true))Set(weapon,"_globalConfig",null);
    var so=new SerializedObject(enemy);so.FindProperty("_dropKeys").arraySize=0;so.ApplyModifiedPropertiesWithoutUndo();
    go.GetComponent<EnemyRank>().Tier=EnemyTier.Boss;go.AddComponent<BossBrain>().Kind=i;
    go.transform.localScale=Vector3.one;
    foreach(var animator in go.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(animator);
    var sr=go.GetComponentsInChildren<SpriteRenderer>(true).First(s=>!s.name.ToLowerInvariant().Contains("shadow"));sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);sr.color=Color.white;sr.transform.localScale=Vector3.one;sr.transform.localPosition=Vector3.zero;sr.sortingOrder=4;
    foreach(var collider in go.GetComponentsInChildren<Collider2D>(true))
    {
     if(collider is CircleCollider2D circle){circle.radius=.75f;circle.offset=new Vector2(0,.7f);}
     else if(collider is CapsuleCollider2D capsule){capsule.size=new Vector2(1.5f,2.2f);capsule.offset=new Vector2(0,1);}
     else if(collider is BoxCollider2D box){box.size=new Vector2(1.5f,2.2f);box.offset=new Vector2(0,1);}
    }
    PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/"+BossCatalog.Ids[i]+".prefab");
   }finally{PrefabUtility.UnloadPrefabContents(go);}
  }
  UnityEngine.Object.DestroyImmediate(source);
  foreach(string path in AssetDatabase.FindAssets("t:SkillDataSO").Select(AssetDatabase.GUIDToAssetPath))
  {
   var skill=AssetDatabase.LoadAssetAtPath<SkillDataSO>(path);
   if(skill.SkillType==SkillType.AttackSkill)
   {
    skill.Damage=skill.Key=="ToNearestSkill"?12:Mathf.Clamp(skill.Damage,6,12);
    foreach(var mod in skill.UpgradeAttribute)mod.IntValue=mod.skillAttributeType switch{UpgradeSkillAttributeType.DamageAdd=>3,UpgradeSkillAttributeType.CDLessPercent=>5,UpgradeSkillAttributeType.ProjectilesCountAdd=>1,_=>10};
   }
   else if(skill.SkillType==SkillType.AttributeSkill)
   {
    int step=skill.AttributeType switch{AttributeType.Armor=>-4,AttributeType.Cooldown=>-4,AttributeType.HPMax=>8,AttributeType.Recovery=>1,AttributeType.Amount=>1,AttributeType.Magnet=>15,AttributeType.Might=>8,_=>5};
    for(int k=0;k<skill.UpgradeAttributeModValue.Count;k++)skill.UpgradeAttributeModValue[k]=step;
   }
   EditorUtility.SetDirty(skill);
  }
  AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/RGame/RoguelikeKit/Scenes/Initialization.unity",OpenSceneMode.Single);
  string result=$"Normal={normal}; Elite={elite}; LegacyBoss={legacyBoss}; wired permanent sources={wired}; hero visible height={heroHeight}; new boss visible height={heroHeight*5}";
  File.WriteAllLines(Work+"/authoring.txt",log.Concat(new[]{result}));return result;
 }
}
