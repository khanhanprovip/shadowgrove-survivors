using System.Linq;
using DATN.Localization;
using DATN.Trial;
using RGame.RoguelikeKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace DATN.UI
{
    // Read-only snapshot: never calls UpgradeSkill, resets stats or changes the game clock.
    public sealed class PauseSkillInventory : MonoBehaviour
    {
        RectTransform content;
        string language;
        bool built;
        bool wasPaused;
        public int DisplayedSkills { get; private set; }
        void OnEnable(){built=false;}
        void Update()
        {
            var round=FindFirstObjectByType<TrialController>();
            if(round==null||!round.IsPaused){wasPaused=false;return;}
            if(!wasPaused){built=false;wasPaused=true;}
            string code=LanguageManager.GetCurrentCode();
            if(!built||language!=code){language=code;Rebuild();built=true;}
        }
        void Rebuild()
        {
            if(content==null)
            {
                var root=new GameObject("Owned skill scroll",typeof(RectTransform),typeof(Image),typeof(ScrollRect));root.transform.SetParent(transform,false);
                ForestUI.Place(root.transform,.34f,.16f,.93f,.77f);root.GetComponent<Image>().color=new Color(.01f,.04f,.05f,.94f);
                var viewport=new GameObject("Viewport",typeof(RectTransform),typeof(Image),typeof(RectMask2D));viewport.transform.SetParent(root.transform,false);
                ForestUI.Place(viewport.transform,.015f,.02f,.985f,.98f);viewport.GetComponent<Image>().color=Color.clear;
                var go=new GameObject("Content",typeof(RectTransform));go.transform.SetParent(viewport.transform,false);content=go.GetComponent<RectTransform>();
                content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.offsetMin=content.offsetMax=Vector2.zero;
                var scroll=root.GetComponent<ScrollRect>();scroll.viewport=viewport.GetComponent<RectTransform>();scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=42;
            }
            foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var manager=FindFirstObjectByType<SkillManager>();var round=FindFirstObjectByType<TrialController>();
            if(manager==null||round==null)return;
            bool vi=language=="vi";float y=8;DisplayedSkills=0;
            foreach(var type in new[]{SkillType.AttackSkill,SkillType.AttributeSkill})
            {
                RowLabel(type==SkillType.AttackSkill?(vi?"KỸ NĂNG TẤN CÔNG":"ATTACK SKILLS"):(vi?"THUỘC TÍNH":"PASSIVE SKILLS"),y,36,25,ForestUI.Gold);y+=44;
                foreach(var data in manager.OwnedSkills.Where(s=>s.SkillType==type))
                {
                    DisplayedSkills++;
                    var row=new GameObject("Owned "+data.Key,typeof(RectTransform),typeof(Image));row.transform.SetParent(content,false);
                    var rt=row.GetComponent<RectTransform>();rt.anchorMin=new Vector2(0,1);rt.anchorMax=new Vector2(1,1);rt.pivot=new Vector2(.5f,1);rt.sizeDelta=new Vector2(-16,126);rt.anchoredPosition=new Vector2(0,-y);
                    row.GetComponent<Image>().color=new Color(.08f,.15f,.16f,.9f);row.GetComponent<Image>().raycastTarget=false;
                    var icon=new GameObject("Icon",typeof(RectTransform),typeof(Image));icon.transform.SetParent(row.transform,false);
                    ForestUI.Place(icon.transform,.02f,.25f,.13f,.86f);icon.GetComponent<Image>().sprite=data.SkillIcon;icon.GetComponent<Image>().preserveAspect=true;icon.GetComponent<Image>().raycastTarget=false;
                    string name=data.StarterWeapon!=StarterWeaponKind.Legacy?StarterWeaponProfile.Name(data.StarterWeapon,vi):type==SkillType.AttributeSkill?ForestUI.AttributeName(data.AttributeType.ToString()):LanguageManager.Get("Skill_"+data.Key);
                    ForestUI.Label(row.transform,"Name",name,name,25,.16f,.68f,.98f,.98f,color:ForestUI.Gold);
                    string info=$"{(vi?"Cấp":"Level")} {data.Level}";
                    if(type==SkillType.AttackSkill)
                    {
                        int damage=(int)(round.PlayerStat.GetValue("Might")*data.Damage*.01f)+1;
                        float cd=data.CD*round.PlayerStat.GetValue("Cooldown")*.01f;
                        info+=$"  ·  {(vi?"Sát thương":"Damage")}: {damage}  ·  {(vi?"Hồi":"CD")}: {cd:0.##} s";
                        bool cleave=data.StarterWeapon==StarterWeaponKind.ThornSlash;
                        string countLabel=cleave?(vi?"Mục tiêu":"Targets"):(vi?"Số đạn":"Projectiles");
                        info+=$"\n{countLabel}: {data.Amount+round.PlayerStat.GetValue("Amount")+(cleave?2:0)}";
                        if(data.AttackRange>0)info+=$"  ·  {(vi?"Tầm":"Range")}: {data.AttackRange:0.#}";
                        else info+=$"  ·  {(vi?"Thời lượng":"Duration")}: {data.Duration:0.#} s";
                    }
                    else
                    {
                        int total=data.UpgradeAttributeModValue.Take(data.Level).Sum();
                        info+=$"  ·  {(vi?"Tổng cộng từ kỹ năng":"Total from this skill")}: {total:+0;-0;0}";
                        info+="\n"+(vi?"Tác dụng được cộng vào chỉ số trong trận.":"Applied to your current run stats.");
                    }
                    ForestUI.Label(row.transform,"Stats",info,info,22,.16f,.05f,.98f,.66f);
                    y+=136;
                }
            }
            content.sizeDelta=new Vector2(0,Mathf.Max(y+8,content.parent.GetComponent<RectTransform>().rect.height));
            content.anchoredPosition=Vector2.zero;
            Canvas.ForceUpdateCanvases();
            var rect=content.GetComponentInParent<ScrollRect>();rect.Rebuild(CanvasUpdate.PostLayout);rect.verticalNormalizedPosition=1;
        }
        void RowLabel(string value,float y,float height,float size,Color color)
        {
            var go=new GameObject("Section",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(content,false);
            var rt=go.GetComponent<RectTransform>();rt.anchorMin=new Vector2(0,1);rt.anchorMax=Vector2.one;rt.pivot=new Vector2(.5f,1);rt.sizeDelta=new Vector2(-20,height);rt.anchoredPosition=new Vector2(0,-y);
            var text=go.GetComponent<TMP_Text>();text.text=value;text.fontSize=size;text.color=color;text.raycastTarget=false;
        }
    }
}
