using UnityEngine;

namespace NSMB.Blocks {
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class BreakableBlock : MonoBehaviour {
        public int scoreOnBreak = 50;

        // Called by BlockBump via SendMessage.
        private void OnBumped() {
            NSMB.Gameplay.GameManager gm = NSMB.Gameplay.GameManager.Instance;
            if (gm != null) {
                gm.AddScore(scoreOnBreak);
            }

            // Simple break effect: disable collider and destroy shortly after.
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) {
                col.enabled = false;
            }

            Destroy(gameObject);
        }
    }
}

