using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DATN.Trial;
using RGame.CommonStat;
using RGame.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace RGame.RoguelikeKit
{
    // World snapshots contain functional actors, missiles, pickups and hazards.
    // UI, pooled damage numbers and decorative trails are rebuilt by their owners.
    internal sealed class RunCheckpointWorld
    {
        readonly TrialController round;
        readonly StageManager stage;
        readonly SkillManager skills;
        readonly RunStateFields fields=new();
        readonly Dictionary<string,GameObject> entities=new();
        static readonly string[] MaterialFloats={"_Cutoff","_EdgeWidth","_TimeSpeed","_Alpha"};
        static readonly string[] MaterialColors={"_BlendColor","_ShadowColor","_FlameRedBlend"};
        public RunCheckpointWorld(TrialController round)
        {this.round=round;stage=Object.FindFirstObjectByType<StageManager>();skills=Object.FindFirstObjectByType<SkillManager>();}
        static bool Functional(GameObject go)=>go.GetComponent<BaseEnemy>()!=null||go.GetComponent<SkillBase>()!=null||go.GetComponent<EnemyMagic>()!=null||go.GetComponent<IDropOut>()!=null||go.GetComponent<TreasureBox>()!=null||go.GetComponent<BossHazard>()!=null;
        static bool Script(Component c)=>c is MonoBehaviour&&c is not SoftContactShadow&&c is not BossSkillVfx&&c is not BossCastingVfx&&c.GetType().Namespace=="RGame.RoguelikeKit";
        static string Path(Transform root,Transform child)
        {if(root==child)return "";return Path(root,child.parent)+"/"+child.GetSiblingIndex();}
        static Transform At(Transform root,string path)
        {
            foreach(string part in path.Split('/',StringSplitOptions.RemoveEmptyEntries))
            {if(!int.TryParse(part,out int i)||i<0||i>=root.childCount)throw new ArgumentException("Missing checkpoint transform");root=root.GetChild(i);}return root;
        }
        IEnumerable<Transform> Children(GameObject root)
        {
            return root.GetComponentsInChildren<Transform>(true).Where(t=>t.GetComponentInParent<Canvas>()==null&&t.GetComponent<LineRenderer>()==null
                &&!entities.Values.Any(e=>e!=root&&(t==e.transform||t.IsChildOf(e.transform))));
        }
        static Component[] Components(Transform t)=>t.GetComponents<Component>().Where(c=>Script(c)||c is Rigidbody2D||c is SpriteRenderer||c is Animator||c is Collider2D).ToArray();
        void BindWorld()
        {
            fields.BindAssets();fields.Bind("player-stats",round.PlayerStat);skills.BindRunSkills(fields);
            foreach(var pair in entities)
            {
                fields.Bind(pair.Key,pair.Value);
                foreach(var t in Children(pair.Value))
                {
                    string path=pair.Key+Path(pair.Value.transform,t);fields.Bind(path+":Transform",t);
                    foreach(var c in Components(t))fields.Bind(path+":"+c.GetType().FullName+":"+Array.IndexOf(t.GetComponents(c.GetType()),c),c);
                }
                if(pair.Value.TryGetComponent<BaseEnemy>(out var enemy))fields.Bind(pair.Key+":stats",enemy.StatRuntime);
            }
        }
        static List<RunStat> Stats(CommonStatRuntimeSO stat)=>stat.GetAllValues().Select(p=>new RunStat{key=p.Key,value=stat.GetValue(p.Key),max=stat.GetMaxValue(p.Key)}).ToList();
        static void RestoreStats(CommonStatRuntimeSO stat,List<RunStat> values)
        {
            foreach(var value in values)
            {
                if(value.max>0)stat.ModifyValue(value.key,value.max-stat.GetMaxValue(value.key),true);
                stat.ModifyValue(value.key,value.value-stat.GetValue(value.key));
            }
        }
        static string SpriteId(Sprite sprite)=>sprite==null?null:sprite.texture.name+":"+sprite.name;
        static Sprite FindSprite(string id)=>string.IsNullOrEmpty(id)?null:Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(s=>SpriteId(s)==id);
        IEnumerable<object> Systems()
        {
            yield return round;yield return round.GlobalConfig;yield return stage;yield return skills;
            yield return Object.FindFirstObjectByType<LevelManager>();yield return Object.FindFirstObjectByType<EndlessBossDirector>();yield return Object.FindFirstObjectByType<DATN.UI.BossArrivalBanner>();
            yield return Object.FindFirstObjectByType<RecoveryAttribute>();
        }
        public RunCheckpoint Capture()
        {
            if(!round.IsPaused||round.IsEnded||round.PlayerStat.GetValue("HP")<=0)throw new InvalidOperationException("Only a paused active round can be saved");
            var state=new RunCheckpoint{worldRevision=DATN.World.StageLayout.Current!=null?DATN.World.StageLayout.CurrentRevision:0,id=Guid.NewGuid().ToString(),savedUtc=DateTime.UtcNow.ToString("O"),hero=round.RunSelection.SelectCharacterSo.CharacterName,stage=round.RunSelection.SelectLevelSO.name,
                clock=round.MatchSeconds,random=JsonUtility.ToJson(UnityEngine.Random.state),stats=Stats(round.PlayerStat)};
            entities.Add("hero",round.GlobalConfig.GlobalPlayer.gameObject);
            int index=0;
            foreach(var item in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.InstanceID))
                if(item is IPoolR&&Functional(item.gameObject)&&!entities.Values.Contains(item.gameObject))entities.Add("entity:"+(index++),item.gameObject);
            foreach(var hazard in Object.FindObjectsByType<BossHazard>(FindObjectsSortMode.InstanceID))entities.Add("hazard:"+(index++),hazard.gameObject);
            foreach(var prop in Object.FindObjectsByType<DropBox>(FindObjectsSortMode.InstanceID))entities.Add("prop:"+(index++),prop.gameObject);
            BindWorld();state.skills=skills.CaptureRunSkills(fields);
            foreach(var pair in entities)state.entities.Add(CaptureEntity(pair.Key,pair.Value));
            foreach(var system in Systems())if(system!=null)state.systems.Add(new RunSystemState{type=system.GetType().FullName,fields=fields.Capture(system)});
            state.mode=fields.Capture(round.RunMode);
            state.deck=Object.FindFirstObjectByType<EndlessBossDirector>().CaptureDeck();stage.CaptureRunSchedule(state);
            state.buffs=Object.FindFirstObjectByType<BuffManager>().CaptureRunBuffs(fields);
            state.timers=Object.FindFirstObjectByType<TimeStepManager>().CaptureRunTimers(fields);
            if(Camera.main!=null){state.cameraPosition=Camera.main.transform.position;state.cameraSize=Camera.main.orthographicSize;}
            return state;
        }
        RunEntity CaptureEntity(string id,GameObject go)
        {
            var entity=new RunEntity{id=id,key=go.GetComponent<IPoolR>()?.Key,position=go.transform.position,rotation=go.transform.rotation};
            if(go.GetComponent<DropBox>()!=null)entity.key="scene:DropBox";
            if(go.TryGetComponent<BaseEnemy>(out var enemy)){entity.stats=Stats(enemy.StatRuntime);entity.maxHP=enemy.MaxHealth;entity.entering=enemy is EndlessBossEnemy b&&b.Entering;entity.deathProgress=enemy.MyHit.CaptureDeathProgress();}
            foreach(var t in Children(go))
            {
                string path=Path(go.transform,t);entity.transforms.Add(new RunTransform{path=path,position=t.localPosition,scale=t.localScale,rotation=t.localRotation,active=t.gameObject.activeSelf});
                foreach(var c in Components(t))
                {
                    var state=new RunComponent{path=path,type=c.GetType().FullName,index=Array.IndexOf(t.GetComponents(c.GetType()),c),enabled=c is Behaviour behaviour?behaviour.enabled:c is Renderer renderer?renderer.enabled:true};
                    if(Script(c))state.fields=fields.Capture(c);
                    if(c is Rigidbody2D rb){state.velocity=rb.linearVelocity;state.angularVelocity=rb.angularVelocity;}
                    if(c is Animator a&&a.runtimeAnimatorController!=null){var pose=a.GetCurrentAnimatorStateInfo(0);state.animationHash=pose.fullPathHash;state.animationTime=pose.normalizedTime;}
                    if(c is Collider2D collider){state.enabled=collider.enabled;state.velocity=collider.offset;if(c is CircleCollider2D circle)state.angularVelocity=circle.radius;if(c is BoxCollider2D box)state.color=new Color(box.size.x,box.size.y,0,0);}
                    if(c is SpriteRenderer sprite)
                    {
                        state.color=go.TryGetComponent<Player>(out var player)?player.GetComponentInChildren<PlayerHit>().RunRestingColor(sprite):sprite.color;
                        state.sprite=SpriteId(sprite.sprite);state.flipX=sprite.flipX;state.flipY=sprite.flipY;state.material=new List<RunField>();
                        var material=sprite.sharedMaterial;
                        if(material!=null)
                        {
                            foreach(string property in MaterialFloats)if(material.HasProperty(property))state.material.Add(new RunField{name=property,declaring="float",value=material.GetFloat(property).ToString("R",System.Globalization.CultureInfo.InvariantCulture)});
                            foreach(string property in MaterialColors)if(material.HasProperty(property))
                            {Color color=property=="_BlendColor"&&enemy!=null?enemy.MyHit.RunNormalBlend:material.GetColor(property);state.material.Add(new RunField{name=property,vector=new Vector4(color.r,color.g,color.b,color.a)});}
                        }
                    }
                    entity.components.Add(state);
                }
            }
            return entity;
        }
        public IEnumerator Restore(RunCheckpoint state)
        {
            Time.timeScale=0;Object.FindFirstObjectByType<BuffManager>().ClearAllBuffs();stage.PrepareRunRestore();
            if(state.worldRevision<1&&!skills.ConfigureLegacyStarter())throw new ArgumentException("Legacy starter configuration is unavailable");
            skills.PrepareRunSkills(state.skills);entities.Add("hero",round.GlobalConfig.GlobalPlayer.gameObject);
            var freshProps=Object.FindObjectsByType<DropBox>(FindObjectsSortMode.None);
            foreach(var entity in state.entities.Where(e=>e.id!="hero"&&!e.id.StartsWith("hazard:")))
            {
                if(entity.key=="scene:DropBox")
                {
                    if(freshProps.Length==0)throw new ArgumentException("Missing scene chest template");
                    var prop=Object.Instantiate(freshProps[0].gameObject,GameObject.Find("Map").transform);
                    prop.SetActive(true);entities.Add(entity.id,prop);continue;
                }
                if(stage.RunPool.GetPrefab(entity.key)==null&&entity.key.StartsWith("EndlessBoss_"))
                {
                    string key=entity.key.Substring("EndlessBoss_".Length);var prefab=Resources.Load<GameObject>("SGBosses/Prefabs/"+key);
                    if(prefab==null)throw new ArgumentException("Missing saved boss prefab");stage.RunPool.Register(new PoolDefinition(entity.key,new SolePool(prefab,1)));
                }
                if(stage.RunPool.GetPrefab(entity.key)==null)throw new ArgumentException("Missing saved pool: "+entity.key);
                var go=stage.RunPool.Request(entity.key);entities.Add(entity.id,go);go.transform.SetPositionAndRotation(entity.position,entity.rotation);
                if(go.TryGetComponent<BaseEnemy>(out var enemy))
                {enemy.ConfigureContext(round.GlobalConfig,stage.RunPool);enemy.SetEncounterHealth(entity.maxHP);stage.AttachRunEnemy(enemy);}
                if(go.TryGetComponent<SkillBase>(out var skill))skill.OnDeath=s=>{s.transform.localScale=stage.RunPool.GetPrefab(s.Key).transform.localScale;stage.RunPool.Return(s.gameObject);};
            }
            foreach(var prop in freshProps){prop.gameObject.SetActive(false);Object.Destroy(prop.gameObject);}
            yield return null; // Run pooled Start initializers with gameplay time frozen.
            Time.timeScale=0;
            var director=Object.FindFirstObjectByType<EndlessBossDirector>();
            foreach(var entity in state.entities.Where(e=>entities.ContainsKey(e.id)))
                if(entities[entity.id].TryGetComponent<EndlessBossEnemy>(out var boss))director.PrepareRestoredBoss(boss,entity.entering);
            foreach(var entity in state.entities.Where(e=>e.id.StartsWith("hazard:")))
            {
                var boss=director.ActiveBoss;if(boss==null)throw new ArgumentException("Saved hazard has no boss");
                var hazard=BossHazard.Create(boss.transform,round.GlobalConfig.GlobalPlayer.GetComponentInChildren<PlayerHit>(),BossHazard.Shape.Disc,Vector2.zero,Vector2.zero,1,1,1,Color.white);
                if(hazard==null)throw new InvalidOperationException("Unable to restore hazard");entities.Add(entity.id,hazard.gameObject);
            }
            BindWorld();
            foreach(var skill in state.skills)fields.Restore(fields.Resolve("skill:"+skill.key),skill.fields);
            Object.FindFirstObjectByType<BuffManager>().RestoreRunBuffs(state.buffs,fields);
            foreach(var entity in state.entities)RestoreEntity(entity);
            foreach(var system in Systems())
            {
                if(system==null)continue;var saved=state.systems.Single(s=>s.type==system.GetType().FullName);fields.Restore(system,saved.fields);
            }
            fields.Restore(round.RunMode,state.mode);Object.FindFirstObjectByType<LevelManager>().RestoreRunStats(()=>RestoreStats(round.PlayerStat,state.stats));
            director.RestoreDeck(state.deck);stage.RestoreRunSchedule(state);
            Object.FindFirstObjectByType<TimeStepManager>().RestoreRunTimers(state.timers,fields);
            foreach(var entity in state.entities)
            {
                var go=entities[entity.id];if(go.TryGetComponent<BaseEnemy>(out var enemy))enemy.MyHit.ResumeDeathProgress(entity.deathProgress);
                if(go.TryGetComponent<BossHazard>(out var hazard))hazard.RestoreVisual();
                go.GetComponent<BossEntrance>()?.RestoreFrozenVisual();go.GetComponent<BossCastingVfx>()?.RestoreFrozenVisual();
            }
            if(Camera.main!=null){Camera.main.transform.position=state.cameraPosition;Camera.main.orthographicSize=state.cameraSize;}
            UnityEngine.Random.state=JsonUtility.FromJson<UnityEngine.Random.State>(state.random);Physics2D.SyncTransforms();round.RestorePauseView();
        }
        void RestoreEntity(RunEntity entity)
        {
            var go=entities[entity.id];
            foreach(var saved in entity.transforms)
            {var t=At(go.transform,saved.path);t.localPosition=saved.position;t.localRotation=saved.rotation;t.localScale=saved.scale;t.gameObject.SetActive(saved.active);}
            go.transform.SetPositionAndRotation(entity.position,entity.rotation);
            foreach(var saved in entity.components)
            {
                var t=At(go.transform,saved.path);var matches=t.GetComponents<Component>().Where(c=>c!=null&&c.GetType().FullName==saved.type).ToArray();
                if(saved.index<0||saved.index>=matches.Length)throw new ArgumentException("Missing saved component "+saved.type);var c=matches[saved.index];
                if(Script(c))fields.Restore(c,saved.fields);
                if(c is Behaviour behaviour)behaviour.enabled=saved.enabled;if(c is Renderer renderer)renderer.enabled=saved.enabled;
                if(c is Rigidbody2D rb){rb.linearVelocity=saved.velocity;rb.angularVelocity=saved.angularVelocity;}
                if(c is Collider2D collider){collider.enabled=saved.enabled;collider.offset=saved.velocity;if(c is CircleCollider2D circle)circle.radius=saved.angularVelocity;if(c is BoxCollider2D box)box.size=new Vector2(saved.color.r,saved.color.g);}
                if(c is Animator a&&saved.animationHash!=0){a.Play(saved.animationHash,0,saved.animationTime);a.Update(0);}
                if(c is SpriteRenderer sprite)
                {
                    var image=FindSprite(saved.sprite);if(!string.IsNullOrEmpty(saved.sprite)&&image==null)throw new ArgumentException("Missing saved sprite "+saved.sprite);
                    sprite.sprite=image;sprite.color=saved.color;sprite.flipX=saved.flipX;sprite.flipY=saved.flipY;
                    if(saved.material!=null)foreach(var value in saved.material)
                    {
                        var material=sprite.material;if(!material.HasProperty(value.name))continue;
                        if(value.declaring=="float")material.SetFloat(value.name,float.Parse(value.value,System.Globalization.CultureInfo.InvariantCulture));
                        else material.SetColor(value.name,new Color(value.vector.x,value.vector.y,value.vector.z,value.vector.w));
                    }
                }
            }
            if(go.TryGetComponent<BaseEnemy>(out var enemy))RestoreStats(enemy.StatRuntime,entity.stats);
        }
    }
}
