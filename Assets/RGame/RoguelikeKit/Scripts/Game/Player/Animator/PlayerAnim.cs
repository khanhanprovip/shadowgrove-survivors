#region

using UnityEngine;

#endregion

namespace RGame.RoguelikeKit
{
    public class PlayerAnim : MonoBehaviour
    {
        [HideInInspector] public Animator MyAnimator;
        [HideInInspector] public Rigidbody2D MyRigidbody;

        private void Awake()
        {
            MyRigidbody = transform.GetComponent<Rigidbody2D>();
            MyAnimator = transform.GetComponent<Animator>();
        }

        private float _attackLockUntil;
        private int _animationHash;

        private void Update()
        {
            // Dang trong cua so attack → khong chuyen lai Idle/Run ngay
            if (Time.time < _attackLockUntil) return;

            if (MyRigidbody.linearVelocity.magnitude != 0)
                ChangeAnimation("Run");
            else
                ChangeAnimation("Idle");
        }

        /// <summary>
        ///     Shadowgrove (Moc 5): phat animation Attack khi ky nang duoc cast.
        ///     Khoa 0.35s de animation kip phat truoc khi Update chuyen lai Idle/Run.
        /// </summary>
        public void PlayAttack()
        {
            if (MyAnimator == null || Time.time < _attackLockUntil) return;
            _attackLockUntil = Time.time + 0.35f;
            _animationHash = Animator.StringToHash("Attack");
            MyAnimator.Play(_animationHash, 0, 0f);
        }

        public void ChangeAnimation(string _animationName)
        {
            if (MyAnimator == null) return;
            int hash = Animator.StringToHash(_animationName);
            if (hash == _animationHash) return;
            _animationHash = hash;
            MyAnimator.Play(hash, 0, 0f);
        }
    }
}