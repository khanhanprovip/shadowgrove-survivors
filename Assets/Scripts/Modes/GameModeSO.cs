using UnityEngine;

namespace DATN.Modes
{
    public enum GameModeId
    {
        Survival = 0,   // Song sot dung thoi luong
        KillTarget = 1, // Ha du so quai
        Endless = 2,    // Vo tan, kho tang dan
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

        /// <summary>Tao instance logic tuong ung. Tap trung o mot cho de tim.</summary>
        public IGameMode CreateInstance()
        {
            switch (ModeId)
            {
                case GameModeId.KillTarget: return new KillTargetMode();
                case GameModeId.Endless: return new EndlessMode();
                default: return new SurvivalMode();
            }
        }
    }
}
