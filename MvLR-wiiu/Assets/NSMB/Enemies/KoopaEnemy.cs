using UnityEngine;

namespace NSMB.Enemies {
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class KoopaEnemy : MonoBehaviour {
        private enum KoopaState {
            Walking = 0,
            ShellIdle = 1,
            ShellMoving = 2,
        }

        public float walkSpeed = 1.6f;
        public float shellSpeed = 6.0f;
        public float gravityScale = 3.5f;

        public int scoreOnStomp = 200;
        public int scoreOnKick = 400;

        public float stompBounceVelocity = 7.5f;
        public float wakeupSeconds = 6.0f;

        private Rigidbody2D _rb;
        private Collider2D _col;
        private SpriteRenderer _sr;
        private NSMB.Visual.SimpleSpriteAnimator _anim;

        private KoopaState _state = KoopaState.Walking;
        private int _dir = -1;
        private float _wakeupTimer;

        private void Awake() {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();
            _rb.gravityScale = gravityScale;
            _rb.freezeRotation = true;

            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) {
                _sr = gameObject.AddComponent<SpriteRenderer>();
            }

            EnsureVisuals();
        }

        private void EnsureVisuals() {
            if (_sr == null) {
                return;
            }

            _anim = GetComponent<NSMB.Visual.SimpleSpriteAnimator>();

            // Default walk frames.
            Sprite[] walk = NSMB.Visual.GameplaySprites.GetKoopaWalkFrames();
            if (walk != null && walk.Length > 0) {
                _sr.sprite = walk[0];
                if (_anim == null) {
                    _anim = gameObject.AddComponent<NSMB.Visual.SimpleSpriteAnimator>();
                }
                _anim.SetFrames(walk, 10f, true);
            }
        }

        private void FixedUpdate() {
            if (_rb == null) {
                return;
            }

            if (_state == KoopaState.Walking) {
                Vector2 v = _rb.velocity;
                v.x = _dir * walkSpeed;
                _rb.velocity = v;
                if (_sr != null) _sr.flipX = (_dir > 0);
                return;
            }

            if (_state == KoopaState.ShellIdle) {
                Vector2 v = _rb.velocity;
                v.x = 0f;
                _rb.velocity = v;
                return;
            }

            if (_state == KoopaState.ShellMoving) {
                Vector2 v = _rb.velocity;
                v.x = _dir * shellSpeed;
                _rb.velocity = v;
                return;
            }
        }

        private void Update() {
            if (_state == KoopaState.ShellIdle) {
                _wakeupTimer -= Time.deltaTime;
                if (_wakeupTimer <= 0f) {
                    ExitShellToWalk();
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision) {
            if (collision == null || collision.collider == null) {
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

            // Shell moving should bonk other enemies (simple parity).
            if (_state == KoopaState.ShellMoving) {
                GoombaEnemy goomba = collision.collider.GetComponent<GoombaEnemy>();
                if (goomba != null) {
                    goomba.KillByShell();
                }
            }
        }

        public bool TryStomp(NSMB.Player.PlayerMotor2D player) {
            if (_state == KoopaState.Walking) {
                EnterShellIdle();
                BouncePlayer(player);
                AddScore(scoreOnStomp);
                PlaySfx(NSMB.Audio.SoundEffectId.Enemy_Generic_Stomp, 0.9f);
                return true;
            }

            if (_state == KoopaState.ShellMoving) {
                // Stomp stops the shell.
                EnterShellIdle();
                BouncePlayer(player);
                AddScore(scoreOnStomp);
                PlaySfx(NSMB.Audio.SoundEffectId.Enemy_Generic_Stomp, 0.9f);
                return true;
            }

            // Shell idle stomp just bounces.
            BouncePlayer(player);
            return true;
        }

        public void HandlePlayerCollision(NSMB.Player.PlayerMotor2D player, Collision2D collision) {
            if (player == null) {
                return;
            }

            Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
            float pVy = (prb != null) ? prb.velocity.y : 0f;

            // Stomp if player is above and moving down.
            float dy = player.transform.position.y - transform.position.y;
            if (pVy <= 0.01f && dy > 0.25f) {
                TryStomp(player);
                return;
            }

            // Side interaction:
            if (_state == KoopaState.Walking) {
                // Damage player.
                NSMB.Player.PlayerHealth ph = player.GetComponent<NSMB.Player.PlayerHealth>();
                if (ph != null) ph.TakeHit();
                return;
            }

            if (_state == KoopaState.ShellIdle) {
                // Kick shell away from player.
                _dir = (player.transform.position.x < transform.position.x) ? 1 : -1;
                EnterShellMoving();
                AddScore(scoreOnKick);
                PlaySfx(NSMB.Audio.SoundEffectId.Enemy_Shell_Kick, 0.9f);
                return;
            }

            if (_state == KoopaState.ShellMoving) {
                // Shell hurts player.
                NSMB.Player.PlayerHealth ph2 = player.GetComponent<NSMB.Player.PlayerHealth>();
                if (ph2 != null) ph2.TakeHit();
            }
        }

        private void EnterShellIdle() {
            _state = KoopaState.ShellIdle;
            _wakeupTimer = Mathf.Max(0.5f, wakeupSeconds);

            if (_anim != null) {
                _anim.enabled = false;
            }

            if (_sr != null) {
                Sprite shell = NSMB.Content.ResourceSpriteCache.FindSprite(NSMB.Content.GameplayAtlasPaths.Koopa, "koopa_2");
                if (shell != null) {
                    _sr.sprite = shell;
                }
                _sr.flipX = false;
            }
        }

        private void EnterShellMoving() {
            _state = KoopaState.ShellMoving;
            _wakeupTimer = 0f;
            if (_anim != null) {
                _anim.enabled = false;
            }
        }

        private void ExitShellToWalk() {
            _state = KoopaState.Walking;

            // Restore walk animation.
            if (_sr != null) {
                Sprite[] walk = NSMB.Visual.GameplaySprites.GetKoopaWalkFrames();
                if (walk != null && walk.Length > 0) {
                    _sr.sprite = walk[0];
                    if (_anim == null) {
                        _anim = gameObject.AddComponent<NSMB.Visual.SimpleSpriteAnimator>();
                    }
                    _anim.enabled = true;
                    _anim.SetFrames(walk, 10f, true);
                }
            }
        }

        private void BouncePlayer(NSMB.Player.PlayerMotor2D player) {
            if (player == null) {
                return;
            }
            Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
            if (prb == null) {
                return;
            }
            Vector2 pv = prb.velocity;
            pv.y = stompBounceVelocity;
            prb.velocity = pv;
        }

        private static void AddScore(int s) {
            NSMB.Gameplay.GameManager gm = NSMB.Gameplay.GameManager.Instance;
            if (gm != null) {
                gm.AddScore(s);
            }
        }

        private static void PlaySfx(NSMB.Audio.SoundEffectId id, float vol) {
            NSMB.Core.GameRoot root = NSMB.Core.GameRoot.Instance;
            if (root == null) return;
            NSMB.Audio.AudioManager audio = root.GetComponent<NSMB.Audio.AudioManager>();
            if (audio != null) {
                audio.PlayOneShot(id, vol);
            }
        }
    }
}
