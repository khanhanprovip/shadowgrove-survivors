using System;
using DATN.Trial;
using DATN.UI;
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
        float nextAt=300, baseCameraSize;
        Vector3 cameraCenter;
        Camera cam;
        bool frameImmediately;
        TrialController round;
        BossArrivalBanner arrival;
        BossVisualAnimation activeVisual;
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
            CancelEncounter();endless=isEndless;EncounterCount=0;nextAt=300;
            round=FindFirstObjectByType<TrialController>();
        }
        public void Tick()
        {
            if (round==null)round=FindFirstObjectByType<TrialController>();
            if(!endless||round==null||round.IsEnded||round.PlayerStat.GetValue("HP")<=0)
            {if(ActiveBoss!=null)CancelEncounter();return;}
            if(Time.deltaTime<=0||global.GlobalPlayer==null)return;
            if(ActiveBoss!=null)
            {
                if(ActiveBoss.StatRuntime!=null&&ActiveBoss.StatRuntime.GetValue("HP")<=0)
                    ActiveBoss.GetComponent<BossBrain>().StopFight();
                return;
            }
            if(round.MatchSeconds>=nextAt)Spawn();
        }
        void Spawn()
        {
            int kind=EncounterCount%5;
            string key="EndlessBoss_"+BossCatalog.Ids[kind];
            if(pool.GetPrefab(key)==null)
            {
                var prefab=Resources.Load<GameObject>("SGBosses/Prefabs/"+BossCatalog.Ids[kind]);
                if(prefab==null){Debug.LogError("Missing boss prefab: "+key);return;}
                pool.Register(new PoolDefinition(key,new SolePool(prefab,1)));
            }
            clearWave();
            foreach(var missile in FindObjectsByType<EnemyMagic>(FindObjectsSortMode.None))pool.Return(missile.gameObject);
            var obj=pool.Request(key);
            ActiveBoss=obj.GetComponent<EndlessBossEnemy>();activeVisual=obj.GetComponent<BossVisualAnimation>();rewarded=false;
            ActiveBoss.ConfigureContext(global,pool);
            ActiveBoss.SetEncounterHealth(300*(EncounterCount+1));
            obj.transform.position=global.GlobalPlayer.transform.position+new Vector3(5.5f,-7,0);
            ActiveBoss.OnDeath+=OnDefeated;ActiveBoss.Returned+=OnReturned;enemies.AddEnemy(ActiveBoss);
            ActiveBoss.GetComponent<BossBrain>().Begin(kind,global.GlobalPlayer.GetComponentInChildren<PlayerHit>());
            arrival.Show(kind);
            EncounterCount++;nextAt=(Mathf.Floor(round.MatchSeconds/300)+1)*300;
            cam=Camera.main;
            if(cam!=null){baseCameraSize=cam.orthographicSize;cameraCenter=cam.transform.position;frameImmediately=true;}
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
            }
        }
        void OnReturned(BaseEnemy enemy)
        {
            enemy.OnDeath-=OnDefeated;enemy.Returned-=OnReturned;enemies.RemoveEnemy(enemy);
            ActiveBoss=null;activeVisual=null;BossHazard.ClearAll();RestoreCamera();
            arrival.Hide();
            if(round!=null&&nextAt<=round.MatchSeconds)nextAt=(Mathf.Floor(round.MatchSeconds/300)+1)*300;
        }
        public void CancelEncounter()
        {
            if(arrival!=null)arrival.Hide();
            BossHazard.ClearAll();
            if(ActiveBoss!=null)
            {
                var enemy=ActiveBoss;enemy.OnDeath-=OnDefeated;enemy.Returned-=OnReturned;
                enemies.RemoveEnemy(enemy);pool.Return(enemy.gameObject);ActiveBoss=null;activeVisual=null;
            }
            RestoreCamera();
        }
        void RestoreCamera(){if(cam!=null&&baseCameraSize>0)cam.orthographicSize=baseCameraSize;baseCameraSize=0;}
        void LateUpdate()
        {
            if(ActiveBoss==null||cam==null||global.GlobalPlayer==null||Time.deltaTime<=0)return;
            var bounds=activeVisual!=null?activeVisual.StableWorldBounds:ActiveBoss.MySpriteRenderer.bounds;
            bounds.Encapsulate(global.GlobalPlayer.transform.position+Vector3.up*2);
            bounds.Encapsulate(global.GlobalPlayer.transform.position-Vector3.up);
            bounds.Expand(new Vector3(5,5,0));
            float size=Mathf.Max(baseCameraSize*1.2f,bounds.size.y*.55f,bounds.size.x*.55f/cam.aspect);
            float blend=frameImmediately?1:Time.deltaTime*3;
            cam.orthographicSize=Mathf.Lerp(cam.orthographicSize,Mathf.Min(baseCameraSize*2.2f,size),blend);
            cameraCenter=Vector3.Lerp(cameraCenter,new Vector3(bounds.center.x,bounds.center.y+cam.orthographicSize*.18f,cam.transform.position.z),blend);
            frameImmediately=false;
            cam.transform.position=cameraCenter;
        }
        void OnDisable(){CancelEncounter();}
    }
}
