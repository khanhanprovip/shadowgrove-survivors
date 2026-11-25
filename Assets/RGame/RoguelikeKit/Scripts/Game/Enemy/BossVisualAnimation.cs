using System;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    public enum BossMotion
    {
        Idle = 0,
        Move = 1,
        Windup = 2,
        Attack = 3,
        Hit = 4,
        Death = 5
    }

    /// <summary>
    /// Selects authored sprite-frame clips from boss combat state. The Animator owns
    /// sprite changes; this component only chooses a state and tracks scaled game time.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossVisualAnimation : MonoBehaviour
    {
        const float WindupDuration = 1.1f;
        const float HitDuration = 0.18f;

        static readonly int[] MotionStateHashes =
        {
            Animator.StringToHash("Base Layer.Idle"),
            Animator.StringToHash("Base Layer.Move"),
            Animator.StringToHash("Base Layer.Windup"),
            Animator.StringToHash("Base Layer.Attack"),
            Animator.StringToHash("Base Layer.Hit"),
            Animator.StringToHash("Base Layer.Death")
        };

        [SerializeField] SpriteRenderer _animatedRenderer;
        [SerializeField] Animator _animator;
        [SerializeField] Sprite _standingSprite;
        [SerializeField] Sprite[] _frames = new Sprite[36];
        [SerializeField] AnimationClip[] _stateClips = new AnimationClip[6];

        public BossMotion Motion { get; private set; }
        public float AnimationClock { get; private set; }
        public SpriteRenderer AnimatedRenderer => _animatedRenderer;
        public Animator NativeAnimator => _animator;
        public Sprite StandingSprite => _standingSprite;
        public Sprite[] Frames => _frames ?? Array.Empty<Sprite>();
        public AnimationClip[] StateClips => _stateClips ?? Array.Empty<AnimationClip>();

        /// <summary>
        /// Stable envelope of every authored frame, transformed to the renderer's current
        /// world pose. Caching the local union prevents camera/header bounds from changing
        /// as the Animator swaps sprites, while the world transform still follows movement.
        /// </summary>
        public Bounds StableWorldBounds
        {
            get
            {
                EnsureStableLocalBounds();
                if (_animatedRenderer == null)
                    return new Bounds(transform.position, Vector3.zero);
                return TransformStableBounds(_stableLocalBounds, _animatedRenderer);
            }
        }

        EndlessBossEnemy _enemy;
        BossBrain _brain;
        Bounds _stableLocalBounds;
        bool _stableLocalBoundsCached;
        float _hitRemaining;
        int _lastHp;
        bool _observedHp;
        int _lastPlayedStateHash;

        void Awake()
        {
            CacheComponents();
            CacheStableLocalBounds();
            _lastPlayedStateHash = 0;
        }

        void OnEnable()
        {
            CacheComponents();
            CacheStableLocalBounds();
            AnimationClock = 0f;
            Motion = BossMotion.Idle;
            _hitRemaining = 0f;
            _observedHp = false;
            _lastPlayedStateHash = 0;

            if (_animatedRenderer != null && StandingSprite != null)
                _animatedRenderer.sprite = StandingSprite;
        }

        void Update()
        {
            if (_animatedRenderer == null)
                return;

            float deltaTime = Time.deltaTime;
            if (deltaTime > 0f)
                AnimationClock += deltaTime;

            int hp = ReadHp();
            bool alive = hp > 0;
            if (!_observedHp && _enemy != null && _enemy.StatRuntime != null)
            {
                _lastHp = hp;
                _observedHp = true;
            }
            else if (alive && hp < _lastHp)
            {
                _hitRemaining = HitDuration;
            }
            _lastHp = hp;

            if (!alive)
            {
                Motion = BossMotion.Death;
            }
            else
            {
                float castAge = _brain != null ? _brain.CastAge : -1f;
                if (_enemy != null && _enemy.Entering)
                    Motion = _enemy.EntranceMotion;
                else if (_enemy != null && _enemy.Casting && castAge >= 0f)
                    Motion = castAge < WindupDuration ? BossMotion.Windup : BossMotion.Attack;
                else if (_hitRemaining > 0f)
                    Motion = BossMotion.Hit;
                else if (ReadVelocity().sqrMagnitude > 0.04f)
                    Motion = BossMotion.Move;
                else
                    Motion = BossMotion.Idle;

                if (_hitRemaining > 0f && deltaTime > 0f)
                    _hitRemaining = Mathf.Max(0f, _hitRemaining - deltaTime);
            }

            PlayMotion(Motion);
        }

        void OnDisable()
        {
            AnimationClock = 0f;
            Motion = BossMotion.Idle;
            _hitRemaining = 0f;
            _observedHp = false;
            _lastPlayedStateHash = 0;
            if (_animatedRenderer != null && StandingSprite != null)
                _animatedRenderer.sprite = StandingSprite;
        }

        void CacheComponents()
        {
            if (_enemy == null)
                _enemy = GetComponent<EndlessBossEnemy>();
            if (_brain == null)
                _brain = GetComponent<BossBrain>();
            if (_animatedRenderer == null)
            {
                _animatedRenderer = _enemy != null && _enemy.MySpriteRenderer != null
                    ? _enemy.MySpriteRenderer
                    : GetComponentInChildren<SpriteRenderer>(true);
            }
            if (_animator == null)
                _animator = GetComponent<Animator>();
            if (_standingSprite == null && _frames != null && _frames.Length > 0)
                _standingSprite = _frames[0];
        }

        void EnsureStableLocalBounds()
        {
            if (!_stableLocalBoundsCached)
            {
                CacheComponents();
                CacheStableLocalBounds();
            }
        }

        void CacheStableLocalBounds()
        {
            bool hasBounds = false;
            Bounds bounds = default;
            if (_frames != null)
            {
                foreach (Sprite frame in _frames)
                {
                    if (frame == null)
                        continue;
                    if (!hasBounds)
                    {
                        bounds = frame.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(frame.bounds);
                    }
                }
            }

            if (!hasBounds && _animatedRenderer != null && _animatedRenderer.sprite != null)
            {
                bounds = _animatedRenderer.sprite.bounds;
                hasBounds = true;
            }

            _stableLocalBounds = hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.zero);
            _stableLocalBoundsCached = true;
        }

        static Bounds TransformStableBounds(Bounds localBounds, SpriteRenderer renderer)
        {
            Transform rendererTransform = renderer.transform;
            Vector3 scale = rendererTransform.lossyScale;
            scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            Quaternion rotation = rendererTransform.rotation;
            Vector3 position = rendererTransform.position;
            Vector3 min = localBounds.min;
            Vector3 max = localBounds.max;
            bool flipX = renderer.flipX;
            bool flipY = renderer.flipY;

            bool hasBounds = false;
            Bounds worldBounds = default;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 localCorner = new Vector3(
                    (corner & 1) == 0 ? min.x : max.x,
                    (corner & 2) == 0 ? min.y : max.y,
                    (corner & 4) == 0 ? min.z : max.z);
                if (flipX)
                    localCorner.x = -localCorner.x;
                if (flipY)
                    localCorner.y = -localCorner.y;

                Vector3 worldCorner = position + rotation * Vector3.Scale(localCorner, scale);
                if (!hasBounds)
                {
                    worldBounds = new Bounds(worldCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    worldBounds.Encapsulate(worldCorner);
                }
            }
            return worldBounds;
        }

        void PlayMotion(BossMotion motion)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
                return;

            int stateHash = MotionStateHashes[(int)motion];
            if (stateHash == _lastPlayedStateHash)
                return;

            // Every controller contains these six named states and no transitions;
            // Play resets a non-looping pose exactly once when combat state changes.
            _animator.Play(stateHash, 0, 0f);
            _lastPlayedStateHash = stateHash;
        }

        int ReadHp()
        {
            return _enemy != null && _enemy.StatRuntime != null
                ? _enemy.StatRuntime.GetValue("HP")
                : 1;
        }

        Vector2 ReadVelocity()
        {
            if (_enemy == null)
                return Vector2.zero;
            if (_enemy.DashVelocity.sqrMagnitude > 0.001f)
                return _enemy.DashVelocity;
            return _enemy.MyRigidbody2D != null ? _enemy.MyRigidbody2D.linearVelocity : Vector2.zero;
        }

        /// <summary>Returns the source-sheet frames used by the requested clip.</summary>
        public Sprite[] FramesFor(BossMotion motion)
        {
            int first;
            int count;
            switch (motion)
            {
                case BossMotion.Idle: first = 0; count = 8; break;
                case BossMotion.Move: first = 8; count = 8; break;
                case BossMotion.Windup: first = 16; count = 8; break;
                case BossMotion.Attack: first = 24; count = 8; break;
                case BossMotion.Hit: first = 32; count = 2; break;
                case BossMotion.Death: first = 34; count = 2; break;
                default: return Array.Empty<Sprite>();
            }

            if (_frames == null || _frames.Length < first + count)
                return Array.Empty<Sprite>();
            var result = new Sprite[count];
            Array.Copy(_frames, first, result, 0, count);
            return result;
        }

        public AnimationClip ClipFor(BossMotion motion)
        {
            int index = (int)motion;
            return _stateClips != null && index >= 0 && index < _stateClips.Length
                ? _stateClips[index]
                : null;
        }
    }
}
