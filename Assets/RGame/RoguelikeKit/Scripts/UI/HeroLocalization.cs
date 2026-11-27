using System.Collections.Generic;

namespace RGame.RoguelikeKit
{
    /// <summary>
    /// Runtime fallback for hero copy. The same keys are authored into the
    /// Shadowgrove Shared/en/vi tables by Tools/Art/HeroLocalizationAuthoring.cs.
    /// </summary>
    public static class HeroLocalization
    {
        public readonly struct Entry
        {
            public readonly string Key;
            public readonly string English;
            public readonly string Vietnamese;

            public Entry(string key, string english, string vietnamese)
            {
                Key = key;
                English = english;
                Vietnamese = vietnamese;
            }
        }

        private static readonly Dictionary<string, Entry> Entries = BuildEntries();

        public static IEnumerable<Entry> TableEntries => Entries.Values;

        public static string Get(string key, bool vi)
        {
            if (Entries.TryGetValue(key, out var entry)) return vi ? entry.Vietnamese : entry.English;
            return key;
        }

        public static string SelectionCopy(string heroId, bool vi) => Get("HeroSelection_" + heroId, vi);

        public static string SelectionPassiveCopy(string heroId, bool vi) =>
            Get("HeroSelection_Passive_" + heroId, vi);

        public static string SelectionSignatureCopy(string heroId, bool vi) =>
            Get("HeroSelection_Signature_" + heroId, vi);

        public static string PassiveName(StarterWeaponKind kind, bool vi) =>
            Get("HeroPassive_Name_" + HeroId(kind), vi);

        public static string PassiveShort(StarterWeaponKind kind, bool vi) =>
            Get("HeroPassive_Short_" + HeroId(kind), vi);

        public static string PassiveLong(StarterWeaponKind kind, bool vi) =>
            Get("HeroPassive_Long_" + HeroId(kind), vi);

        public static string SkillName(StarterWeaponKind kind, bool vi) =>
            Get("HeroSkill_Name_" + HeroId(kind), vi);

        public static string SkillShort(StarterWeaponKind kind, bool vi) =>
            Get("HeroSkill_Short_" + HeroId(kind), vi);

        public static string SkillLong(StarterWeaponKind kind, bool vi) =>
            Get("HeroSkill_Long_" + HeroId(kind), vi);

        public static string UpgradeTitle(StarterWeaponKind kind, int level, bool vi) =>
            Get($"HeroSkill_UpgradeTitle_{HeroId(kind)}_{level}", vi);

        public static string UpgradeDescription(StarterWeaponKind kind, int level, bool vi) =>
            Get($"HeroSkill_UpgradeDesc_{HeroId(kind)}_{level}", vi);

        public static string MaxLevel(StarterWeaponKind kind, bool vi) =>
            Get("HeroSkill_Max_" + HeroId(kind), vi);

        public static string HeroId(StarterWeaponKind kind) => kind switch
        {
            StarterWeaponKind.ThornSlash => "Rowan",
            StarterWeaponKind.DarkOrb => "Lyra",
            StarterWeaponKind.Arrow => "Kael",
            _ => ""
        };

        public static StarterWeaponKind KindForHeroId(string heroId) => heroId switch
        {
            "Rowan" => StarterWeaponKind.ThornSlash,
            "Lyra" => StarterWeaponKind.DarkOrb,
            "Kael" => StarterWeaponKind.Arrow,
            _ => StarterWeaponKind.Legacy
        };

        private static Dictionary<string, Entry> BuildEntries()
        {
            var entries = new Dictionary<string, Entry>();
            void Add(string key, string en, string vi) => entries.Add(key, new Entry(key, en, vi));

            Add("HeroSelection_Rowan", "Close-range cleave for clustered enemies. Steady recovery, but he must get close to deal damage.", "Cận chiến, quét nhiều mục tiêu phía trước. Hồi phục bền bỉ; phải áp sát để gây sát thương.");
            Add("HeroSelection_Lyra", "Mid-range caster who fires dark orbs at the nearest target. Slower than an arrow, so moving enemies can evade it.", "Pháp sư tầm trung, bắn cầu bóng đêm vào mục tiêu gần nhất. Đạn chậm hơn tên, dễ hụt mục tiêu đang di chuyển.");
            Add("HeroSelection_Kael", "Long-range archer who quickly shoots the nearest target in range. Strong reach, single-target damage; reposition when enemies close in.", "Cung thủ tầm xa, bắn tên nhanh và chuẩn vào mục tiêu gần nhất trong tầm. Sát thương đơn mục tiêu; cần di chuyển khi quái áp sát.");

            Add("HeroSelection_Passive_Rowan", "Passive · Barkheart: Recover 1 HP/sec.", "Nội tại · Tâm Vỏ Gỗ: Hồi 1 HP/giây.");
            Add("HeroSelection_Signature_Rowan", "Auto · Thorn Cleave: Short melee arc.", "Tự động · Nhát Chém Gai: Quét cận chiến.");
            Add("HeroSelection_Passive_Lyra", "Passive · Eclipse Study: Orbs travel 12% faster.", "Nội tại · Nghi Thức Nhật Thực: Cầu nhanh hơn 12%.");
            Add("HeroSelection_Signature_Lyra", "Auto · Dark Orb: Mid-range projectile.", "Tự động · Cầu Bóng Đêm: Đạn tầm trung.");
            Add("HeroSelection_Passive_Kael", "Passive · Hawkwind Step: Move 8% faster.", "Nội tại · Bước Chân Ưng: Di chuyển nhanh hơn 8%.");
            Add("HeroSelection_Signature_Kael", "Auto · Ember Arrow: Long-range arrow.", "Tự động · Tên Tàn Lửa: Tên bay tầm xa.");

            Add("HeroPassive_Name_Rowan", "Barkheart", "Tâm Vỏ Gỗ");
            Add("HeroPassive_Short_Rowan", "Recover 1 HP each second.", "Hồi 1 HP mỗi giây.");
            Add("HeroPassive_Long_Rowan", "Old-growth vitality restores 1 HP to Rowan each second. This passive is always active and has no upgrade levels.", "Sinh lực rừng già hồi cho Rowan 1 HP mỗi giây. Nội tại luôn hoạt động và không có cấp nâng.");
            Add("HeroSkill_Name_Rowan", "Thorn Cleave", "Nhát Chém Gai");
            Add("HeroSkill_Short_Rowan", "Automatically cleave the nearest enemy and nearby foes in front.", "Tự động chém kẻ địch gần nhất, quét trúng nhiều mục tiêu phía trước.");
            Add("HeroSkill_Long_Rowan", "Rowan turns toward the nearest enemy in range and sweeps a broad thorn arc. The slash only hits enemies within melee range and has a target limit.", "Rowan tự xoay về phía kẻ địch gần nhất trong tầm rồi quét một cung gai rộng. Nhát chém chỉ trúng mục tiêu trong tầm cận chiến và giới hạn số kẻ địch bị chém.");
            Add("HeroSkill_UpgradeTitle_Rowan_1", "First Briar Cut", "Nhát Gai Khởi Đầu");
            Add("HeroSkill_UpgradeDesc_Rowan_1", "Cleave in a 110° arc; hit up to 3 targets within 2.2 units.", "Chém theo cung 110°; tối đa 3 mục tiêu trong 2.2 đơn vị.");
            Add("HeroSkill_UpgradeTitle_Rowan_2", "Sharpened Briars", "Gai Tẩm Sắc");
            Add("HeroSkill_UpgradeDesc_Rowan_2", "+2 damage coefficient.", "+2 hệ số sát thương.");
            Add("HeroSkill_UpgradeTitle_Rowan_3", "Thorn Fan", "Quạt Gai");
            Add("HeroSkill_UpgradeDesc_Rowan_3", "+1 maximum target per cleave.", "+1 mục tiêu tối đa mỗi nhát chém.");
            Add("HeroSkill_UpgradeTitle_Rowan_4", "Farther Thorns", "Gai Vươn Xa");
            Add("HeroSkill_UpgradeDesc_Rowan_4", "Increase cleave range to 2.4 units.", "Tầm chém tăng lên 2.4 đơn vị.");
            Add("HeroSkill_UpgradeTitle_Rowan_5", "Rending Roots", "Rễ Xé Thịt");
            Add("HeroSkill_UpgradeDesc_Rowan_5", "+2 damage coefficient and reduce cooldown to 0.81 seconds.", "+2 hệ số sát thương và giảm hồi chiêu còn 0.81 giây.");
            Add("HeroSkill_Max_Rowan", "Maximum level reached. No evolution.", "Đã đạt cấp tối đa. Không có tiến hóa.");

            Add("HeroPassive_Name_Lyra", "Eclipse Study", "Nghi Thức Nhật Thực");
            Add("HeroPassive_Short_Lyra", "Dark Orbs travel 12% faster.", "Cầu Bóng Đêm bay nhanh hơn 12%.");
            Add("HeroPassive_Long_Lyra", "Lyra compresses the Dark Orb's path, increasing its travel speed by 12%. This passive supports her signature only and has no upgrade levels.", "Lyra nén quỹ đạo của Cầu Bóng Đêm, tăng tốc bay 12%. Nội tại chỉ hỗ trợ chiêu độc quyền và không có cấp nâng.");
            Add("HeroSkill_Name_Lyra", "Dark Orb", "Cầu Bóng Đêm");
            Add("HeroSkill_Short_Lyra", "Automatically fire a dark orb at the nearest enemy in range.", "Tự động bắn cầu bóng đêm vào kẻ địch gần nhất trong tầm.");
            Add("HeroSkill_Long_Lyra", "Lyra fires a bright-cored dark orb at the nearest enemy in mid-range. Each orb damages one target and fades; she does not fire when no valid enemy is in range.", "Lyra bắn một cầu bóng đêm có lõi sáng vào kẻ địch gần nhất trong tầm trung. Mỗi cầu gây sát thương một mục tiêu rồi tan; không bắn khi không có kẻ địch hợp lệ.");
            Add("HeroSkill_UpgradeTitle_Lyra_1", "Eclipse Orb", "Cầu Nhật Thực");
            Add("HeroSkill_UpgradeDesc_Lyra_1", "1 orb, 1 target; 7-unit range.", "1 cầu, 1 mục tiêu; tầm 7 đơn vị.");
            Add("HeroSkill_UpgradeTitle_Lyra_2", "Dense Eclipse", "Lõi Bóng Đậm");
            Add("HeroSkill_UpgradeDesc_Lyra_2", "+2 damage coefficient.", "+2 hệ số sát thương.");
            Add("HeroSkill_UpgradeTitle_Lyra_3", "Swift Umbral Core", "Lõi Bay Nhanh");
            Add("HeroSkill_UpgradeDesc_Lyra_3", "Increase orb speed from 9 to 10.8.", "Tốc độ cầu tăng từ 9 lên 10.8.");
            Add("HeroSkill_UpgradeTitle_Lyra_4", "Wider Orbit", "Quỹ Đạo Mở Rộng");
            Add("HeroSkill_UpgradeDesc_Lyra_4", "Increase range to 7.5; reduce cooldown to 1.08 seconds.", "Tầm tăng lên 7.5; hồi chiêu còn 1.08 giây.");
            Add("HeroSkill_UpgradeTitle_Lyra_5", "Twin Eclipse", "Song Nguyệt");
            Add("HeroSkill_UpgradeDesc_Lyra_5", "+1 orb toward another target; gain 1 more damage coefficient.", "+1 cầu vào mục tiêu khác; sát thương tăng thêm 1 hệ số.");
            Add("HeroSkill_Max_Lyra", "Maximum level reached. No evolution.", "Đã đạt cấp tối đa. Không có tiến hóa.");

            Add("HeroPassive_Name_Kael", "Hawkwind Step", "Bước Chân Ưng");
            Add("HeroPassive_Short_Kael", "Move 8% faster.", "Tốc độ di chuyển tăng 8%.");
            Add("HeroPassive_Long_Kael", "Kael keeps his distance with a light step, increasing his movement speed by 8%. This passive is always active and has no upgrade levels.", "Kael giữ khoảng cách bằng bước chân nhẹ, tăng tốc độ di chuyển 8%. Nội tại luôn hoạt động và không có cấp nâng.");
            Add("HeroSkill_Name_Kael", "Ember Arrow", "Tên Tàn Lửa");
            Add("HeroSkill_Short_Kael", "Automatically fire a fast arrow at the nearest enemy in long range.", "Tự động bắn tên nhanh vào kẻ địch gần nhất trong tầm xa.");
            Add("HeroSkill_Long_Kael", "Kael fires a fast ember arrow at the nearest enemy in long range. Each arrow hits one target and disappears; he does not fire outside range.", "Kael bắn tên tàn lửa tốc độ cao vào kẻ địch gần nhất trong tầm xa. Mỗi tên trúng một mục tiêu rồi biến mất; không bắn nếu ngoài tầm.");
            Add("HeroSkill_UpgradeTitle_Kael_1", "Ember Arrow", "Tên Tàn Lửa");
            Add("HeroSkill_UpgradeDesc_Kael_1", "1 arrow, 1 target; 10.5-unit range.", "1 tên, 1 mục tiêu; tầm 10.5 đơn vị.");
            Add("HeroSkill_UpgradeTitle_Kael_2", "Ash-Forged Point", "Mũi Tên Tôi Tro");
            Add("HeroSkill_UpgradeDesc_Kael_2", "+2 damage coefficient.", "+2 hệ số sát thương.");
            Add("HeroSkill_UpgradeTitle_Kael_3", "Wind-Thread Fletching", "Lông Ưng Bắt Gió");
            Add("HeroSkill_UpgradeDesc_Kael_3", "Increase arrow speed from 16 to 18.4.", "Tốc độ tên tăng từ 16 lên 18.4.");
            Add("HeroSkill_UpgradeTitle_Kael_4", "Farshot", "Tầm Bắn Xa");
            Add("HeroSkill_UpgradeDesc_Kael_4", "Increase arrow range to 12 units.", "Tầm bắn tăng lên 12 đơn vị.");
            Add("HeroSkill_UpgradeTitle_Kael_5", "Twin Embers", "Song Tên Than Hồng");
            Add("HeroSkill_UpgradeDesc_Kael_5", "+1 arrow toward another target; +2 damage coefficient; reduce cooldown to 0.78 seconds.", "+1 tên vào mục tiêu khác; +2 hệ số sát thương; hồi chiêu còn 0.78 giây.");
            Add("HeroSkill_Max_Kael", "Maximum level reached. No evolution.", "Đã đạt cấp tối đa. Không có tiến hóa.");

            return entries;
        }
    }
}
