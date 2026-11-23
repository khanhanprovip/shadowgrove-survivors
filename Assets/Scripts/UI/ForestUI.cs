using System;
using System.Collections.Generic;
using System.Linq;
using DATN.Localization;
using DATN.Trial;
using RGame.RoguelikeKit;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace DATN.UI
{
    // Presentation only: retain the kit's serialized references and callbacks.
    public static class ForestUI
    {
        // Jade Eclipse: ivory type, jade focus, obsidian surfaces.
        public static readonly Color Ink = new(.929f,.945f,.902f), Gold = new(.549f,.827f,.741f), Muted = new(.643f,.710f,.737f), Dark = new(.027f,.082f,.102f);
        private static readonly Dictionary<string,Sprite> Cache = new();
        public static readonly string[] LevelKeys = { "TrialLevel","LV2_MoonlitRuins","LV3_SunkenMarsh","LV4_AshenQuarry","LV5_FrostHollow","LV6_CursedVillage","LV7_ClockworkCitadel","LV8_CrimsonCathedral","LV9_EndlessAbyss","LV10_EclipseThrone" };
        public static string Tr(string vi,string en) => LanguageManager.GetCurrentCode()=="vi"?vi:en;
        public static Sprite Art(string key)
        {
            if(string.IsNullOrEmpty(key)) return null;
            if(Cache.TryGetValue(key,out var s))return s;
            s=Resources.Load<Sprite>("SGJade/"+key);
            if(s!=null){Cache.Add(key,s);return s;}
            var t=Resources.Load<Texture2D>("SGForest/"+key); if(t==null)return null;
            s=Sprite.Create(t,new Rect(0,0,t.width,t.height),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);s.name="SGForest_"+key;Cache.Add(key,s);return s;
        }
        public static Sprite LevelArt(string key)=>Art("Level"+Mathf.Max(0,Array.IndexOf(LevelKeys,key)));
        private static Sprite White
        {
            get { if(!Cache.TryGetValue("White",out var s)){s=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);Cache["White"]=s;}return s; }
        }
        public static Sprite HeroArt(string hero)
        {
            var jade=Resources.Load<Sprite>("SGJade/"+hero);if(jade!=null)return jade;
            string key="Portrait_"+hero;if(Cache.TryGetValue(key,out var sprite))return sprite;
            var texture=Resources.Load<Texture2D>("SGForest/"+hero);if(texture==null)return null;
            texture.filterMode=FilterMode.Point;
            sprite=Sprite.Create(texture,new Rect(0,texture.height-96,96,96),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);Cache[key]=sprite;return sprite;
        }
        public static void Place(Transform t,float x0,float y0,float x1,float y1)
        {
            if(t is not RectTransform r)return;
            r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=r.offsetMax=Vector2.zero;r.pivot=Vector2.one*.5f;r.localScale=Vector3.one;
        }
        public static void StopLayout(Transform t)
        {
            foreach(var l in t.GetComponents<LayoutGroup>())l.enabled=false;
            foreach(var f in t.GetComponents<ContentSizeFitter>())f.enabled=false;
        }
        public static Image Picture(Transform p,string name,string key,float x0,float y0,float x1,float y1,bool aspect=false)
        {
            var t=p.Find(name);if(t==null){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(p,false);t=go.transform;}
            var i=t.GetComponent<Image>();i.sprite=Art(key);i.color=Color.white;i.type=i.sprite!=null&&i.sprite.border.sqrMagnitude>0?Image.Type.Sliced:Image.Type.Simple;i.preserveAspect=aspect;i.raycastTarget=false;Place(t,x0,y0,x1,y1);return i;
        }
        public static TMP_Text Label(Transform p,string name,string vi,string en,float size,float x0,float y0,float x1,float y1,TextAlignmentOptions align=TextAlignmentOptions.MidlineLeft,Color? color=null)
        {
            var t=p.Find(name);if(t==null){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));go.transform.SetParent(p,false);t=go.transform;}
            var l=t.GetComponent<TMP_Text>();l.fontSize=size;l.enableAutoSizing=true;l.fontSizeMin=size*.72f;l.fontSizeMax=size;l.color=color??Ink;l.alignment=align;l.raycastTarget=false;l.text=Tr(vi,en);
            var b=t.GetComponent<ForestText>()??t.gameObject.AddComponent<ForestText>();b.Vietnamese=vi;b.English=en;Place(t,x0,y0,x1,y1);return l;
        }
        public static void ButtonStyle(Button b,bool primary=false)
        {
            if(b==null)return;
            if(b.TryGetComponent<Image>(out var i)){i.sprite=Art(primary?"Primary":"Button");i.type=Image.Type.Sliced;i.preserveAspect=false;i.color=Color.white;}
            var c=b.colors;c.normalColor=Color.white;c.selectedColor=new Color(1.05f,1.16f,1.12f);c.highlightedColor=new Color(1.12f,1.25f,1.20f);c.pressedColor=new Color(.64f,.78f,.73f);c.disabledColor=new Color(.37f,.44f,.44f,.68f);c.fadeDuration=.12f;b.colors=c;
            var effect=b.transform.Find("SelectEffect");if(effect!=null)effect.gameObject.SetActive(false);
            foreach(var l in b.GetComponentsInChildren<TMP_Text>(true)){l.color=primary?Dark:Ink;l.enableAutoSizing=true;l.fontSizeMin=20;l.fontSizeMax=28;l.alignment=TextAlignmentOptions.Center;l.raycastTarget=false;Place(l.transform,.04f,.05f,.96f,.95f);}
            foreach(var l in b.GetComponentsInChildren<Text>(true)){l.color=primary?Dark:Ink;l.resizeTextForBestFit=true;l.resizeTextMinSize=20;l.resizeTextMaxSize=28;l.alignment=TextAnchor.MiddleCenter;l.raycastTarget=false;Place(l.transform,.04f,.05f,.96f,.95f);}
        }
        public static Button NewButton(Transform p,string name,string vi,string en,UnityAction action,float x0,float y0,float x1,float y1,bool primary=false)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(p,false);Place(go.transform,x0,y0,x1,y1);var b=go.GetComponent<Button>();b.onClick.AddListener(action);Label(go.transform,"Forest_Label",vi,en,28,0,0,1,1,TextAlignmentOptions.Center);ButtonStyle(b,primary);return b;
        }
        private static bool First(Component c){if(c.GetComponent<ForestLayout>()!=null)return false;c.gameObject.AddComponent<ForestLayout>();return true;}
        public static void Background(Transform p,bool dim=true)
        {
            var old=p.Find("BG");if(old!=null)old.gameObject.SetActive(false);
            Picture(p,"Forest_Background","Forest",0,0,1,1).transform.SetAsFirstSibling();if(!dim)return;
            var shade=Picture(p,"Forest_Shade",null,0,0,1,1);shade.color=new Color(.025f,.065f,.04f,.84f);shade.transform.SetSiblingIndex(1);
        }
        private static void Header(Transform p,string vi,string en,string noteVi,string noteEn)
        {
            Label(p,"Forest_Chapter","SHADOWGROVE · RỪNG CỔ","SHADOWGROVE · ANCIENT FOREST",20,.06f,.91f,.7f,.95f,color:Gold);
            Label(p,"Forest_Title",vi,en,48,.06f,.83f,.84f,.91f);Label(p,"Forest_Subtitle",noteVi,noteEn,25,.06f,.78f,.9f,.83f,color:Muted);
        }
        private static void Existing(Button b,string vi,string en,float x0,float y0,float x1,float y1,bool primary=false)
        {
            Place(b.transform,x0,y0,x1,y1);
            foreach(var localizer in b.GetComponentsInChildren<UnityEngine.Localization.Components.LocalizeStringEvent>(true))localizer.enabled=false;
            var l=b.GetComponentInChildren<TMP_Text>(true);if(l==null)l=Label(b.transform,"Forest_Label",vi,en,28,0,0,1,1,TextAlignmentOptions.Center);
            else{l.text=Tr(vi,en);var bind=l.GetComponent<ForestText>()??l.gameObject.AddComponent<ForestText>();bind.Vietnamese=vi;bind.English=en;}ButtonStyle(b,primary);
        }
        private static void Footer(Component p)
        {
            foreach(var b in p.GetComponentsInChildren<Button>(true)){
                if(b.name.Trim()=="ButtonBack")Existing(b,"QUAY LẠI","BACK",.06f,.06f,.23f,.125f);
                if(b.name.Trim()=="ButtonConfirm")Existing(b,"XÁC NHẬN","CONFIRM",.77f,.06f,.94f,.125f,true);}
        }
        private static void Close(Component p)
        {
            var wrapper=p.transform.Find("ButtonClose");if(wrapper==null)return;Place(wrapper,.77f,.05f,.94f,.12f);
            foreach(var i in wrapper.GetComponentsInChildren<Image>(true))if(i.GetComponent<Button>()==null)i.enabled=false;
            var b=wrapper.GetComponentInChildren<Button>(true);if(b!=null)Existing(b,"LƯU VÀ ĐÓNG","SAVE & CLOSE",0,0,1,1,true);
        }
        public static void Refresh()
        {
            foreach(var p in UnityEngine.Object.FindObjectsByType<UIMainMenu>(FindObjectsSortMode.None))if(First(p))Menu(p);
            foreach(var p in UnityEngine.Object.FindObjectsByType<CharacterSelectPanel>(FindObjectsSortMode.None))if(First(p))Characters(p);
            foreach(var i in UnityEngine.Object.FindObjectsByType<CharacterItem>(FindObjectsSortMode.None)){
                if(First(i))CharacterCard(i);i.BackgroundImage.color=i.IsSelected?new Color(1.18f,1.45f,1.32f):Color.white;
                Label(i.transform,"Forest_Selected",i.IsSelected?"ĐÃ CHỌN":"CHỌN NHÂN VẬT",i.IsSelected?"SELECTED":"CHOOSE SURVIVOR",21,.1f,.025f,.9f,.09f,TextAlignmentOptions.Center,Gold);}
            foreach(var p in UnityEngine.Object.FindObjectsByType<LevelSelectPanel>(FindObjectsSortMode.None))if(First(p))Levels(p);
            foreach(var i in UnityEngine.Object.FindObjectsByType<LevelItem>(FindObjectsSortMode.None)){if(First(i))LevelCard(i);i.BackgroundImage.color=i.IsSelected?new Color(1.18f,1.45f,1.32f):Color.white;}
            foreach(var p in UnityEngine.Object.FindObjectsByType<SettingPanel>(FindObjectsSortMode.None))if(First(p))Settings(p);
            foreach(var p in UnityEngine.Object.FindObjectsByType<PowerUpPanel>(FindObjectsSortMode.None))if(First(p))Powers(p);
            foreach(var p in UnityEngine.Object.FindObjectsByType<MainPanel>(FindObjectsSortMode.None))if(First(p))Hud(p);
            foreach(var p in UnityEngine.Object.FindObjectsByType<UpgradeSkillPanel>(FindObjectsSortMode.None))if(First(p))Upgrade(p);
            foreach(var p in UnityEngine.Object.FindObjectsByType<TreasurePanel>(FindObjectsSortMode.None))if(First(p)){var bg=p.GetComponent<Image>();if(bg!=null){bg.sprite=Art("Panel");bg.color=Color.white;bg.type=Image.Type.Sliced;}foreach(var b in p.GetComponentsInChildren<Button>(true))ButtonStyle(b,true);}
            var pause=GameObject.Find("TrialPausePanel");if(pause!=null&&First(pause.transform))Pause(pause.transform);
            var lang=GameObject.Find("LangButton");if(lang!=null&&First(lang.transform)){ButtonStyle(lang.GetComponent<Button>());Place(lang.transform,.93f,.85f,.985f,.90f);}
        }
        private static void Menu(UIMainMenu p)
        {
            Background(p.transform,false);var shade=Picture(p.transform,"Forest_MenuShade",null,0,0,.53f,1);shade.color=new Color(.025f,.055f,.035f,.60f);shade.transform.SetSiblingIndex(1);
            Label(p.transform,"Forest_Tag","KHU RỪNG KHÔNG NGỦ","THE FOREST NEVER SLEEPS",23,.075f,.80f,.65f,.85f,color:Gold);
            Picture(p.transform,"Forest_Logo","Logo",.055f,.475f,.57f,.79f,true);
            Label(p.transform,"Forest_Copy","Khi khu rừng thức giấc, bạn có sống sót đến bình minh?","When the forest wakes, will you survive until dawn?",27,.075f,.39f,.45f,.46f,color:Muted);
            var group=p.transform.Find("PanelButtons");if(group!=null){StopLayout(group);Place(group,0,0,1,1);}
            foreach(var b in p.GetComponentsInChildren<Button>(true)){
                if(b.name=="ButtonNewGame")Existing(b,"BẮT ĐẦU HÀNH TRÌNH","BEGIN THE JOURNEY",.075f,.29f,.34f,.36f,true);
                if(b.name=="ButtonPowerUp")Existing(b,"NÂNG CẤP","POWER UPS",.075f,.205f,.34f,.275f);
                if(b.name=="ButtonSetting")Existing(b,"CÀI ĐẶT","SETTINGS",.075f,.12f,.34f,.19f);
                if(b.name=="ButtonExit")Existing(b,"THOÁT GAME","QUIT GAME",.075f,.035f,.34f,.105f);}
            Label(p.transform,"Forest_Footer","3 người lữ hành · 10 vùng đất · 3 chế độ","3 survivors · 10 lands · 3 modes",22,.58f,.055f,.95f,.1f,TextAlignmentOptions.MidlineRight,Muted);
        }
        private static void Characters(CharacterSelectPanel p)
        {
            Background(p.transform);Header(p.transform,"Chọn người đồng hành","Choose your companion","Ba kẻ lữ hành. Một khu rừng chưa có lối về.","Three wanderers. One forest without a way home.");Footer(p);
            var list=p.transform.Find("CharacterList");if(list==null)return;Place(list,.1f,.19f,.9f,.75f);
            var g=list.GetComponent<GridLayoutGroup>()??list.gameObject.AddComponent<GridLayoutGroup>();g.cellSize=new Vector2(440,570);g.spacing=new Vector2(35,0);g.constraint=GridLayoutGroup.Constraint.FixedColumnCount;g.constraintCount=3;g.childAlignment=TextAnchor.MiddleCenter;
        }
        private static void CharacterCard(CharacterItem i)
        {
            i.BackgroundImage.sprite=Art("Card");i.BackgroundImage.type=Image.Type.Sliced;i.BackgroundImage.color=Color.white;
            var deco=i.transform.Find("Image");if(deco!=null)deco.gameObject.SetActive(false);
            Picture(i.transform,"Forest_Seal","sigil",.15f,.4f,.85f,.94f,true).transform.SetSiblingIndex(0);
            Place(i.transform.Find("CharacterImage"),.045f,.39f,.955f,.965f);var portrait=i.transform.Find("CharacterImage")?.GetComponent<Image>();if(portrait!=null){portrait.preserveAspect=true;portrait.sprite=HeroArt(i.transform.Find("CharacterName").GetComponent<TMP_Text>().text);}
            var title=i.transform.Find("CharacterName");Place(title,.1f,.28f,.9f,.38f);if(title!=null){var t=title.GetComponent<TMP_Text>();t.fontSize=42;t.color=Ink;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;}
            string heroName=i.transform.Find("CharacterName").GetComponent<TMP_Text>().text;
            string roleVi=heroName=="Rowan"?"Cận chiến · Tầm 2,2\nNhát Chém Gai":heroName=="Lyra"?"Pháp sư · Tầm 7\nCầu Bóng Đêm":"Cung thủ · Tầm 10,5\nTên Tàn Lửa";
            string roleEn=heroName=="Rowan"?"Melee · Range 2.2\nThorn Cleave":heroName=="Lyra"?"Mage · Range 7\nDark Orb":"Archer · Range 10.5\nEmber Arrow";
            Place(i.transform.Find("Skill"),.1f,.13f,.25f,.25f);Label(i.transform,"Forest_Role",roleVi,roleEn,24,.29f,.12f,.91f,.25f,color:Muted);
        }
        private static void Levels(LevelSelectPanel p)
        {
            Background(p.transform);Header(p.transform,"Chọn vùng đất","Choose a land","Mỗi vùng đất là một thử thách khác nhau.","Every land holds a different challenge.");Footer(p);
            var s=p.GetComponentInChildren<ScrollRect>(true);if(s==null)return;Place(s.transform,.055f,.17f,.52f,.75f);
            var l=s.content.GetComponent<VerticalLayoutGroup>()??s.content.gameObject.AddComponent<VerticalLayoutGroup>();l.spacing=12;l.padding=new RectOffset(8,20,8,8);l.childControlHeight=l.childControlWidth=l.childForceExpandWidth=true;l.childForceExpandHeight=false;
            var f=s.content.GetComponent<ContentSizeFitter>()??s.content.gameObject.AddComponent<ContentSizeFitter>();f.verticalFit=ContentSizeFitter.FitMode.PreferredSize;s.scrollSensitivity=45;s.movementType=ScrollRect.MovementType.Clamped;
            foreach(var bar in s.GetComponentsInChildren<Scrollbar>(true))foreach(var image in bar.GetComponentsInChildren<Image>(true)){image.sprite=White;image.type=Image.Type.Sliced;image.color=image.name=="Handle"?Gold:Dark;}
            var d=Picture(p.transform,"Forest_LevelDetail","Panel",.555f,.24f,.94f,.75f);Picture(d.transform,"Forest_Preview","Level0",.055f,.42f,.945f,.94f);
            Label(d.transform,"Forest_LevelName","","",36,.07f,.16f,.93f,.39f,color:Gold);var view=p.gameObject.AddComponent<ForestLevelPreview>();view.Panel=p;view.Detail=d.transform;
        }
        private static void LevelCard(LevelItem i)
        {
            i.BackgroundImage.sprite=Art("Button");i.BackgroundImage.type=Image.Type.Sliced;var e=i.GetComponent<LayoutElement>()??i.gameObject.AddComponent<LayoutElement>();e.preferredHeight=e.minHeight=180;
            foreach(Transform c in i.transform)if(c.name!="Preview"&&c!=i.BackgroundImage.transform&&c.GetComponent<TMP_Text>()==null)c.gameObject.SetActive(false);
            Place(i.PreviewImage.transform,.025f,.09f,.35f,.91f);i.PreviewImage.preserveAspect=false;i.PreviewImage.raycastTarget=false;i.PreviewImage.sprite=LevelArt(i.Config.name);i.PreviewImage.color=Color.white;
            Place(i.Description.transform,.39f,.1f,.96f,.9f);i.Description.fontSize=28;i.Description.enableAutoSizing=true;i.Description.fontSizeMin=23;i.Description.fontSizeMax=28;i.Description.color=Ink;i.Description.raycastTarget=false;
        }
        private static void Settings(SettingPanel p)
        {
            Background(p.transform);Header(p.transform,"Cài đặt","Settings","Điều chỉnh trải nghiệm theo cách của bạn.","Make the experience your own.");Close(p);
            Picture(p.transform,"Forest_SettingsSeal","shrine",.08f,.34f,.39f,.74f,true);Label(p.transform,"Forest_SettingsLore","Lắng nghe khu rừng","Listen to the forest",37,.06f,.26f,.42f,.34f,TextAlignmentOptions.Center,Gold);
            Label(p.transform,"Forest_SettingsNote","Tiếng lá, ánh trăng và những bước chân.","Leaves, moonlight and footsteps.",24,.07f,.19f,.41f,.26f,TextAlignmentOptions.Center,Muted);
            var group=p.transform.Find("SettingGroup");if(group==null)return;StopLayout(group);Place(group,.47f,.17f,.94f,.75f);var list=group.Find("Setting Items");if(list==null)return;StopLayout(list);Place(list,0,0,1,1);
            Picture(group,"Forest_SettingsPlate","Panel",-.03f,-.04f,1.03f,1.04f).transform.SetAsFirstSibling();
            var names=new[]{"Setting_Item","Setting_Item (1)","Setting_Item (2)","Setting_Item (3)","Setting_Item (4)","Setting_Item (6)","Setting_Item (5)"};
            for(int n=0;n<names.Length;n++){
                var row=list.Find(names[n]);if(row==null)continue;StopLayout(row);float y=.97f-n*.137f;Place(row,.025f,y-.12f,.975f,y);Place(row.Find("Setting_Title"),.01f,.05f,.43f,.95f);Place(row.Find("CurrentOption"),.54f,.10f,.90f,.90f);
                foreach(var b in row.GetComponentsInChildren<Button>(true)){bool left=b.name=="ButtonPrevious";Place(b.transform,left?.45f:.92f,.18f,left?.53f:1,.82f);ButtonStyle(b);Label(b.transform,"Forest_Arrow",left?"‹":"›",left?"‹":"›",32,0,0,1,1,TextAlignmentOptions.Center,Gold);}
                foreach(var t in row.GetComponentsInChildren<TMP_Text>(true)){t.fontSize=24;t.enableAutoSizing=true;t.fontSizeMin=18;t.fontSizeMax=24;t.color=Ink;t.raycastTarget=false;}
                if(n<3)row.GetComponent<SettingItem>().AddForestSlider();
            }
        }
        public static string AttributeName(string key){key=key.Trim();string v=LanguageManager.Get("SkillAttr_"+key);return v=="SkillAttr_"+key?key:v;}
        private static void Powers(PowerUpPanel p)
        {
            Background(p.transform);Header(p.transform,"Chuẩn bị cho hành trình","Prepare for the journey","Nâng cấp lâu dài bên đống lửa trại.","Permanent upgrades by the campfire.");Close(p);
            var list=p.transform.Find("CharacterList");if(list==null)return;StopLayout(list);Place(list,.055f,.22f,.71f,.73f);var all=p.GetComponentsInChildren<PowerUpAttributeUpgrade>(true);foreach(var a in all)a.transform.SetParent(list,false);
            var g=list.GetComponent<GridLayoutGroup>()??list.gameObject.AddComponent<GridLayoutGroup>();g.enabled=true;g.constraint=GridLayoutGroup.Constraint.FixedColumnCount;g.constraintCount=4;g.cellSize=new Vector2(292,122);g.spacing=new Vector2(14,14);g.childAlignment=TextAnchor.UpperLeft;
            foreach(var a in all){foreach(var i in a.GetComponentsInChildren<Image>(true)){if(i.name=="BG"||i.transform==a.transform){i.sprite=Art("Button");i.color=Color.white;i.type=Image.Type.Sliced;}else if(i.name=="Image"&&i.transform.parent==a.transform)i.enabled=false;}
                // The tile's full rectangle is its hit target; decorative child
                // graphics must not be the only (small) clickable area.
                foreach(var graphic in a.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
                var surface=a.GetComponent<Image>()??a.gameObject.AddComponent<Image>();surface.sprite=Art("Button");surface.type=Image.Type.Sliced;surface.color=Color.white;
                a.SetHitSurface(surface);
                var item=a.transform.Find("Item");if(item==null)continue;Place(item,0,0,1,1);Place(item.Find("Icon"),.06f,.27f,.27f,.83f);Place(item.Find("Text (TMP)"),.33f,.4f,.94f,.9f);Place(item.Find("Level"),.33f,.12f,.94f,.32f);
                var t=item.GetComponentInChildren<TMP_Text>();if(t!=null){t.text=AttributeName(a.name);t.fontSize=25;t.enableAutoSizing=true;t.fontSizeMin=19;t.fontSizeMax=25;t.color=Ink;t.raycastTarget=false;var bind=t.gameObject.AddComponent<ForestAttributeText>();bind.Key=a.name.Trim();}}
            var d=p.transform.Find("Description");if(d!=null){Place(d,.75f,.25f,.94f,.70f);var bg=d.Find("BG")?.GetComponent<Image>();if(bg!=null){bg.sprite=Art("Panel");bg.color=Color.white;bg.type=Image.Type.Sliced;}
                var rootImage=d.GetComponent<Image>();if(rootImage!=null){rootImage.sprite=Art("Panel");rootImage.color=Color.white;rootImage.type=Image.Type.Sliced;}foreach(Transform c in d)if(c.name=="Image")c.gameObject.SetActive(false);
                Place(d.Find("Title"),.1f,.70f,.9f,.9f);Place(d.Find("Description"),.1f,.35f,.9f,.68f);Place(d.Find("GlodCount"),.3f,.20f,.9f,.30f);Place(d.Find("GlodIcon"),.1f,.20f,.28f,.30f);foreach(var text in d.GetComponentsInChildren<TMP_Text>()){text.enableAutoSizing=true;text.fontSizeMin=20;text.fontSizeMax=32;text.color=Ink;if(text.text=="New Text")text.text=text.name=="Title"?Tr("Chọn nâng cấp","Choose an upgrade"):text.name=="Description"?Tr("Chọn thuộc tính để xem hiệu ứng và giá.","Select an attribute to view its effect and cost."):"";}
                var b=d.GetComponentInChildren<Button>(true);if(b!=null)Existing(b,"NÂNG CẤP","UPGRADE",.1f,.05f,.9f,.17f,true);}
            var gold=p.transform.Find("Gold");if(gold!=null){Place(gold,.74f,.71f,.94f,.77f);foreach(var image in gold.GetComponentsInChildren<Image>(true))if(image.name=="BG"||image.name=="Image")image.enabled=false;var root=gold.GetComponent<Image>();if(root!=null)root.enabled=false;Place(gold.Find("GoldIcon"),0,0,.2f,1);var coin=gold.Find("GoldIcon")?.GetComponent<Image>();if(coin!=null)coin.preserveAspect=true;Place(gold.Find("GoldCount"),.25f,0,.95f,1);foreach(var text in gold.GetComponentsInChildren<TMP_Text>()){text.color=Gold;text.fontSize=28;}}
            Picture(p.transform,"Forest_Camp","camp",.055f,.03f,.19f,.21f,true);Label(p.transform,"Forest_CampNote","Từ tàn lửa, sức mạnh lại nảy mầm.","From the embers, strength grows again.",24,.20f,.15f,.69f,.21f,color:Muted);
        }
        private static void Hud(MainPanel p)
        {
            Place(p.transform,0,0,1,1);Picture(p.transform,"Jade_HealthPlate","Panel",.015f,.885f,.245f,.99f).transform.SetAsFirstSibling();Picture(p.transform,"Jade_TimePlate","Panel",.37f,.865f,.63f,.99f).transform.SetAsFirstSibling();var health=p.transform.Find("Health");Place(health,.025f,.905f,.225f,.936f);
            if(health!=null)foreach(var i in health.GetComponentsInChildren<Image>(true)){i.sprite=White;i.color=i.name=="HPValue"?new Color(.72f,.48f,.37f):i.name=="HPTransition"?Gold:new Color(.12f,.22f,.15f);}
            var slots=p.transform.Find("SkillIcons");Place(slots,.025f,.045f,.305f,.20f);if(slots!=null){StopLayout(slots);var g=slots.GetComponent<GridLayoutGroup>()??slots.gameObject.AddComponent<GridLayoutGroup>();g.enabled=true;g.cellSize=new Vector2(72,72);g.spacing=new Vector2(8,8);g.constraint=GridLayoutGroup.Constraint.FixedColumnCount;g.constraintCount=6;g.childAlignment=TextAnchor.LowerLeft;
                foreach(Transform slot in slots){var image=slot.GetComponent<Image>();var original=image.sprite;var bg=slot.Find("BG");if(bg!=null)bg.gameObject.SetActive(false);var icon=Picture(slot,"Forest_SkillIcon",null,.12f,.12f,.88f,.88f,true);icon.sprite=original;icon.color=original!=null?Color.white:Color.clear;image.sprite=Art("Button");image.color=Color.white;image.type=Image.Type.Sliced;image.raycastTarget=false;}}
            var level=p.transform.Find("Level");Place(level,.015f,.012f,.985f,.027f);if(level!=null)foreach(var i in level.GetComponentsInChildren<Image>(true)){i.sprite=White;i.color=i.name=="LevelValue"?new Color(.39f,.73f,.67f):new Color(.04f,.12f,.14f);}
            Place(level?.Find("Text (TMP)"),.88f,1.8f,1,5);if(level!=null)foreach(var t in level.GetComponentsInChildren<TMP_Text>(true)){t.color=Ink;t.fontSize=24;}
            Place(p.transform.Find("GoldIcon"),.028f,.84f,.055f,.88f);Place(p.transform.Find("KillCount"),.12f,.84f,.147f,.88f);
            foreach(string counter in new[]{"GoldIcon","KillCount"}){var icon=p.transform.Find(counter);if(icon==null)continue;var image=icon.GetComponent<Image>();if(image!=null)image.preserveAspect=true;foreach(var text in icon.GetComponentsInChildren<TMP_Text>(true)){Place(text.transform,1.15f,0,3.4f,1);text.color=Ink;text.fontSize=28;text.alignment=TextAlignmentOptions.MidlineLeft;}}
            Label(p.transform,"Forest_PlayerName","","",27,.025f,.943f,.23f,.985f);var round=UnityEngine.Object.FindFirstObjectByType<TrialController>();if(round!=null)NewButton(p.transform,"Forest_PauseButton","TẠM DỪNG","PAUSE",round.Pause,.83f,.93f,.96f,.985f);p.gameObject.AddComponent<ForestHud>();
        }
        private static void Upgrade(UpgradeSkillPanel p)
        {
            // The inherited HorizontalLayoutGroup must not drive manually anchored
            // cards or the new heading: it collapses their widths to zero.
            StopLayout(p.transform);
            Place(p.transform,0,0,1,1);var shade=p.GetComponent<Image>()??p.gameObject.AddComponent<Image>();shade.color=new Color(.015f,.045f,.055f,.97f);shade.raycastTarget=true;
            Label(p.transform,"Forest_UpgradeTitle","Chọn sức mạnh tiếp theo","Choose your next strength",46,.12f,.83f,.88f,.91f,TextAlignmentOptions.Center,Gold);Label(p.transform,"Forest_UpgradeNote","Một lựa chọn cho chặng đường dài hơn.","One choice for a longer journey.",25,.12f,.77f,.88f,.83f,TextAlignmentOptions.Center,Muted);
            var cards=p.GetComponentsInChildren<UpgradeSkillItem>(true);for(int n=0;n<cards.Length;n++){float x=.105f+n*.27f;Place(cards[n].transform,x,.115f,x+.25f,.755f);var bg=cards[n].GetComponent<Image>()??cards[n].gameObject.AddComponent<Image>();bg.sprite=Art("Card");bg.type=Image.Type.Sliced;bg.color=Color.white;foreach(var b in cards[n].GetComponentsInChildren<Button>(true)){Place(b.transform,.08f,.045f,.92f,.145f);ButtonStyle(b);}}
        }
        public static void Pause(Transform p)
        {
            Picture(p,"Forest_PausePlate","Panel",.32f,.15f,.68f,.85f).transform.SetAsFirstSibling();Picture(p,"Forest_PauseSeal","sigil",.44f,.65f,.56f,.82f,true);
            foreach(var l in p.GetComponentsInChildren<TMP_Text>(true))if(l.transform.parent==p){Place(l.transform,.34f,.56f,.66f,.65f);l.color=Gold;}
            foreach(var b in p.GetComponentsInChildren<Button>(true)){float y=b.name=="TiepTucBtn"?.43f:b.name=="ChoiLaiBtn"?.32f:.21f;Place(b.transform,.37f,y,.63f,y+.075f);ButtonStyle(b,b.name=="TiepTucBtn");}
        }
        public static void Result(Canvas canvas,bool won,int seconds,int kills,int level,string hero)
        {
            var go=new GameObject("TrialResultSummary",typeof(RectTransform),typeof(CanvasGroup));go.transform.SetParent(canvas.transform,false);Place(go.transform,0,0,1,1);go.GetComponent<CanvasGroup>().blocksRaycasts=false;Background(go.transform);
            Picture(go.transform,"Forest_ResultSeal",won?"dawn":"fallen",.39f,.69f,.61f,.93f,true);
            Label(go.transform,"Forest_ResultTitle",won?"Bình minh đã đến":"Hành trình khép lại",won?"Dawn has arrived":"The journey ends",58,.17f,.59f,.83f,.69f,TextAlignmentOptions.Center,Gold);
            Label(go.transform,"Forest_ResultCopy",won?"Bạn đã vượt qua bóng tối của vùng đất này.":"Khu rừng vẫn chờ bạn trở lại.",won?"You survived the shadows of this land.":"The forest awaits your return.",27,.2f,.51f,.8f,.58f,TextAlignmentOptions.Center,Muted);
            Label(go.transform,"Forest_ResultHero",hero,hero,30,.2f,.43f,.8f,.5f,TextAlignmentOptions.Center);
            string[] values={$"{seconds/60:00}:{seconds%60:00}",kills.ToString(),level.ToString()};for(int n=0;n<3;n++){float x=.24f+n*.18f;Label(go.transform,"Forest_ResultValue"+n,values[n],values[n],48,x,.32f,x+.16f,.41f,TextAlignmentOptions.Center,Gold);Label(go.transform,"Forest_ResultCaption"+n,new[]{"Thời gian","Quái đã hạ","Cấp đạt được"}[n],new[]{"Time","Enemies defeated","Level reached"}[n],24,x,.26f,x+.16f,.32f,TextAlignmentOptions.Center,Muted);}
            var native=canvas.transform.Find(won?"GameWin":"GameOver");if(native==null)return;
            foreach(var i in native.GetComponentsInChildren<Image>(true))if(i.GetComponentInParent<Button>()==null)i.enabled=false;
            foreach(var l in native.GetComponentsInChildren<TMP_Text>(true))if(l.GetComponentInParent<Button>()==null)l.enabled=false;
            foreach(var b in native.GetComponentsInChildren<Button>(true)){bool r=b.name=="RestartBtn";Existing(b,r?"CHƠI LẠI":"VỀ TRANG CHỦ",r?"PLAY AGAIN":"MAIN MENU",r?.32f:.52f,.12f,r?.49f:.69f,.19f,r);}native.SetAsLastSibling();
        }
    }
    public sealed class ForestLayout:MonoBehaviour{}
    public sealed class ForestAttributeText:MonoBehaviour
    {
        public string Key;private string language;
        private void LateUpdate(){var code=LanguageManager.GetCurrentCode();if(code==language)return;language=code;GetComponent<TMP_Text>().text=ForestUI.AttributeName(Key);}
    }
    public sealed class ForestText:MonoBehaviour
    {
        public string Vietnamese,English,LevelAsset,LevelFallback;private string _language;private TMP_Text _label;
        private void LateUpdate(){string code=LanguageManager.GetCurrentCode();if(_language==code)return;_language=code;if(_label==null)_label=GetComponent<TMP_Text>();_label.text=!string.IsNullOrEmpty(LevelAsset)?LanguageManager.GetLevelDescription(LevelAsset,LevelFallback):code=="vi"?Vietnamese:English;}
    }
    public sealed class ForestLevelPreview:MonoBehaviour
    {
        public LevelSelectPanel Panel;public Transform Detail;
        private void Update(){var all=Panel.GetComponentsInChildren<LevelItem>();var i=all.FirstOrDefault(x=>x.IsSelected)??all.FirstOrDefault();if(i==null||i.Config==null)return;Detail.Find("Forest_Preview").GetComponent<Image>().sprite=ForestUI.LevelArt(i.Config.name);var t=Detail.Find("Forest_LevelName").GetComponent<TMP_Text>();t.text=LanguageManager.GetLevelDescription(i.Config.name,i.Config.LevelDescription);t.GetComponent<ForestText>().enabled=false;}
    }
    public sealed class ForestHud:MonoBehaviour
    {
        private TrialController _round;
        private void Update(){if(_round==null)_round=FindFirstObjectByType<TrialController>();if(_round==null||_round.GlobalConfig.GlobalPlayer==null)return;var t=transform.Find("Forest_PlayerName").GetComponent<TMP_Text>();t.GetComponent<ForestText>().enabled=false;string level=ForestUI.Tr("Cấp ","Level ")+_round.PlayerStat.GetValue("Level");t.text=_round.GlobalConfig.GlobalPlayer.name.Replace("(Clone)","")+" · "+level;var levelText=transform.Find("Level/Text (TMP)")?.GetComponent<TMP_Text>();if(levelText!=null)levelText.text=level;}
    }
}
