using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DATN.Trial;
using UnityEngine;
namespace RGame.RoguelikeKit
{
    public static class RunCheckpointSession
    {
        static RunCheckpoint pending;
        public static string LastError {get;private set;}
        public static bool Restoring {get;private set;}
        public static bool HasPending=>pending!=null;
        public static bool HasSavedRun=>RunCheckpointStore.TryRead(out _);
        public static bool SaveAndExit(TrialController round)
        {
            LastError=null;
            try
            {
                if(!round.IsPaused||round.IsEnded||round.PlayerStat.GetValue("HP")<=0)throw new InvalidOperationException("Only a paused active round can be saved");
                if(!round.GlobalConfig.TryBankGold())throw new InvalidOperationException("Cannot persist the earned gold");
                var state=new RunCheckpointWorld(round).Capture();
                if(!RunCheckpointStore.Save(state))throw new InvalidOperationException("Cannot write the run checkpoint");
                round.ExitToMainMenu();return true;
            }
            catch(Exception error)
            {LastError=error.Message;Debug.LogWarning("Run save failed: "+error.Message);return false;}
        }
        public static bool PrepareResume(LevelAndCharacterSO selection)
        {
            LastError=null;if(Restoring||!RunCheckpointStore.TryRead(out var state))return false;
            var hero=Resources.FindObjectsOfTypeAll<CharacterSelectConfigSO>().FirstOrDefault(x=>x.CharacterName==state.hero);
            var level=Resources.FindObjectsOfTypeAll<LevelConfigSO>().FirstOrDefault(x=>x.name==state.stage);
            if(hero==null||level==null){LastError="Saved character or stage is unavailable";return false;}
            selection.SelectCharacterSo=hero;selection.SelectLevelSO=level;pending=state;return true;
        }
        public static IEnumerator RestoreAfterSpawn()
        {
            if(pending==null)yield break;Restoring=true;LastError=null;
            var state=pending;var round=UnityEngine.Object.FindFirstObjectByType<TrialController>();
            var stack=new Stack<IEnumerator>();stack.Push(new RunCheckpointWorld(round).Restore(state));
            while(stack.Count>0)
            {
                bool more=false;object current=null;
                try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}
                catch(Exception error){LastError=error.ToString();break;}
                if(!more){stack.Pop();continue;}
                if(current is IEnumerator nested){stack.Push(nested);continue;}
                yield return current;
            }
            pending=null;Restoring=false;
            if(LastError==null&&!RunCheckpointStore.Consume(state))LastError="Cannot mark the restored checkpoint as consumed";
            if(LastError!=null)
            {
                Debug.LogWarning("Run restore failed: "+LastError);
                round.GlobalConfig.OnGameStart();round.ExitToMainMenu();
            }
        }
    }
}
