using System.Collections;
using System.Linq;
using DATN.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace RGame.RoguelikeKit
{
    [DefaultExecutionOrder(1500)]
    public sealed class BossBrain : MonoBehaviour
    {
        public int Kind;
        public int CastCount { get; private set; }
        public int CurrentSkill { get; private set; } = -1;
        public int UsedSkillMask { get; private set; }
        public Vector2 CastDirection { get; private set; } = Vector2.right;
        private float castStarted;
        public float CastAge => enemy!=null&&enemy.Casting?Mathf.Max(0,Time.time-castStarted):-1;
        EndlessBossEnemy enemy;
        PlayerHit player;
        Image fill;
        TMP_Text title, health;
        RectTransform header;
        // Explicit phases make an in-flight cast resumable across application runs.
        enum FightPhase { Stopped, Ready, Windup, Dash, Recovery }
        FightPhase phase;
        float remaining;
        int nextSkill;
        Vector2 lockedTarget;
        SpriteRenderer survivorRenderer;
        BossVisualAnimation visual;
        public void Begin(int kind, PlayerHit target)
        {
            Kind=kind; player=target; enemy=GetComponent<EndlessBossEnemy>();
            visual=GetComponent<BossVisualAnimation>();
            if(GetComponent<BossCastingVfx>()==null)gameObject.AddComponent<BossCastingVfx>();
            var sorting=GetComponent<DynamicSortOrder>();if(sorting!=null)sorting.enabled=false;
            survivorRenderer=target.GetComponentInParent<Player>().GetComponentsInChildren<SpriteRenderer>().Where(s=>s.sprite!=null&&!s.name.ToLowerInvariant().Contains("shadow")).OrderByDescending(s=>s.bounds.size.y).First();
            CastCount=0; CurrentSkill=-1; UsedSkillMask=0;
            CastDirection=enemy.MySpriteRenderer.flipX?Vector2.left:Vector2.right;
            phase=FightPhase.Ready;remaining=1.5f;nextSkill=Random.Range(0,5);
        }
        void Update()
        {
            if(phase==FightPhase.Stopped||Time.deltaTime<=0)return;
            if(player==null||enemy.StatRuntime==null)return;
            if(enemy.StatRuntime.GetValue("HP")<=0){StopFight();return;}
            remaining-=Time.deltaTime;if(remaining>0)return;
            if(phase==FightPhase.Ready)
            {
                CurrentSkill=nextSkill; UsedSkillMask|=1<<nextSkill; CastCount++; enemy.Casting=true;castStarted=Time.time;
                Vector2 p=player.transform.position, b=transform.position;
                Vector2 dir=(p-b).normalized; if(dir.sqrMagnitude<.1f)dir=Vector2.down;
                CastDirection=dir;lockedTarget=p;
                enemy.FaceDirection(CastDirection);
                Cast(Kind,nextSkill,b,p,dir);
                phase=FightPhase.Windup;remaining=1.1f;return;
            }
            if(phase==FightPhase.Windup)
            {
                if (Kind==3 && CurrentSkill==0)
                {
                    enemy.DashVelocity=CastDirection*9f;phase=FightPhase.Dash;remaining=.7f;return;
                }
                if ((Kind==0 && CurrentSkill==3)||(Kind==3 && CurrentSkill==3))transform.position=lockedTarget;
                phase=FightPhase.Recovery;remaining=.9f;return;
            }
            if(phase==FightPhase.Dash)
            {enemy.DashVelocity=Vector2.zero;phase=FightPhase.Recovery;remaining=.9f;return;}
            if(phase==FightPhase.Recovery)
            {
                enemy.Casting=false;
                int pick=Random.Range(0,4);
                nextSkill=pick>=CurrentSkill?pick+1:pick;
                phase=FightPhase.Ready;remaining=2.5f;
            }
        }
        static Vector2 Turn(Vector2 d,float degrees) => Quaternion.Euler(0,0,degrees)*d;
        void Hazard(BossHazard.Shape s,Vector2 a,Vector2 b,float r,float duration=1f,Vector2 velocity=default,float delay=1.1f)
            => BossHazard.Create(transform,player,s,a,b,r,delay,duration,BossCatalog.Colors[Kind],velocity);
        void Disc(Vector2 p,float r=1.2f,float delay=1.1f)=>Hazard(BossHazard.Shape.Disc,p,p,r,.65f,default,delay);
        void Line(Vector2 a,Vector2 b,float r=.45f,float delay=1.1f)=>Hazard(BossHazard.Shape.Line,a,b,r,.7f,default,delay);
        void Bolt(Vector2 b,Vector2 dir,float speed=5f,float radius=.3f,float delay=1.1f)=>Hazard(BossHazard.Shape.Bolt,b,b,radius,3f,dir*speed,delay);
        void Fan(Vector2 b,Vector2 dir,int count,float spread,float speed=5f)
        { for(int i=0;i<count;i++)Bolt(b,Turn(dir,(i-(count-1)/2f)*spread),speed); }
        void Ring(Vector2 b,Vector2 dir,int count=12)
        { for(int i=2;i<count;i++)Bolt(b,Turn(dir,i*360f/count),4); } // Two missing spokes form the escape wedge.
        // Each boss has five named attacks with distinct readable geometry/timing.
        void Cast(int kind,int skill,Vector2 b,Vector2 p,Vector2 d)
        {
            Vector2 side=Turn(d,90);
            switch(kind)
            {
                case 0:
                    if(skill==0) Fan(b,d,5,18);
                    if(skill==1) for(int i=-2;i<=2;i++)Disc(b+Turn(d,i*25)*2.6f,.72f);
                    if(skill==2) {Disc(p);Disc(p+side*2.8f,1.2f,1.7f);}
                    if(skill==3) Disc(p,1.8f);
                    if(skill==4) for(int i=0;i<6;i++)Hazard(BossHazard.Shape.Orbit,b,Turn(Vector2.right,i*60),.38f,3.2f);
                    break;
                case 1:
                    if(skill==0) for(int i=-1;i<=1;i++)Disc(p+side*i*2.5f,.9f,1.1f+.25f*(i+1));
                    if(skill==1) for(int i=-1;i<=1;i++)Line(b+side*i*2.5f,p+d*4+side*i*2.5f,.4f);
                    if(skill==2) for(int i=2;i<10;i++)Disc(p+Turn(d,i*36)*3.7f,.65f);
                    if(skill==3) {Line(p-Vector2.right*6,p+Vector2.right*6);Line(p-Vector2.up*6,p+Vector2.up*6,.45f,1.65f);}
                    if(skill==4) for(int i=1;i<=3;i++)Hazard(BossHazard.Shape.Ring,b,b,i*2f,.6f,default,1.1f+i*.45f);
                    break;
                case 2:
                    if(skill==0) Ring(b,d);
                    if(skill==1) for(int i=0;i<3;i++)Disc(p+side*(i-1)*2.6f,1f,1.1f+i*.5f);
                    if(skill==2) for(int i=0;i<4;i++)Line(b,b+Turn(d,i*90+35)*8,.45f,1.1f+i*.3f);
                    if(skill==3) Fan(b,d,7,12,3.6f);
                    if(skill==4) {Hazard(BossHazard.Shape.Ring,p,p,2.5f,1.5f); Disc(p,.6f,1.9f);}
                    break;
                case 3:
                    if(skill==0) Line(b,b+d*7,1f);
                    if(skill==1) for(int i=-2;i<=2;i++)Disc(b+Turn(d,i*22)*2.3f,.8f);
                    if(skill==2) for(int i=-1;i<=1;i++)Line(p-d*4+side*i*2.8f,p+d*4+side*i*2.8f,.5f,1.1f+.35f*(i+1));
                    if(skill==3) Disc(p,1.9f);
                    if(skill==4) {Bolt(b,d,5,.65f); Bolt(b+d*10,-d,5,.65f,3.1f);}
                    break;
                case 4:
                    if(skill==0) Line(b,p+d*5,.7f);
                    if(skill==1) for(int i=0;i<4;i++)Disc(p+Turn(d,i*90)*2f,.95f,1.1f+i*.35f);
                    if(skill==2) for(int i=0;i<12;i++)Bolt(b,Turn(d,i*30),4,.25f,1.1f+i*.12f);
                    if(skill==3) Ring(p,d,16);
                    if(skill==4) for(int i=-1;i<=1;i++){Line(p+new Vector2(-6,i*3),p+new Vector2(6,i*3),.35f,1.1f);Line(p+new Vector2(i*3,-6),p+new Vector2(i*3,6),.35f,2f);}
                    break;
            }
        }
        void MakeHeader()
        {
            var go=new GameObject("Boss health and name",typeof(RectTransform),typeof(Canvas));
            go.transform.SetParent(transform,false); header=go.GetComponent<RectTransform>();header.sizeDelta=new Vector2(390,80);
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=25000;
            var plate=go.AddComponent<Image>();plate.sprite=DATN.UI.ForestUI.Art("Panel");plate.type=Image.Type.Sliced;plate.raycastTarget=false;
            title=Text(go.transform,"Boss name",32,new Vector2(0,22));
            health=Text(go.transform,"HP",22,new Vector2(0,-10));
            var back=new GameObject("HP background",typeof(RectTransform),typeof(Image));back.transform.SetParent(go.transform,false);
            var rt=back.GetComponent<RectTransform>();rt.sizeDelta=new Vector2(350,20);
            rt.anchoredPosition=new Vector2(0,-10);
            back.GetComponent<Image>().color=new Color(.025f,.035f,.04f,.96f);back.transform.SetAsFirstSibling();
            var bar=new GameObject("HP fill",typeof(RectTransform),typeof(Image));bar.transform.SetParent(back.transform,false);fill=bar.GetComponent<Image>();
            var br=bar.GetComponent<RectTransform>();br.anchorMin=Vector2.zero;br.anchorMax=Vector2.one;br.offsetMin=new Vector2(2,2);br.offsetMax=new Vector2(-2,-2);
        }
        static TMP_Text Text(Transform parent,string name,float size,Vector2 pos)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
            var rt=go.GetComponent<RectTransform>();rt.sizeDelta=new Vector2(390,32);rt.anchoredPosition=pos;
            var t=go.GetComponent<TMP_Text>();t.fontSize=size;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;
            t.enableAutoSizing=true;t.fontSizeMin=size*.8f;t.fontSizeMax=size;t.outlineWidth=.22f;t.outlineColor=Color.black;return t;
        }
        void LateUpdate()
        {
            if(enemy==null||enemy.StatRuntime==null)return;
            if(survivorRenderer!=null)enemy.MySpriteRenderer.sortingOrder=survivorRenderer.sortingOrder-5;
            if(header==null)MakeHeader();
            bool alive=enemy.StatRuntime.GetValue("HP")>0;
            header.gameObject.SetActive(alive);
            if(!alive){ StopFight(); return; }
            bool vi=LanguageManager.GetCurrentCode()=="vi";
            title.text=BossCatalog.Name(Kind,vi);title.color=BossCatalog.Colors[Kind];
            health.text=$"{enemy.StatRuntime.GetValue("HP")} / {enemy.MaxHealth}";
            fill.color=BossCatalog.Colors[Kind];
            var rt=fill.rectTransform;rt.anchorMax=new Vector2(Mathf.Clamp01((float)enemy.StatRuntime.GetValue("HP")/enemy.MaxHealth),1);
            float zoom=Camera.main!=null?Mathf.Max(1,Camera.main.orthographicSize/10):1;
            header.localScale=Vector3.one*.018f*zoom;
            var bounds=visual!=null?visual.StableWorldBounds:enemy.MySpriteRenderer.bounds;
            header.position=new Vector3(bounds.center.x,bounds.max.y+.5f*zoom,0);
            var camera=Camera.main;
            if(camera!=null)
            {
                var screen=camera.WorldToScreenPoint(header.position);
                float halfWidth=390*.018f*zoom*Screen.height/(4*camera.orthographicSize);
                screen.x=Mathf.Clamp(screen.x,halfWidth+20,Screen.width-halfWidth-20);
                screen.y=Mathf.Clamp(screen.y,Screen.height*.20f,Screen.height*.74f);
                header.position=camera.ScreenToWorldPoint(screen);
            }
        }
        public void StopFight()
        {
            if(enemy!=null){enemy.Casting=false;enemy.DashVelocity=Vector2.zero;}
            phase=FightPhase.Stopped;remaining=0;
            BossHazard.ClearAll();
        }
        void OnDisable(){StopFight();if(header!=null)header.gameObject.SetActive(false);}
    }
}
