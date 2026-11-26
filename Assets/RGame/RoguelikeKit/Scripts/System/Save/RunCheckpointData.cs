using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace RGame.RoguelikeKit
{
    [Serializable] public sealed class RunStat { public string key;public int value,max; }
    [Serializable] public sealed class RunComponent
    {
        public string path,type;public int index;public bool enabled;
        public List<RunField> fields;
        public int animationHash;public float animationTime;
        public Vector3 velocity;public float angularVelocity;
        public Color color;public bool flipX,flipY;
        public string sprite;
        public List<RunField> material;
    }
    [Serializable] public sealed class RunTransform { public string path;public Vector3 position,scale;public Quaternion rotation;public bool active; }
    [Serializable] public sealed class RunEntity
    {
        public string id,key;public Vector3 position;public Quaternion rotation;
        public int maxHP;public bool entering;public float deathProgress=-1;
        public List<RunTransform> transforms=new();public List<RunComponent> components=new();public List<RunStat> stats;
    }
    [Serializable] public sealed class RunSkill
    {public string key;public int level;public SkillCurrentState state;public List<RunField> fields;public float cycle,timer;}
    [Serializable] public sealed class RunBuff
    {public string owner,asset;public float age;public List<RunField> fields;}
    [Serializable] public sealed class RunTimer {public string key;public float elapsed,end;}
    [Serializable] public sealed class RunSystemState {public string type;public List<RunField> fields;}
    [Serializable] public sealed class RunCheckpoint
    {
        public int version=1;public string id,hero,stage,savedUtc,random;
        public float clock,cameraSize;public Vector3 cameraPosition;
        public List<RunStat> stats=new();public List<RunSkill> skills=new();public List<RunEntity> entities=new();
        public List<RunSystemState> systems=new();public List<RunField> mode;
        public BossDeckState deck;public List<float> spawnBudgets=new();public List<int> burstSets=new();public List<Vector3> burstOffsets=new();
        public List<RunBuff> buffs=new();
        public List<RunTimer> timers=new();
    }
    // Separate from settings and purchased upgrades. The envelope catches partial
    // writes/corruption; FileManager supplies atomic replacement and a backup.
    public static class RunCheckpointStore
    {
        public const string Filename="Shadowgrove.run.json",Backup="Shadowgrove.run.bak",Consumed="Shadowgrove.run.consumed";
        [Serializable] sealed class Envelope {public string payload,sha256;}
        static string Hash(string value){using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
        public static bool Save(RunCheckpoint state)
        {
            Validate(state);string payload=JsonUtility.ToJson(state);
            string envelope=JsonUtility.ToJson(new Envelope{payload=payload,sha256=Hash(payload)});
            if(envelope.Length>8*1024*1024)throw new ArgumentException("Run checkpoint exceeds the supported size");
            return FileManager.WriteToFile(Filename,envelope,Backup);
        }
        public static bool TryRead(out RunCheckpoint state)
        {
            state=null;FileManager.LoadFromFile(Consumed,out var consumed);
            foreach(string name in new[]{Filename,Backup})
            {
                if(!FileManager.LoadFromFile(name,out var json)||json.Length>8*1024*1024)continue;
                try
                {
                    var envelope=JsonUtility.FromJson<Envelope>(json);
                    if(envelope==null||string.IsNullOrEmpty(envelope.payload)||Hash(envelope.payload)!=envelope.sha256)continue;
                    var candidate=JsonUtility.FromJson<RunCheckpoint>(envelope.payload);Validate(candidate);
                    if(consumed.Split('\n').Contains(candidate.id))continue;state=candidate;return true;
                }
                catch(ArgumentException){ }
            }
            return false;
        }
        public static bool Consume(RunCheckpoint state)
        {
            FileManager.LoadFromFile(Consumed,out var ledger);
            return ledger.Split('\n').Contains(state.id)||FileManager.WriteToFile(Consumed,string.IsNullOrEmpty(ledger)?state.id:ledger+"\n"+state.id);
        }
        static void Validate(RunCheckpoint state)
        {
            if(state==null||state.version!=1||!Guid.TryParse(state.id,out _)||!new[]{"Rowan","Lyra","Kael"}.Contains(state.hero)||!DATN.UI.ForestUI.LevelKeys.Contains(state.stage)
                ||float.IsNaN(state.clock)||float.IsInfinity(state.clock)||state.clock<0||state.entities==null||state.entities.Count<1||state.entities.Count>2048
                ||state.skills==null||state.skills.Count>24||state.stats==null||state.stats.Count>64||state.systems==null||state.systems.Count>32
                ||state.entities.Any(x=>x==null||string.IsNullOrEmpty(x.id)||x.transforms==null||x.components==null||x.transforms.Count>256||x.components.Count>256)
                ||state.skills.Any(x=>x==null)||state.stats.Any(x=>x==null)||state.systems.Any(x=>x==null)
                ||state.entities.Select(x=>x.id).Distinct().Count()!=state.entities.Count)throw new ArgumentException("Invalid run checkpoint");
        }
    }
}
