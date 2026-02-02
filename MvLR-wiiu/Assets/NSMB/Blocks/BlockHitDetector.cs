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

            // Player hits the block from below: normal points down (from block perspective, contact normal points from block to player).
            // We want "player is below block" => normal.y < -0.5.
            ContactPoint2D cp = collision.contacts[0];
            if (cp.normal.y < -0.5f) {
                BlockBump bump = GetComponent<BlockBump>();
                if (bump != null) {
                    bump.TriggerBump();
                }
            }
        }
    }
}

