using UnityEngine;

namespace NSMB.Blocks {
    public sealed class BlockHitDetector : MonoBehaviour {
        private void OnCollisionEnter2D(Collision2D collision) {
            if (collision == null || collision.contacts == null || collision.contacts.Length == 0) {
                return;
            }

            NSMB.Player.PlayerMotor2D motor = collision.collider.GetComponent<NSMB.Player.PlayerMotor2D>();
            if (motor == null) {
                return;
            }

            // Player hits the block from below: require that the player is below the block and moving upwards.
            Rigidbody2D prb = motor.GetComponent<Rigidbody2D>();
            float vy = prb != null ? prb.velocity.y : 0f;
            if (vy <= 0.01f) {
                return;
            }

            if (motor.transform.position.y > transform.position.y) {
                return;
            }

            bool hitFromBelow = false;
            ContactPoint2D[] contacts = collision.contacts;
            for (int i = 0; i < contacts.Length; i++) {
                // From the block's perspective, a hit from below produces a downward normal.
                if (contacts[i].normal.y < -0.35f) {
                    hitFromBelow = true;
                    break;
                }
            }

            if (hitFromBelow) {
                BlockBump bump = GetComponent<BlockBump>();
                if (bump != null) {
                    bump.TriggerBump();
                }
            }
        }
    }
}
