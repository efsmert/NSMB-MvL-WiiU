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

            NSMB.Core.GameRoot root = NSMB.Core.GameRoot.Instance;
            if (root != null) {
                NSMB.Audio.AudioManager audio = root.GetComponent<NSMB.Audio.AudioManager>();
                if (audio != null) {
                    audio.PlayOneShot(NSMB.Audio.SoundEffectId.World_Block_Break, 0.85f);
                }
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
