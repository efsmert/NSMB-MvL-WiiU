using UnityEngine;

namespace NSMB.Enemies {
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class GoombaEnemy : MonoBehaviour {
        public float moveSpeed = 1.4f;
        public float gravityScale = 3.5f;
        public int scoreOnStomp = 200;

        public float stompBounceVelocity = 7.5f;

        private Rigidbody2D _rb;
        private int _dir = -1;
        private bool _dead;
        private Transform _graphics;
        private SpriteRenderer _sr;
        private Vector3 _baseGraphicsScale;

        private void Awake() {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = gravityScale;
            _rb.freezeRotation = true;

            NSMB.Visual.SimpleSpriteAnimator anim;
            Unity6EnemyPrototypes.ApplyGoomba(gameObject, out _graphics, out _sr, out anim);
            _baseGraphicsScale = (_graphics != null) ? _graphics.localScale : Vector3.one;
        }

        private void FixedUpdate() {
            if (_dead) {
                return;
            }

            Vector2 v = _rb.velocity;
            v.x = _dir * moveSpeed;
            _rb.velocity = v;
            if (_sr != null) {
                _sr.flipX = (_dir > 0);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision) {
            if (_dead || collision == null || collision.collider == null) {
                return;
            }

            // Turn around on walls/obstacles.
            ContactPoint2D[] contacts = collision.contacts;
            for (int i = 0; i < contacts.Length; i++) {
                ContactPoint2D cp = contacts[i];
                if (Mathf.Abs(cp.normal.x) > 0.5f) {
                    _dir = -_dir;
                    break;
                }
            }
        }

        public void KillByShell() {
            if (_dead) {
                return;
            }

            _dead = true;
            AddScore(scoreOnStomp);
            PlaySfx(NSMB.Audio.SoundEffectId.Enemy_Generic_Stomp, 0.8f);

            // Flatten visual and remove collisions; no player bounce.
            if (_graphics != null) {
                _graphics.localScale = new Vector3(_baseGraphicsScale.x, _baseGraphicsScale.y * 0.2f, _baseGraphicsScale.z);
            }

            _rb.velocity = Vector2.zero;
            _rb.isKinematic = true;
            Collider2D c = GetComponent<Collider2D>();
            if (c != null) {
                c.enabled = false;
            }

            Destroy(gameObject, 0.4f);
        }

        public bool TryStomp(NSMB.Player.PlayerMotor2D player) {
            if (_dead || player == null) {
                return false;
            }

            _dead = true;

            // Score + SFX
            AddScore(scoreOnStomp);
            PlaySfx(NSMB.Audio.SoundEffectId.Enemy_Generic_Stomp, 0.9f);

            // Flatten visual (placeholder-friendly)
            if (_graphics != null) {
                _graphics.localScale = new Vector3(_baseGraphicsScale.x, _baseGraphicsScale.y * 0.2f, _baseGraphicsScale.z);
            }

            // Stop moving & disable collider so the player doesn't get snagged.
            _rb.velocity = Vector2.zero;
            _rb.isKinematic = true;
            Collider2D c = GetComponent<Collider2D>();
            if (c != null) {
                c.enabled = false;
            }

            // Bounce the player upward.
            Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
            if (prb != null) {
                Vector2 pv = prb.velocity;
                pv.y = stompBounceVelocity;
                prb.velocity = pv;
            }

            Destroy(gameObject, 0.6f);
            return true;
        }

        private static void AddScore(int s) {
            NSMB.Gameplay.GameManager gm = NSMB.Gameplay.GameManager.Instance;
            if (gm != null) {
                gm.AddScore(s);
            }
        }

        private static void PlaySfx(NSMB.Audio.SoundEffectId id, float vol) {
            NSMB.Core.GameRoot root = NSMB.Core.GameRoot.Instance;
            if (root != null) {
                NSMB.Audio.AudioManager audio = root.GetComponent<NSMB.Audio.AudioManager>();
                if (audio != null) {
                    audio.PlayOneShot(id, vol);
                }
            }
        }
    }
}
