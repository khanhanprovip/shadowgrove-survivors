using UnityEngine;

namespace DATN.Modes
{
    /// <summary>
    ///     Vong doi che do choi (Moc 4): tach logic thang/thua/muc tieu khoi
    ///     TrialController. Moi man tham chieu mot GameModeSO trong LevelConfigSO;
    ///     TrialController chi lam "host" (HUD, pause, restart) va uoc luong
    ///     phuong thuc cua mode theo dung vai vong.
    /// </summary>
    public interface IGameMode
    {
        /// <summary>Khoi tao voi du lieu can thiet cho che do nay.</summary>
        void Initialize(GameModeContext context);

        /// <summary>Bat dau mot luot choi moi (reset bo dem, muc tieu).</summary>
        void Begin();

        /// <summary>Cap nhat moi frame boi host. Tra ve chuoi muc tieu da choi dich cho HUD.</summary>
        string UpdateObjective();

        /// <summary>Kiem tra dieu kien thang (goi boi host moi frame).</summary>
        bool CheckWin();

        /// <summary>Kiem tra dieu kien thua rieng cua mode (player chet da do host xu ly).</summary>
        bool CheckLose();

        /// <summary>Don dep khi roi khoi man / restart.</summary>
        void Cleanup();
    }

    /// <summary>Cac du lieu chung host cung cap cho mode.</summary>
    public class GameModeContext
    {
        public GameModeSO Config;
        public RGame.RoguelikeKit.GlobalConfigSO GlobalConfig;
        public RGame.CommonStat.CommonStatRuntimeSO PlayerStat;
    }
}
