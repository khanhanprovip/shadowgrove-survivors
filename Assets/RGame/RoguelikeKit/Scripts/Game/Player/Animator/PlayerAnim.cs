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
            MyAnimator.CrossFade("Attack", 0f);
        }

        public void ChangeAnimation(string _animationName)
        {
            MyAnimator.CrossFade(_animationName, 0);
        }
    }
}