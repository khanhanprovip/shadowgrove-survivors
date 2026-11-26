using System.Collections.Generic;
using System.Linq;
using DATN.Localization;
using DATN.Trial;
using RGame.RoguelikeKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace DATN.UI
{
    public sealed class PauseSkillInventory : MonoBehaviour
    {
        RectTransform content,detailContent;
        TMP_Text detailTitle,detailText;
        ScrollRect detailScroll;
        readonly Dictionary<string,Button> buttons=new();
        string language,selectedKey;
        bool built,wasPaused;
        public int DisplayedSkills { get; private set; }
        public string SelectedSkillKey=>selectedKey;
        public string DisplayedDetail=>detailText!=null?detailText.text:"";
        void OnEnable(){built=false;}
        void Update()
        {
            var round=FindFirstObjectByType<TrialController>();
            if(round==null||!round.IsPaused){wasPaused=false;return;}
            if(!wasPaused){built=false;wasPaused=true;}
            string code=LanguageManager.GetCurrentCode();
            if(!built||language!=code){language=code;Rebuild();built=true;}
        }
        ScrollRect Scroll(string name,float left,float right,out RectTransform rows,float top=0.98f)
        {
            var root=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(ScrollRect));root.transform.SetParent(transform,false);
            ForestUI.Place(root.transform,left,.16f,right,.77f);root.GetComponent<UnityEngine.UI.Image>().color=new Color(.01f,.04f,.05f,.96f);
            var viewport=new GameObject("Viewport",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(RectMask2D));viewport.transform.SetParent(root.transform,false);
            ForestUI.Place(viewport.transform,.02f,.02f,.98f,top);viewport.GetComponent<UnityEngine.UI.Image>().color=Color.clear;
            var child=new GameObject("Content",typeof(RectTransform));child.transform.SetParent(viewport.transform,false);rows=child.GetComponent<RectTransform>();
            rows.anchorMin=new Vector2(0,1);rows.anchorMax=Vector2.one;rows.pivot=new Vector2(.5f,1);rows.offsetMin=rows.offsetMax=Vector2.zero;
            var scroll=root.GetComponent<ScrollRect>();scroll.viewport=viewport.GetComponent<RectTransform>();scroll.content=rows;scroll.horizontal=false;
            scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=42;return scroll;
        }
        void EnsureView()
        {
            if(content!=null)return;
            Scroll("Owned skill scroll",.34f,.57f,out content);
            detailScroll=Scroll("Skill detail scroll",.58f,.93f,out detailContent,.83f);
            detailTitle=ForestUI.Label(detailScroll.transform,"Selected skill name","","",30,.035f,.85f,.965f,.98f,color:ForestUI.Gold);
            detailTitle.GetComponent<ForestText>().enabled=false;
            detailText=ForestUI.Label(detailContent,"Detailed description","","",24,0,0,1,1,TextAlignmentOptions.TopLeft);
            detailText.GetComponent<ForestText>().enabled=false;detailText.enableAutoSizing=false;detailText.enableWordWrapping=true;
            detailText.overflowMode=TextOverflowModes.Overflow;
            var rect=detailText.rectTransform;rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-12);
        }
        void Rebuild()
        {
            EnsureView();
            foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}buttons.Clear();
            var manager=FindFirstObjectByType<SkillManager>();if(manager==null)return;
            bool vi=language=="vi";float y=8;DisplayedSkills=0;
            var owned=manager.OwnedSkills.ToArray();
            if(!owned.Any(d=>d.Key==selectedKey))selectedKey=owned.FirstOrDefault()?.Key;
            foreach(var type in new[]{SkillType.AttackSkill,SkillType.AttributeSkill})
            {
                RowLabel(type==SkillType.AttackSkill?(vi?"TẤN CÔNG":"ATTACK"):(vi?"THUỘC TÍNH":"PASSIVES"),y,34,23,ForestUI.Gold);y+=42;
                foreach(var data in owned.Where(s=>s.SkillType==type))
                {
                    DisplayedSkills++;
                    var row=new GameObject("Owned "+data.Key,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));row.transform.SetParent(content,false);
                    var rt=row.GetComponent<RectTransform>();rt.anchorMin=new Vector2(0,1);rt.anchorMax=new Vector2(1,1);rt.pivot=new Vector2(.5f,1);rt.sizeDelta=new Vector2(-12,108);rt.anchoredPosition=new Vector2(0,-y);
                    var button=row.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=row.GetComponent<UnityEngine.UI.Image>();
                    string key=data.Key;button.onClick.AddListener(()=>{ShadowgroveAudio.Play(SoundCue.UiSelect);SelectSkill(key);});buttons[key]=button;
                    var icon=new GameObject("Icon",typeof(RectTransform),typeof(UnityEngine.UI.Image));icon.transform.SetParent(row.transform,false);
                    ForestUI.Place(icon.transform,.03f,.18f,.23f,.84f);icon.GetComponent<UnityEngine.UI.Image>().sprite=data.SkillIcon;icon.GetComponent<UnityEngine.UI.Image>().preserveAspect=true;icon.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
                    string name=SkillInspection.Name(data,vi);ForestUI.Label(row.transform,"Name",name,name,23,.28f,.34f,.96f,.94f,color:ForestUI.Gold);
                    int max=type==SkillType.AttackSkill?data.UpgradeAttribute.Count+1:data.UpgradeAttributeModValue.Count;
                    string level=$"{(vi?"Cấp":"Level")} {data.Level}/{max}";
                    ForestUI.Label(row.transform,"Stats",level,level,21,.28f,.06f,.96f,.32f);
                    y+=118;
                }
            }
            content.sizeDelta=new Vector2(0,Mathf.Max(y+8,content.parent.GetComponent<RectTransform>().rect.height));content.anchoredPosition=Vector2.zero;
            Canvas.ForceUpdateCanvases();var scroll=content.GetComponentInParent<ScrollRect>();scroll.Rebuild(CanvasUpdate.PostLayout);scroll.verticalNormalizedPosition=1;
            SelectSkill(selectedKey);
        }
        public void SelectSkill(string key)
        {
            var manager=FindFirstObjectByType<SkillManager>();var round=FindFirstObjectByType<TrialController>();
            var data=manager?.OwnedSkills.FirstOrDefault(d=>d.Key==key);if(data==null||round==null)return;
            selectedKey=key;
            foreach(var entry in buttons)
            {
                var colors=entry.Value.colors;colors.normalColor=entry.Key==key?new Color(.24f,.27f,.16f):new Color(.08f,.15f,.16f);
                colors.highlightedColor=new Color(.28f,.34f,.23f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.36f,.32f,.16f);entry.Value.colors=colors;
            }
            bool vi=language=="vi";detailTitle.text=SkillInspection.Name(data,vi);detailText.text=SkillInspection.Details(data,round,manager,vi);
            Canvas.ForceUpdateCanvases();
            float width=detailContent.parent.GetComponent<RectTransform>().rect.width-24;
            float height=detailText.GetPreferredValues(detailText.text,Mathf.Max(50,width),Mathf.Infinity).y+8;
            detailText.rectTransform.sizeDelta=new Vector2(-24,height);
            detailContent.sizeDelta=new Vector2(0,Mathf.Max(height+24,detailContent.parent.GetComponent<RectTransform>().rect.height));
            detailContent.anchoredPosition=Vector2.zero;detailScroll.Rebuild(CanvasUpdate.PostLayout);detailScroll.verticalNormalizedPosition=1;
        }
        void RowLabel(string value,float y,float height,float size,Color color)
        {
            var go=new GameObject("Section",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(content,false);
            var rt=go.GetComponent<RectTransform>();rt.anchorMin=new Vector2(0,1);rt.anchorMax=Vector2.one;rt.pivot=new Vector2(.5f,1);rt.sizeDelta=new Vector2(-20,height);rt.anchoredPosition=new Vector2(0,-y);
            var text=go.GetComponent<TMP_Text>();text.text=value;text.fontSize=size;text.color=color;text.raycastTarget=false;
        }
    }
}
