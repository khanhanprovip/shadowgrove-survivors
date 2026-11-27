using System;
using System.Collections.Generic;
using RGame.CommonStat;
using RGame.Framework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace RGame.RoguelikeKit
{
    /// <summary>
    /// Base class for enemies: handles stat initialization, movement, death, drops, and pooling.
    /// </summary>
    public abstract class BaseEnemy : MonoBehaviour, IPoolR
    {
        [SerializeField] protected GlobalConfigSO _globalConfig;
        [SerializeField] private ValueConfigSO _valueConfig;
        [SerializeField] protected PoolRuntimeSO _poolRuntime;
        [SerializeField] private List<string> _dropKeys;

        public Rigidbody2D MyRigidbody2D { get; private set; }
        public SpriteRenderer MySpriteRenderer { get; private set; }
        public EnemyHit MyHit { get; private set; }
        public BaseEnemyWeapon MyWeapon { get; private set; }
        public UnityAction<BaseEnemy> OnDeath { get; set; }
        public event Action<BaseEnemy> Returned;
        public CommonStatRuntimeSO StatRuntime => _stat;
        public string Key { get; set; }

        private CommonStatRuntimeSO _stat;
        protected Transform _playerTransform;
        private bool _isDead;
        private int MaxHP;
        private int encounterHealth;
        public int MaxHealth => MaxHP;
        public void ConfigureContext(GlobalConfigSO global,PoolRuntimeSO pool)
        {
            _globalConfig=global;_poolRuntime=pool;MyHit?.SetPool(pool);MyWeapon?.SetContext(global);
        }
        public void SetEncounterHealth(int health)
        {
            encounterHealth = Mathf.Max(1, health);
            MaxHP = encounterHealth;
            if (_stat != null) _stat.ModifyValue("HP", MaxHP - _stat.GetValue("HP"));
        }
        
        protected virtual void Awake()
        {
            MyRigidbody2D = GetComponentInChildren<Rigidbody2D>();
            MySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            MyHit = GetComponentInChildren<EnemyHit>();
            MyWeapon = GetComponentInChildren<BaseEnemyWeapon>();
        }

        protected virtual void Start()
        {
            _stat = ScriptableObject.CreateInstance<CommonStatRuntimeSO>();
            _stat.SetValueConfigSO(_valueConfig);

            MyWeapon?.SetStat(_stat);
            MyHit?.SetStat(_stat);

            if (MyHit != null)
            {
                MyHit.OnDeath += HandleDeath;
                MyHit.OnDeathEnd += HandleDeathEnd;
            }

            MaxHP = _stat.GetValue("HP");
            if (encounterHealth > 0) SetEncounterHealth(encounterHealth);
        }

        private void OnEnable()
        {
           
        }

        protected virtual void OnDestroy()
        {
            if (MyHit != null)
            {
                MyHit.OnDeath -= HandleDeath;
                MyHit.OnDeathEnd -= HandleDeathEnd;
            }
        }

        protected virtual void FixedUpdate()
        {
            if (_isDead) return;

            if (_playerTransform == null && _globalConfig.GlobalPlayer != null)
                _playerTransform = _globalConfig.GlobalPlayer.transform;
         
            if (_playerTransform != null)
            {
                Move();
                MyRigidbody2D.linearVelocity=DATN.World.StageLayout.Constrain(MyRigidbody2D.position,MyRigidbody2D.linearVelocity,this is EndlessBossEnemy?2f:.55f);
            }
        }

        protected abstract void Move();

        private void HandleDeath()
        {
            _isDead = true;
            MyRigidbody2D.linearVelocity = Vector2.zero;

            foreach (var key in _dropKeys)
            {
                var drop = _poolRuntime.Request(key);
                drop.transform.position = transform.position;
            }
        }

        private void HandleDeathEnd()
        {
            if (!_isDead || !isActiveAndEnabled) return;
            OnDeath?.Invoke(this);
            _poolRuntime.Return(gameObject);
            _globalConfig.RegisterEnemyDefeat();
        }

        public virtual void Request() { }

        public virtual void Return()
        {
            _isDead = false;
            _playerTransform = null;
            MyRigidbody2D.linearVelocity = Vector2.zero;
            if (_stat != null) _stat.ModifyValue("HP", MaxHP - _stat.GetValue("HP"));
            Returned?.Invoke(this);
        }
    }

}
