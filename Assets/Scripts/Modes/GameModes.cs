using DATN.Localization;
using UnityEngine;

namespace DATN.Modes
{
    /// <summary>
    ///     Che do co dien: song sot dung Duration giay thi thang.
    ///     (Chinh la logic 90 giay cua Trial Forest, tach ra khoi TrialController.)
    /// </summary>
    public class SurvivalMode : IGameMode
    {
        private GameModeContext _ctx;
        private double _remaining;
        private bool _finished;

        public float Remaining => Mathf.Max(0f, (float)_remaining);

        public void Initialize(GameModeContext context)
        {
            _ctx = context;
        }

        public void Begin()
        {
            _remaining = _ctx.Config != null ? _ctx.Config.Duration : 90f;
            _finished = false;
        }

        public string UpdateObjective()
        {
            if (_finished) return null;
            _remaining -= Time.deltaTime;
            return LanguageManager.GetFormatted("Objective_Survive", Mathf.CeilToInt(Remaining));
        }

        public bool CheckWin()
        {
            if (_finished || _remaining > 0f) return false;
            _finished = true;
            return true;
        }

        public bool CheckLose() => false; // het mau do host xu ly

        public void Cleanup()
        {
            _finished = false;
        }
    }

    /// <summary>
    ///     Che do ha quai: thang khi tong kill dat TargetKills.
    /// </summary>
    public class KillTargetMode : IGameMode
    {
        private GameModeContext _ctx;
        private bool _finished;

        public void Initialize(GameModeContext context) => _ctx = context;

        public void Begin() => _finished = false;

        public string UpdateObjective()
        {
            int target = _ctx.Config != null ? _ctx.Config.TargetKills : 30;
            int kills = _ctx.GlobalConfig != null ? _ctx.GlobalConfig.CurrentGetKill : 0;
            return LanguageManager.GetFormatted("Objective_Kills", kills, target);
        }

        public bool CheckWin()
        {
            if (_finished || _ctx.GlobalConfig == null || _ctx.Config == null) return false;
            if (_ctx.GlobalConfig.CurrentGetKill < _ctx.Config.TargetKills) return false;
            _finished = true;
            return true;
        }

        public bool CheckLose() => false;

        public void Cleanup() => _finished = false;
    }

    /// <summary>
    ///     Che do vo tan: khong co thang — dem thoi gian song sot tang dan va
    ///     tang chi so "Curse" (he so kho cua kit) theo phut choi.
    /// </summary>
    public class EndlessMode : IGameMode
    {
        private GameModeContext _ctx;
        private float _elapsed;
        private float _nextCurseAt = 60f;
        private int _curseSteps;

        public void Initialize(GameModeContext context) => _ctx = context;

        public void Begin()
        {
            _elapsed = 0f;
            _nextCurseAt = 60f;
            _curseSteps = 0;
        }

        public string UpdateObjective()
        {
            _elapsed += Time.deltaTime;

            // Cang choi lau cang kho: moi phut tang Curse (kit nhan trong StageManager)
            if (_elapsed >= _nextCurseAt && _ctx.PlayerStat != null && _ctx.Config != null)
            {
                _curseSteps++;
                _nextCurseAt += 60f;
                int add = Mathf.Max(1, Mathf.RoundToInt(_ctx.Config.DifficultyPerMinute * 100f));
                _ctx.PlayerStat.ModifyValue("Curse", add);
            }

            int minutes = (int)(_elapsed / 60f);
            int seconds = (int)(_elapsed % 60f);
            return $"{minutes:00}:{seconds:00}";
        }

        public bool CheckWin() => false; // vo tan: chi ket thuc khi chet

        public bool CheckLose() => false;

        public void Cleanup() { }
    }
}
