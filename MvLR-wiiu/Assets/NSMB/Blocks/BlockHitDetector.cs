using UnityEngine;

namespace NSMB.Blocks {
    public sealed class BlockHitDetector : MonoBehaviour {
        public float minUpVelocity = 0.10f;
        public float contactEpsilon = 0.08f;
        public float debounceSeconds = 0.08f;

        private float _lastBumpTime = -999f;

        private void OnCollisionEnter2D(Collision2D collision) {
            TryHandleBump(collision);
        }

        private void OnCollisionStay2D(Collision2D collision) {
            TryHandleBump(collision);
        }

        private void OnTriggerEnter2D(Collider2D other) {
            TryHandleBumpTrigger(other);
        }

        private void OnTriggerStay2D(Collider2D other) {
            TryHandleBumpTrigger(other);
        }

	        private void TryHandleBump(Collision2D collision) {
            if (collision == null) {
                return;
            }
            if (Time.time - _lastBumpTime < debounceSeconds) {
                return;
            }

            ContactPoint2D[] contacts = collision.contacts;
            if (contacts == null || contacts.Length == 0) {
                return;
            }

            NSMB.Player.PlayerMotor2D motor = collision.collider != null ? collision.collider.GetComponentInParent<NSMB.Player.PlayerMotor2D>() : null;
            if (motor == null) {
                return;
            }

            Rigidbody2D playerRb = collision.collider != null ? collision.collider.attachedRigidbody : null;
            if (playerRb == null) {
                playerRb = motor.GetComponent<Rigidbody2D>();
            }
	            if (playerRb == null) {
	                return;
	            }

	            // Only count upward hits (jumping into the underside).
	            // In Unity 2017, Rigidbody2D.velocity can already be zeroed by the solver at this callback,
	            // so we accept either positive relativeVelocity OR a near-zero post-solve velocity while airborne.
	            float relUp = collision.relativeVelocity.y;
	            float vy = playerRb.velocity.y;
	            bool goingUp = (relUp > minUpVelocity) || (vy > minUpVelocity);
	            if (!goingUp && motor != null && !motor.IsGrounded) {
	                goingUp = (vy >= -0.05f) && (relUp >= -0.05f);
	            }
	            if (!goingUp) {
	                return;
	            }

            // Prefer the collider that actually participated in this collision.
            Collider2D blockCol = collision.otherCollider != null ? collision.otherCollider : GetComponent<Collider2D>();
            if (blockCol == null) {
                return;
            }
            float bottom = blockCol.bounds.min.y;
            float centerY = blockCol.bounds.center.y;

	            bool fromBelow = false;
	            for (int i = 0; i < contacts.Length; i++) {
	                ContactPoint2D cp = contacts[i];

	                // When this script is on the block, cp.normal should generally point from the block to the player.
	                // Prefer the normal check; keep a point heuristic for edge cases.
	                if (cp.normal.y < -0.5f && cp.point.y <= centerY) {
	                    fromBelow = true;
	                    break;
	                }
	                if (cp.point.y <= bottom + contactEpsilon) {
	                    fromBelow = true;
	                    break;
	                }
	            }
	            if (!fromBelow) {
	                return;
	            }

            BlockBump bump = GetComponent<BlockBump>();
            if (bump == null) {
                return;
            }

            _lastBumpTime = Time.time;
            bump.TriggerBump(motor);
        }

	        private void TryHandleBumpTrigger(Collider2D other) {
            if (other == null) {
                return;
            }
            if (Time.time - _lastBumpTime < debounceSeconds) {
                return;
            }

            NSMB.Player.PlayerMotor2D motor = other.GetComponentInParent<NSMB.Player.PlayerMotor2D>();
            if (motor == null) {
                return;
            }

            Rigidbody2D playerRb = other.attachedRigidbody;
            if (playerRb == null) {
                playerRb = motor.GetComponent<Rigidbody2D>();
            }
            if (playerRb == null) {
                return;
            }

	            bool goingUp = playerRb.velocity.y > minUpVelocity;
	            if (!goingUp && motor != null && !motor.IsGrounded) {
	                goingUp = playerRb.velocity.y >= -0.05f;
	            }
	            if (!goingUp) {
	                return;
	            }

            Collider2D blockCol = GetComponent<Collider2D>();
            if (blockCol == null) {
                return;
            }

            float bottom = blockCol.bounds.min.y;
            float centerY = blockCol.bounds.center.y;

            // Trigger overlaps don't provide contact points. Use bounds heuristics: player's top edge at the block's
            // bottom edge, and player is mostly below the block.
            bool fromBelow = other.bounds.max.y <= bottom + contactEpsilon;
            if (!fromBelow) {
                fromBelow = other.bounds.center.y <= centerY && other.bounds.max.y <= bottom + (contactEpsilon * 2f);
            }
            if (!fromBelow) {
                return;
            }

            BlockBump bump = GetComponent<BlockBump>();
            if (bump == null) {
                return;
            }

            _lastBumpTime = Time.time;
            bump.TriggerBump(motor);
        }
    }
}
