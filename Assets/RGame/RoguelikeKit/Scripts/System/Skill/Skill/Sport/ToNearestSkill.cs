using System;
using System.Collections.Generic;
using RGame.Framework;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    public class ToNearestSkill : SkillBase
    {
        private int _damage;
        private Vector3 _moveDir;
        private float _velocity;
        private float _timer;
        private float _travelled;
        private float _maxRange = float.PositiveInfinity;
        private bool _expired;
        private Vector3 _origin;

        private void OnEnable()
        {
            _timer = 0;
            _travelled = 0;
            _maxRange = float.PositiveInfinity;
            _expired = false;
        }

        private void FixedUpdate()
        {
            _timer += Time.fixedDeltaTime;
            
            if (_expired) return;
            if (_timer > 5 || _travelled >= _maxRange) { Expire(); return; }
            float step = Mathf.Min(_velocity * Time.fixedDeltaTime, _maxRange - _travelled);
            transform.position += _moveDir * step;
            _travelled += step;
        }

        public void Init(float velocity, int damage, Vector3 targetPos)
        {
            _timer = 0;
            _travelled = 0;
            _maxRange = float.PositiveInfinity;
            _expired = false;
            _damage = damage;
            _velocity = velocity;
            _origin = transform.position;
            
            _moveDir = (targetPos - transform.position).normalized;
        }

        public void ConfigureStarter(StarterWeaponProfile profile)
        {
            _maxRange = profile.Range;
            var renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = profile.Effect;
            renderer.color = Color.white;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(_moveDir.y, _moveDir.x) * Mathf.Rad2Deg);
            var circle = GetComponent<CircleCollider2D>();
            if (circle != null)
            {
                circle.radius = profile.Kind == StarterWeaponKind.Arrow ? .10f : .22f;
                circle.offset = profile.Kind == StarterWeaponKind.Arrow ? new Vector2(.35f, 0) : Vector2.zero;
            }
            var box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.size = profile.Kind == StarterWeaponKind.Arrow ? new Vector2(.18f, .14f) : new Vector2(.44f, .44f);
                box.offset = profile.Kind == StarterWeaponKind.Arrow ? new Vector2(.35f, 0) : Vector2.zero;
            }
        }

        private void Expire()
        {
            if (_expired) return;
            _expired = true;
            OnDeath?.Invoke(this);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_expired && other.CompareTag("EnemyHit"))
            {
                if (!float.IsPositiveInfinity(_maxRange) &&
                    Vector2.Distance(_origin, other.transform.position) > _maxRange) return;
                Attack(other.transform.parent.GetComponent<BaseEnemy>());
                other.GetComponent<EnemyHit>().Hit(GetDamage());
                Expire();
            }
        }

        private int GetDamage()
        {
            return _damage;
        }
    }
}
