using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using DATN.Localization;
using DATN.UI;
using RGame.Framework;
using RGame.CommonStat;
using RGame.RoguelikeKit;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace DATN.Trial
{
    public sealed partial class DatnRegression
    {
        const BindingFlags Private = BindingFlags.Instance|BindingFlags.NonPublic;
        void JumpClock(float seconds) => typeof(TrialController).GetField("_matchSeconds",Private).SetValue(_round,(double)seconds);
        void CheckBossDeck()
        {
            var randomState=UnityEngine.Random.state;
            try
            {
                string first=null;bool varied=false;
                for(int seed=1;seed<=12;seed++)
                {
                    UnityEngine.Random.InitState(seed);var deck=new BossEncounterDeck();int previous=-1;bool valid=true;string sequence="";
                    for(int bag=0;bag<20;bag++)
                    {
                        int mask=0;
                        for(int i=0;i<5;i++){int kind=deck.Draw();valid&=kind>=0&&kind<5&&(mask&(1<<kind))==0&&kind!=previous;mask|=1<<kind;previous=kind;if(bag==0)sequence+=kind;}
                        valid&=mask==31;
                    }
                    Require(valid,$"Shuffle seed {seed}: 20 complete bags with no early or adjacent repeat");
                    if(first==null)first=sequence;else varied|=first!=sequence;
                    deck.Reset();deck.Draw();deck.Draw();var snapshot=deck.Capture();var restored=new BossEncounterDeck();
                    Require(restored.Restore(snapshot),"Valid partial boss bag restores");
                    Require(Enumerable.Range(0,3).All(_=>deck.Draw()==restored.Draw()),"Restored bag preserves all remaining boss choices");
                    snapshot.cards[0]=snapshot.cards[1];Require(!restored.Restore(snapshot),"Duplicate boss entries are rejected");
                }
                Require(varied,"Boss order varies between random seeds");
            }
            finally{UnityEngine.Random.state=randomState;}
        }
        IEnumerator CheckBosses()
        {
            CheckBossDeck();
            yield return SelectLanguage("vi");
            yield return EnterRound(8,"Rowan");
            _round.PlayerStat.ModifyValue("HP",100000,true);
            _round.PlayerStat.ModifyValue("HP",100000);
            var starterWeapon=_round.GlobalConfig.GlobalPlayer.GetComponent<StarterWeaponProfile>();
            float originalWeaponRange=starterWeapon.Range;
            starterWeapon.Range=0;
            var director=FindFirstObjectByType<EndlessBossDirector>();
            Require(director!=null&&director.NextAt==300,"Endless boss schedule begins at exactly 300 in-game seconds");
            float deadline=Time.realtimeSinceStartup+240;
            while(_round.MatchSeconds<299 && Time.realtimeSinceStartup<deadline)
            {
                HealTestPlayer();if(Time.timeScale==0)PickUpgradeOrTreasure();else Time.timeScale=5;
                if(_round.IsEnded)Require(false,"Five-minute QA bot died before the boundary");
                if(director.ActiveBoss!=null)Require(false,"Boss appeared before 300 seconds");
                yield return null;
            }
            Require(_round.MatchSeconds>=299,"Real simulation reached five-minute boundary without clock injection");
            Require(director.ActiveBoss==null,"No boss before the first five-minute boundary");
            while(director.ActiveBoss==null&&Time.realtimeSinceStartup<deadline){HealTestPlayer();if(Time.timeScale==0)PickUpgradeOrTreasure();else Time.timeScale=1;yield return null;}
            Require(director.ActiveBoss!=null&&_round.MatchSeconds>=300,"First boss appears at the real five-minute boundary");
            yield return CheckBossArrivalBanner(director,director.ActiveBoss.GetComponent<BossBrain>().Kind,true);
            Require(FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None).All(e=>e==director.ActiveBoss),"Boss arrival clears ordinary enemies");
            using var visibility=new SpriteVisibilityProbe();
            int kindMask=0,previousKind=-1;
            for(int encounter=0;encounter<6;encounter++)
            {
                if(encounter>0)
                {
                    JumpClock(director.NextAt-.05f); Time.timeScale=1;
                    yield return Wait(()=>director.ActiveBoss!=null,"Next five-minute boss boundary");
                    yield return CheckBossArrivalBanner(director,director.ActiveBoss.GetComponent<BossBrain>().Kind,false);
                    Record("INFO","Later boundary reached with QA clock jump; first boundary used normal simulation");
                }
                var boss=director.ActiveBoss;var brain=boss.GetComponent<BossBrain>();
                yield return Wait(()=>boss.StatRuntime!=null,"Boss stats initialize");
                yield return CheckBossEntrance(boss,director,visibility);
                var rig=boss.GetComponent<BossVisualAnimation>();
                // QA only: reward choices may add untargeted weapons. Isolate their
                // collision damage while measuring animation and exact reward amounts.
                var receiverColliders=boss.MyHit.GetComponents<Collider2D>();
                foreach(var receiver in receiverColliders)receiver.enabled=false;
                yield return new WaitForSeconds(.3f);
                Record("INFO","Boss receiver collider isolated for visual sampling; hit/reward tests call the real EnemyHit directly");
                Require(brain.Kind>=0&&brain.Kind<5&&brain.Kind!=previousKind&&boss.MaxHealth==300*(encounter+1)&&boss.StatRuntime.GetValue("HP")>0,
                    $"Encounter {encounter+1}: rotation {brain.Kind}, HP {boss.MaxHealth}");
                if(encounter<5){Require((kindMask&(1<<brain.Kind))==0,"Boss bag does not repeat before all five appear");kindMask|=1<<brain.Kind;}
                if(encounter==4)Require(kindMask==31,"Live encounters include all five bosses before a new bag");
                previousKind=brain.Kind;
                Require(rig!=null&&rig.StandingSprite!=null,$"{BossCatalog.Ids[brain.Kind]} exposes Idle frame 0 as its canonical standing sprite");
                if(encounter<5)yield return CheckBossFrameCoverage(boss,rig,visibility);
                Require(visibility.VisiblePixels(boss.MySpriteRenderer)>200,"Boss actual prefab material renders opaque pixels");
                Require(Resources.FindObjectsOfTypeAll<GlobalConfigSO>().Count(x=>x.name=="GlobalConfigSO")==1&&Resources.FindObjectsOfTypeAll<PoolRuntimeSO>().Count(x=>x.name=="Pool")==1,"Resource boss reuses the running global state and pool without duplicate asset instances");
                var qaPool=Resources.FindObjectsOfTypeAll<PoolRuntimeSO>().Single(x=>x.name=="Pool");
                var qaDefs=(System.Collections.Generic.List<PoolDefinition>)typeof(PoolRuntimeSO).GetField("_poolDefinitions",Private).GetValue(qaPool);
                foreach(var def in qaDefs.Where(x=>x.Pool.Prefab.GetComponentInChildren<Exp>(true)!=null))qaPool.RecycleAll(def.Key);
                foreach(var def in qaDefs.Where(x=>x.Pool.Prefab.GetComponentInChildren<SkillBase>(true)!=null))qaPool.RecycleAll(def.Key);
                CheckBossStatusShader(boss,qaPool,visibility);
                yield return CaptureFrame("boss-"+encounter+"-production-health");
                var heroRenderer=_round.GlobalConfig.GlobalPlayer.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.sprite!=null&&!s.name.ToLowerInvariant().Contains("shadow")).OrderByDescending(s=>s.bounds.size.y).First();
                float poseRatio=VisibleHeight(boss.MySpriteRenderer)/VisibleHeight(heroRenderer);
                var animator=_round.GlobalConfig.GlobalPlayer.GetComponent<PlayerAnim>().MyAnimator;
                var state=animator.GetCurrentAnimatorStateInfo(0);
                animator.Play("Idle",0,0);animator.Update(0);
                float ratio=rig!=null&&rig.StandingSprite!=null
                    ?VisibleHeight(rig.StandingSprite,boss.transform.lossyScale.y)/VisibleHeight(heroRenderer)
                    :0;
                animator.Play(state.fullPathHash,0,state.normalizedTime);animator.Update(0);
                Require(ratio>=4.9f&&ratio<=5.1f,$"Boss Idle frame 0 visible height / canonical standing player = {ratio:0.00} (current pose: {poseRatio:0.00})");
                Require(boss.MySpriteRenderer.sortingOrder<heroRenderer.sortingOrder,"Giant boss body remains behind the survivor for combat readability");
                int expectedHP=boss.MaxHealth;
                if(encounter<5)
                {
                    boss.SetEncounterHealth(100000); // QA durability only; all production HP was asserted above.
                    yield return CheckLiveBossAnimation(boss,visibility);
                    bool windupSeen=false,attackSeen=false;
                    bool capturedWindup=false,capturedAttack=false;
                    bool vfxPauseSampled=false;
                    int fxSkillMask=0;
                    var qaPlayer=_round.GlobalConfig.GlobalPlayer.transform;
                    var qaTarget=qaPlayer.GetComponentInChildren<PlayerHit>().transform;
                    Vector3 savedPlayerPosition=qaPlayer.position;
                    int facingMask=0,positionSequence=0,observedCast=brain.CastCount,lockedCrossings=0;
                    bool expectedVerticalFlip=boss.MySpriteRenderer.flipX,castFlip=boss.MySpriteRenderer.flipX,crossedTarget=false,crossPending=false;
                    Vector2 lockedDirection=brain.CastDirection;
                    float until=Time.realtimeSinceStartup+80;
                    while((brain.UsedSkillMask!=31||fxSkillMask!=31||facingMask!=7||lockedCrossings<3) && Time.realtimeSinceStartup<until)
                    {
                        if(!boss.Casting)
                        {
                            Vector2 offset=positionSequence%3==0?Vector2.left*4:positionSequence%3==1?Vector2.right*4:Vector2.up*4;
                            qaPlayer.position+=(Vector3)((Vector2)boss.transform.position+offset-(Vector2)qaTarget.position);
                            expectedVerticalFlip=boss.MySpriteRenderer.flipX;
                        }
                        if(brain.CastCount!=observedCast)
                        {
                            observedCast=brain.CastCount;positionSequence++;
                            lockedDirection=brain.CastDirection;castFlip=boss.MySpriteRenderer.flipX;
                            crossedTarget=false;crossPending=false;
                            if(Mathf.Abs(lockedDirection.x)>.08f)
                            {
                                Require(castFlip==(lockedDirection.x<0),BossCatalog.Ids[brain.Kind]+" faces its actual horizontal cast direction");
                                facingMask|=lockedDirection.x<0?1:2;
                            }
                            else
                            {
                                Require(castFlip==expectedVerticalFlip,BossCatalog.Ids[brain.Kind]+" keeps its horizontal facing for a near-vertical cast");
                                facingMask|=4;
                            }
                        }
                        if(crossPending&&boss.Casting)
                        {
                            Require(brain.CastDirection==lockedDirection&&boss.MySpriteRenderer.flipX==castFlip,
                                BossCatalog.Ids[brain.Kind]+" keeps its cast facing when the target crosses behind it");
                            lockedCrossings++;crossPending=false;
                        }
                        if(boss.Casting&&!crossedTarget&&brain.CastAge>=.3f)
                        {
                            Vector2 offset=new Vector2(castFlip?4:-4,0);
                            qaPlayer.position+=(Vector3)((Vector2)boss.transform.position+offset-(Vector2)qaTarget.position);
                            crossedTarget=true;crossPending=true;
                        }
                        if(brain.CurrentSkill>=0&&brain.CastAge>=1.1f&&BossSkillVfx.ActiveVisualCount>0)
                        {
                            fxSkillMask|=1<<brain.CurrentSkill;
                            if(encounter==0&&!vfxPauseSampled)
                            {
                                var vfxLines=ActiveBossSkillVfxLines();
                                if(vfxLines.Length>0)
                                {
                                    var frozenPositions=CaptureLinePositions(vfxLines);
                                    Vector2 frozenDirection=brain.CastDirection;float frozenCastAge=brain.CastAge;bool frozenFlip=boss.MySpriteRenderer.flipX;
                                    _round.Pause();
                                    yield return new WaitForSecondsRealtime(.15f);
                                    Require(Time.timeScale==0f&&SameLinePositions(vfxLines,frozenPositions),
                                        "Active boss skill VFX line positions freeze during pause");
                                    Require(brain.CastDirection==frozenDirection&&brain.CastAge==frozenCastAge&&boss.MySpriteRenderer.flipX==frozenFlip,
                                        "Pause freezes boss cast direction, age and facing");
                                    _round.Resume();Time.timeScale=3;
                                    vfxPauseSampled=true;
                                }
                            }
                        }
                        if(rig!=null)
                        {
                            if(rig.Motion==BossMotion.Windup&&brain.CastAge>=.3f&&brain.CastAge<1.1f)
                            {
                                windupSeen=true;
                                if(!capturedWindup)
                                {
                                    var castLines=boss.GetComponentsInChildren<LineRenderer>(true).Where(l=>l.name.StartsWith("Boss Cast ")&&l.enabled).ToArray();
                                    var fxBounds=LineBounds(castLines);
                                    var canonical=boss.GetComponent<BossVisualAnimation>().StandingSprite.bounds;
                                    Require(castLines.Length>=3&&fxBounds.max.y>boss.transform.position.y+canonical.size.y*.40f&&
                                            (fxBounds.size.x>canonical.size.x*.55f||fxBounds.size.y>canonical.size.y*.5f),
                                        "Casting VFX scale with the boss body and reach the upper body instead of remaining at its feet");
                                    capturedWindup=true;yield return CaptureFrame("boss-"+encounter+"-windup");
                                }
                            }
                            if(rig.Motion==BossMotion.Attack&&brain.CastAge>=1.28f&&BossSkillVfx.ActiveVisualCount>0)
                            {
                                attackSeen=true;
                                if(!capturedAttack){capturedAttack=true;yield return CaptureFrame("boss-"+encounter+"-attack");}
                            }
                        }
                        HealTestPlayer();if(Time.timeScale==0)PickUpgradeOrTreasure();else Time.timeScale=3;
                        yield return null;
                    }
                    Require(facingMask==7&&lockedCrossings>=3,$"{BossCatalog.Ids[brain.Kind]} covers left/right/vertical casts and locked target crossings (mask={facingMask}, crossings={lockedCrossings})");
                    qaPlayer.position=savedPlayerPosition;
                    Require(brain.UsedSkillMask==31,$"{BossCatalog.Ids[brain.Kind]} randomly executes all five thematic skills ({brain.CastCount} casts)");
                    Require(fxSkillMask==31,
                        $"{BossCatalog.Ids[brain.Kind]} produces an active skill VFX for all five attacks (mask={fxSkillMask}, active={BossSkillVfx.ActiveVisualCount})");
                    Require(windupSeen&&attackSeen,
                        $"{BossCatalog.Ids[brain.Kind]} transitions through 1.1-second windup and attack animation states");
                    Require(capturedWindup&&capturedAttack,
                        $"{BossCatalog.Ids[brain.Kind]} produced actual screenshots during windup and attack");
                    Require(FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None).All(e=>e==boss),"No small enemy spawns while boss is active");
                    brain.StopFight();
                    // RandomSkill deals instant damage and bypasses colliders.
                    // Remove the QA boss from auto-targeting only while observing
                    // Idle/Hit, so real player attacks cannot replace Idle with Hit.
                    var idleRoster=(EnemySystem)typeof(StageManager).GetField("enemySystem",Private).GetValue(FindFirstObjectByType<StageManager>());
                    idleRoster.RemoveEnemy(boss);
                    var idleBuffs=FindFirstObjectByType<BuffManager>();
                    var priorStatuses=(System.Collections.IEnumerable)typeof(BuffManager).GetField("mBuffActivateKeys",Private).GetValue(idleBuffs);
                    foreach(var status in priorStatuses.Cast<BaseBuff>().Where(b=>b.Owner==boss).ToArray())idleBuffs.RemoveBuff(status);
                    Time.timeScale=1;
                    boss.transform.position=_round.GlobalConfig.GlobalPlayer.transform.position+Vector3.right*1.2f;
                    boss.DashVelocity=Vector2.zero;boss.MyRigidbody2D.linearVelocity=Vector2.zero;
                    yield return Wait(()=>rig!=null&&rig.Motion==BossMotion.Idle,"Prior hit feedback finishes before idle observation");
                    Require(rig!=null&&rig.Motion==BossMotion.Idle,$"{BossCatalog.Ids[brain.Kind]} enters its idle pose when movement and casting stop");
                    if(rig!=null)
                    {
                        Time.timeScale=1;
                        var idleBefore=visibility.RenderedSignature(rig.AnimatedRenderer);float idleClock=rig.AnimationClock;
                        int idleHP=boss.StatRuntime.GetValue("HP");
                        var idleClip=rig.ClipFor(BossMotion.Idle);
                        float idleWindow=Mathf.Clamp(idleClip!=null?idleClip.length*1.1f:1f,1f,3f);
                        var previousIdleFrame=rig.AnimatedRenderer.sprite;var previousIdleMotion=rig.Motion;
                        bool idleFrameChanged=false,idlePixelsChanged=false;
                        float idleElapsed=0f;
                        while(idleElapsed<idleWindow)
                        {
                            yield return new WaitForSeconds(Mathf.Min(.12f,idleWindow-idleElapsed));
                            var idleAfter=visibility.RenderedSignature(rig.AnimatedRenderer);
                            if(previousIdleMotion==BossMotion.Idle&&rig.Motion==BossMotion.Idle)
                            {
                                idleFrameChanged|=rig.AnimatedRenderer.sprite!=previousIdleFrame;
                                idlePixelsChanged|=idleAfter.AlphaHash!=idleBefore.AlphaHash||idleAfter.OpaquePixels!=idleBefore.OpaquePixels;
                            }
                            previousIdleFrame=rig.AnimatedRenderer.sprite;idleBefore=idleAfter;
                            previousIdleMotion=rig.Motion;
                            idleElapsed=rig.AnimationClock-idleClock;
                        }
                        Require(rig.Motion==BossMotion.Idle&&idleElapsed>=idleWindow&&idleFrameChanged&&idlePixelsChanged,
                            $"{BossCatalog.Ids[brain.Kind]} idle clip changes sprite and rendered alpha over its {idleWindow:0.00}s observation window ({idleElapsed:0.00}s elapsed, clip={idleClip?.length:0.00}s, frame={previousIdleFrame?.name})");
                        Require(boss.StatRuntime.GetValue("HP")==idleHP,"Idle observation is isolated from player direct-damage casts");
                        yield return CaptureFrame("boss-"+encounter+"-idle");

                        int bossHP=boss.StatRuntime.GetValue("HP");
                        Time.timeScale=.1f;
                        Record("INFO","Hit observation uses 0.1x QA time so GPU readback cannot consume the 0.18 game-second clip");
                        boss.MyHit.Hit(1);yield return null;
                        Require(rig.Motion==BossMotion.Hit&&boss.StatRuntime.GetValue("HP")==bossHP-1,
                            $"{BossCatalog.Ids[brain.Kind]} enters its hit animation from real damage");
                        var hitBefore=visibility.RenderedSignature(rig.AnimatedRenderer);var previousHitFrame=rig.AnimatedRenderer.sprite;
                        var previousHitMotion=rig.Motion;
                        bool hitFrameChanged=false,hitPixelsChanged=false;
                        // A 1080p PNG encode can outlast this 0.18-second clip. Observe
                        // the native frame change first; capture only after proof exists.
                        float hitDeadline=Time.realtimeSinceStartup+3f;
                        int hitSamples=0;
                        while(!(hitFrameChanged&&hitPixelsChanged)&&rig.Motion==BossMotion.Hit&&Time.realtimeSinceStartup<hitDeadline)
                        {
                            yield return null;
                            var hitAfter=visibility.RenderedSignature(rig.AnimatedRenderer);
                            hitSamples++;
                            if(previousHitMotion==BossMotion.Hit&&rig.Motion==BossMotion.Hit)
                            {
                                hitFrameChanged|=rig.AnimatedRenderer.sprite!=previousHitFrame;
                                hitPixelsChanged|=hitAfter.AlphaHash!=hitBefore.AlphaHash||hitAfter.OpaquePixels!=hitBefore.OpaquePixels;
                            }
                            previousHitFrame=rig.AnimatedRenderer.sprite;hitBefore=hitAfter;
                            previousHitMotion=rig.Motion;
                        }
                        Require(hitFrameChanged&&hitPixelsChanged,
                            $"{BossCatalog.Ids[brain.Kind]} hit clip advances actual frames beyond the color flash before screenshot encoding (samples={hitSamples})");
                        if(rig.Motion==BossMotion.Hit)
                        {
                            Time.timeScale=0;yield return CaptureFrame("boss-"+encounter+"-hit");Time.timeScale=1;
                        }
                        Time.timeScale=1;
                    }
                    idleRoster.AddEnemy(boss);
                    _round.Pause();yield return null;
                    float clock=_round.MatchSeconds;var position=boss.transform.position;
                    int active=BossHazard.ActiveCount;
                    float animationClock=rig!=null?rig.AnimationClock:-1;
                    var pausedSignature=rig!=null?visibility.RenderedSignature(rig.AnimatedRenderer):default;
                    var pausedSprite=rig!=null?rig.AnimatedRenderer.sprite:null;
                    yield return new WaitForSecondsRealtime(.35f);
                    Require(clock==_round.MatchSeconds&&position==boss.transform.position&&active==BossHazard.ActiveCount,"Pause freezes boss, warnings and match clock");
                    var stillPausedSignature=rig!=null?visibility.RenderedSignature(rig.AnimatedRenderer):default;
                    Require(rig!=null&&rig.AnimationClock==animationClock&&rig.AnimatedRenderer.sprite==pausedSprite&&
                            pausedSignature.AlphaHash==stillPausedSignature.AlphaHash&&
                            pausedSignature.OpaquePixels==stillPausedSignature.OpaquePixels,
                        "Pause freezes the boss Animator frame, animation clock and actual rendered alpha pixels");
                    yield return SelectLanguage("vi");yield return null;
                    var title=boss.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Boss name");
                    Require(title.text==BossCatalog.Name(brain.Kind,true)&&title.color==BossCatalog.Colors[brain.Kind],"Vietnamese boss name matches boss palette above health bar");
                    yield return CaptureFrame("boss-"+encounter+"-pause-vi");
                    _round.Resume();Time.timeScale=1;yield return null;
                    yield return SelectLanguage("en");yield return null;
                    Require(title.text==BossCatalog.Name(brain.Kind,false),"English boss name is localized");
                    yield return CaptureFrame("boss-"+encounter+"-arena-en");
                    if(encounter==0)yield return CheckBossHazardDamage(boss);
                    boss.SetEncounterHealth(expectedHP);
                }
                if(encounter==5)
                {
                    JumpClock(director.NextAt+600);yield return null;
                    Require(director.EncounterCount==6&&director.ActiveBoss==boss,"Overdue five-minute boundaries never stack a second living boss");
                }
                int beforeLevel=_round.PlayerStat.GetValue("Level"),beforeGold=_round.GlobalConfig.CurrentGetGold,beforeExp=_round.PlayerStat.GetValue("Exp");
                var wallet=Resources.FindObjectsOfTypeAll<AttributeSO>().Single(x=>x.name=="AttributeSO");int walletBefore=wallet.GoldCount,unbanked=_round.GlobalConfig.UnbankedGold;
                int choicesBefore=_upgradeCount;
                foreach(var receiver in receiverColliders)receiver.enabled=true;
                boss.MyHit.Hit(expectedHP+100);
                var deathRig=boss.GetComponent<BossVisualAnimation>();
                yield return null;
                Require(deathRig!=null&&deathRig.isActiveAndEnabled&&deathRig.Motion==BossMotion.Death,
                    $"{BossCatalog.Ids[brain.Kind]} enters its death animation on defeat");
                float deathStartClock=deathRig.AnimationClock,lastDeathClock=deathStartClock;
                var lastDeathSprite=deathRig.AnimatedRenderer.sprite;
                var lastDeathSignature=visibility.RenderedSignature(deathRig.AnimatedRenderer);
                bool deathFrameChanged=false,deathPixelsChanged=false;
                int deathSamples=0;
                yield return CaptureFrame("boss-"+encounter+"-death");
                float choiceDeadline=Time.realtimeSinceStartup+12;
                var rewardLevelManager=FindFirstObjectByType<LevelManager>();
                // Reward choices can pause on the same frame the dissolve completes.
                // Sample the live death concurrently and use only realtime-bounded yields;
                // a scaled WaitForSeconds here can strand the test behind a new choice.
                while((_upgradeCount-choicesBefore<5||Time.timeScale<=0f||
                       (rewardLevelManager!=null&&rewardLevelManager.PendingChoices>0))&&
                      Time.realtimeSinceStartup<choiceDeadline)
                {
                    if(deathRig.isActiveAndEnabled&&deathRig.Motion==BossMotion.Death&&
                       Time.timeScale>0f&&deathRig.AnimationClock-lastDeathClock>=.045f)
                    {
                        var signature=visibility.RenderedSignature(deathRig.AnimatedRenderer);
                        deathFrameChanged|=lastDeathSprite!=deathRig.AnimatedRenderer.sprite;
                        deathPixelsChanged|=lastDeathSignature.AlphaHash!=signature.AlphaHash||
                                            lastDeathSignature.OpaquePixels!=signature.OpaquePixels;
                        lastDeathSignature=signature;lastDeathSprite=deathRig.AnimatedRenderer.sprite;
                        lastDeathClock=deathRig.AnimationClock;deathSamples++;
                    }
                    if(Time.timeScale<=0f&&_upgradeCount-choicesBefore<5)PickUpgradeOrTreasure();
                    yield return null;
                }
                int resolvedChoiceCount=_upgradeCount-choicesBefore;
                int pendingChoices=rewardLevelManager!=null?rewardLevelManager.PendingChoices:-1;
                Require(resolvedChoiceCount==5&&pendingChoices==0&&Time.timeScale>0f,
                    $"Boss reward choices resolve without a scaled wait (choices={resolvedChoiceCount}/5, pending={pendingChoices}, timeScale={Time.timeScale:0.00})");
                Require(lastDeathClock>deathStartClock&&(deathFrameChanged||deathPixelsChanged),
                    $"{BossCatalog.Ids[brain.Kind]} death changes live sprite or rendered alpha before pool return (samples={deathSamples}, clock={deathStartClock:0.000}->{lastDeathClock:0.000}, frameChanged={deathFrameChanged}, alphaChanged={deathPixelsChanged})");
                yield return Wait(()=>director.ActiveBoss==null,"Boss real hit/death returns to pool");
                Require(_round.PlayerStat.GetValue("Level")==beforeLevel+5&&_round.GlobalConfig.CurrentGetGold==beforeGold+500&&_round.PlayerStat.GetValue("Exp")==beforeExp,
                    "Boss directly awards exactly five levels and 500 gold, preserves XP");
                Require(BossHazard.ActiveCount==0,"All boss hazards are cleared on death");
                Require(director.NextAt>_round.MatchSeconds,"Next encounter uses a future five-minute boundary after a long boss fight");
                Require(wallet.GoldCount==walletBefore+unbanked+500,"Boss gold is checkpointed to persistent wallet exactly once");
                Require(_upgradeCount-choicesBefore==5,"Five reward choices open sequentially and resume after the fifth");
                int gold=_round.GlobalConfig.CurrentGetGold;yield return new WaitForSeconds(.15f);
                Require(gold==_round.GlobalConfig.CurrentGetGold,"Boss reward cannot repeat after pool return");
                HealTestPlayer();
            }
            yield return Wait(()=>FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None).Any(e=>e.GetComponent<EnemyRank>()?.Tier!=EnemyTier.Boss),"Ordinary waves resume after boss");
            starterWeapon.Range=originalWeaponRange;
            JumpClock(director.NextAt-.02f);Time.timeScale=1;
            yield return Wait(()=>director.ActiveBoss!=null,"Arrival available for exit cancellation");
            Require(director.ActiveBoss.Entering,"Exit is exercised during the boss entrance");
            var exitWallet=Resources.FindObjectsOfTypeAll<AttributeSO>().Single(x=>x.name=="AttributeSO");
            int expectedExitGold=exitWallet.GoldCount+_round.GlobalConfig.UnbankedGold;
            _round.ExitToMainMenu();yield return WaitForMenu();
            yield return new WaitForSecondsRealtime(.25f);
            Require(FindObjectsByType<BossEntrance>(FindObjectsSortMode.None).All(x=>!x.IsEntering)&&
                    BossHazard.ActiveCount==0&&exitWallet.GoldCount==expectedExitGold,
                "Exit during entrance clears the ceremony without attacks or free boss rewards");
            foreach(int stage in new[]{0,1})
            {
                yield return EnterRound(stage,"Kael");
                yield return new WaitForSeconds(1);
                Require(!FindFirstObjectByType<EndlessBossDirector>().IsEncounterActive&&FindFirstObjectByType<EndlessBossDirector>().EncounterCount==0,"Survival/KillTarget never start boss encounters");
                yield return LeaveFromPause();
            }
        }
        IEnumerator CheckBossEntrance(EndlessBossEnemy boss,EndlessBossDirector director,SpriteVisibilityProbe visibility)
        {
            Time.timeScale=1;
            var entry=boss.GetComponent<BossEntrance>();var brain=boss.GetComponent<BossBrain>();
            Require(entry!=null&&entry.IsEntering&&boss.Entering,"Boss reserves the encounter before entering combat");
            Require(entry.Elapsed<BossEntrance.WarningSeconds&&boss.MySpriteRenderer.color.a<.01f&&director.ArrivalBanner.IsVisible,
                "Warning banner precedes the visible boss reveal");
            int hp=boss.StatRuntime.GetValue("HP");boss.MyHit.Hit(999999);
            Require(boss.StatRuntime.GetValue("HP")==hp&&!brain.enabled&&!boss.MyWeapon.enabled&&
                    boss.GetComponentsInChildren<Collider2D>(true).All(c=>!c.enabled)&&BossHazard.ActiveCount==0,
                "Entrance blocks damage, contact attacks and boss AI");
            int speed=boss.StatRuntime.GetValue("Speed");
            var slow=ScriptableObject.CreateInstance<SlowBuff>();slow.SlowPercentage=100;slow.BuffName="SlowBuff";
            FindFirstObjectByType<BuffManager>().AddBuff(boss,slow);Destroy(slow);
            Require(boss.StatRuntime.GetValue("Speed")==speed,"Entrance rejects premature status effects");
            yield return CaptureFrame("arrival-"+entry.Kind+"-warning");
            float deadline=Time.realtimeSinceStartup+10;
            bool sampledPause=false,captured=false,moved=false,visible=false,noEarlyAttack=true;
            var hero=_round.GlobalConfig.GlobalPlayer.transform;Vector3 heroStart=hero.position;
            var heroSprite=hero.GetComponent<PlayerAnim>().MyAnimator.GetComponent<SpriteRenderer>();
            var entryLines=boss.GetComponentsInChildren<LineRenderer>(true).Where(l=>l.name.StartsWith("Boss Entrance FX ")).ToArray();
            bool movedHero=false,heroVisible=true;
            Vector3 firstVisible=boss.transform.position;
            while(entry.IsEntering&&Time.realtimeSinceStartup<deadline)
            {
                if(boss.MySpriteRenderer.color.a>.25f)
                {
                    if(!visible){firstVisible=boss.transform.position;visible=true;}
                    moved|=Vector3.Distance(firstVisible,boss.transform.position)>.05f;
                    noEarlyAttack&=!brain.enabled&&!boss.MyWeapon.enabled&&BossHazard.ActiveCount==0;
                    heroVisible&=boss.MySpriteRenderer.sortingOrder<heroSprite.sortingOrder&&entryLines.All(l=>!l.enabled||l.sortingOrder<heroSprite.sortingOrder);
                }
                if(!movedHero&&entry.Elapsed>BossEntrance.WarningSeconds+.2f){hero.position+=Vector3.up*.8f;movedHero=true;}
                if(!sampledPause&&entry.Elapsed>BossEntrance.WarningSeconds+.5f)
                {
                    _round.Pause();
                    float elapsed=entry.Elapsed,clock=boss.GetComponent<BossVisualAnimation>().AnimationClock;
                    var pos=boss.transform.position;var color=boss.MySpriteRenderer.color;
                    var lines=boss.GetComponentsInChildren<LineRenderer>(true).Where(l=>l.name.StartsWith("Boss Entrance FX ")&&l.enabled).ToArray();
                    var positions=CaptureLinePositions(lines);
                    yield return new WaitForSecondsRealtime(.2f);
                    Require(entry.Elapsed==elapsed&&boss.transform.position==pos&&boss.MySpriteRenderer.color==color&&
                            boss.GetComponent<BossVisualAnimation>().AnimationClock==clock&&SameLinePositions(lines,positions),
                        "Pause freezes entrance position, opacity, native animation and ceremony VFX");
                    _round.Resume();sampledPause=true;
                }
                if(!captured&&entry.Elapsed>BossEntrance.WarningSeconds+BossEntrance.RevealSeconds*.70f)
                {
                    var fxBounds=LineBounds(boss.GetComponentsInChildren<LineRenderer>(true).Where(l=>l.name.StartsWith("Boss Entrance FX ")&&l.enabled).ToArray());
                    Require(fxBounds.max.y>boss.transform.position.y+entry.BodyHeight*.25f&&
                            (fxBounds.size.x>entry.TargetBounds.size.x*.5f||fxBounds.size.y>entry.BodyHeight*.5f),
                        "Entrance VFX span the giant body and extend above the feet");
                    yield return CaptureFrame("arrival-"+entry.Kind+"-reveal");captured=true;
                }
                yield return null;
            }
            Require(!entry.IsEntering&&!boss.Entering&&sampledPause&&captured&&visible&&moved,
                $"{entry.Style} completes a visible moving entrance on scaled time (elapsed={entry.Elapsed:0.00}, visible={visible}, moved={moved}, pause={sampledPause}, captured={captured}, entering={entry.IsEntering})");
            Require(noEarlyAttack,"Boss cannot cast or deal contact damage during its visible arrival");
            Require(movedHero&&heroVisible,"Moving survivor stays in front of the giant body and entrance VFX");
            hero.position=heroStart;
            bool direction=entry.Kind==2?firstVisible.y>boss.transform.position.y:
                entry.Kind==3?firstVisible.x>boss.transform.position.x:firstVisible.y<boss.transform.position.y;
            Require(direction,$"{entry.Style} follows its distinct entry direction");
            Require(brain.enabled&&boss.MyWeapon.enabled&&boss.GetComponentsInChildren<Collider2D>(true).Any(c=>c.enabled)&&
                    boss.MySpriteRenderer.color.a>.99f&&visibility.RenderedSignature(boss.MySpriteRenderer).OpaquePixels>200,
                "Completed arrival restores visible body, colliders and combat exactly once");
            Require(boss.GetComponentsInChildren<LineRenderer>(true).Where(l=>l.name.StartsWith("Boss Entrance FX ")).All(l=>!l.enabled),
                "Ceremony VFX are hidden after entry; they are not attack telegraphs");
        }
        static Bounds LineBounds(LineRenderer[] lines)
        {
            bool first=true;Bounds bounds=default;
            foreach(var line in lines)for(int i=0;i<line.positionCount;i++)
            {var point=line.GetPosition(i);if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);}
            return bounds;
        }
        void CheckBossStatusShader(EndlessBossEnemy boss,PoolRuntimeSO pool,SpriteVisibilityProbe visibility)
        {
            int speed=boss.StatRuntime.GetValue("Speed");
            var material=boss.MySpriteRenderer.material;
            Vector4 shadow=material.GetVector("_ShadowOffset");
            foreach(bool freeze in new[]{false,true})
            {
                BaseBuff buff;
                if(freeze){var value=ScriptableObject.CreateInstance<FreezingBuff>();value.PoolRuntimeSo=pool;buff=value;}
                else{var value=ScriptableObject.CreateInstance<SlowBuff>();value.PoolRuntimeSo=pool;value.SlowPercentage=30;buff=value;}
                buff.Owner=boss;bool activated=false;
                try
                {
                    buff.Activate();activated=true;
                    int expected=freeze?0:speed-(int)(speed*.30f);
                    Require(boss.StatRuntime.GetValue("Speed")==expected,
                        $"{BossCatalog.Ids[boss.GetComponent<BossBrain>().Kind]} {(freeze?"freeze":"slow")} preserves its existing Speed modifier on the native sprite shader");
                    Require(visibility.RenderedSignature(boss.MySpriteRenderer).OpaquePixels>200,
                        "Status effect keeps the actual boss sprite visible without legacy shader properties");
                    buff.DeActivate();activated=false;
                    Require(boss.StatRuntime.GetValue("Speed")==speed&&material.GetVector("_ShadowOffset")==shadow,
                        "Status effect cleanup restores Speed and shadow offset exactly");
                }
                finally{if(activated)buff.DeActivate();Destroy(buff);}
            }
            var burn=ScriptableObject.CreateInstance<BurnBuff>();burn.Owner=boss;bool burning=false;
            try
            {
                burn.Activate();burning=true;
                Require(material.HasProperty("_FlameRedBlend")&&material.GetColor("_FlameRedBlend")==Color.red&&
                        visibility.RenderedSignature(boss.MySpriteRenderer).OpaquePixels>200,
                    "Burn tint is supported by the native boss shader and preserves sprite opacity");
                burn.DeActivate();burning=false;
                Require(material.GetColor("_FlameRedBlend")==Color.white,"Burn cleanup restores the neutral boss tint");
            }
            finally{if(burning)burn.DeActivate();Destroy(burn);}
        }
        IEnumerator CheckLiveBossAnimation(EndlessBossEnemy boss,SpriteVisibilityProbe visibility)
        {
            var rig=boss.GetComponent<BossVisualAnimation>();
            Require(rig!=null&&rig.AnimatedRenderer==boss.MySpriteRenderer&&rig.AnimatedRenderer!=null&&
                    !rig.AnimatedRenderer.name.ToLowerInvariant().Contains("shadow")&&rig.AnimatedRenderer.sprite!=null,
                $"{BossCatalog.Ids[boss.GetComponent<BossBrain>().Kind]} uses a real animated body sprite renderer");
            if(rig==null||rig.AnimatedRenderer==null)yield break;

            Require(rig.StandingSprite!=null,"Boss animation rig retains its canonical Idle frame 0 sprite");
            int hp=boss.StatRuntime.GetValue("HP");
            Time.timeScale=3;
            float startClock=rig.AnimationClock;
            var previous=visibility.RenderedSignature(rig.AnimatedRenderer);
            var previousFrame=rig.AnimatedRenderer.sprite;
            var previousMotion=rig.Motion;
            float previousClock=rig.AnimationClock;
            bool silhouetteChanged=false,frameChanged=false,moveSeen=rig.Motion==BossMotion.Move;
            for(int sample=0;sample<6;sample++)
            {
                yield return new WaitForSeconds(.22f);
                var current=visibility.RenderedSignature(rig.AnimatedRenderer);
                moveSeen|=rig.Motion==BossMotion.Move;
                if(rig.Motion==previousMotion&&rig.AnimationClock-previousClock>=.1f&&
                   (current.AlphaHash!=previous.AlphaHash||current.OpaquePixels!=previous.OpaquePixels))
                    silhouetteChanged=true;
                if(rig.Motion==previousMotion&&rig.AnimatedRenderer.sprite!=previousFrame)frameChanged=true;
                previous=current;previousFrame=rig.AnimatedRenderer.sprite;previousMotion=rig.Motion;previousClock=rig.AnimationClock;
            }
            Require(rig.AnimationClock>startClock+1f,$"{BossCatalog.Ids[boss.GetComponent<BossBrain>().Kind]} animation clock advances during live AI");
            Require(moveSeen,$"{BossCatalog.Ids[boss.GetComponent<BossBrain>().Kind]} enters its move animation while approaching the survivor");
            Require(frameChanged&&silhouetteChanged&&previous.OpaquePixels>200,
                $"{BossCatalog.Ids[boss.GetComponent<BossBrain>().Kind]} advances native sprite frames with changing opaque pixels in fresh GPU renders");
            Require(boss.StatRuntime.GetValue("HP")==hp,
                $"{BossCatalog.Ids[boss.GetComponent<BossBrain>().Kind]} rendered animation samples do not rely on hit flash");
        }
        IEnumerator CheckBossFrameCoverage(EndlessBossEnemy boss,BossVisualAnimation rig,SpriteVisibilityProbe visibility)
        {
            string bossName=BossCatalog.Ids[boss.GetComponent<BossBrain>().Kind];
            var frames=rig!=null?rig.Frames:null;
            Require(frames!=null&&frames.Length==36,$"{bossName} imports the complete 36-sprite animation set");
            if(frames==null||frames.Length!=36)yield break;
            Require(frames.Distinct().Count()==36,$"{bossName} animation set has 36 unique imported sprite frames");
            Require(rig.AnimatedRenderer!=null&&rig.AnimatedRenderer.sprite!=null,
                $"{bossName} animation frames are assigned to an active body renderer");
            if(rig.AnimatedRenderer==null)yield break;

            var clips=rig.StateClips;
            Require(clips!=null&&clips.Length==6&&clips.Distinct().Count()==6&&clips.All(clip=>clip!=null&&clip.length>0f),
                $"{bossName} has six nonempty native Animator clips");
            var animator=rig.NativeAnimator??rig.GetComponentInChildren<Animator>(true);
            var controllerClips=animator!=null&&animator.runtimeAnimatorController!=null
                ?animator.runtimeAnimatorController.animationClips
                :Array.Empty<AnimationClip>();
            Require(animator!=null&&clips!=null&&clips.All(clip=>controllerClips.Contains(clip)),
                $"{bossName} wires all six authored clips into its native Animator controller");
            var material=rig.AnimatedRenderer.sharedMaterial;
            var blankFrames=new System.Collections.Generic.List<string>();
            for(int i=0;i<frames.Length;i++)
            {
                if(frames[i]==null){blankFrames.Add(i+" (null)");continue;}
                int pixels=visibility.VisiblePixels(frames[i],material);
                int minimum=i<34?100:1; // Death may dissolve, but its authored poses must still contain real pixels.
                if(pixels<minimum)blankFrames.Add(i+" ("+pixels+" opaque pixels)");
            }
            Require(blankFrames.Count==0,
                $"{bossName} imported frames render through the actual boss material with no blank alive pose: {string.Join(", ",blankFrames)}");

            foreach(BossMotion motion in Enum.GetValues(typeof(BossMotion)))
            {
                int first=motion switch
                {
                    BossMotion.Idle=>0,BossMotion.Move=>8,BossMotion.Windup=>16,
                    BossMotion.Attack=>24,BossMotion.Hit=>32,_=>34
                };
                int expected=motion==BossMotion.Idle||motion==BossMotion.Move||
                             motion==BossMotion.Windup||motion==BossMotion.Attack?8:2;
                var stateFrames=rig.FramesFor(motion);
                var clip=rig.ClipFor(motion);
                bool matches=stateFrames!=null&&stateFrames.Length==expected&&
                    stateFrames.SequenceEqual(frames.Skip(first).Take(expected))&&
                    clips!=null&&clips.Length==6&&clip!=null&&clips[(int)motion]==clip&&controllerClips.Contains(clip);
                Require(matches,
                    $"{bossName} {motion} clip references its {expected} imported sprite frames");
            }

            if(animator!=null&&animator.runtimeAnimatorController!=null)
            {
                var originalMotion=rig.Motion;
                var originalState=animator.GetCurrentAnimatorStateInfo(0);
                var mismatches=new System.Collections.Generic.List<string>();
                const int playbackSamples=41;
                foreach(BossMotion motion in Enum.GetValues(typeof(BossMotion)))
                {
                    var stateFrames=rig.FramesFor(motion);
                    if(stateFrames==null||stateFrames.Length==0)continue;
                    int stateHash=Animator.StringToHash("Base Layer."+motion);
                    var observedFrames=new System.Collections.Generic.HashSet<Sprite>();
                    int unexpectedSamples=0;
                    for(int sample=0;sample<playbackSamples;sample++)
                    {
                        float normalizedTime=sample/(float)(playbackSamples-1)*.999f;
                        animator.Play(stateHash,0,normalizedTime);
                        animator.Update(0f);
                        var displayed=rig.AnimatedRenderer.sprite;
                        if(stateFrames.Contains(displayed))observedFrames.Add(displayed);
                        else unexpectedSamples++;
                    }
                    var missingFrames=stateFrames.Where(frame=>!observedFrames.Contains(frame)).Select(frame=>frame.name);
                    string missing=string.Join(",",missingFrames);
                    if(unexpectedSamples>0||missing.Length>0)
                        mismatches.Add(motion+" unexpected samples="+unexpectedSamples+" missing frames=["+missing+"]");
                }
                int restoreHash=Animator.StringToHash("Base Layer."+originalMotion);
                animator.Play(restoreHash,0,originalState.normalizedTime);
                animator.Update(0f);
                Require(mismatches.Count==0,
                    $"{bossName} native Animator clips display only their expected frames and include every pose across 41 normalized-time samples: {string.Join("; ",mismatches)}");
            }
        }
        IEnumerator CheckBossArrivalBanner(EndlessBossDirector director,int kind,bool exerciseShowHide)
        {
            var banner=director.ArrivalBanner;
            yield return new WaitForSecondsRealtime(.08f);
            Require(banner!=null&&banner.IsVisible&&banner.DisplayedKind==kind,
                $"Boss arrival banner appears for {BossCatalog.Ids[kind]}");
            if(banner==null)yield break;

            bool vi=LanguageManager.GetCurrentCode()=="vi";
            var name=banner.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="BossArrivalName");
            var announcement=banner.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="BossArrivalAnnouncement");
            Require(name.text==BossCatalog.Name(kind,vi)&&name.color==BossCatalog.Colors[kind]&&
                    announcement.text==(vi?"BOSS ĐANG ĐẾN":"BOSS INCOMING"),
                $"Arrival banner localizes the { (vi?"Vietnamese":"English") } boss name and applies its catalog color");
            var group=banner.GetComponentInChildren<CanvasGroup>(true);
            var graphics=banner.GetComponentsInChildren<Graphic>(true);
            Require(group!=null&&!group.blocksRaycasts&&!group.interactable&&graphics.All(g=>!g.raycastTarget),
                "Boss arrival banner does not intercept pointer input");
            Require(banner.ShownSeconds>=0f&&banner.ShownSeconds<BossArrivalBanner.DisplayDuration,"Arrival banner tracks its bounded warning and reveal display");
            Canvas.ForceUpdateCanvases();
            name.ForceMeshUpdate();announcement.ForceMeshUpdate();
            Require(!name.isTextOverflowing&&!announcement.isTextOverflowing,"Boss arrival name and announcement fit their text rectangles");
            var plate=(RectTransform)banner.transform.Find("BossArrivalBannerCanvas/BossArrivalContent/BossArrivalPlate");
            var timer=(RectTransform)GameObject.Find("Jade_TimePlate").transform;
            var plateCorners=new Vector3[4];var timerCorners=new Vector3[4];
            plate.GetWorldCorners(plateCorners);timer.GetWorldCorners(timerCorners);
            Require(plateCorners[1].y<timerCorners[0].y,"Arrival banner remains below the match clock without covering its panel");

            if(exerciseShowHide)
            {
                banner.Hide();
                Require(!banner.IsVisible&&banner.DisplayedKind==-1,"Banner Hide clears its active state and displayed boss");
                banner.Show(kind);
                yield return new WaitForSecondsRealtime(.08f);
                Require(banner.IsVisible&&banner.DisplayedKind==kind,"Banner Show restores the selected boss name");
            }
        }
        IEnumerator CheckBossHazardDamage(EndlessBossEnemy boss)
        {
            boss.GetComponent<BossBrain>().StopFight();boss.enabled=false;boss.MyWeapon.enabled=false;
            var target=_round.GlobalConfig.GlobalPlayer.GetComponentInChildren<PlayerHit>();
            Vector3 previousBossPosition=boss.transform.position;
            boss.transform.position=target.transform.position+Vector3.right*30;
            int recovery=_round.PlayerStat.GetValue("Recovery");_round.PlayerStat.ModifyValue("Recovery",-recovery);
            Time.timeScale=1;
            foreach(BossHazard.Shape shape in Enum.GetValues(typeof(BossHazard.Shape)))
            {
                Vector2 p=target.transform.position;
                Vector2 from=shape==BossHazard.Shape.Line?p-Vector2.right*3:shape==BossHazard.Shape.Ring?p-Vector2.right*3:shape==BossHazard.Shape.Orbit?p-Vector2.right*2.8f:p;
                Vector2 to=shape==BossHazard.Shape.Line?p+Vector2.right*3:Vector2.right;
                float radius=shape==BossHazard.Shape.Ring?3:.6f;
                int hp=_round.PlayerStat.GetValue("HP");
                var attack=BossHazard.Create(boss.transform,target,shape,from,to,radius,1.1f,.4f,Color.red);
                var warningLines=attack.GetComponentsInChildren<LineRenderer>(true);
                bool hidden=true,noEarlyDamage=true;int warningSamples=0;
                float created=Time.time,firstDamageAfter=-1,deadline=Time.realtimeSinceStartup+4;
                // Measure the actual first damage frame. Chained scaled waits can
                // overshoot the windup when a Windows frame stalls or encodes a PNG.
                while(attack.gameObject.activeSelf&&Time.realtimeSinceStartup<deadline)
                {
                    if(attack.IsTelegraph)
                    {
                        warningSamples++;hidden&=warningLines.All(line=>!line.enabled);
                        noEarlyDamage&=_round.PlayerStat.GetValue("HP")==hp;
                    }
                    yield return null;
                    if(_round.PlayerStat.GetValue("HP")!=hp&&firstDamageAfter<0)firstDamageAfter=Time.time-created;
                }
                Require(warningSamples>0&&noEarlyDamage,shape+" warning cannot damage before its windup");
                Require(warningLines.Length>0&&hidden,shape+" warning keeps every root and child line renderer hidden throughout the windup");
                Require(!attack.gameObject.activeSelf&&firstDamageAfter>=1.099f,
                    $"{shape} waits for the full 1.1-second windup (actual first damage={firstDamageAfter:0.000}s, warning samples={warningSamples})");
                int damage=Mathf.Max(1,(int)(8*_round.PlayerStat.GetValue("Armor")*.01f));
                Require(_round.PlayerStat.GetValue("HP")==hp-damage,shape+" active geometry hits through real PlayerHit exactly once");
                BossHazard.ClearAll();hp=_round.PlayerStat.GetValue("HP");
                var safe=BossHazard.Create(boss.transform,target,shape,p+Vector2.right*12,p+Vector2.right*15,.6f,1.1f,.4f,Color.red);
                deadline=Time.realtimeSinceStartup+4;
                while(safe.gameObject.activeSelf&&Time.realtimeSinceStartup<deadline)yield return null;
                Require(!safe.gameObject.activeSelf&&_round.PlayerStat.GetValue("HP")==hp,shape+" allows safe positions outside the visible danger area");
            }
            BossHazard.ClearAll();boss.transform.position=previousBossPosition;_round.PlayerStat.ModifyValue("Recovery",recovery);boss.enabled=true;boss.MyWeapon.enabled=true;Time.timeScale=1;
        }
        static LineRenderer[] ActiveBossSkillVfxLines()
        {
            return FindObjectsByType<LineRenderer>(FindObjectsSortMode.None)
                .Where(line=>line!=null&&line.enabled&&line.name.StartsWith("Boss VFX ",StringComparison.Ordinal))
                .ToArray();
        }
        static Vector3[][] CaptureLinePositions(LineRenderer[] lines)
        {
            var positions=new Vector3[lines.Length][];
            for(int i=0;i<lines.Length;i++)
            {
                positions[i]=new Vector3[lines[i].positionCount];
                lines[i].GetPositions(positions[i]);
            }
            return positions;
        }
        static bool SameLinePositions(LineRenderer[] lines,Vector3[][] expected)
        {
            if(lines==null||expected==null||lines.Length!=expected.Length)return false;
            for(int i=0;i<lines.Length;i++)
            {
                if(lines[i]==null||!lines[i].enabled||lines[i].positionCount!=expected[i].Length)return false;
                var current=new Vector3[lines[i].positionCount];
                lines[i].GetPositions(current);
                if(!expected[i].SequenceEqual(current))return false;
            }
            return true;
        }
        IEnumerator CheckSpawnPacing()
        {
            Require(SpawnPacing.Rate(0)<SpawnPacing.Rate(30)&&SpawnPacing.Rate(30)<SpawnPacing.Rate(60)&&SpawnPacing.Rate(60)<SpawnPacing.Rate(120)&&SpawnPacing.Rate(120)<SpawnPacing.Rate(300),"Shared spawn budget rises smoothly across opening, mid and late game");
            yield return EnterRound(8,"Kael");
            _round.PlayerStat.ModifyValue("HP",100000,true);_round.PlayerStat.ModifyValue("HP",100000);
            _round.GlobalConfig.GlobalPlayer.GetComponent<StarterWeaponProfile>().Range=0; // Isolate spawn counts from combat deaths.
            int initial=ActiveEnemies(),at30=0;
            float limit=Time.realtimeSinceStartup+70;
            while(_round.MatchSeconds<60&&Time.realtimeSinceStartup<limit)
            {
                Time.timeScale=3;
                if(at30==0&&_round.MatchSeconds>=30)at30=ActiveEnemies();
                yield return null;
            }
            int at60=ActiveEnemies();
            Require(_round.MatchSeconds>=60&&at30>initial,"Measured two consecutive 30-second spawn windows");
            Require(at30-initial<=35&&at60-at30>at30-initial,$"Opening is lighter than next window: first {at30-initial}, next {at60-at30} enemies");
            Require(!FindFirstObjectByType<EndlessBossDirector>().IsEncounterActive,"Spawn pacing retains five-minute boss schedule");
            Time.timeScale=1;_round.Pause();yield return CaptureFrame("spawn-pacing-60s");
            _round.ExitToMainMenu();yield return WaitForMenu();
        }
        static float VisibleHeight(SpriteRenderer renderer)
        {
            return renderer==null?0:VisibleHeight(renderer.sprite,renderer.transform.lossyScale.y);
        }
        static float ReadMaterialFloat(SpriteRenderer renderer,string property)
        {
            var material=renderer!=null?renderer.sharedMaterial:null;
            return material!=null&&material.HasProperty(property)?material.GetFloat(property):float.NaN;
        }
        static float VisibleHeight(Sprite sprite,float scale=1f)
        {
            if(sprite==null||sprite.texture==null||sprite.pixelsPerUnit<=0)return 0;
            var rect=sprite.rect;var pixels=sprite.texture.GetPixels32();int min=99999,max=-1;
            for(int y=(int)rect.y;y<rect.yMax;y++)for(int x=(int)rect.x;x<rect.xMax;x++)if(pixels[y*sprite.texture.width+x].a>=128){min=Math.Min(min,y);max=Math.Max(max,y);}
            return max<min?0:(max-min+1)/sprite.pixelsPerUnit*scale;
        }
        IEnumerator CheckBalance()
        {
            var owned=Resources.FindObjectsOfTypeAll<AttributeSO>().Single(x=>x.name=="AttributeSO");
            var stats=Resources.FindObjectsOfTypeAll<CommonStatRuntimeSO>().Single(x=>x.name=="PowerUpStat");
            string original=JsonUtility.ToJson(owned);int gold=owned.GoldCount;
            PermanentBalance.Apply(owned,stats);int initial=stats.GetValue("Might");
            for(int i=0;i<3;i++)
            {
                Click(ButtonIn(FindFirstObjectByType<UIMainMenu>(),"ButtonPowerUp"));yield return new WaitForSecondsRealtime(.3f);
                Require(stats.GetValue("Might")==initial&&owned.GoldCount==gold,"Opening power menu neither stacks stats nor spends gold");
                Click(ButtonIn(FindFirstObjectByType<PowerUpPanel>(),"Button Close"));yield return null;
            }
            Require(original==JsonUtility.ToJson(owned),"Rebalance preserves every purchased level and saved currency");
            // Simulate an existing save containing mixed purchases, then rebuild repeatedly.
            owned.SetAttribute("Might",3);owned.SetAttribute("HPMax",2);owned.SetAttribute("Armor",2);owned.SetAttribute("GoldCount",4321);
            Resources.FindObjectsOfTypeAll<SaveSystemSO>().Single().SaveDataToDisk();
            for(int i=0;i<4;i++)PermanentBalance.Apply(owned,stats);
            Require(stats.GetValue("Might")==12&&stats.GetValue("HPMax")==16&&stats.GetValue("Armor")==-10&&owned.Might==3&&owned.GoldCount==4321,
                "Existing purchased levels receive new bonuses exactly once with currency untouched");
            yield return EnterRound(0,"Rowan");
            Require(_round.PlayerStat.GetValue("Might")==112&&_round.PlayerStat.GetMaxValue("HP")==116&&_round.PlayerStat.GetValue("Armor")==90,
                $"Purchased bonuses apply after run reset even without opening power menu: Might={_round.PlayerStat.GetValue("Might")}, HPMax={_round.PlayerStat.GetMaxValue("HP")}, Armor={_round.PlayerStat.GetValue("Armor")}, owned={owned.Might}/{owned.HPMax}/{owned.Armor}");
            var manager=FindFirstObjectByType<SkillManager>();var skill=manager.OwnedSkills.First();
            Require(skill.Damage==12&&Mathf.Abs(skill.CD-.9f)<.001f,"Rowan starter retains balanced damage and cooldown");
            var pool=Resources.FindObjectsOfTypeAll<PoolRuntimeSO>().Single(x=>x.name=="Pool");
            var defs=(System.Collections.Generic.List<PoolDefinition>)typeof(PoolRuntimeSO).GetField("_poolDefinitions",Private).GetValue(pool);
            foreach(var def in defs.Where(x=>x.Pool.Prefab.GetComponent<BaseEnemy>()!=null))
            {
                var enemy=def.Pool.Prefab.GetComponent<BaseEnemy>();var rank=enemy.GetComponent<EnemyRank>();
                var config=(ValueConfigSO)typeof(BaseEnemy).GetField("_valueConfig",Private).GetValue(enemy);
                int hp=config.ValueDefinitions.Single(x=>x.ValueName=="HP").DefaultValue;
                Require(rank!=null&&hp==(rank.Tier==EnemyTier.Boss?300:rank.Tier==EnemyTier.Elite?90:30),def.Key+" tier and HP are balanced: "+hp);
            }
            // Non-reentrant large XP award should consume the thresholds for successive levels.
            var levelManager=FindFirstObjectByType<LevelManager>();int level=_round.PlayerStat.GetValue("Level");
            var configXP=(ExpConfig)typeof(LevelManager).GetField("expConfig",Private).GetValue(levelManager);
            int xp=configXP.GetExperienceForLevel(level)+configXP.GetExperienceForLevel(level+1)+7;
            _round.PlayerStat.ModifyValue("Exp",xp);
            Require(_round.PlayerStat.GetValue("Level")==level+2&&_round.PlayerStat.GetValue("Exp")==7,"Large XP gain uses successive level thresholds without recursive double spending");
            for(int i=0;i<2;i++){yield return null;PickUpgradeOrTreasure();}
            Require(Time.timeScale>0,"Two XP choices close sequentially");
            int walletBefore=owned.GoldCount;
            _round.GlobalConfig.CurrentGetGold+=137;
            int amount=_round.GlobalConfig.UnbankedGold;
            _round.GlobalConfig.BankGold();_round.GlobalConfig.BankGold();
            Require(owned.GoldCount==walletBefore+amount,"Banking twice credits run earnings only once");
            int banked=owned.GoldCount;
            _round.ExitToMainMenu();yield return WaitForMenu();
            Require(owned.GoldCount==banked,"Exit-to-menu does not credit already banked gold twice");
            Resources.FindObjectsOfTypeAll<SaveSystemSO>().Single().LoadSaveDataFromDisk();
            Require(owned.GoldCount==banked&&owned.Might==3&&owned.HPMax==2,"Reloading save preserves earned currency and existing purchases");
            Click(ButtonIn(FindFirstObjectByType<UIMainMenu>(),"ButtonPowerUp"));yield return new WaitForSecondsRealtime(.3f);
            var panel=FindFirstObjectByType<PowerUpPanel>();
            var tile=panel.GetComponentsInChildren<PowerUpAttributeUpgrade>().Single(x=>x.name.Trim()=="Might");
            typeof(PowerUpAttributeUpgrade).GetMethod("Upgrade",Private).Invoke(tile,null);
            Require(owned.Might==4&&owned.GoldCount==banked-PermanentBalance.Cost("Might",3)&&stats.GetValue("Might")==16,"Buying next permanent rank retains old ranks, charges one cost, applies new bonus once");
            Click(ButtonIn(panel,"Button Close"));yield return null;
            JsonUtility.FromJsonOverwrite(original,owned);PermanentBalance.Apply(owned,stats);
            Resources.FindObjectsOfTypeAll<SaveSystemSO>().Single().SaveDataToDisk();
        }
        IEnumerator CheckPauseSkills()
        {
            foreach(string hero in Heroes)
            {
                yield return EnterRound(0,hero);_round.Pause();
                CheckContactShadows(hero);
                var manager=FindFirstObjectByType<SkillManager>();
                foreach(string key in new[]{"BothSideSkill","RandomSkill","BouncingSkill","ChainSkill","CircularMotionSkill","Armor","Might","HPMax","Cooldown","Magnet","Growth"})manager.UpgradeSkill(key);
                foreach(string language in new[]{"vi","en"})
                {
                    yield return SelectLanguage(language);yield return null;
                    PauseSkillInventory view=null;
                    yield return Wait(()=>{view=FindFirstObjectByType<PauseSkillInventory>();return view!=null&&view.DisplayedSkills==manager.OwnedSkills.Count();},"Pause inventory shows every owned skill");
                    string before=string.Join(",",_round.PlayerStat.GetAllValues().Select(x=>x.Key+":"+_round.PlayerStat.GetValue(x.Key)));
                    float clock=_round.MatchSeconds;
                    var scroll=view.GetComponentInChildren<ScrollRect>();
                    Require(scroll.content.rect.height>scroll.viewport.rect.height,"Owned skills exceed viewport and can scroll");
                    yield return CaptureFrame("pause-"+hero+"-"+language+"-before-scroll");
                    float originalScroll=scroll.verticalNormalizedPosition;
                    var pointer=PointerAt(scroll.transform);pointer.scrollDelta=new Vector2(0,-8);
                    var hits=Raycast(pointer);Require(hits.Count>0,"Inventory receives pointer input");
                    var receiver=ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.scrollHandler);
                    Require(receiver==scroll.gameObject,"Inventory scroll receives wheel event; top hit="+hits[0].gameObject.name);
                    yield return null;
                    Require(scroll.verticalNormalizedPosition<originalScroll-.01f,$"Mouse wheel scrolls owned skills: {originalScroll:0.00} -> {scroll.verticalNormalizedPosition:0.00}");
                    yield return CaptureFrame("pause-"+hero+"-"+language+"-scrolled");
                    scroll.verticalNormalizedPosition=1;yield return null;
                    var firstButton=scroll.content.GetComponentsInChildren<Button>().First();
                    var click=PointerAt(firstButton.transform);var clickHits=Raycast(click);
                    Require(clickHits.Count>0&&ExecuteEvents.ExecuteHierarchy(clickHits[0].gameObject,click,ExecuteEvents.pointerClickHandler)==firstButton.gameObject,"Owned skill row opens details through pointer input");
                    Require(!string.IsNullOrEmpty(view.SelectedSkillKey)&&view.DisplayedDetail.Length>200,"Selected skill shows its mechanic and live values");
                    if(hero=="Rowan")Require(!view.DisplayedDetail.Contains("Thiêu đốt:")&&!view.DisplayedDetail.Contains("Burn:")&&(view.DisplayedDetail.Contains("Góc chém:")||view.DisplayedDetail.Contains("Cleave angle:")),"Rowan details show the real cleave cone and do not inherit the ranged projectile burn");
                    var detail=view.GetComponentsInChildren<ScrollRect>().Single(s=>s!=scroll);
                    Require(detail.content.rect.height>detail.viewport.rect.height,"Long skill description has its own scroll area");
                    var wheel=PointerAt(detail.viewport);wheel.scrollDelta=new Vector2(0,-8);var detailHits=Raycast(wheel);
                    Require(detailHits.Count>0&&ExecuteEvents.ExecuteHierarchy(detailHits[0].gameObject,wheel,ExecuteEvents.scrollHandler)==detail.gameObject,"Detail panel receives wheel input");
                    yield return null;Require(detail.verticalNormalizedPosition<.99f,"Skill details can scroll to evolution information");
                    detail.verticalNormalizedPosition=1;yield return CaptureFrame("pause-"+hero+"-"+language);
                    yield return new WaitForSecondsRealtime(.2f);
                    Require(clock==_round.MatchSeconds&&before==string.Join(",",_round.PlayerStat.GetAllValues().Select(x=>x.Key+":"+_round.PlayerStat.GetValue(x.Key))),"Reading inventory changes neither time nor stats");
                }
                _round.Resume();yield return LeaveFromPause();
            }
        }
        void CheckContactShadows(string hero)
        {
            using var probe=new SpriteVisibilityProbe();
            var shadow=_round.GlobalConfig.GlobalPlayer.GetComponentInChildren<PlayerHit>().ContactShadow;
            Require(shadow!=null&&shadow.GetComponent<SoftContactShadow>()!=null,$"{hero}: existing contact shadow uses the soft ellipse");
            Require(probe.RenderedSignature(shadow).OpaquePixels>0,$"{hero}: contact shadow contains visible rendered pixels");
            var ellipse=probe.LastRenderedPixels();
            Require(ellipse.Max(x=>x.a)<=60&&ellipse.Count(x=>x.a>0)<128*128/3,$"{hero}: shadow alpha={ellipse.Max(x=>x.a)}, coverage={ellipse.Count(x=>x.a>0)} / 16384; faint flattened ellipse");
            var go=new GameObject("QA shadow alpha mask",typeof(SpriteRenderer));
            var tex=new Texture2D(32,32,TextureFormat.RGBA32,false);
            var activeEnemy=FindFirstObjectByType<BaseEnemy>();var sourceMaterial=activeEnemy.MySpriteRenderer.sharedMaterial;
            Record("INFO","Actual enemy material: "+sourceMaterial.name+" / shader "+sourceMaterial.shader.name);
            Require(sourceMaterial.HasProperty("_ShadowScale"),"Actual enemy prefab material supports silhouette shadows");
            var material=new Material(sourceMaterial);
            var sprite=Sprite.Create(tex,new Rect(0,0,32,32),new Vector2(.5f,.5f),32,0,SpriteMeshType.FullRect);
            try
            {
                var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=material;
                material.SetFloat("_Cutoff",0);material.SetFloat("_DistortionStrength",0);material.SetVector("_ShadowOffset",new Vector4(0,-.35f,0,0));
                material.SetColor("_ShadowColor",new Color(0,0,0,.9f));
                var pixels=new Color32[32*32];tex.SetPixels32(pixels);tex.Apply();
                Require(probe.RenderedSignature(renderer).OpaquePixels==0,"Fully transparent enemy pixels cast no rectangular shadow");
                for(int y=8;y<24;y++)for(int x=8;x<24;x++)if(Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))<8)pixels[y*32+x]=new Color32(255,255,255,255);
                tex.SetPixels32(pixels);tex.Apply();probe.RenderedSignature(renderer);var rendered=probe.LastRenderedPixels();
                var lower=rendered.Take(128*34).ToArray();
                Require(lower.Any(x=>x.a>0)&&lower.All(x=>x.a<=60),"Enemy silhouette shadow is visible below the body with capped opacity");
            }
            finally{Destroy(go);Destroy(sprite);Destroy(tex);Destroy(material);}
        }
    }
}
