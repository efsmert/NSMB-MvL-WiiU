using UnityEngine;

namespace NSMB.Player {
    [RequireComponent(typeof(PlayerMotor2D))]
    public sealed class PlayerHealth : MonoBehaviour {
        public float invulnerableSeconds = 1.2f;
        public Vector3 respawnPosition = Vector3.zero;

        private float _invuln;
        private SpriteRenderer _renderer;

        private void Awake() {
            _renderer = GetComponent<SpriteRenderer>();
            respawnPosition = transform.position;
        }

        private void Update() {
            if (_invuln > 0f) {
                _invuln -= Time.deltaTime;
                if (_invuln < 0f) _invuln = 0f;

                if (_renderer != null) {
                    // Simple blink.
                    bool visible = ((int)(Time.time * 12f) % 2) == 0;
                    _renderer.enabled = visible;
                }
            } else {
                if (_renderer != null && !_renderer.enabled) {
                    _renderer.enabled = true;
                }
            }
        }

        public bool CanTakeHit() {
            return _invuln <= 0f;
        }

        public void TakeHit() {
            if (!CanTakeHit()) {
                return;
            }

            _invuln = invulnerableSeconds;

            NSMB.Core.GameRoot root = NSMB.Core.GameRoot.Instance;
            if (root != null) {
                NSMB.Audio.AudioManager audio = root.GetComponent<NSMB.Audio.AudioManager>();
                if (audio != null) {
                    audio.PlayOneShot(NSMB.Audio.SoundEffectId.Player_Sound_Collision, 0.9f);
                }
            }

            // For now: respawn at start.
            Transform t = transform;
            t.position = respawnPosition;

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null) {
                rb.velocity = Vector2.zero;
            }
        }
    }
}

