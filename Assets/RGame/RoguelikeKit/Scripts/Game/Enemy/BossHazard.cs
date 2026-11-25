using System.Collections.Generic;
using UnityEngine;
namespace RGame.RoguelikeKit
{
    // Only active attacks are drawn; their geometry also defines the damage area.
    public sealed class BossHazard : MonoBehaviour
    {
        public enum Shape { Disc, Line, Ring, Bolt, Orbit }
        static readonly List<BossHazard> Pool = new();
        static Material material;
        LineRenderer line;
        BossSkillVfx skillVfx;
        Shape shape;
        Vector2 from, to, velocity;
        float radius, age, warning, duration, orbitAngle;
        Color tint;
        PlayerHit target;
        Transform owner;
        bool hit;
        // Retain the windup timing without drawing a precast floor marker.
        public bool IsTelegraph => age < warning;
        public static int ActiveCount { get { Pool.RemoveAll(x => x == null); return Pool.FindAll(x => x.gameObject.activeSelf).Count; } }
        public static void ClearAll() { foreach (var item in Pool) if (item != null) item.gameObject.SetActive(false); }
        public static BossHazard Create(Transform owner, PlayerHit target, Shape shape, Vector2 from, Vector2 to,
            float radius, float warning, float duration, Color tint, Vector2 velocity = default)
        {
            Pool.RemoveAll(x => x == null);
            var h = Pool.Find(x => !x.gameObject.activeSelf);
            if (h == null)
            {
                if (Pool.Count >= 96) return null;
                var go = new GameObject("Boss skill effect");
                h = go.AddComponent<BossHazard>(); h.line = go.AddComponent<LineRenderer>();
                if (material == null) material = new Material(Shader.Find("Sprites/Default"));
                h.line.sharedMaterial = material; h.line.useWorldSpace = true; h.line.sortingOrder = 20000;
                Pool.Add(h);
            }
            h.owner = owner; h.target = target; h.shape = shape; h.from = from; h.to = to;
            h.radius = radius; h.warning = Mathf.Max(.8f, warning); h.duration = duration;
            h.velocity = velocity; h.age = 0; h.hit = false; h.tint = tint; h.orbitAngle = Mathf.Atan2(to.y,to.x);
            if(h.skillVfx==null)h.skillVfx=h.gameObject.AddComponent<BossSkillVfx>();
            int kind=owner!=null&&owner.TryGetComponent<BossBrain>(out var brain)?brain.Kind:0;
            h.skillVfx.Init(kind,shape,from,to,radius,tint);
            h.gameObject.SetActive(true); h.Draw(); return h;
        }
        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 d = b-a; return Vector2.Distance(p, a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude)));
        }
        void Update()
        {
            if (owner == null || !owner.gameObject.activeInHierarchy || target == null) { gameObject.SetActive(false); return; }
            if (Time.deltaTime <= 0) return;
            float previous=age;
            age += Time.deltaTime;
            float activeDelta=Mathf.Max(0,Mathf.Min(age,warning+duration)-Mathf.Max(previous,warning));
            if (activeDelta>0)
            {
                Vector2 oldCenter=Center();float oldAngle=orbitAngle;
                if (shape == Shape.Bolt) from += velocity * activeDelta;
                if (shape == Shape.Orbit) orbitAngle += activeDelta * 1.4f;
                Vector2 p = target.transform.position;
                bool inside = shape == Shape.Line ? SegmentDistance(p,from,to) <= radius :
                    shape == Shape.Ring ? Mathf.Abs(Vector2.Distance(p,from)-radius) <= .35f :
                    shape == Shape.Bolt ? SegmentDistance(p,oldCenter,Center())<=radius :
                    Vector2.Distance(p, Center()) <= radius;
                if(shape==Shape.Orbit)
                {
                    int steps=Mathf.Clamp(Mathf.CeilToInt(activeDelta*1.4f*2.8f/Mathf.Max(.05f,radius*.4f)),1,64);
                    for(int i=0;i<=steps&&!inside;i++)
                    {float a=Mathf.Lerp(oldAngle,orbitAngle,(float)i/steps);inside=Vector2.Distance(p,from+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*2.8f)<=radius;}
                }
                if (inside && !hit) { hit = true; target.OnHit(8); }
            }
            if (age > warning + duration) { gameObject.SetActive(false); return; }
            Draw();
        }
        Vector2 Center() => shape == Shape.Orbit ? from + new Vector2(Mathf.Cos(orbitAngle),Mathf.Sin(orbitAngle))*2.8f : from;
        void Draw()
        {
            bool active = age >= warning;
            line.enabled=active;
            if(!active){if(skillVfx!=null)skillVfx.Hide();return;}
            if(skillVfx!=null)skillVfx.Tick(age-warning,duration,Center(),from,to);
            var c = tint;
            c.a *= Mathf.Clamp01((warning+duration-age)/.25f);
            line.startColor = line.endColor = c;
            line.widthMultiplier = .16f;
            if (shape == Shape.Line)
            {
                // Outline capsule, preserving a safe corridor outside its visible border.
                line.loop = true; line.positionCount = 34; float a = Mathf.Atan2((to-from).y,(to-from).x);
                for (int i=0;i<34;i++) { bool end=i<17; float angle=a+(end?-Mathf.PI/2:Mathf.PI/2)+(i%17)*Mathf.PI/16;
                    Vector2 p=(end?to:from)+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius; line.SetPosition(i,new Vector3(p.x,p.y,0)); }
            }
            else
            {
                line.loop = true; line.positionCount = 48; Vector2 center=Center();
                for(int i=0;i<48;i++) { float a=i*Mathf.PI*2/48; var p=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius; line.SetPosition(i,new Vector3(p.x,p.y,0)); }
            }
        }
    }
}
