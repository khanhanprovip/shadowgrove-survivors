using UnityEngine;

namespace DATN.Modes
{
    public enum GameModeId
    {
        Survival = 0,   // Song sot dung thoi luong
        KillTarget = 1, // Ha du so quai
        Endless = 2,    // Vo tan, kho tang dan
        BossRush = 3,
    }

    /// <summary>
    ///     Cau hinh che do choi (ScriptableObject). Moi man tham chieu mot asset
    ///     nay qua LevelConfigSO.GameMode — du lieu ly, khong viet cung code.
    /// </summary>
    [CreateAssetMenu(fileName = "GameMode_", menuName = "DATN/Shadowgrove/Game Mode")]
    public class GameModeSO : ScriptableObject
    {
        [Tooltip("Loai che do — quyet dinh lop logic duoc tao boi GameModeFactory.")]
        public GameModeId ModeId = GameModeId.Survival;

        [Tooltip("Thoi luong man (giay) — dung cho Survival.")]
        public float Duration = 90f;

        [Tooltip("So quai can ha — dung cho KillTarget.")]
        public int TargetKills = 30;

        [Tooltip("He so kho tang moi phut — dung cho Endless.")]
        public float DifficultyPerMinute = 0.1f;

        [Tooltip("Survival end boss: -1 means no boss; otherwise BossCatalog index 0..4.")]
        public int FinalBossKind = -1;
        [Tooltip("0 means an infinite Boss Rush.")]
        public int BossRushCount = 0;
        public float BossBreakSeconds = 5f;
        public int BossRushStartingChoices = 10;
        public bool BossRushStartingChoicesGrantLevels = true;

        /// <summary>Tao instance logic tuong ung. Tap trung o mot cho de tim.</summary>
        public IGameMode CreateInstance()
        {
            switch (ModeId)
            {
                case GameModeId.KillTarget: return new KillTargetMode();
                case GameModeId.Endless: return new EndlessMode();
                case GameModeId.BossRush: return new BossRushMode();
                default: return new SurvivalMode();
            }
        }
    }
}
