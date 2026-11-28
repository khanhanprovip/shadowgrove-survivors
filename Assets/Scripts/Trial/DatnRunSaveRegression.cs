using System;
using System.Collections;
using System.Linq;
using DATN.Localization;
using DATN.UI;
using RGame.RoguelikeKit;
using UnityEngine;
using UnityEngine.UI;
namespace DATN.Trial
{
    public sealed partial class DatnRegression
    {
        IEnumerator RoundTripCurrentRun(string label)
        {
            Require(RunCheckpointSession.SaveAndExit(_round),"Save round succeeds: "+RunCheckpointSession.LastError);
            yield return WaitForMenu();Require(RunCheckpointStore.TryRead(out var expected),"Saved boss checkpoint is readable");
            var button=GameObject.Find("ButtonContinueRun").GetComponent<Button>();yield return Wait(()=>CanClick(button),"Continue saved boss receives pointer input");Click(button);
            yield return Wait(()=>{_round=FindFirstObjectByType<TrialController>();return _round!=null&&_round.IsPaused&&!RunCheckpointSession.HasPending;},"Boss checkpoint restores into pause");
            Require(RunCheckpointSession.LastError==null,"Boss checkpoint has no restore error: "+RunCheckpointSession.LastError);
            CheckRunState(expected,new RunCheckpointWorld(_round).Capture());yield return CaptureFrame(label);
        }
        IEnumerator CheckRunSaveBosses()
        {
            yield return EnterRound(8,"Rowan");_round.PlayerStat.ModifyValue("HP",100000,true);_round.PlayerStat.ModifyValue("HP",100000);
            int mask=0;
            for(int encounter=0;encounter<5;encounter++)
            {
                var director=FindFirstObjectByType<EndlessBossDirector>();JumpClock(director.NextAt-.01f);
                yield return Wait(()=>director.ActiveBoss!=null,"Next boss spawns for save test");var boss=director.ActiveBoss;
                int kind=boss.GetComponent<BossBrain>().Kind;Require((mask&(1<<kind))==0,"Saved run preserves the five-boss shuffle bag");mask|=1<<kind;
                yield return Wait(()=>boss.GetComponent<BossEntrance>().Elapsed>BossEntrance.WarningSeconds+.3f,"Boss is partway through its unique entrance");
                _round.Pause();yield return null;yield return RoundTripCurrentRun("saved-boss-"+kind+"-entrance");
                director=FindFirstObjectByType<EndlessBossDirector>();boss=director.ActiveBoss;
                Require(boss.Entering,"Partially completed entrance remains active after restore");_round.Resume();
                yield return Wait(()=>!boss.Entering,"Restored entrance completes and enables combat");
                yield return Wait(()=>boss.GetComponent<BossBrain>().CastAge>.35f,"Boss is preparing a skill before saving again");
                _round.Pause();yield return null;yield return RoundTripCurrentRun("saved-boss-"+kind+"-cast");
                director=FindFirstObjectByType<EndlessBossDirector>();boss=director.ActiveBoss;_round.Resume();
                boss.MyHit.Hit(1000000);float deadline=Time.realtimeSinceStartup+35;
                while((director.ActiveBoss!=null||FindFirstObjectByType<LevelManager>().PendingChoices>0||FindObjectsByType<UpgradeSkillPanel>(FindObjectsSortMode.None).Any(p=>p.IsVisible))&&Time.realtimeSinceStartup<deadline)
                {HealTestPlayer();if(Time.timeScale<=0)PickUpgradeOrTreasure();else Time.timeScale=5;yield return null;}
                Require(director.ActiveBoss==null&&FindFirstObjectByType<LevelManager>().PendingChoices==0,"Restored boss dies and its five reward choices complete");Time.timeScale=1;
            }
            Require(mask==31,"All five boss entrances and in-flight casts survive save/resume");yield return LeaveFromPause();
        }
        IEnumerator CheckRunSaveModes()
        {
            RunCheckpoint last=null;
            for(int i=0;i<Heroes.Length;i++)
            {
                yield return EnterRound(new[]{0,1,8}[i],Heroes[i]);
                _round.PlayerStat.ModifyValue("HP",10000,true);_round.PlayerStat.ModifyValue("HP",10000);
                var prop=FindFirstObjectByType<DropBox>();Require(prop!=null,"A placed chest exists before collection");
                var hero=_round.GlobalConfig.GlobalPlayer;hero.transform.position=prop.transform.position;hero.GetComponent<Rigidbody2D>().position=hero.transform.position;Physics2D.SyncTransforms();
                yield return Wait(()=>prop==null||!prop.gameObject.activeSelf,"Real overlap collects a chest before saving");
                _round.Pause();yield return null;
                var manager=FindFirstObjectByType<SkillManager>();
                foreach(string key in new[]{"BothSideSkill","BouncingSkill","ChainSkill","InsideScopeSkill","RandomSkill","Armor","HPMax"})manager.UpgradeSkill(key);
                var enemy=FindFirstObjectByType<BaseEnemy>();enemy.SetEncounterHealth(3000);enemy.transform.position=hero.transform.position+Vector3.right*4;
                var freeze=Resources.FindObjectsOfTypeAll<FreezingBuff>().FirstOrDefault(b=>!b.name.EndsWith("(Clone)"));
                Require(freeze!=null,"A real freeze status definition is loaded");FindFirstObjectByType<BuffManager>().AddBuff(enemy,freeze);
                if(i==0)
                {
                    var testPool=FindFirstObjectByType<StageManager>().RunPool;var recycled=testPool.Request(enemy.Key).GetComponent<BaseEnemy>();yield return null;
                    int speed=recycled.StatRuntime.GetValue("Speed");var buffs=FindFirstObjectByType<BuffManager>();buffs.AddBuff(recycled,freeze);
                    Require(recycled.StatRuntime.GetValue("Speed")==0,"Freeze applies to a real pooled enemy before recycling");testPool.Return(recycled.gameObject);
                    var active=(System.Collections.IEnumerable)typeof(BuffManager).GetField("mBuffActivateKeys",Private).GetValue(buffs);
                    Require(!active.Cast<BaseBuff>().Any(b=>b.Owner==recycled)&&recycled.StatRuntime.GetValue("Speed")==speed,"Returning an enemy removes its statuses and restores speed before reuse");
                }
                foreach(var cast in FindObjectsByType<SkillCast>(FindObjectsSortMode.None))
                {var data=manager.OwnedSkills.FirstOrDefault(s=>s.Key==cast.Key&&s.SkillType==SkillType.AttackSkill);if(data!=null&&cast.CanCast())cast.Cast(data);}
                int propsBefore=FindObjectsByType<DropBox>(FindObjectsSortMode.None).Length;
                hero.GetComponentInChildren<PlayerHit>().OnHit(1);yield return new WaitForSecondsRealtime(.03f);
                if(i==0)
                {
                    Require(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATN_TEST_PROFILE_DIR")),"Failure fixture is restricted to an isolated QA profile");
                    var save=(SaveSystemSO)typeof(GlobalConfigSO).GetField("_saveSystem",Private).GetValue(_round.GlobalConfig);
                    var attribute=(AttributeSO)typeof(SaveSystemSO).GetField("_attribute",Private).GetValue(save);
                    string filename=save.saveFilename;int wallet=attribute.GoldCount,unbanked=_round.GlobalConfig.UnbankedGold;
                    FileManager.LoadFromFile(RunCheckpointStore.Filename,out var checkpointBefore);bool failed;
                    try{save.saveFilename=".";failed=!RunCheckpointSession.SaveAndExit(_round);}finally{save.saveFilename=filename;}
                    Require(failed&&_round.IsPaused&&!_round.IsEnded&&Time.timeScale==0,"A real failed wallet write keeps the current run paused");
                    Require(attribute.GoldCount==wallet&&_round.GlobalConfig.UnbankedGold==unbanked,"Failed wallet persistence rolls back banking and retains earned gold");
                    FileManager.LoadFromFile(RunCheckpointStore.Filename,out var checkpointAfter);Require(checkpointBefore==checkpointAfter,"Failed save retains the previous run checkpoint verbatim");
                }
                Require(RunCheckpointSession.SaveAndExit(_round),"Save current mode succeeds: "+RunCheckpointSession.LastError);
                yield return WaitForMenu();Require(RunCheckpointStore.TryRead(out var expected),"Saved mode checkpoint is readable");
                last=expected;
                yield return Wait(()=>CanClick(GameObject.Find("ButtonContinueRun").GetComponent<Button>()),"Continue is enabled for this mode");Click(GameObject.Find("ButtonContinueRun").GetComponent<Button>());
                yield return Wait(()=>{_round=FindFirstObjectByType<TrialController>();return _round!=null&&_round.IsPaused&&!RunCheckpointSession.HasPending;},"Saved mode restores into pause");
                Require(RunCheckpointSession.LastError==null,"Mode restore has no error: "+RunCheckpointSession.LastError);
                CheckRunState(expected,new RunCheckpointWorld(_round).Capture());
                Require(FindObjectsByType<DropBox>(FindObjectsSortMode.None).Length==propsBefore,"Collected chest remains absent after resume");
                Require(Vector4.Distance(_round.GlobalConfig.GlobalPlayer.GetComponent<SpriteRenderer>().color,Color.white)<.001f,"Saving during hit feedback does not leave the survivor permanently tinted");
                yield return CaptureFrame("saved-mode-"+Heroes[i]);
                _round.Resume();yield return new WaitForSeconds(.3f);Require(!_round.IsEnded,"Restored mode continues playing");
                yield return LeaveFromPause();
            }
            last.id=Guid.NewGuid().ToString();string backupId=last.id;Require(RunCheckpointStore.Save(last),"First isolated checkpoint writes atomically");
            last.id=Guid.NewGuid().ToString();Require(RunCheckpointStore.Save(last),"Replacing a checkpoint retains the previous backup");
            Require(FileManager.WriteToFile(RunCheckpointStore.Filename,"broken checkpoint"),"QA corrupts only its isolated current-run file");
            Require(RunCheckpointStore.TryRead(out var recovered)&&recovered.id==backupId,"Corrupt primary checkpoint falls back to the valid backup");
            Require(RunCheckpointStore.Consume(recovered)&&!RunCheckpointStore.TryRead(out _),"Consumed backup cannot replay rewards");
        }
        IEnumerator CheckRunSaveWrite()
        {
            yield return EnterRound(8,"Lyra");
            _round.PlayerStat.ModifyValue("HP",10000,true);_round.PlayerStat.ModifyValue("HP",10000);
            var manager=FindFirstObjectByType<SkillManager>();
            foreach(var key in new[]{"BouncingSkill","InsideScopeSkill","CircularMotionSkill","Might","Armor"})manager.UpgradeSkill(key);
            JumpClock(299.95f);var director=FindFirstObjectByType<EndlessBossDirector>();
            yield return Wait(()=>director.ActiveBoss!=null,"Boss spawns before checkpoint test");
            var boss=director.ActiveBoss;yield return Wait(()=>!boss.Entering&&boss.StatRuntime!=null,"Boss entrance completes");
            boss.SetEncounterHealth(100000); // QA durability, not a product balance change.
            yield return Wait(()=>boss.GetComponent<BossBrain>().CastAge>1.2f&&FindObjectsByType<BossHazard>(FindObjectsSortMode.None).Length>0,"Boss cast is in flight before saving");
            var starter=manager.OwnedSkills.Single(s=>s.Key=="ToNearestSkill");
            boss.transform.position=_round.GlobalConfig.GlobalPlayer.transform.position+Vector3.right*4;
            var cast=FindObjectsByType<ToNearestSkillCast>(FindObjectsSortMode.None).Single();cast.Cast(starter);
            var pool=FindFirstObjectByType<StageManager>().RunPool;
            var incoming=pool.Request("BlueMagic");incoming.transform.position=_round.GlobalConfig.GlobalPlayer.transform.position+new Vector3(-5,2,0);incoming.GetComponent<EnemyMagic>().Init(8,Vector3.right);
            var exp=pool.Request("Exp80");exp.transform.position=_round.GlobalConfig.GlobalPlayer.transform.position+Vector3.up*3;
            var gold=pool.Request("Box");gold.transform.position=_round.GlobalConfig.GlobalPlayer.transform.position+new Vector3(-3,-2,0);
            yield return new WaitForSeconds(.14f);
            _round.GlobalConfig.CurrentGetGold+=37;
            _round.Pause();yield return null;yield return null;
            Require(FindObjectsByType<SkillBase>(FindObjectsSortMode.None).Length>0,"Player missiles are present at save time");
            var button=GameObject.Find("SaveRunBtn").GetComponent<Button>();yield return Wait(()=>CanClick(button),"Save button can be clicked");Click(button);
            yield return Wait(()=>ConfirmationDialog.IsOpen,"Save confirmation opens after clicking Save & Main Menu");
            var confirmation=GameObject.Find("ConfirmationDialog");
            Require(confirmation!=null&&HasText(confirmation.transform,LanguageManager.GetCurrentCode()=="vi"?"LƯU TRẬN VÀ VỀ MENU?":"SAVE RUN & RETURN?"),
                "Save action asks for confirmation before writing the checkpoint");
            var confirm=ButtonIn(confirmation.transform,"ConfirmButton");yield return Wait(()=>CanClick(confirm),"Save confirmation action receives pointer input");Click(confirm);
            yield return Wait(()=>FindFirstObjectByType<UIMainMenu>()!=null,"Save returns to main menu");
            Require(RunCheckpointSession.LastError==null,"Save completes without an error: "+RunCheckpointSession.LastError);
            Require(RunCheckpointStore.TryRead(out var state),"Atomic checkpoint can be read after returning to menu");
            Require(state.entities.Any(e=>e.key!=null&&e.key.StartsWith("EndlessBoss_"))&&state.entities.Any(e=>e.id.StartsWith("hazard:")),"Checkpoint contains boss and active hazards");
            Require(state.entities.Any(e=>e.key=="ToNearestSkill"),"Checkpoint includes the existing player projectile");
            Require(state.entities.Any(e=>e.key=="BlueMagic")&&state.entities.Any(e=>e.key=="Exp80")&&state.entities.Any(e=>e.key=="Box"),"Checkpoint includes incoming enemy magic, XP and a pooled gold chest");
            Require(FileManager.WriteToFile("qa-run-expected.json",JsonUtility.ToJson(state)),"Expected state persists for a separate Player process");
            yield return CaptureFrame("saved-run-menu");
        }
        IEnumerator CheckRunSaveRead()
        {
            Require(FileManager.LoadFromFile("qa-run-expected.json",out var json),"Previous Player process saved its expected checkpoint");
            var expected=JsonUtility.FromJson<RunCheckpoint>(json);
            Require(RunCheckpointStore.TryRead(out var saved)&&saved.id==expected.id,"New Player process discovers the same saved run");
            var button=GameObject.Find("ButtonContinueRun").GetComponent<Button>();yield return Wait(()=>CanClick(button),"Continue saved run button receives pointer input");Click(button);
            yield return Wait(()=>{_round=FindFirstObjectByType<TrialController>();return _round!=null&&_round.IsPaused&&!RunCheckpointSession.HasPending&&!RunCheckpointSession.Restoring;},"Checkpoint restores into pause");
            Require(RunCheckpointSession.LastError==null,"Restore reports no error: "+RunCheckpointSession.LastError);
            var actual=new RunCheckpointWorld(_round).Capture();CheckRunState(expected,actual);
            Require(!RunCheckpointSession.HasSavedRun,"A successfully resumed checkpoint is consumed to prevent reward replay");
            var director=FindFirstObjectByType<EndlessBossDirector>();var brain=director.ActiveBoss.GetComponent<BossBrain>();
            int casts=brain.CastCount;float age=brain.CastAge;float clock=_round.MatchSeconds;
            yield return CaptureFrame("restored-run-paused");_round.Resume();
            yield return new WaitForSeconds(.25f);
            Require(_round.MatchSeconds>clock&&brain.CastCount==casts&&brain.CastAge>age,"Clock and existing boss cast continue rather than restarting");
            _round.Pause();yield return null;
            Require(FindObjectsByType<SkillBase>(FindObjectsSortMode.None).All(s=>s.transform.localScale.sqrMagnitude>0),"Restored projectiles retain valid scale");
            yield return CaptureFrame("restored-run-continued");
        }
        void CheckRunState(RunCheckpoint expected,RunCheckpoint actual)
        {
            Require(expected.hero==actual.hero&&expected.stage==actual.stage,"Saved survivor and stage restored");
            Require(Mathf.Abs(expected.clock-actual.clock)<.001f,"Match clock is restored exactly while paused");
            var expectedGlobal=expected.systems.Single(s=>s.type==typeof(GlobalConfigSO).FullName);var actualGlobal=actual.systems.Single(s=>s.type==typeof(GlobalConfigSO).FullName);
            Require(PlainFieldsEqual(new RunComponent{fields=expectedGlobal.fields},new RunComponent{fields=actualGlobal.fields}),"Round gold, banked amount and kill count are restored without duplication");
            Require(PlainFieldsEqual(new RunComponent{fields=expected.mode},new RunComponent{fields=actual.mode}),"Mode objective clock and difficulty progression are restored");
            foreach(var value in expected.stats)
            {var other=actual.stats.Single(v=>v.key==value.key);Require(other.value==value.value&&other.max==value.max,"Restored player stat "+value.key);}
            Require(expected.skills.Count==actual.skills.Count,"All owned skills restored without duplicate icons or passive grants");
            foreach(var skill in expected.skills)
            {var other=actual.skills.Single(s=>s.key==skill.key);Require(skill.level==other.level&&skill.state==other.state&&Mathf.Abs(skill.cycle-other.cycle)<.001f,"Restored skill level, evolution and cooldown: "+skill.key);}
            Require(expected.entities.Count==actual.entities.Count,$"World entity count restored: {expected.entities.Count} → {actual.entities.Count}");
            var remaining=actual.entities.ToList();
            foreach(var entity in expected.entities)
            {
                var candidates=remaining.Where(e=>(e.key??"")==(entity.key??"")&&e.id.StartsWith("hazard:")==entity.id.StartsWith("hazard:")).ToArray();
                // Several projectiles start at the same position in one cast.
                // Match the full multiset of their states, then consume each match.
                var other=candidates.OrderByDescending(e=>TrackedComponents(entity).All(c=>e.components.Any(v=>v.type==c.type&&v.path==c.path&&v.index==c.index&&PlainFieldsEqual(c,v)))).ThenBy(e=>Vector3.Distance(e.position,entity.position)).FirstOrDefault();
                Require(other!=null&&Vector3.Distance(entity.position,other.position)<.001f,"Restored world position: "+(entity.key??entity.id));
                remaining.Remove(other);
                if(entity.stats!=null)Require(entity.stats.All(v=>other.stats.Any(x=>x.key==v.key&&x.value==v.value&&x.max==v.max)),"Restored enemy health and stats: "+entity.key);
                foreach(var component in TrackedComponents(entity))
                {
                    var restored=other.components.Single(c=>c.type==component.type&&c.path==component.path&&c.index==component.index);
                    Require(PlainFieldsEqual(component,restored),"Restored current phase, timer, direction and hit state: "+component.type);
                }
            }
            Require(expected.deck.cursor==actual.deck.cursor&&expected.deck.cards.SequenceEqual(actual.deck.cards)&&expected.deck.last==actual.deck.last,"Boss shuffle bag and cursor restored");
            Require(expected.spawnBudgets.Count==actual.spawnBudgets.Count&&expected.spawnBudgets.Zip(actual.spawnBudgets,(a,b)=>Mathf.Abs(a-b)<.001f).All(x=>x),"Wave spawn budgets restored");
            Require(expected.buffs.Count==actual.buffs.Count&&expected.buffs.All(b=>actual.buffs.Any(v=>v.asset==b.asset&&Mathf.Abs(v.age-b.age)<.001f)),"Status effects and their elapsed durations restored");
            foreach(var timer in expected.timers)Require(actual.timers.Any(t=>t.key==timer.key&&Mathf.Abs(t.elapsed-timer.elapsed)<.001f),"Attack timer resumes at the saved position: "+timer.key);
        }
        static System.Collections.Generic.IEnumerable<RunComponent> TrackedComponents(RunEntity entity)=>entity.components.Where(c=>c.fields!=null&&
            (c.type.EndsWith("BossBrain")||c.type.EndsWith("BossEntrance")||c.type.EndsWith("BossHazard")||c.type.EndsWith("Skill")||c.type.EndsWith("EnemyMagic")||c.type.EndsWith("EnemyWeapon")));
        static bool PlainFieldsEqual(RunComponent expected,RunComponent actual)
        {
            foreach(var field in expected.fields)
            {
                var value=actual.fields.SingleOrDefault(f=>f.name==field.name&&f.declaring==field.declaring);if(value==null)return false;
                if(field.items!=null&&field.items.Count>0){if(value.items==null||value.items.Count!=field.items.Count)return false;continue;}
                string raw=field.value??"";
                if(raw.StartsWith("asset:")||raw.StartsWith("entity:")||raw.StartsWith("hazard:")||raw.StartsWith("hero")||raw.StartsWith("skill:")||raw.StartsWith("cooldown:"))continue;
                if(float.TryParse(raw,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float number)&&float.TryParse(value.value,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float number2))
                {if(number!=number2&&Mathf.Abs(number-number2)>=.002f)return false;}
                else if(raw!=(value.value??""))return false;
                if(Vector4.Distance(field.vector,value.vector)>=.001f||Vector4.Distance(field.size,value.size)>=.001f)return false;
            }
            return true;
        }
    }
}
