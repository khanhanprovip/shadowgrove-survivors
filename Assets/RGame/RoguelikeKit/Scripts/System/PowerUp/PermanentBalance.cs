using RGame.CommonStat;

namespace RGame.RoguelikeKit
{
    // Save files continue storing purchased levels, never recalculated currency.
    public static class PermanentBalance
    {
        public static readonly string[] Keys = { "Might", "HPMax", "Recovery", "Armor", "MoveSpeed", "SkillSpeed", "Amount", "Cooldown", "Area", "Duration", "Magnet", "Curse", "Growth", "Greed" };
        public static int Step(string key) => key switch
        {
            "Might" => 4, "HPMax" => 8, "Recovery" => 1, "Armor" => -5,
            "MoveSpeed" => 4, "SkillSpeed" => 5, "Amount" => 1, "Cooldown" => 4,
            "Area" => 6, "Duration" => 5, "Magnet" => 15, "Curse" => 5,
            "Growth" => 4, "Greed" => 5, _ => 0
        };
        public static int Cost(string key, int level) => key == "Amount" ? 1000 :
            level switch { 0 => 100, 1 => 180, 2 => 320, 3 => 520, _ => 800 + (level - 4) * 300 };
        public static void Apply(AttributeSO owned, CommonStatRuntimeSO stats)
        {
            foreach (var key in Keys)
            {
                stats.ResetValue(key);
                stats.ModifyValue(key, owned.GetAttribute(key) * Step(key));
            }
        }
        public static string Description(string key, bool vi)
        {
            int step = System.Math.Abs(Step(key));
            if (key == "HPMax") return vi ? $"Mỗi bậc cộng {step} máu tối đa." : $"Each rank adds {step} maximum HP.";
            if (key == "Recovery") return vi ? "Mỗi bậc hồi thêm 1 HP mỗi giây." : "Each rank regenerates 1 extra HP per second.";
            if (key == "Amount") return vi ? "Thêm 1 đạn hoặc 1 mục tiêu chém cho kỹ năng phù hợp." : "Adds 1 projectile or cleave target to supported skills.";
            if (key == "Curse") return vi ? $"Mỗi bậc tăng {step}% tần suất sinh quái." : $"Each rank increases enemy spawn rate by {step}%.";
            if (key == "Armor") return vi ? $"Mỗi bậc giảm {step}% sát thương nhận vào." : $"Each rank reduces incoming damage by {step}%.";
            if (key == "Cooldown") return vi ? $"Mỗi bậc giảm {step}% thời gian hồi cơ bản." : $"Each rank reduces base cooldown by {step}%.";
            return vi ? $"Mỗi bậc tăng {step}% {DATN.UI.ForestUI.AttributeName(key).ToLowerInvariant()}." : $"Each rank increases {key} by {step}%.";
        }
    }
}
