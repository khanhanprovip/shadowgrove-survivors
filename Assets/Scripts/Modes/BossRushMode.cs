using DATN.Localization;
using RGame.RoguelikeKit;
using UnityEngine;

namespace DATN.Modes
{
    // The director owns encounters/rewards; this mode only exposes the goal.
    public sealed class BossRushMode : IGameMode
    {
        bool finished;
        public void Initialize(GameModeContext context) { }
        public void Begin() => finished = false;
        public string UpdateObjective()
        {
            var director = Object.FindFirstObjectByType<EndlessBossDirector>();
            return director == null ? LanguageManager.Get("Objective_BossRush") : director.FiniteObjective;
        }
        public bool CheckWin()
        {
            if (finished) return false;
            var director = Object.FindFirstObjectByType<EndlessBossDirector>();
            if (director == null || !director.FiniteComplete) return false;
            finished = true;
            return true;
        }
        public bool CheckLose() => false;
        public void Cleanup() => finished = false;
    }
}
