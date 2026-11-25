using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using RGame.RoguelikeKit;
public static class BossRuntimeInspect
{
 public static string Prepare(){EditorSceneManager.OpenScene("Assets/RGame/RoguelikeKit/Scenes/Initialization.unity");AudioListener.volume=0;return "Initialization opened; isolated preview only";}
 public static string Setup(){AudioListener.volume=0;for(int kind=0;kind<5;kind++){var boss=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("SGBosses/Prefabs/"+BossCatalog.Ids[kind]),new Vector3(10000+kind*20,10000,0),Quaternion.identity);boss.name="QA_Preview_"+kind;boss.GetComponent<EndlessBossEnemy>().enabled=false;boss.GetComponent<BossBrain>().enabled=false;boss.GetComponent<EndlessBossEnemy>().MyWeapon.enabled=false;}return "Spawned five isolated native boss animators; wait for Update";}
 public static string Snapshot(){var rig=GameObject.Find("QA_Preview_0").GetComponent<BossVisualAnimation>();return $"frame={Time.frameCount}, clock={rig.AnimationClock}, sprite={rig.AnimatedRenderer.sprite.name}, authoredFrames={rig.Frames.Length}, stateClips={rig.StateClips.Length}, animatorTime={rig.NativeAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime}";}
}
