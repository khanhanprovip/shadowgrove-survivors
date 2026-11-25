using UnityEngine;
namespace RGame.RoguelikeKit
{
    public sealed class EndlessBossEnemy : BaseEnemy
    {
        public bool Casting;
        public Vector2 DashVelocity;
        // The authored three-quarter poses face right. Mirror the body only;
        // the nameplate, physics and world-space attack geometry stay upright.
        public void FaceDirection(Vector2 direction)
        {
            if(MySpriteRenderer!=null&&Mathf.Abs(direction.x)>.08f)
                MySpriteRenderer.flipX=direction.x<0f;
        }
        protected override void Move()
        {
            if (StatRuntime == null) return;
            Vector2 delta = _playerTransform.position - transform.position;
            MyRigidbody2D.linearVelocity = DashVelocity.sqrMagnitude > 0 ? DashVelocity :
                Casting || delta.magnitude < 2f ? Vector2.zero : delta.normalized * 1.05f;
            if(!Casting)FaceDirection(MyRigidbody2D.linearVelocity);
        }
        public override void Return()
        {
            Casting = false; DashVelocity = Vector2.zero;
            if(MySpriteRenderer!=null)MySpriteRenderer.flipX=false;
            base.Return();
        }
    }
}
