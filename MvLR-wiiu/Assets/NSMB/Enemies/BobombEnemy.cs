using UnityEngine;

namespace NSMB.Enemies {
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class BobombEnemy : MonoBehaviour {
        public float moveSpeed = 1.2f;
        public float gravityScale = 3.5f;
        public float explodeDelaySeconds = 0.15f;
        public float explodeRadius = 1.25f;

        public int scoreOnExplode = 200;

        private Rigidbody2D _rb;
        private int _dir = -1;
        private bool _exploding;
        private float _explodeTimer;

        private SpriteRenderer _sr;

        private void Awake() {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb != null) {
                _rb.gravityScale = gravityScale;
                _rb.freezeRotation = true;
            }

            NSMB.Visual.SimpleSpriteAnimator anim;
            Transform graphics;
            Unity6EnemyPrototypes.ApplyBobomb(gameObject, out graphics, out _sr, out anim);
        }

        private void FixedUpdate() {
            if (_exploding || _rb == null) {
                return;
            }

            Vector2 v = _rb.velocity;
            v.x = _dir * moveSpeed;
            _rb.velocity = v;

            if (_sr != null) {
                _sr.flipX = (_dir > 0);
            }
        }

        private void Update() {
            if (!_exploding) {
                return;
            }

            _explodeTimer -= Time.deltaTime;
            if (_explodeTimer <= 0f) {
                ExplodeNow();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision) {
            if (_exploding || collision == null) {
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

        public void HandlePlayerCollision(NSMB.Player.PlayerMotor2D player, Collision2D collision) {
            if (_exploding || player == null) {
                return;
            }

            Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
            float pVy = (prb != null) ? prb.velocity.y : 0f;
            float dy = player.transform.position.y - transform.position.y;

            // If player stomped, bounce them and arm explosion (like many Mario games).
            if (pVy <= 0.01f && dy > 0.25f) {
                if (prb != null) {
                    Vector2 pv = prb.velocity;
                    pv.y = 7.5f;
                    prb.velocity = pv;
                }
                BeginExplode();
                return;
            }

            // Side contact: damage player and explode.
            NSMB.Player.PlayerHealth ph = player.GetComponent<NSMB.Player.PlayerHealth>();
            if (ph != null) {
                ph.TakeHit();
            }
            BeginExplode();
        }

        private void BeginExplode() {
            if (_exploding) {
                return;
            }

            _exploding = true;
            _explodeTimer = Mathf.Max(0.01f, explodeDelaySeconds);

            // Stop moving.
            if (_rb != null) {
                _rb.velocity = Vector2.zero;
                _rb.isKinematic = true;
            }

            Collider2D c = GetComponent<Collider2D>();
            if (c != null) {
                c.enabled = false;
            }
        }

        private void ExplodeNow() {
            AddScore(scoreOnExplode);
            PlaySfx(NSMB.Audio.SoundEffectId.Enemy_Generic_Stomp, 0.7f);

            // Damage player if in radius.
            NSMB.Player.PlayerMotor2D player = FindObjectOfType<NSMB.Player.PlayerMotor2D>();
            if (player != null) {
                float d = Vector2.Distance(player.transform.position, transform.position);
                if (d <= explodeRadius) {
                    NSMB.Player.PlayerHealth ph = player.GetComponent<NSMB.Player.PlayerHealth>();
                    if (ph != null) {
                        ph.TakeHit();
                    }
                }
            }

            Destroy(gameObject);
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
