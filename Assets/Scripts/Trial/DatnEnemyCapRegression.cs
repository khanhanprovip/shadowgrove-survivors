using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RGame.RoguelikeKit;
using RGame.RoguelikeKit.RGame.RoguelikeKit;
using UnityEngine;
namespace DATN.Trial
{
    public sealed partial class DatnRegression
    {
        IEnumerator CheckEnemyCap()
        {
            yield return EnterRound(8,"Kael");
            _round.PlayerStat.ModifyValue("HP",100000,true);_round.PlayerStat.ModifyValue("HP",100000);
            _round.GlobalConfig.GlobalPlayer.GetComponent<StarterWeaponProfile>().Range=0;
            var stage=FindFirstObjectByType<StageManager>();
            Require(stage.EnemyLimit==40,"Shared enemy budget is 40 active normal enemies across the entire map");
            float until=Time.realtimeSinceStartup+80;
            int previous=stage.SpawnedThisRun,peakPerFrame=0;
            while(ActiveEnemies()<stage.EnemyLimit&&Time.realtimeSinceStartup<until)
            {
                Time.timeScale=5;
                int spawned=stage.SpawnedThisRun;
                peakPerFrame=Mathf.Max(peakPerFrame,spawned-previous);previous=spawned;
                yield return null;
            }
            Time.timeScale=1;
            Require(ActiveEnemies()==40,"Real wave spawning reaches but does not exceed the enemy cap");
            Require(peakPerFrame<=2,$"Largest normal spawn batch observed in a frame: {peakPerFrame} (budget 2)");
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var sets=(List<SpawnSet>)typeof(StageManager).GetField("_spawnSets",flags).GetValue(stage);
            var spawn=typeof(StageManager).GetMethod("SpawnEnemy",flags);
            typeof(StageManager).GetField("spawnTokens",flags).SetValue(stage,2f);
            int before=stage.SpawnedThisRun;
            for(int i=0;i<200;i++)spawn.Invoke(stage,new object[]{sets[0],_round.GlobalConfig.GlobalPlayer.transform.position+Vector3.right*14});
            Require(stage.SpawnedThisRun==before&&ActiveEnemies()==40,"200 additional spawn requests at full capacity create no extra enemy");
            float started=Time.realtimeSinceStartup,worstMs=0;int frames=0;
            while(Time.realtimeSinceStartup-started<3)
            {
                if(ActiveEnemies()>40)Require(false,"Enemy budget exceeded while the cap was full");
                frames++;worstMs=Mathf.Max(worstMs,Time.unscaledDeltaTime*1000);yield return null;
            }
            Record("INFO",$"Full-cap stationary bot: {frames/(Time.realtimeSinceStartup-started):0} FPS average, worst sampled frame {worstMs:0.0} ms, GPU {SystemInfo.graphicsDeviceName}");
            var enemy=FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None).First();
            enemy.MyHit.Hit(1000);
            yield return Wait(()=>stage.SpawnedThisRun>before,"A real enemy death frees one slot for the next spawn");
            Require(ActiveEnemies()<=40,"Replacement spawning obeys the same cap after a real kill");
            Require(!FindFirstObjectByType<EndlessBossDirector>().IsEncounterActive,"Normal-enemy cap does not trigger or accelerate the boss schedule");
            _round.Pause();yield return CaptureFrame("enemy-cap-40");
            var oldPlayer=_round.GlobalConfig.GlobalPlayer;
            _round.Restart();
            yield return Wait(()=>
            {
                var next=FindFirstObjectByType<TrialController>();
                if(next==null||next.GlobalConfig.GlobalPlayer==null||next.GlobalConfig.GlobalPlayer==oldPlayer)return false;
                _round=next;return true;
            },"Restart recreates the capped run");
            stage=FindFirstObjectByType<StageManager>();
            Require(stage.EnemyLimit==40&&stage.SpawnedThisRun<=2&&ActiveEnemies()<=2&&_round.GlobalConfig.CurrentGetKill==0,
                "Restart keeps the 40-enemy setting and clears the old wave, queue and kill count");
            var wallet=Resources.FindObjectsOfTypeAll<AttributeSO>().Single(x=>x.name=="AttributeSO");
            int walletBefore=wallet.GoldCount;
            JumpClock(300);yield return Wait(()=>FindFirstObjectByType<EndlessBossDirector>().ActiveBoss!=null,"QA clock advance starts a boss for cancellation coverage");
            oldPlayer=_round.GlobalConfig.GlobalPlayer;
            _round.Restart();
            yield return Wait(()=>
            {
                var next=FindFirstObjectByType<TrialController>();
                if(next==null||next.GlobalConfig.GlobalPlayer==null||next.GlobalConfig.GlobalPlayer==oldPlayer)return false;
                _round=next;return true;
            },"Restart cancels an undefeated boss");
            var director=FindFirstObjectByType<EndlessBossDirector>();
            Require(director.ActiveBoss==null&&director.EncounterCount==0&&director.NextAt==300&&BossHazard.ActiveCount==0,
                "Boss restart resets schedule and removes the old boss and all hazards");
            Require(_round.PlayerStat.GetValue("Level")==1&&wallet.GoldCount==walletBefore,
                "Cancelling a living boss grants neither levels nor gold");
            _round.ExitToMainMenu();yield return WaitForMenu();
        }
    }
}
