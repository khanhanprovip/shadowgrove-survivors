using UnityEngine;
namespace RGame.RoguelikeKit
{
    public static class BossCatalog
    {
        public static readonly string[] Ids = { "EclipseWraith", "ThornRegent", "FrostMourner", "CrimsonHarvester", "AbyssOracle" };
        static readonly string[] Vi = { "Ảnh Hồn Nhật Thực", "Cổ Vương Gai", "Ai Nữ Băng Sương", "Huyết Nguyệt Đao Phủ", "Tiên Tri Vực Thẳm" };
        static readonly string[] En = { "Eclipse Wraith", "Thorn Regent", "Frost Mourner", "Crimson Harvester", "Abyss Oracle" };
        public static readonly Color[] Colors = { new(.48f,1f,.72f), new(.96f,.79f,.39f), new(.52f,.91f,1f), new(1f,.42f,.46f), new(.82f,.57f,1f) };
        static readonly string[,] SkillsVi = {
            { "Quạt Hồn", "Lưỡi Liềm Nhật Thực", "Ấn Bóng Đêm", "Bước Qua Hư Ảnh", "Vệ Tinh Linh Hồn" },
            { "Gai Trồi", "Rễ Truy Đuổi", "Lồng Gai", "Thập Tự Cổ Mộc", "Sóng Rễ" },
            { "Vòng Băng Khuyết", "Sao Băng Giá", "Thập Tự Băng", "Quạt Tuyết", "Vành Sương" },
            { "Xung Phong Huyết", "Trảm Đao", "Ba Vết Chém", "Bổ Từ Trên Cao", "Rìu Hồi Quy" },
            { "Nhãn Quang", "Ấn Vực Sâu", "Xoắn Hư Không", "Vòng Tiên Tri", "Lưới Định Mệnh" }
        };
        static readonly string[,] SkillsEn = {
            { "Soul Fan", "Eclipse Crescent", "Shadow Seals", "Phantom Step", "Soul Satellites" },
            { "Thorn Eruption", "Hunting Roots", "Thorn Cage", "Elder Cross", "Root Wave" },
            { "Broken Ice Ring", "Frost Comets", "Ice Cross", "Snow Fan", "Frost Halo" },
            { "Blood Charge", "Execution Cleave", "Triple Slash", "Sky Execution", "Returning Axe" },
            { "Eye Beam", "Abyss Seals", "Void Spiral", "Oracle Ring", "Fate Grid" }
        };
        public static string Name(int kind, bool vi) => (vi ? Vi : En)[kind];
        public static string Skill(int kind, int index, bool vi) => (vi ? SkillsVi : SkillsEn)[kind,index];
    }
}
