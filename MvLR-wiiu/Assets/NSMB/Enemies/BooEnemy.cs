using UnityEngine;

namespace NSMB.Enemies {
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class BooEnemy : MonoBehaviour {
        public float chaseSpeed = 1.5f;
        public float gravityScale = 0f;
        public float chaseStartRadius = 10f;

        private Rigidbody2D _rb;
        private SpriteRenderer _sr;

        private void Awake() {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb != null) {
                _rb.gravityScale = gravityScale;
                _rb.freezeRotation = true;
            }

            NSMB.Visual.SimpleSpriteAnimator anim;
            Transform graphics;
            Unity6EnemyPrototypes.ApplyBoo(gameObject, out graphics, out _sr, out anim);
        }

        private void FixedUpdate() {
            if (_rb == null) {
                return;
            }

            NSMB.Player.PlayerMotor2D player = FindObjectOfType<NSMB.Player.PlayerMotor2D>();
            if (player == null) {
                return;
            }

            Vector2 delta = player.transform.position - transform.position;
            if (delta.magnitude > chaseStartRadius) {
                _rb.velocity = Vector2.zero;
                return;
            }

            bool playerFacingRight = true;
            NSMB.Player.PlayerVisualFromOriginal vis = player.GetComponent<NSMB.Player.PlayerVisualFromOriginal>();
            if (vis != null) {
                // Approximate facing using x velocity (same logic as PlayerVisualFromOriginal).
                Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
                if (prb != null && Mathf.Abs(prb.velocity.x) > 0.05f) {
                    playerFacingRight = prb.velocity.x > 0f;
                }
            } else {
                Rigidbody2D prb2 = player.GetComponent<Rigidbody2D>();
                if (prb2 != null && Mathf.Abs(prb2.velocity.x) > 0.05f) {
                    playerFacingRight = prb2.velocity.x > 0f;
                }
            }

            bool booIsRightOfPlayer = transform.position.x > player.transform.position.x;
            bool playerLookingAtBoo = (booIsRightOfPlayer && playerFacingRight) || (!booIsRightOfPlayer && !playerFacingRight);

            if (playerLookingAtBoo) {
                _rb.velocity = Vector2.zero;
            } else {
                Vector2 dir = delta.normalized;
                _rb.velocity = dir * chaseSpeed;
            }

            if (_sr != null) {
                _sr.flipX = (_rb.velocity.x > 0.05f);
            }
        }

        private void OnTriggerEnter2D(Collider2D other) {
            NSMB.Player.PlayerHealth ph = other != null ? other.GetComponent<NSMB.Player.PlayerHealth>() : null;
            if (ph != null) {
                ph.TakeHit();
            }
        }
    }
}
