using System;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    // A non-damaging arrival ceremony. Scaled time freezes every part with pause.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1200)]
    public sealed class BossEntrance : MonoBehaviour
    {
        public const float WarningSeconds = .85f;
        public const float RevealSeconds = 2.55f;
        public const float TotalSeconds = WarningSeconds + RevealSeconds;
        public bool IsEntering { get; private set; }
        public float Elapsed { get; private set; }
        public int Kind { get; private set; }
        public Bounds TargetBounds { get; private set; }
        public float BodyHeight { get; private set; }
        float bodyWidth;
        static readonly string[] Styles={"Jade portal","Root emergence","Crystal descent","Scythe charge","Void awakening"};
        public string Style => Styles[Kind];

        EndlessBossEnemy enemy;
        BossBrain brain;
        SpriteRenderer body;
        SpriteRenderer survivor;
        Collider2D[] colliders;
        bool[] colliderStates;
        bool weaponEnabled;
        Color bodyColor;
        Vector3 target, start;
        Action completed;
        readonly Vector3[] points = new Vector3[32];
        readonly LineRenderer[] lines = new LineRenderer[4];

        public void Begin(int kind, Action onComplete, SpriteRenderer survivorRenderer=null)
        {
            Cancel();
            enemy = GetComponent<EndlessBossEnemy>();
            brain = GetComponent<BossBrain>();body = enemy.MySpriteRenderer;
            survivor=survivorRenderer;
            Kind = Mathf.Clamp(kind, 0, 4);Elapsed = 0;completed = onComplete;
            target = transform.position;
            var visual = GetComponent<BossVisualAnimation>();
            TargetBounds = visual != null ? visual.StableWorldBounds : body.bounds;
            var bodyBounds=BossCastingVfx.CanonicalBounds(enemy);
            BodyHeight=bodyBounds.size.y;bodyWidth=bodyBounds.size.x;
            bodyColor = body.color;
            colliders = GetComponentsInChildren<Collider2D>(true);
            colliderStates = new bool[colliders.Length];
            for(int i=0;i<colliders.Length;i++){colliderStates[i]=colliders[i].enabled;colliders[i].enabled=false;}
            weaponEnabled = enemy.MyWeapon.enabled;enemy.MyWeapon.enabled = false;
            brain.StopFight();brain.enabled = false;
            enemy.Entering = true;enemy.Casting = false;enemy.DashVelocity = Vector2.zero;
            enemy.MyRigidbody2D.linearVelocity = Vector2.zero;
            float edge = Camera.main != null ? Camera.main.orthographicSize * Camera.main.aspect * 2f : 30f;
            start = target + (Kind==0?Vector3.down*2.8f:Kind==1?Vector3.down*3.6f:
                Kind==2?new Vector3(-2.4f,4.5f,0):Kind==3?Vector3.right*(edge+TargetBounds.size.x):Vector3.down*1.1f);
            IsEntering = true;
            for(int i=0;i<lines.Length;i++)
                if(lines[i]==null)lines[i]=BossVfxShared.CreateLine(transform,"Boss Entrance FX "+i,body.sortingOrder-1,.09f);
            SetOpacity(0);HideLines();
            enemy.EntranceMotion = BossMotion.Idle;
            // Remain at the final location while hidden, so the camera starts from a
            // fixed envelope, rather than following the offscreen staging position.
        }

        void Update()
        {
            if(!IsEntering||Time.deltaTime<=0)return;
            if(survivor!=null)body.sortingOrder=survivor.sortingOrder-5;
            Elapsed += Time.deltaTime;
            if(Elapsed < WarningSeconds){SetOpacity(0);return;}
            float t=Mathf.Clamp01((Elapsed-WarningSeconds)/RevealSeconds);
            float ease=Mathf.SmoothStep(0,1,t);
            float travel=Kind==3?Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.10f)/.78f)):ease;
            transform.position=Vector3.Lerp(start,target,travel);
            if(Kind==2)transform.position+=Vector3.right*Mathf.Sin(t*Mathf.PI)*1.1f;
            SetOpacity(Mathf.SmoothStep(0,1,Mathf.Clamp01(t/(Kind==1?.48f:.25f))));
            enemy.EntranceMotion=Kind==1?BossMotion.Windup:Kind==4?(t<.46f?BossMotion.Windup:BossMotion.Attack):BossMotion.Move;
            if(Kind==3)enemy.FaceDirection(Vector2.left);
            Draw(t);
            if(t>=1)
            {
                transform.position=target;
                var callback=completed;completed=null;Restore();
                if(brain!=null)brain.enabled=true;
                callback?.Invoke();
            }
        }

        void SetOpacity(float alpha)
        {
            if(body!=null)body.color=new Color(bodyColor.r,bodyColor.g,bodyColor.b,bodyColor.a*alpha);
        }
        void HideLines(){foreach(var line in lines)if(line!=null)line.enabled=false;}
        void Restore()
        {
            IsEntering=false;HideLines();
            if(body!=null)body.color=bodyColor;
            if(colliders!=null)for(int i=0;i<colliders.Length;i++)if(colliders[i]!=null)colliders[i].enabled=colliderStates[i];
            if(enemy!=null)
            {
                enemy.Entering=false;enemy.EntranceMotion=BossMotion.Idle;
                if(enemy.MyWeapon!=null)enemy.MyWeapon.enabled=weaponEnabled;
                if(enemy.MyRigidbody2D!=null)enemy.MyRigidbody2D.linearVelocity=Vector2.zero;
            }
        }
        public void Cancel(){completed=null;if(IsEntering)Restore();else HideLines();}
        void OnDisable(){Cancel();}

        void Apply(int index,Color color,float alpha,bool loop,float width)
        {
            var line=lines[index];BossVfxShared.BindMaterial(line);
            color.a=alpha;line.startColor=line.endColor=color;
            line.sortingLayerID=body.sortingLayerID;line.sortingOrder=body.sortingOrder+(Kind==3?1:-1);
            line.positionCount=points.Length;line.SetPositions(points);line.loop=loop;line.widthMultiplier=width;line.enabled=true;
        }
        void Ellipse(int index,Vector3 center,float x,float y,float rotation,Color color,float alpha,float width)
        {
            for(int i=0;i<points.Length;i++)
            {
                float a=i*Mathf.PI*2/points.Length+rotation;
                points[i]=center+new Vector3(Mathf.Cos(a)*x,Mathf.Sin(a)*y,0);
            }
            Apply(index,color,alpha,true,width);
        }
        void Draw(float t)
        {
            HideLines();
            Color color=BossCatalog.Colors[Kind];
            float envelope=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.18f))*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.78f)/.22f)));
            float open=Mathf.Sin(Mathf.PI*Mathf.Clamp01(t*1.1f))*.35f+.65f;
            Vector3 ground=target+Vector3.up*.15f;
            if(Kind==0)
            {
                var center=target+Vector3.up*BodyHeight*.48f;
                Ellipse(0,center,bodyWidth*.52f*open,BodyHeight*.58f,0,color,.50f*envelope,.13f);
                Ellipse(1,center,bodyWidth*.48f*open,BodyHeight*.54f,0,color,.20f*envelope,.35f);
                for(int i=0;i<points.Length;i++){float u=i/31f;float a=u*Mathf.PI*5+t*6;points[i]=ground+new Vector3(Mathf.Cos(a)*(1-u)*bodyWidth*.45f,u*BodyHeight,0);}
                Apply(2,color,.48f*envelope,false,.08f);
            }
            else if(Kind==1)
            {
                for(int branch=0;branch<4;branch++)
                {
                    float side=branch<2?-1:1;float spread=bodyWidth*(branch%2==0?.55f:.38f);
                    for(int i=0;i<points.Length;i++)
                    {float u=i/31f;points[i]=ground+new Vector3(side*u*spread*easeRoot(t),Mathf.Sin(u*Mathf.PI*3+branch)*u*BodyHeight*.028f+u*u*BodyHeight*(branch%2==0?.16f:.38f)*easeRoot(t),0);}
                    Apply(branch,color,.65f*envelope,false,.16f-(branch%2)*.04f);
                }
            }
            else if(Kind==2)
            {
                for(int shard=0;shard<3;shard++)
                {
                    Vector3 center=ground+new Vector3((shard-1)*bodyWidth*.4f,BodyHeight*(.3f+(1-t)*.65f+shard*.05f),0);
                    for(int i=0;i<points.Length;i++)
                    {float a=i*Mathf.PI*2/points.Length;float r=1/(Mathf.Abs(Mathf.Cos(a))+Mathf.Abs(Mathf.Sin(a)));points[i]=center+new Vector3(Mathf.Cos(a)*BodyHeight*.075f*r,Mathf.Sin(a)*BodyHeight*.22f*r,0);}
                    Apply(shard,color,.7f*envelope,true,.1f);
                }
                Ellipse(3,ground,bodyWidth*.6f*open,BodyHeight*.1f*open,t,color,.5f*envelope,.09f);
            }
            else if(Kind==3)
            {
                for(int streak=0;streak<4;streak++)
                {
                    for(int i=0;i<points.Length;i++)
                    {float u=i/31f;points[i]=transform.position+new Vector3(u*BodyHeight*.85f,BodyHeight*(.18f+streak*.20f+Mathf.Sin(u*Mathf.PI)*.06f),0);}
                    Apply(streak,color,(.6f-streak*.1f)*envelope,false,.14f);
                }
            }
            else
            {
                Ellipse(0,ground,bodyWidth*.65f*open,BodyHeight*.19f*open,-t*2,color,.55f*envelope,.13f);
                for(int i=0;i<points.Length;i++){float u=i/31f;float a=u*Mathf.PI*7-t*8;points[i]=ground+new Vector3(Mathf.Cos(a)*u*bodyWidth*.57f,Mathf.Sin(a)*u*BodyHeight*.16f,0);}
                Apply(1,color,.7f*envelope,false,.1f);
                Ellipse(2,target+Vector3.up*BodyHeight*.65f,bodyWidth*.22f*open,BodyHeight*.1f*open,0,color,.5f*envelope,.12f);
                Ellipse(3,target+Vector3.up*BodyHeight*.65f,BodyHeight*.022f,BodyHeight*.085f*open,0,color,.75f*envelope,.12f);
            }
        }
        static float easeRoot(float t)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.7f));
    }
}
