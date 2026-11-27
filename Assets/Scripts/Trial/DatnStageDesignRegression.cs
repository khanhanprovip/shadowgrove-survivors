using System.Collections;
using System.Linq;
using DATN.Modes;
using DATN.World;
using RGame.RoguelikeKit;
using UnityEngine;

namespace DATN.Trial
{
    public sealed partial class DatnRegression
    {
        IEnumerator CheckEndlessBoundary()
        {
            yield return EnterRound(8,"Kael");
            _round.PlayerStat.ModifyValue("HP",100000,true);_round.PlayerStat.ModifyValue("HP",100000);
            var hero=_round.GlobalConfig.GlobalPlayer;hero.transform.position=new Vector3(50,-50,0);
            var director=FindFirstObjectByType<EndlessBossDirector>();JumpClock(director.NextAt-.01f);
            yield return Wait(()=>director.ActiveBoss!=null&&!director.ActiveBoss.Entering,"Edge boss completes its entrance outside the original ground extent");
            var boss=director.ActiveBoss;Vector3 before=boss.transform.position;
            Require(StageLayout.IsOpen(before,2),"Endless retains open space beyond the decorative map edge");
            yield return new WaitForSeconds(.5f);
            Require(Vector3.Distance(before,boss.transform.position)>.1f,"An Endless boss spawned beyond the map edge can approach the player");
            yield return CaptureFrame("endless-edge-boss");
        }
        IEnumerator FinishFiniteBoss(EndlessBossDirector director,string label,bool isolateRushAutoAttacks=false)
        {
            if(isolateRushAutoAttacks)
            {
                yield return Wait(()=>director.ActiveBoss!=null,label+" spawns before auto-attack isolation");
                var stage=FindFirstObjectByType<StageManager>();
                var roster=stage!=null?(EnemySystem)typeof(StageManager).GetField("enemySystem",Private)?.GetValue(stage):null;
                Require(roster!=null,label+" can isolate the boss from automatic player targeting");
                roster?.RemoveEnemy(director.ActiveBoss);
            }
            yield return Wait(()=>director.ActiveBoss!=null&&!director.ActiveBoss.Entering,label+" entrance completes");
            var boss=director.ActiveBoss;
            Require(isolateRushAutoAttacks?boss.StatRuntime.GetValue("HP")==boss.MaxHealth:boss.StatRuntime.GetValue("HP")>0,
                isolateRushAutoAttacks?label+" retains full HP before the manual hit":label+" has live HP");
            int level=_round.PlayerStat.GetValue("Level"),gold=_round.GlobalConfig.CurrentGetGold;
            boss.MyHit.Hit(1000000);
            var levels=FindFirstObjectByType<LevelManager>();
            float deadline=Time.realtimeSinceStartup+30;
            while((director.ActiveBoss!=null||levels.PendingChoices>0||(isolateRushAutoAttacks&&levels.HasOpenChoice))&&Time.realtimeSinceStartup<deadline)
            {
                if(Time.timeScale<=0&&!_round.IsEnded)PickUpgradeOrTreasure();
                yield return null;
            }
            Require(director.ActiveBoss==null,label+" returns after death");
            Require(_round.PlayerStat.GetValue("Level")==level+5&&_round.GlobalConfig.CurrentGetGold==gold+500,
                label+" awards exactly five levels and 500 gold");
            if(isolateRushAutoAttacks)
                Require(levels.PendingChoices==0&&!levels.HasOpenChoice&&Time.timeScale>0,
                    label+" fully resolves all five reward choices before the intermission clock starts");
        }

        void HealBossRushPlayer()
        {
            HealTestPlayer();
        }
        IEnumerator CheckCampaignBosses()
        {
            foreach(int index in new[]{2,4,7})
            {
                yield return EnterRound(index,Heroes[index%3]);
                _round.PlayerStat.ModifyValue("HP",100000,true);_round.PlayerStat.ModifyValue("HP",100000);
                var director=FindFirstObjectByType<EndlessBossDirector>();
                Require(director.HasSurvivalBoss,"Campaign stage declares a final boss");
                var profile=_round.GlobalConfig.GlobalPlayer.GetComponent<StarterWeaponProfile>();profile.Range=0;
                // Explicit boundary injection; this suite is not a claim of playing 120 real seconds.
                _round.Pause();
                typeof(SurvivalMode).GetField("_remaining",Private).SetValue(_round.RunMode,.02d);
                JumpClock(_selection.SelectLevelSO.GameMode.Duration-.02f);
                _round.Resume();
                yield return Wait(()=>director.ActiveBoss!=null,"Survival timer starts final boss instead of winning");
                Require(!_round.IsEnded&&!Visible("GameWin"),"Survival does not win at timer when a boss remains");
                Require(FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None).All(e=>e is EndlessBossEnemy),"Final boss removes all ordinary mobs");
                Require(director.ActiveBoss.GetComponent<BossBrain>().Kind==_selection.SelectLevelSO.GameMode.FinalBossKind,"Boss matches the stage motif");
                Require(StageLayout.Current.BossArenaLocked,"The wide central boss arena contains the fight");
                if(index==2)
                {
                    _round.Pause();yield return RoundTripCurrentRun("campaign-save-entrance");
                    director=FindFirstObjectByType<EndlessBossDirector>();
                    Require(StageLayout.Current.BossArenaLocked,"Resume reconstructs the boss arena constraint");
                    _round.Resume();
                }
                yield return CaptureFrame("campaign-boss-"+index);
                yield return FinishFiniteBoss(director,"Stage "+index);
                yield return Wait(()=>Visible("GameWin"),"Victory appears only after the final boss dies");
                _round.ExitToMainMenu();yield return WaitForMenu();
            }
        }
        IEnumerator CheckBossRush()
        {
            yield return EnterRound(9,"Kael");
            Require(_selection.SelectLevelSO.GameMode.ModeId==GameModeId.BossRush,"Stage 10 is Boss Rush");
            Require(_selection.SelectLevelSO.GameMode.BossRushStartingChoices==10&&
                    _selection.SelectLevelSO.GameMode.BossRushStartingChoicesGrantLevels&&
                    _round.PlayerStat.GetValue("Level")==11,
                "Boss Rush completes ten starting level choices and begins at level 11");
            _round.PlayerStat.ModifyValue("HP",100000,true);_round.PlayerStat.ModifyValue("HP",100000);
            _round.GlobalConfig.GlobalPlayer.GetComponent<StarterWeaponProfile>().Range=0;
            var director=FindFirstObjectByType<EndlessBossDirector>();
            Require(_selection.SelectLevelSO.GameMode.BossRushCount==0,"Boss Rush is infinite");
            int seen=0,target=6;float intermissionStartedAt=0;
            for(int i=0;i<target;i++)
            {
                yield return Wait(()=>director.ActiveBoss!=null,"Next Boss Rush encounter appears");
                if(i>0)
                    Require(Mathf.Abs((_round.MatchSeconds-intermissionStartedAt)-
                                      _selection.SelectLevelSO.GameMode.BossBreakSeconds)<.3f,
                        "Boss Rush waits five match seconds from the resolved reward choice to the next boss");
                var boss=director.ActiveBoss;int kind=boss.GetComponent<BossBrain>().Kind;
                if(i==5){Require(seen==31,"All five bosses appear before the next bag");seen=0;}
                Require((seen&(1<<kind))==0,"Boss Rush does not repeat before all five types");seen|=1<<kind;
                Require(boss.MaxHealth==300*(i+1),"Boss Rush HP rises by 300 per encounter");
                Require(FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None).All(e=>e is EndlessBossEnemy),"Boss Rush has no ordinary enemies");
                yield return CaptureFrame("boss-rush-"+i);
                yield return FinishFiniteBoss(director,"Rush "+i,true);
                HealBossRushPlayer();
                if(i<target-1)
                {
                    Require(!_round.IsEnded,"Intermediate boss does not end the rush");
                    intermissionStartedAt=_round.MatchSeconds;
                    if(i==2)
                    {
                        float intermissionDeadline=Time.realtimeSinceStartup+8f;
                        while(_round.MatchSeconds-intermissionStartedAt<2f&&director.ActiveBoss==null&&
                              Time.realtimeSinceStartup<intermissionDeadline)yield return null;
                        Require(_round.MatchSeconds-intermissionStartedAt>=2f&&director.ActiveBoss==null,
                            "Boss Rush enters a live intermission before its midpoint save");
                        float pausedAt=_round.MatchSeconds;_round.Pause();yield return new WaitForSecondsRealtime(.3f);
                        Require(_round.MatchSeconds==pausedAt&&director.ActiveBoss==null,"Pause freezes the intermission countdown");
                        yield return RoundTripCurrentRun("boss-rush-intermission");
                        director=FindFirstObjectByType<EndlessBossDirector>();
                        Require(_round.IsPaused&&_round.MatchSeconds==pausedAt&&director.ActiveBoss==null&&
                                director.DefeatedCount==i+1&&!director.FiniteComplete,
                            "Save/resume restores the paused Boss Rush intermission and defeated count");
                        _round.Resume();
                    }
                    else
                    {
                        float before=_round.MatchSeconds;_round.Pause();yield return new WaitForSecondsRealtime(.3f);
                        Require(_round.MatchSeconds==before&&director.ActiveBoss==null,"Pause freezes the intermission countdown");_round.Resume();
                    }
                }
            }
            Require(!Visible("GameWin")&&!director.FiniteComplete&&director.DefeatedCount==6,
                "Boss Rush keeps running after the sixth boss, beyond its first bag");
            // Random upgrade choices can reduce received damage to one. Set the
            // QA survivor to one HP, then exercise the real hit/death path.
            _round.PlayerStat.ModifyValue("HP",1-_round.PlayerStat.GetValue("HP"));
            _round.PlayerStat.ModifyValue("Armor",-50-_round.PlayerStat.GetValue("Armor"));
            _round.GlobalConfig.GlobalPlayer.GetComponentInChildren<PlayerHit>().OnHit(1000000);
            Require(_round.PlayerStat.GetValue("HP")==0,"Negative armor cannot turn incoming damage into healing");
            yield return Wait(()=>Visible("GameOver"),"Infinite Boss Rush ends only when the player dies");
        }
    }
}
