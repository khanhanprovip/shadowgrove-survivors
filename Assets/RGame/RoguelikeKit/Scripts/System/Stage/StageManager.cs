using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using RGame.CommonStat;
using UnityEngine;
using RGame.Framework;
using RGame.RoguelikeKit.RGame.RoguelikeKit;
using Random = UnityEngine.Random;

namespace RGame.RoguelikeKit
{
    /// <summary>
    /// Reads a StageConfigSO asset and spawns enemies according to SpawnSegments / SpawnSets.
    /// Supports Random, Circle and Area patterns and curse-based spawn scaling.
    /// StageConfig.Segments are copied & sorted by StartTime at runtime (original asset is untouched).
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        [Header("Context")]
        [SerializeField] private PoolRuntimeSO poolRuntime;
        [SerializeField] private GlobalConfigSO globalConfig;
        [SerializeField] private EnemySystem enemySystem;
        [SerializeField] private CommonStatRuntimeSO statRuntime;

        [Header("Events")]
        [SerializeField] private PlayerSpawnChannelSO playerSpawnSO;
        [SerializeField] private StartStageEventChannelSO startStageChannelSO;

        [Header("Spawn Distance (Random)")]
        [SerializeField] private float minSpawnRadius = 14f;
        [SerializeField] private float maxSpawnRadius = 16f;

        // Runtime state
        private List<SpawnSet> _spawnSets;          // sorted working copy
        private float _elapsed;
        private int _spawnSetIndex;
        private Transform _player;
        private int _aliveCount;
        private bool _isEndless;
        private float _scheduleDuration;
        [Header("Performance budget")]
        [SerializeField, Range(10,100)] private int maxActiveEnemies = 40;
        public int EnemyLimit => maxActiveEnemies;
        public int SpawnedThisRun { get; private set; }
        internal PoolRuntimeSO RunPool=>poolRuntime;
        internal void CaptureRunSchedule(RunCheckpoint state)
        {
            foreach(var set in _spawnSets)state.spawnBudgets.Add(_accumulators.TryGetValue(set,out var value)?value:0);
            foreach(var pending in pendingBursts){state.burstSets.Add(_spawnSets.IndexOf(pending.set));state.burstOffsets.Add(pending.offset);}
        }
        internal void PrepareRunRestore()
        {StopAllCoroutines();bosses.CancelEncounter();CleanupEnemies();poolRuntime.RecycleAll();pendingBursts.Clear();_accumulators.Clear();}
        internal void AttachRunEnemy(BaseEnemy enemy)
        {
            enemySystem.AddEnemy(enemy);if(enemy is EndlessBossEnemy)return;
            enemy.Returned-=HandleEnemyReturned;enemy.Returned+=HandleEnemyReturned;
        }
        internal void RestoreRunSchedule(RunCheckpoint state)
        {
            if(state.spawnBudgets.Count!=_spawnSets.Count||state.burstSets.Count!=state.burstOffsets.Count)throw new ArgumentException("Invalid spawn schedule");
            StopAllCoroutines();_accumulators.Clear();pendingBursts.Clear();
            for(int i=0;i<_spawnSets.Count;i++)
            {
                _accumulators[_spawnSets[i]]=state.spawnBudgets[i];
                if(_spawnSets[i].Pattern==SpawnPatternType.Random&&_elapsed>=_spawnSets[i].StartTime&&_elapsed<_spawnSets[i].EndTime)StartCoroutine(RunSegment(_spawnSets[i]));
            }
            for(int i=0;i<state.burstSets.Count;i++)
            {
                int index=state.burstSets[i];if(index<0||index>=_spawnSets.Count)throw new ArgumentException("Invalid queued spawn");
                pendingBursts.Enqueue((_spawnSets[index],state.burstOffsets[i]));
            }
        }
        private readonly Dictionary<SpawnSet, float> _accumulators = new();
        private EndlessBossDirector bosses;
        private float runSeconds,spawnTokens=1;
        private readonly Queue<(SpawnSet set,Vector3 offset)> pendingBursts=new();
        public float CurrentSpawnRate => Mathf.Min(8.5f,SpawnPacing.Rate(runSeconds)*Mathf.Max(1,statRuntime.GetValue("Curse")*.01f));

        private void Awake()
        {
            DOTween.SetTweensCapacity(200, 200);
            bosses=gameObject.AddComponent<EndlessBossDirector>();
            bosses.Initialize(poolRuntime,globalConfig,enemySystem,ClearWave);
        }

        private void OnEnable()
        {
            playerSpawnSO.RegisterListener(OnPlayerSpawned);
            startStageChannelSO.RegisterListener(OnStageStart);
        }

        private void OnDisable()
        {
            playerSpawnSO.UnregisterListener(OnPlayerSpawned);
            startStageChannelSO.UnregisterListener(OnStageStart);
            StopAllCoroutines();
            bosses.CancelEncounter();
            CleanupEnemies();
        }

        private void OnPlayerSpawned(Player player) => _player = player.transform;

        private void OnStageStart(RuntimeStageConfigSO runtime)
        {
            // Make a sorted copy of segments so designer order is irrelevant
            // Dong thoi huy cac coroutine segment cua luot choi truoc (restart)
            // de khong sinh quai kep khi _elapsed duoc reset ve 0.
            StopAllCoroutines();
            CleanupEnemies();
            StageConfigSO cfg = runtime.SelectStageConfig;
            _spawnSets = new List<SpawnSet>(cfg.SpawnSets);
            _spawnSets.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            _isEndless = runtime.IsEndless;
            bosses.ResetRun(_isEndless);
            _scheduleDuration = 1f;
            foreach (var set in _spawnSets)
                _scheduleDuration = Mathf.Max(_scheduleDuration, Mathf.Max(set.StartTime, set.EndTime));
            _elapsed = 0f;
            _spawnSetIndex = 0;
            _aliveCount = 0;
            _accumulators.Clear();
            runSeconds=0;spawnTokens=1;pendingBursts.Clear();
            SpawnedThisRun=0;
        }

        private void Update()
        {
            if(_player!=null&&globalConfig.GlobalPlayer!=null)runSeconds+=Time.deltaTime;
            bosses.Tick();
            if(bosses.IsEncounterActive||bosses.SuppressOrdinarySpawns)return;
            if (_spawnSets == null || _spawnSets.Count == 0) return;

            // Luc dang tai lai scene, player cu da bi huy — tam dung sinh quai
            // den khi player moi duoc sinh (OnPlayerSpawned cap nhat lai _player).
            if (_player == null || globalConfig.GlobalPlayer == null || Time.deltaTime <= 0f) return;
            spawnTokens=Mathf.Min(2,spawnTokens+Time.deltaTime*CurrentSpawnRate);
            while(pendingBursts.Count>0&&spawnTokens>=1)
            {
                var pending=pendingBursts.Dequeue();SpawnEnemy(pending.set,_player.position+pending.offset);
            }

            _elapsed += Time.deltaTime;

            while (_spawnSetIndex < _spawnSets.Count &&
                   _elapsed >= _spawnSets[_spawnSetIndex].StartTime)
            {
                switch (_spawnSets[_spawnSetIndex].Pattern)
                {
                    case SpawnPatternType.Random:
                        StartCoroutine(RunSegment(_spawnSets[_spawnSetIndex]));
                        break;
                    case SpawnPatternType.Circle:
                        CircleSpawnEnemy(_spawnSets[_spawnSetIndex],_spawnSets[_spawnSetIndex].Count);
                        break;
                    case SpawnPatternType.Dense:
                        DenseSpawnEnemy(_spawnSets[_spawnSetIndex],_spawnSets[_spawnSetIndex].Count);
                        break;
                }

                _spawnSetIndex++;
            }

            if (_isEndless && _elapsed >= _scheduleDuration)
            {
                StopAllCoroutines();
                _elapsed %= _scheduleDuration;
                _spawnSetIndex = 0;
                _accumulators.Clear();
            }
        }

        private IEnumerator RunSegment(SpawnSet spawnSet)
        {
            SpawnSet set = spawnSet;
            if (!_accumulators.ContainsKey(set)) _accumulators[set] = 0f;

            while (_elapsed < spawnSet.EndTime && globalConfig.GlobalPlayer != null)
            {
                float t01 = Mathf.InverseLerp(spawnSet.StartTime, spawnSet.EndTime, _elapsed);
                float intensity = spawnSet.IntensityCurve.Evaluate(t01);
                float curseMul = statRuntime != null ? statRuntime.GetValue("Curse") * 0.01f : 1f;

                float addBudget = set.BaseRatePerSecond * intensity * curseMul * Time.deltaTime;
                _accumulators[set] += addBudget;
                
                while (_accumulators[set] >= 1f)
                {
                    RandomSpawnEnemy(set);
                    _accumulators[set] -= 1f;
                }

                yield return null;
            }
        }

        private void RandomSpawnEnemy(SpawnSet set)
        {
            SpawnEnemy(set, GetSpawnPosition());
        }

        private void CircleSpawnEnemy(SpawnSet set, int count)
        {
            var enemies = GetCirclePosition(_player.position,set.CircleRadius, count);
            
            foreach (var position in enemies)
                EnqueueBurst(set, position);
        }

        private void DenseSpawnEnemy(SpawnSet set, int count)
        {
            var enemies = GetDensePosition(count);
            
            foreach (var position in enemies)
                EnqueueBurst(set, position);
        }
        private void EnqueueBurst(SpawnSet set,Vector3 position)
        {
            if(pendingBursts.Count<24)pendingBursts.Enqueue((set,position-_player.position));
        }
        
        private void SpawnEnemy(SpawnSet set, Vector3 position)
        {
            if(spawnTokens<1)return;
            if (_aliveCount >= maxActiveEnemies) return;
            if(!DATN.World.StageLayout.TrySpawn(position,_player.position,out position))return;
            var info = PickRandom(set.Entries);
            if (info == null) return;

            var obj = poolRuntime.Request(info.PoolKey);
            if (obj == null) return;
            spawnTokens-=1;
            obj.transform.position = position;
            var enemy = obj.GetComponent<BaseEnemy>();
            enemy.Returned += HandleEnemyReturned;
            enemySystem.AddEnemy(enemy);
            _aliveCount++;
            SpawnedThisRun++;
        }

        private void HandleEnemyReturned(BaseEnemy enemy)
        {
            enemy.Returned -= HandleEnemyReturned;
            enemySystem.RemoveEnemy(enemy);
            _aliveCount--;
        }

        private Vector3 GetSpawnPosition()
        {
            Vector3 origin = _player ? _player.position : Vector3.zero;
            
            float angleR = Random.value * Mathf.PI * 2f;
            float radius = Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector2 offset = new Vector2(Mathf.Cos(angleR), Mathf.Sin(angleR)) * radius;
            return origin + (Vector3)offset;
        }

        /// <summary>
        /// Returns evenly spaced points on a circle's circumference.
        /// </summary>
        /// <param name="center">World-space center position (x,y).</param>
        /// <param name="radius">Distance from center to each point.</param>
        /// <param name="count">Number of points (≥ 1).</param>
        /// <returns>List of Vector3 (z = 0) ordered counter-clockwise.</returns>
        private List<Vector3> GetCirclePosition(Vector2 center, float radius, int count)
        {
            var points = new List<Vector3>(count);
            if (count <= 0 || radius <= 0f) return points;

            float step = Mathf.PI * 2f / count;
            for (int i = 0; i < count; i++)
            {
                float angle = i * step;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                points.Add(new Vector3(center.x + offset.x, center.y + offset.y, 0f));
            }
            return points;
        }

        private List<Vector3> GetDensePosition(int count)
        {
            Vector3 origin = _player ? _player.position : Vector3.zero;
            var list = new List<Vector3>(count);
            
            float angleR = Random.value * Mathf.PI * 2f;
            Vector2 center = new Vector2(Mathf.Cos(angleR), Mathf.Sin(angleR)) * maxSpawnRadius;
            
            const float spacing = 0.3f;
            
            int side = Mathf.CeilToInt(Mathf.Sqrt(count));
            float half = (side - 1) * 0.5f * spacing;

            for (int i = 0; i < count; i++)
            {
                int row = i / side;
                int col = i % side;
                
                float x = center.x + (col * spacing) - half;
                float y = center.y + (row * spacing) - half;

                list.Add(origin + new Vector3(x, y, 0f));
            }
            return list;
        }
        
        private EnemySpawnInfo PickRandom(List<EnemySpawnInfo> list)
        {
            if (list == null || list.Count == 0) return null;
            float total = 0f;
            foreach (var e in list) if(Allowed(e)) total += Mathf.Max(0.0001f, e.Weight);
            if(total<=0)return null;
            float r = Random.value * total;
            float cum = 0f;
            foreach (var e in list)
            {
                if(!Allowed(e))continue;
                cum += Mathf.Max(0.0001f, e.Weight);
                if (r <= cum) return e;
            }
            return null;
        }
        private bool Allowed(EnemySpawnInfo info)
        {
            var prefab=poolRuntime.GetPrefab(info.PoolKey);
            var rank=prefab!=null?prefab.GetComponent<EnemyRank>():null;
            return rank==null||rank.Tier!=EnemyTier.Boss;
        }
        private void ClearWave()
        {
            StopAllCoroutines();CleanupEnemies();_elapsed=0;_spawnSetIndex=0;_accumulators.Clear();pendingBursts.Clear();spawnTokens=1;
        }

        private void CleanupEnemies()
        {
            foreach (BaseEnemy e in enemySystem.GetEnemies())
            {
                if (e != null)
                {
                    e.Returned -= HandleEnemyReturned;
                    poolRuntime.Return(e.gameObject);
                }
            }
            enemySystem.ClearEnemies();
            _aliveCount = 0;
        }
    }
}
