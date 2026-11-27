using System;
using DATN.Trial;
using DATN.UI;
using DATN.Modes;
using DATN.Localization;
using RGame.Framework;
using UnityEngine;
namespace RGame.RoguelikeKit
{
    [DefaultExecutionOrder(1000)]
    public sealed class EndlessBossDirector : MonoBehaviour
    {
        PoolRuntimeSO pool;
        GlobalConfigSO global;
        EnemySystem enemies;
        Action clearWave;
        bool endless, rewarded;
        bool bossRush, survivalBossRequested;
        int finalKind=-1, finiteDefeated, finiteTarget;
        float breakSeconds=5, nextFiniteAt;
        bool rushPrepared,intermissionPending;
        int startingChoices;
        bool startingChoicesGrantLevels;
        public bool SuppressOrdinarySpawns => bossRush || survivalBossRequested;
        public bool FiniteComplete => finiteTarget>0 && finiteDefeated>=finiteTarget && ActiveBoss==null;
        public int DefeatedCount => finiteDefeated;
        public bool HasSurvivalBoss => finalKind>=0;
        public string FiniteObjective
        {
            get
            {
                string text=finiteTarget>0?LanguageManager.GetFormatted("Objective_BossProgress",finiteDefeated,finiteTarget)
                    :LanguageManager.GetFormatted("Objective_BossRushCount",finiteDefeated);
                if(bossRush&&rushPrepared&&!intermissionPending&&ActiveBoss==null&&round!=null&&nextFiniteAt>round.MatchSeconds)
                    text+="\n"+LanguageManager.GetFormatted("Objective_BossCountdown",Mathf.CeilToInt(nextFiniteAt-round.MatchSeconds));
                return text;
            }
        }
        public bool WaitingAtArena => survivalBossRequested&&ActiveBoss==null&&!FiniteComplete&&DATN.World.StageLayout.Current!=null
            &&global.GlobalPlayer!=null&&!DATN.World.StageLayout.Current.ReadyForBoss(global.GlobalPlayer.transform.position);
        float nextAt=300, baseCameraSize;
        Vector3 cameraCenter;
        Camera cam;
        TrialController round;
        BossArrivalBanner arrival;
        BossVisualAnimation activeVisual;
        BossEntrance entrance;
        readonly BossEncounterDeck deck=new BossEncounterDeck();
        public BossDeckState CaptureDeck()=>deck.Capture();
        internal void RestoreDeck(BossDeckState state){if(!deck.Restore(state))throw new ArgumentException("Invalid saved boss bag");}
        internal void PrepareRestoredBoss(EndlessBossEnemy boss,bool entering)
        {
            ActiveBoss=boss;activeVisual=boss.GetComponent<BossVisualAnimation>();
            if(!endless&&DATN.World.StageLayout.Current!=null)DATN.World.StageLayout.Current.BossArenaLocked=true;
            boss.ConfigureContext(global,pool);boss.OnDeath+=OnDefeated;boss.Returned+=OnReturned;
            var brain=boss.GetComponent<BossBrain>();var hero=global.GlobalPlayer.GetComponentInChildren<PlayerHit>();
            brain.Begin(brain.Kind,hero);
            cam=Camera.main;
            entrance=boss.GetComponent<BossEntrance>()??boss.gameObject.AddComponent<BossEntrance>();
            if(entering)
            {
                entrance.Begin(brain.Kind,()=>brain.Begin(brain.Kind,hero),global.GlobalPlayer.GetComponent<SpriteRenderer>());
                arrival.Show(brain.Kind);
            }
        }
        public BossArrivalBanner ArrivalBanner => arrival;
        public EndlessBossEnemy ActiveBoss { get; private set; }
        public int EncounterCount { get; private set; }
        public float NextAt => nextAt;
        public bool IsEncounterActive => ActiveBoss != null;
        public void Initialize(PoolRuntimeSO p,GlobalConfigSO g,EnemySystem e,Action clear)
        {
            pool=p;global=g;enemies=e;clearWave=clear;
            arrival=GetComponent<BossArrivalBanner>()??gameObject.AddComponent<BossArrivalBanner>();
        }
        public void ResetRun(bool isEndless)
        {
            CancelEncounter();endless=isEndless;EncounterCount=0;nextAt=300;deck.Reset();
            round=FindFirstObjectByType<TrialController>();
            var config=round!=null?round.RunModeConfig:null;
            if(config==null&&round!=null)config=round.RunSelection.SelectLevelSO.GameMode;
            bossRush=config!=null&&config.ModeId==GameModeId.BossRush;
            finalKind=config!=null&&config.ModeId==GameModeId.Survival?config.FinalBossKind:-1;
            if(RunCheckpointSession.UsesLegacyWorld)finalKind=-1;
            finiteTarget=bossRush?Mathf.Max(0,config.BossRushCount):finalKind>=0?1:0;
            finiteDefeated=0;survivalBossRequested=false;
            if(DATN.World.StageLayout.Current!=null)DATN.World.StageLayout.Current.BossArenaLocked=bossRush;
            breakSeconds=config!=null?Mathf.Max(0,config.BossBreakSeconds):5;
            nextFiniteAt=0;
            rushPrepared=false;intermissionPending=false;
            startingChoices=bossRush?Mathf.Max(0,config.BossRushStartingChoices):0;
            startingChoicesGrantLevels=config!=null&&config.BossRushStartingChoicesGrantLevels;
        }
        public void RequestSurvivalBoss()
        {
            if(finalKind<0||survivalBossRequested)return;
            survivalBossRequested=true;nextFiniteAt=round.MatchSeconds;clearWave();
        }
        public void Tick()
        {
            if (round==null)round=FindFirstObjectByType<TrialController>();
            if((!endless&&!bossRush&&!survivalBossRequested)||round==null||round.IsEnded||round.PlayerStat.GetValue("HP")<=0)
            {if(ActiveBoss!=null)CancelEncounter();return;}
            if(Time.deltaTime<=0||global.GlobalPlayer==null)return;
            if(bossRush)
            {
                var levels=FindFirstObjectByType<LevelManager>();
                if(!rushPrepared)
                {
                    rushPrepared=true;
                    if(startingChoicesGrantLevels)levels.GrantLevels(startingChoices);else levels.GrantUpgradeChoices(startingChoices);
                    return;
                }
                if(levels.PendingChoices>0||levels.HasOpenChoice)return;
                if(intermissionPending){intermissionPending=false;nextFiniteAt=round.MatchSeconds+breakSeconds;}
            }
            if(ActiveBoss!=null)
            {
                if(ActiveBoss.StatRuntime!=null&&ActiveBoss.StatRuntime.GetValue("HP")<=0)
                    ActiveBoss.GetComponent<BossBrain>().StopFight();
                return;
            }
            if(endless&&round.MatchSeconds>=nextAt)Spawn();
            else if(!endless&&!FiniteComplete&&!WaitingAtArena&&round.MatchSeconds>=nextFiniteAt)Spawn();
        }
        void Spawn()
        {
            int kind=!endless&&!bossRush?finalKind:deck.Peek();
            string key="EndlessBoss_"+BossCatalog.Ids[kind];
            if(pool.GetPrefab(key)==null)
            {
                var prefab=Resources.Load<GameObject>("SGBosses/Prefabs/"+BossCatalog.Ids[kind]);
                if(prefab==null){Debug.LogError("Missing boss prefab: "+key);return;}
                pool.Register(new PoolDefinition(key,new SolePool(prefab,1)));
            }
            if(endless||bossRush)deck.Draw();
            clearWave();
            foreach(var missile in FindObjectsByType<EnemyMagic>(FindObjectsSortMode.None))pool.Return(missile.gameObject);
            var obj=pool.Request(key);
            ActiveBoss=obj.GetComponent<EndlessBossEnemy>();activeVisual=obj.GetComponent<BossVisualAnimation>();rewarded=false;
            ActiveBoss.ConfigureContext(global,pool);
            ActiveBoss.SetEncounterHealth(300*(EncounterCount+1));
            var layout=DATN.World.StageLayout.Current;
            if(!endless&&layout!=null)
            {
                layout.BossArenaLocked=true;
                obj.transform.position=layout.BossSpawn;
            }
            else obj.transform.position=global.GlobalPlayer.transform.position+new Vector3(5.5f,-7,0);
            ActiveBoss.OnDeath+=OnDefeated;ActiveBoss.Returned+=OnReturned;enemies.AddEnemy(ActiveBoss);
            var brain=ActiveBoss.GetComponent<BossBrain>();brain.Kind=kind;
            var sorting=ActiveBoss.GetComponent<DynamicSortOrder>();if(sorting!=null)sorting.enabled=false;
            var heroRenderer=global.GlobalPlayer.GetComponent<PlayerAnim>().MyAnimator.GetComponent<SpriteRenderer>();
            ActiveBoss.MySpriteRenderer.sortingOrder=heroRenderer.sortingOrder-5;
            entrance=obj.GetComponent<BossEntrance>()??obj.AddComponent<BossEntrance>();
            var arrivingBoss=ActiveBoss;
            entrance.Begin(kind,()=>
            {
                if(ActiveBoss==arrivingBoss&&round!=null&&!round.IsEnded&&round.PlayerStat.GetValue("HP")>0)
                    brain.Begin(kind,global.GlobalPlayer.GetComponentInChildren<PlayerHit>());
            },heroRenderer);
            arrival.Show(kind);
            EncounterCount++;nextAt=(Mathf.Floor(round.MatchSeconds/300)+1)*300;
            cam=Camera.main;
            if(cam!=null){baseCameraSize=cam.orthographicSize;cameraCenter=cam.transform.position;}
        }
        void OnDefeated(BaseEnemy enemy)
        {
            if(rewarded||enemy!=ActiveBoss)return;
            rewarded=true;
            if(round!=null&&!round.IsEnded&&round.PlayerStat.GetValue("HP")>0)
            {
                global.CurrentGetGold+=500;
                global.BankGold();
                FindFirstObjectByType<LevelManager>()?.GrantLevels(5);
                if(!endless)finiteDefeated++;
                if(bossRush)intermissionPending=true;
            }
        }
        void OnReturned(BaseEnemy enemy)
        {
            enemy.OnDeath-=OnDefeated;enemy.Returned-=OnReturned;enemies.RemoveEnemy(enemy);
            ActiveBoss=null;activeVisual=null;entrance=null;BossHazard.ClearAll();RestoreCamera();
            if(DATN.World.StageLayout.Current!=null)DATN.World.StageLayout.Current.BossArenaLocked=bossRush;
            arrival.Hide();
            if(!endless&&!bossRush)nextFiniteAt=round!=null?round.MatchSeconds+breakSeconds:breakSeconds;
            if(round!=null&&nextAt<=round.MatchSeconds)nextAt=(Mathf.Floor(round.MatchSeconds/300)+1)*300;
        }
        public void CancelEncounter()
        {
            if(DATN.World.StageLayout.Current!=null)DATN.World.StageLayout.Current.BossArenaLocked=false;
            if(arrival!=null)arrival.Hide();
            BossHazard.ClearAll();
            if(ActiveBoss!=null)
            {
                entrance?.Cancel();
                var enemy=ActiveBoss;enemy.OnDeath-=OnDefeated;enemy.Returned-=OnReturned;
                enemies.RemoveEnemy(enemy);pool.Return(enemy.gameObject);ActiveBoss=null;activeVisual=null;
                entrance=null;
            }
            RestoreCamera();
        }
        void RestoreCamera(){if(cam!=null&&baseCameraSize>0)cam.orthographicSize=baseCameraSize;baseCameraSize=0;}
        void LateUpdate()
        {
            if(ActiveBoss==null||cam==null||global.GlobalPlayer==null||Time.deltaTime<=0)return;
            var bounds=entrance!=null&&entrance.IsEntering?entrance.TargetBounds:
                activeVisual!=null?activeVisual.StableWorldBounds:ActiveBoss.MySpriteRenderer.bounds;
            bounds.Encapsulate(global.GlobalPlayer.transform.position+Vector3.up*2);
            bounds.Encapsulate(global.GlobalPlayer.transform.position-Vector3.up);
            bounds.Expand(new Vector3(5,5,0));
            float size=Mathf.Max(baseCameraSize*1.2f,bounds.size.y*.55f,bounds.size.x*.55f/cam.aspect);
            float blend=1-Mathf.Exp(-Time.deltaTime*3);
            cam.orthographicSize=Mathf.Lerp(cam.orthographicSize,Mathf.Min(baseCameraSize*2.2f,size),blend);
            cameraCenter=Vector3.Lerp(cameraCenter,new Vector3(bounds.center.x,bounds.center.y+cam.orthographicSize*.18f,cam.transform.position.z),blend);
            cam.transform.position=cameraCenter;
        }
        void OnDisable(){CancelEncounter();}
    }
}
