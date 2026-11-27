using System.Collections.Generic;
using RGame.RoguelikeKit;
using UnityEngine;

namespace DATN.World
{
    // Deterministic geometry: the same stage rebuilds the same topology on resume.
    // A shared flood field routes all ordinary enemies, with no per-enemy A* allocations.
    public sealed class StageLayout : MonoBehaviour
    {
        public const int CurrentRevision = 1;
        public static StageLayout Current { get; private set; }
        public string StageKey { get; private set; }
        public int ReachableCells { get; private set; }
        public int WalkableCells { get; private set; }
        public int WallRuns { get; private set; }
        public bool BossArenaLocked { get; set; }
        bool[] open;
        int[] distance, queue;
        int width,height,lastTarget=-1;
        float ox,oy;
        int stageIndex;
        bool unbounded;
        readonly List<Vector2> candidates=new();
        static readonly int[] Dx={1,-1,0,0},Dy={0,0,1,-1};

        public static void Apply(LevelConfigSO level,Transform map)
        {
            if(RunCheckpointSession.UsesLegacyWorld)return;
            var layout=map.gameObject.AddComponent<StageLayout>();
            layout.Build(level,map);
        }
        void OnDestroy(){if(Current==this)Current=null;}
        public static bool IsOpen(Vector2 p,float radius=.45f)=>Current==null||Current.Free(p,radius);
        public static Vector3 ResolvePosition(Vector3 p,float radius=.6f)=>Current==null?p:Current.Nearest(p,radius);
        public static bool TrySpawn(Vector3 proposed,Vector3 player,out Vector3 position)
        {
            position=proposed;if(Current==null||Current.unbounded)return true;
            float score=float.PositiveInfinity;bool found=false;
            foreach(var candidate in Current.candidates)
            {
                float fromPlayer=(candidate-(Vector2)player).sqrMagnitude;
                if(fromPlayer<64||fromPlayer>18*18||!Current.Free(candidate,.7f))continue;
                float cost=(candidate-(Vector2)proposed).sqrMagnitude;if(cost>=score)continue;
                score=cost;position=candidate;found=true;
            }
            return found;
        }
        public static Vector2 Constrain(Vector2 p,Vector2 velocity,float radius=.45f)
        {
            if(Current==null)return velocity;
            float dt=Time.fixedDeltaTime;
            if(dt<=0)return Vector2.zero;
            Vector2 next=p+velocity*dt;
            if(Current.Free(next,radius))return velocity;
            // Sliding preserves the user's tangential input at a wall.
            Vector2 result=Vector2.zero;
            if(Current.Free(new Vector2(next.x,p.y),radius))result.x=velocity.x;
            if(Current.Free(new Vector2(p.x+result.x*dt,next.y),radius))result.y=velocity.y;
            return result;
        }
        public static Vector2 Chase(Vector2 p,Vector2 target)
        {
            if(Current==null)return(target-p).normalized;
            return Current.Direction(p,target);
        }
        public bool ReadyForBoss(Vector2 player)=>player.sqrMagnitude<=64;
        public Vector3 BossSpawn=>Nearest(new Vector3(5.5f,-7,0),2f);
        bool Free(Vector2 p,float radius)
        {
            if(unbounded)return true;
            if(BossArenaLocked&&p.sqrMagnitude>Mathf.Pow(14-radius,2))return false;
            return CellOpen(p)&&CellOpen(p+Vector2.right*radius)&&CellOpen(p-Vector2.right*radius)
                &&CellOpen(p+Vector2.up*radius)&&CellOpen(p-Vector2.up*radius);
        }
        bool CellOpen(Vector2 p)
        {
            int x=Mathf.FloorToInt(p.x-ox+.5f),y=Mathf.FloorToInt(p.y-oy+.5f);
            return x>=0&&x<width&&y>=0&&y<height&&open[y*width+x];
        }
        int Cell(Vector2 p)
        {
            int x=Mathf.Clamp(Mathf.RoundToInt(p.x-ox),0,width-1),y=Mathf.Clamp(Mathf.RoundToInt(p.y-oy),0,height-1);
            return y*width+x;
        }
        Vector2 Point(int i)=>new Vector2(ox+i%width,oy+i/width);
        Vector3 Nearest(Vector3 p,float radius)
        {
            if(Free(p,radius))return p;
            Vector2 best=Vector2.zero;float score=float.PositiveInfinity;
            foreach(var candidate in candidates)
            {
                float d=((Vector2)p-candidate).sqrMagnitude;
                if(d>=score||!Free(candidate,radius))continue;
                score=d;best=candidate;
            }
            return new Vector3(best.x,best.y,p.z);
        }
        Vector2 Direction(Vector2 p,Vector2 target)
        {
            if(unbounded)return(target-p).normalized;
            int goal=Cell(target);
            if(goal!=lastTarget)Flood(goal);
            int cell=Cell(p);
            if(!open[cell])return((Vector2)Nearest(p,.45f)-p).normalized;
            Vector2 direct=target-p;
            // Use direct movement wherever the full segment is inside the floor.
            bool clear=true;
            int steps=Mathf.CeilToInt(direct.magnitude*2);
            for(int i=1;i<=steps;i++)if(!Free(p+direct*(i/(float)steps),.55f)){clear=false;break;}
            if(clear)return direct.normalized;
            int best=cell,score=distance[cell];int x=cell%width,y=cell/width;
            for(int k=0;k<4;k++)
            {
                int xx=x+Dx[k],yy=y+Dy[k];if(xx<0||xx>=width||yy<0||yy>=height)continue;
                int n=yy*width+xx;if(distance[n]>=0&&(score<0||distance[n]<score)&&Free(Point(n),.55f)){best=n;score=distance[n];}
            }
            return best==cell?Vector2.zero:(Point(best)-p).normalized;
        }
        void Flood(int goal)
        {
            lastTarget=goal;System.Array.Fill(distance,-1);
            int head=0,tail=0;if(!open[goal])return;
            queue[tail++]=goal;distance[goal]=0;
            while(head<tail)
            {
                int at=queue[head++],x=at%width,y=at/width;
                for(int k=0;k<4;k++)
                {
                    int xx=x+Dx[k],yy=y+Dy[k];if(xx<0||xx>=width||yy<0||yy>=height)continue;
                    int n=yy*width+xx;if(!open[n]||distance[n]>=0)continue;
                    distance[n]=distance[at]+1;queue[tail++]=n;
                }
            }
        }
        bool Shape(float x,float y)
        {
            float ax=Mathf.Abs(x),ay=Mathf.Abs(y);
            bool central=x*x+y*y<16*16;
            switch(stageIndex)
            {
                case 0:return ax<29&&ay<29&&!(ax>18&&ay>18&&(ax-23)*(ax-23)+(ay-23)*(ay-23)<7);
                case 1:return(ax<29&&ay<9)||(Mathf.Abs(ax-18)<6&&ay<23);
                case 2:return central||((x-22)*(x-22)+y*y<11*11)||((x+22)*(x+22)+y*y<11*11)||(ax<23&&ay<5);
                case 3:return(ax<7&&ay<29)||(ax<28&&(ay<5||Mathf.Abs(ay-21)<5))||(Mathf.Abs(ax-23)<4&&ay<24);
                case 4:return central||(ax<7&&ay<29)||(x*x+(ay-21)*(ay-21)<12*12);
                case 5:return central||(ax<6&&ay<29)||(ay<6&&ax<29)||(Mathf.Abs(ay-19)<4&&ax<25)||(Mathf.Abs(ax-21)<8&&Mathf.Abs(ay-19)<8);
                case 6:return(ax<29&&ay<29)&&(ax<5||ay<5||Mathf.Abs(ax-21)<5||Mathf.Abs(ay-21)<5);
                case 7:return central||(ax<10&&ay<29)||(ay<10&&ax<29);
                default:return ax<30&&ay<30;
            }
        }
        void Build(LevelConfigSO level,Transform map)
        {
            Current=this;StageKey=level.name;stageIndex=System.Array.IndexOf(DATN.UI.ForestUI.LevelKeys,StageKey);
            unbounded=level.GameMode!=null&&level.GameMode.ModeId==DATN.Modes.GameModeId.Endless;
            width=level.MapConfig.Width;height=level.MapConfig.Height;ox=-width*.5f;oy=-height*.5f;
            open=new bool[width*height];distance=new int[open.Length];queue=new int[open.Length];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                int at=y*width+x;open[at]=unbounded||Shape(ox+x,oy+y);
                if(open[at]){WalkableCells++;candidates.Add(Point(at));}
            }
            Flood(Cell(Vector2.zero));ReachableCells=0;
            foreach(var d in distance)if(d>=0)ReachableCells++;
            if(ReachableCells!=WalkableCells)Debug.LogError("Disconnected stage geometry: "+StageKey);
            var floor=map.GetComponentInChildren<LevelBackground>().GroundRenderer;
            // Reuse the stage's authored pixel texture; darkened impassable ledges have a visible rim.
            for(int y=0;y<height;y++)for(int x=0;x<width;)
            {
                if(open[y*width+x]){x++;continue;}
                int start=x;while(x<width&&!open[y*width+x])x++;
                var wall=new GameObject("Ledge_"+y+"_"+start,typeof(SpriteRenderer),typeof(BoxCollider2D));
                wall.transform.SetParent(map,false);wall.transform.position=new Vector3(ox+(start+x-1)*.5f,oy+y,0);
                var r=wall.GetComponent<SpriteRenderer>();r.sprite=floor.sprite;r.drawMode=SpriteDrawMode.Tiled;r.size=new Vector2(x-start,1);
                r.sortingLayerName="Map";r.sortingOrder=short.MinValue+2;r.color=stageIndex==2?new Color(.12f,.24f,.25f):new Color(.21f,.23f,.24f);
                wall.GetComponent<BoxCollider2D>().size=new Vector2(x-start,1);WallRuns++;
            }
            // The old prop grid was unaware of walls. Move real chests onto reachable floor.
            foreach(var chest in FindObjectsByType<DropBox>(FindObjectsSortMode.None))chest.transform.position=Nearest(chest.transform.position,1.2f);
        }
    }
}
