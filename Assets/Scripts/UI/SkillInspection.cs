using System.Linq;
using System.Text;
using DATN.Trial;
using DATN.Localization;
using RGame.RoguelikeKit;
namespace DATN.UI
{
    // Read-only explanations use the same values/formulas as the active casts.
    public static class SkillInspection
    {
        static string T(bool vi,string a,string b)=>vi?a:b;
        public static string Name(SkillDataSO d,bool vi)=>d.StarterWeapon!=StarterWeaponKind.Legacy?StarterWeaponProfile.Name(d.StarterWeapon,vi):
            d.SkillType==SkillType.AttributeSkill?ForestUI.AttributeName(d.AttributeType.ToString()):LanguageManager.Get("Skill_"+d.Key);
        public static string Mechanic(SkillDataSO d,bool vi)
        {
            if(d.StarterWeapon!=StarterWeaponKind.Legacy)return StarterWeaponProfile.Description(d.StarterWeapon,vi)+" "+T(vi,"Chỉ ra đòn khi có quái hợp lệ trong tầm; không tốn đạn dự trữ.","Attacks only when a valid enemy is in range; no ammunition reserve is consumed.");
            return d.Key switch
            {
                "BothSideSkill"=>T(vi,"Phóng hai lưỡi gai sang trái và phải. Lưỡi gai xuyên qua các quái trên đường bay và biến mất sau 1 giây. Hữu ích khi bị ép từ hai bên.","Launches a thorn blade left and right. Blades pierce enemies along their path and expire after one second. Useful against flanking groups."),
                "BouncingSkill"=>T(vi,"Bào tử bay theo hướng ban đầu ngẫu nhiên, bật lại khi chạm mép khung hình và gây sát thương khi chạm quái. Hết thời lượng thì biến mất; không tự đổi hướng theo quái.","Spores start in random directions, bounce off the screen edges and damage enemies on contact. They expire after their lifetime; they do not home between enemies."),
                "ChainSkill"=>T(vi,"Roi rễ tự tìm mục tiêu ở gần rồi chuyển sang mục tiêu khác khi tới nơi. Kết thúc sau một chuỗi chuyển mục tiêu hoặc khi không còn quái phù hợp. Hiệu quả khi quái tụ gần nhau.","A root lash seeks nearby enemies and switches targets as it reaches them. It ends after a short chain or when no suitable target remains. Best against clustered enemies."),
                "CircularAttackSkill"=>T(vi,"Phóng lưỡi sáng theo các góc nối tiếp quanh người. Bản tiến hóa phóng một vòng 16 lưỡi với kích thước lớn hơn.","Launches blades at successive angles around you. The evolved version releases a ring of 16 larger blades."),
                "CircularMotionSkill"=>T(vi,"Các lưỡi gai xoay quanh nhân vật, đi theo khi bạn di chuyển và gây sát thương tiếp xúc. Chúng tồn tại trong thời lượng kỹ năng.","Thorn blades orbit the survivor, follow movement and deal contact damage. They remain for the skill lifetime."),
                "FourCornerSkill"=>T(vi,"Phóng đồng thời theo bốn hướng chéo. Đạn xuyên quái trong đường bay và tồn tại 1 giây. Bản tiến hóa mở đủ tám hướng.","Fires simultaneously along four diagonals. Projectiles pierce enemies and last one second. Evolution opens all eight directions."),
                "InsideScopeSkill"=>T(vi,"Tạo vùng linh hồn đi theo người chơi. Quái đứng trong vùng nhận bốn nhịp sát thương, mỗi giây một nhịp; di chuyển để giữ quái trong vùng.","Creates a spirit field that follows you. Enemies inside receive four damage pulses, one second apart. Move to keep enemies within the field."),
                "RandomSkill"=>T(vi,"Chọn ngẫu nhiên một số quái ở gần và gây sát thương trực tiếp tại vị trí của chúng. Mỗi lần chọn các mục tiêu khác nhau trong cùng đợt; không phải đạn bay.","Chooses nearby enemies at random and deals instant damage at their positions. Targets within one cast are distinct; this is not a travelling projectile."),
                _=>LanguageManager.Get("SkillDesc_"+d.Key)
            };
        }
        public static string Details(SkillDataSO d,TrialController round,SkillManager manager,bool vi)
        {
            var text=new StringBuilder();int max=d.SkillType==SkillType.AttackSkill?d.UpgradeAttribute.Count+1:d.UpgradeAttributeModValue.Count;
            text.AppendLine(T(vi,"Cấp ","Level ")+$"{d.Level} / {max}").AppendLine();
            if(d.SkillType==SkillType.AttributeSkill)
            {
                string key=d.AttributeType.ToString();
                string purpose=key switch
                {
                    "Might"=>T(vi,"Tăng sát thương các đòn đánh được hưởng hệ số sức mạnh.","Increases damage for attacks that use the Might multiplier."),
                    "Armor"=>T(vi,"Giảm hệ số sát thương nhận vào. Hệ số càng thấp, mỗi đòn nhận càng ít máu.","Reduces the incoming damage multiplier. Lower values mean less HP lost per hit."),
                    "HPMax"=>T(vi,"Tăng lượng máu tối đa trong trận.","Increases maximum HP for this run."),
                    "Recovery"=>T(vi,"Tăng lượng máu tự hồi theo thời gian.","Increases passive HP regeneration."),
                    "Cooldown"=>T(vi,"Rút ngắn chu kỳ ra chiêu. Hệ số hồi càng thấp thì đánh càng nhanh.","Shortens attack cycles. A lower cooldown multiplier means faster attacks."),
                    "Amount"=>T(vi,"Thêm đạn hoặc mục tiêu cho những kỹ năng hỗ trợ số lượng; các kỹ năng có hướng cố định giữ số hướng riêng.","Adds projectiles or targets to supported skills. Fixed-direction skills retain their own direction counts."),
                    "Curse"=>T(vi,"Tăng nhịp xuất hiện quái; đổi lấy áp lực chiến đấu cao hơn.","Increases enemy spawn rate and combat pressure."),
                    "Area"=>T(vi,"Mở rộng quy mô vùng đánh hoặc kích thước đạn của kỹ năng hỗ trợ diện tích.","Increases area or projectile size for skills that support area scaling."),
                    "Duration"=>T(vi,"Kéo dài những kỹ năng sử dụng thời lượng. Không thay đổi các đòn có số nhịp hoặc thời gian cố định.","Extends skills that use lifetime scaling. Fixed pulse counts and fixed-duration attacks are unchanged."),
                    "SkillSpeed"=>T(vi,"Tăng tốc độ di chuyển đạn và chuyển động của những kỹ năng phù hợp.","Increases projectile or motion speed for supported skills."),
                    "MoveSpeed"=>T(vi,"Tăng tốc độ di chuyển của nhân vật để né đòn và giữ khoảng cách.","Increases survivor movement speed for dodging and spacing."),
                    "Magnet"=>T(vi,"Mở rộng phạm vi hút vật phẩm kinh nghiệm.","Extends experience pickup attraction range."),
                    "Growth"=>T(vi,"Tăng kinh nghiệm nhận được từ vật phẩm kinh nghiệm.","Increases experience gained from experience pickups."),
                    "Greed"=>T(vi,"Tăng lượng vàng nhận từ những nguồn áp dụng hệ số vàng.","Increases gold gained from sources using the gold multiplier."),_=>""
                };
                int total=d.UpgradeAttributeModValue.Take(d.Level).Sum();
                text.AppendLine(purpose).AppendLine().AppendLine(T(vi,"Tổng từ kỹ năng: ","Total from this skill: ")+total.ToString("+0;-0;0"));
                int current=key=="HPMax"?round.PlayerStat.GetMaxValue("HP"):round.PlayerStat.GetValue(key);
                text.AppendLine(T(vi,"Chỉ số hiện tại trong trận: ","Current run value: ")+current+(key=="HPMax"?" HP":key=="Recovery"?" HP/s":key=="Amount"?"":"%"));
                if(d.Level<max)text.AppendLine(T(vi,"Cấp kế tiếp: ","Next level: ")+d.UpgradeAttributeModValue[d.Level].ToString("+0;-0;0"));
                else text.AppendLine(T(vi,"Đã đạt cấp tối đa.","Maximum level reached."));
                return text.ToString();
            }
            text.AppendLine(Mechanic(d,vi)).AppendLine().AppendLine(T(vi,"<color=#D8BA73>CHỈ SỐ ĐANG DÙNG</color>","<color=#D8BA73>CURRENT VALUES</color>"));
            int damage=(int)(round.PlayerStat.GetValue("Might")*d.Damage*.01f)+1;
            float cd=d.CurrentCD!=null?d.CurrentCD.Value:d.CD*round.PlayerStat.GetValue("Cooldown")*.01f;
            text.AppendLine(T(vi,"Sát thương mỗi lần trúng: ","Damage per hit: ")+damage);
            text.AppendLine(T(vi,"Chu kỳ ra chiêu: ","Attack cycle: ")+$"{cd:0.##} s");
            int amount=d.Amount+round.PlayerStat.GetValue("Amount");
            bool slash=d.StarterWeapon==StarterWeaponKind.ThornSlash;
            if(slash)text.AppendLine(T(vi,"Số mục tiêu tối đa: ","Maximum targets: ")+(amount+2));
            else if(d.Key=="BothSideSkill")text.AppendLine(T(vi,"2 lưỡi / đợt · trái và phải","2 blades / cast · left and right"));
            else if(d.Key=="FourCornerSkill")text.AppendLine(T(vi,"Số hướng: ","Directions: ")+(d.CurrentState==SkillCurrentState.Mixed?8:4));
            else if(d.Key=="InsideScopeSkill")text.AppendLine(T(vi,"4 nhịp · 1 giây / nhịp","4 pulses · 1 second apart"));
            else text.AppendLine(T(vi,d.Key=="RandomSkill"?"Số mục tiêu tối đa: ":"Số đạn / đợt: ",d.Key=="RandomSkill"?"Maximum targets: ":"Projectiles / cast: ")+(d.Key=="CircularAttackSkill"&&d.CurrentState==SkillCurrentState.Mixed?16:amount));
            if(d.AttackRange>0)text.AppendLine(T(vi,"Tầm đánh: ","Attack range: ")+$"{d.AttackRange:0.##}");
            if(slash)text.AppendLine(T(vi,"Góc chém: ","Cleave angle: ")+$"{UnityEngine.Mathf.Clamp(55+(round.PlayerStat.GetValue("Area")+d.Area-100)*.2f,55,85)*2:0.#}°");
            if(!slash&&d.Key!="RandomSkill"&&d.Key!="InsideScopeSkill")text.AppendLine(T(vi,"Tốc độ đạn/chuyển động: ","Projectile/motion speed: ")+$"{d.Velocity*round.PlayerStat.GetValue("SkillSpeed")*.01f:0.##}");
            if(d.Key=="BouncingSkill"||d.Key=="CircularMotionSkill"||d.Key=="CircularAttackSkill")text.AppendLine(T(vi,"Thời lượng: ","Lifetime: ")+$"{d.Duration*round.PlayerStat.GetValue("Duration")*.01f:0.##} s");
            text.AppendLine(T(vi,"Quy mô vùng/đạn: ","Area/projectile scale: ")+(round.PlayerStat.GetValue("Area")+d.Area)+"%");
            var status=!slash&&d.SkillPrefab!=null?d.SkillPrefab.GetComponentInChildren<SkillBase>(true)?.StatusEffect:null;
            if(status is FreezingBuff)text.AppendLine(T(vi,"Đóng băng chuyển động: ","Movement freeze: ")+$"{status.Duration:0.##} s");
            else if(status is SlowBuff slow)text.AppendLine(T(vi,"Làm chậm: ","Slow: ")+$"{slow.SlowPercentage}% · {slow.Duration:0.##} s");
            else if(status is BurnBuff burn)text.AppendLine(T(vi,"Thiêu đốt: ","Burn: ")+$"{burn.Damage} / {burn.TickInterval:0.##} s · {burn.Duration:0.##} s");
            text.AppendLine().AppendLine(T(vi,"<color=#D8BA73>NÂNG CẤP & TIẾN HÓA</color>","<color=#D8BA73>UPGRADE & EVOLUTION</color>"));
            if(d.Level>0&&d.Level<=d.UpgradeAttribute.Count)
            {
                var next=d.UpgradeAttribute[d.Level-1];
                string change=next.skillAttributeType switch
                {
                    UpgradeSkillAttributeType.DamageAdd=>T(vi,"Sát thương cơ bản +","Base damage +")+next.IntValue,
                    UpgradeSkillAttributeType.CDLessPercent=>T(vi,"Hồi chiêu cơ bản -","Base cooldown -")+next.IntValue+"%",
                    UpgradeSkillAttributeType.ProjectilesCountAdd=>T(vi,"Số lượng +","Amount +")+next.IntValue,
                    UpgradeSkillAttributeType.VelocityAddPercent=>T(vi,"Tốc độ đạn +","Projectile speed +")+next.IntValue+"%",
                    _=>T(vi,"Diện tích +","Area +")+next.IntValue+"%"
                };
                text.AppendLine(T(vi,"Cấp kế tiếp: ","Next level: ")+change);
            }
            if(d.CurrentState==SkillCurrentState.Mixed)text.AppendLine(T(vi,"Đã tiến hóa; chỉ số trên đã gồm cường hóa tiến hóa.","Evolved; the values above already include evolution bonuses."));
            else if(!string.IsNullOrEmpty(d.MixAttributeSkillKey))
            {
                bool owned=manager.OwnedSkills.Any(s=>s.Key==d.MixAttributeSkillKey);
                text.AppendLine(T(vi,$"Cần kỹ năng tấn công cấp {max} và sở hữu ",$"Requires attack level {max} and ownership of ")+ForestUI.AttributeName(d.MixAttributeSkillKey)+T(vi,owned?" (đã có).":" (chưa có).",owned?" (owned).":" (not owned)."));
                text.AppendLine(T(vi,"Sau khi đủ điều kiện, chọn kỹ năng thêm một lần khi lên cấp hoặc mở rương.","Once eligible, select this skill again on level-up or from a chest."));
            }
            text.AppendLine().Append(T(vi,"Chỉ số đã tính các thuộc tính hiện tại của nhân vật.","Values include the survivor's current run attributes."));
            return text.ToString();
        }
    }
}
