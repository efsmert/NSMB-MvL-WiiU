using UnityEngine;

namespace NSMB.Items {
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class MushroomPowerup : MonoBehaviour {
        public float moveSpeed = 2.2f;
        public int scoreValue = 1000;
        public float collectSfxVolume = 0.8f;

        private Rigidbody2D _rb;
        private int _dir = 1;

        private void Awake() {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 3.5f;
            _rb.freezeRotation = true;
        }

        private void FixedUpdate() {
            Vector2 v = _rb.velocity;
            v.x = _dir * moveSpeed;
            _rb.velocity = v;
        }

        private void OnCollisionEnter2D(Collision2D collision) {
            if (collision != null && collision.collider != null && collision.collider.GetComponent<NSMB.Player.PlayerMotor2D>() != null) {
                Collect();
                return;
            }

            // bounce off walls
            ContactPoint2D[] contacts = collision != null ? collision.contacts : null;
            int count = contacts != null ? contacts.Length : 0;
            for (int i = 0; i < count; i++) {
                ContactPoint2D cp = contacts[i];
                if (Mathf.Abs(cp.normal.x) > 0.5f) {
                    _dir = -_dir;
                    break;
                }
            }
        }

        private void Collect() {

            NSMB.Gameplay.GameManager gm = NSMB.Gameplay.GameManager.Instance;
            if (gm != null) {
                gm.AddScore(scoreValue);
            }

            NSMB.Core.GameRoot root = NSMB.Core.GameRoot.Instance;
            if (root != null) {
                NSMB.Audio.AudioManager audio = root.GetComponent<NSMB.Audio.AudioManager>();
                if (audio != null) {
                    audio.PlayOneShot(NSMB.Audio.SoundEffectId.World_Block_Powerup, collectSfxVolume);
                }
            }

            Destroy(gameObject);
        }
    }
}
