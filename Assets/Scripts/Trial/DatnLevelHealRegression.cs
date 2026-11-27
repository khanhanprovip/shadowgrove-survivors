using System.Collections;
using RGame.RoguelikeKit;
using UnityEngine;

namespace DATN.Trial
{
    public sealed partial class DatnRegression
    {
        IEnumerator CheckLevelHeal()
        {
            yield return EnterRound(0,"Rowan");
            var manager=FindFirstObjectByType<LevelManager>();
            var stats=_round.PlayerStat;
            var xp=(ExpConfig)typeof(LevelManager).GetField("expConfig",Private).GetValue(manager);
            int level=stats.GetValue("Level");
            stats.ModifyValue("HP",1-stats.GetValue("HP"));
            int cost=Mathf.Max(1,xp.GetExperienceForLevel(level));
            stats.ModifyValue("Exp",cost-stats.GetValue("Exp"));
            Require(stats.GetValue("Level")==level+1&&stats.GetValue("HP")==stats.GetMaxValue("HP"),
                "A normal EXP level immediately restores full HP");
            while(Time.timeScale<=0){PickUpgradeOrTreasure();yield return null;}
            stats.ModifyValue("HP",1-stats.GetValue("HP"));
            int before=stats.GetValue("Level"),exp=stats.GetValue("Exp");manager.GrantLevels(5);
            Require(stats.GetValue("Level")==before+5&&stats.GetValue("HP")==stats.GetMaxValue("HP")&&stats.GetValue("Exp")==exp,
                "Five direct reward levels fully heal and preserve EXP");
            while(Time.timeScale<=0||manager.PendingChoices>0){if(Time.timeScale<=0)PickUpgradeOrTreasure();yield return null;}
            _round.Pause();stats.ModifyValue("HP",1-stats.GetValue("HP"));
            yield return RoundTripCurrentRun("level-heal-save");
            Require(_round.PlayerStat.GetValue("HP")==1,"Restoring a saved level does not heal the player");
            _round.Resume();
            stats=_round.PlayerStat;manager=FindFirstObjectByType<LevelManager>();
            stats.ModifyValue("HP",-stats.GetValue("HP"));
            manager.GrantLevels(1);
            Require(stats.GetValue("HP")==0,"A direct level reward cannot resurrect a dead player");
        }
    }
}
