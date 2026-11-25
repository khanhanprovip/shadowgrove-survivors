using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RGame.RoguelikeKit;
public static class BossWire
{
 public static string Run()
 {
  const string path="Assets/RGame/RoguelikeKit/Scenes/Managers/GamePlay.unity";
  var global=AssetDatabase.LoadAssetAtPath<GlobalConfigSO>("Assets/RGame/RoguelikeKit/ScriptableObjects/RuntimeDataSO/Global/GlobalConfigSO.asset");
  var save=AssetDatabase.LoadAssetAtPath<SaveSystemSO>("Assets/RGame/RoguelikeKit/ScriptableObjects/LogicSO/SaveSystem/SaveSystem.asset");
  var ledger=new SerializedObject(global);ledger.FindProperty("_saveSystem").objectReferenceValue=save;ledger.ApplyModifiedProperties();EditorUtility.SetDirty(global);AssetDatabase.SaveAssets();
  var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
  var owned=AssetDatabase.LoadAssetAtPath<AttributeSO>("Assets/RGame/RoguelikeKit/ScriptableObjects/LogicSO/PowerUp/AttributeSO.asset");
  var am=UnityEngine.Object.FindFirstObjectByType<AttributeManager>(FindObjectsInactive.Include);
  var stage=UnityEngine.Object.FindFirstObjectByType<StageManager>(FindObjectsInactive.Include);
  var budget=new SerializedObject(stage);budget.FindProperty("maxActiveEnemies").intValue=40;budget.ApplyModifiedProperties();EditorUtility.SetDirty(stage);
  var lm=UnityEngine.Object.FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
  var level=new SerializedObject(lm);level.FindProperty("_attributes").objectReferenceValue=am;level.FindProperty("_playerSpawn").objectReferenceValue=new SerializedObject(stage).FindProperty("playerSpawnSO").objectReferenceValue;level.ApplyModifiedProperties();EditorUtility.SetDirty(lm);
  var so=new SerializedObject(am);var p=so.FindProperty("_ownedUpgrades");p.objectReferenceValue=owned;bool applied=so.ApplyModifiedProperties();
  EditorUtility.SetDirty(am);EditorSceneManager.MarkSceneDirty(scene);bool saved=EditorSceneManager.SaveScene(scene);
  string immediate=AssetDatabase.GetAssetPath(new SerializedObject(am).FindProperty("_ownedUpgrades").objectReferenceValue);
  EditorSceneManager.OpenScene("Assets/RGame/RoguelikeKit/Scenes/Initialization.unity",OpenSceneMode.Single);
  scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);am=UnityEngine.Object.FindFirstObjectByType<AttributeManager>(FindObjectsInactive.Include);
  var value=new SerializedObject(am).FindProperty("_ownedUpgrades").objectReferenceValue;
  string reloaded=AssetDatabase.GetAssetPath(value);
  return $"source={owned}, applied={applied}, saved={saved}, immediate={immediate}, reloaded={reloaded}, scene={scene.path}";
 }
}
