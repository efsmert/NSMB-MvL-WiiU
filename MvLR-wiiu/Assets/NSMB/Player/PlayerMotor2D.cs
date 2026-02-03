using UnityEngine;
using NSMB.Input;

namespace NSMB.Player {
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerMotor2D : MonoBehaviour {
        // Physics tuned from the Unity 6 reference project (MarioPlayerPhysicsInfo / MarioPlayerSystem),
        // but implemented using Unity's Rigidbody2D for the Wii U Unity 2017 port.

        [Header("Units")]
        [Tooltip("Scale factor from original simulation units to Unity world units. Original uses 1 unit = 32px/s; our world uses 16px per unit (PPU=16), so 2.0 matches original feel.")]
        public float originalToUnityScale = 2f;
        [Tooltip("Vertical scale multiplier relative to horizontal. Lower this if jumps feel too fast/tall in the current camera framing.")]
        public float verticalScaleMultiplier = 0.75f;

        [Header("Tuning (Feel)")]
        [Tooltip("Multiplier applied to stage-based ground acceleration. Increase to reach top speed quicker.")]
        public float accelMultiplier = 1.5f;
        [Tooltip("Multiplier applied to deceleration (button release / skid). Increase to stop quicker.")]
        public float decelMultiplier = 1.35f;
        [Tooltip("Air control multiplier relative to ground acceleration.")]
        public float airControlMultiplier = 0.75f;

        [Header("Walk / Run (Original Defaults)")]
        public int walkSpeedStage = 1;
        public int runSpeedStage = 3;
        public float[] walkMaxVelocity = new float[] { 0.9375f, 2.8125f, 4.21875f, 5.625f, 8.4375f };
        public float[] walkAcceleration = new float[] { 7.91015625f, 3.9550817f, 3.515625f, 2.6367188f, 84.375f };
        public float walkButtonReleaseDeceleration = 3.9550781f;
        public float skiddingMinimumVelocity = 4.6875f;
        public float skiddingDeceleration = 10.546875f;

        [Header("Jump (Original Defaults)")]
        public int jumpBufferFrames = 12;
        public int coyoteTimeFrames = 3;
        public float jumpVelocity = 6.62109375f;
        public float jumpSpeedBonusVelocity = 0.46875f;

        [Header("Gravity / Terminal (Original Defaults)")]
        public float[] gravityVelocity = new float[] { 4.16015625f, 2.109375f, 0f, -5.859375f };
        public float[] gravityAcceleration = new float[] { -7.03125f, -28.125f, -38.671875f, -28.125f, -38.671875f };
        public float terminalVelocity = -7.5f;

        [Header("Grounding")]
        public LayerMask groundMask = -1;
        public float groundCheckDistance = 0.10f;
        public float stickToGroundVelocity = -0.01f;

        [Header("Debug")]
        public bool debugHud;

        private Rigidbody2D _rb;
        private Collider2D _col;
        private ContactFilter2D _groundFilter;
        private RaycastHit2D[] _groundHits = new RaycastHit2D[4];
        private static PhysicsMaterial2D _noFrictionMaterial;

        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _isGrounded;
        private bool _jumpHeld;
        private bool _jumpedThisFrame;
        private bool _isSkidding;

        public bool IsGrounded {
            get { return _isGrounded; }
        }

        private void Awake() {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();

            // We apply gravity manually (stage-based like the original).
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.drag = 0f;
            _rb.angularDrag = 0f;

            // Prevent Physics2D materials from fighting our script-driven friction/accel.
            if (_noFrictionMaterial == null) {
                _noFrictionMaterial = new PhysicsMaterial2D("NSMB_NoFriction");
                _noFrictionMaterial.friction = 0f;
                _noFrictionMaterial.bounciness = 0f;
            }
            if (_col != null) {
                _col.sharedMaterial = _noFrictionMaterial;
            }

            _groundFilter = new ContactFilter2D();
            _groundFilter.useLayerMask = true;
            _groundFilter.layerMask = groundMask;
            _groundFilter.useTriggers = false;
        }

        private void Update() {
            if (LegacyInput.GetJumpDown()) {
                _jumpBufferTimer = FramesToSeconds(jumpBufferFrames);
            }

            _jumpHeld = LegacyInput.GetJump();
        }

        private void FixedUpdate() {
            _jumpedThisFrame = false;
            UpdateGrounded();
            TickTimers();
            ApplyHorizontalOriginal();
            ApplyJump();
            ApplyGravityOriginal();
        }

        private void TickTimers() {
            if (_jumpBufferTimer > 0f) {
                _jumpBufferTimer -= Time.fixedDeltaTime;
                if (_jumpBufferTimer < 0f) _jumpBufferTimer = 0f;
            }

            if (_isGrounded) {
                _coyoteTimer = FramesToSeconds(coyoteTimeFrames);
            } else if (_coyoteTimer > 0f) {
                _coyoteTimer -= Time.fixedDeltaTime;
                if (_coyoteTimer < 0f) _coyoteTimer = 0f;
            }
        }

        private void ApplyHorizontalOriginal() {
            if (_rb == null) {
                return;
            }

            float scale = Mathf.Max(0.0001f, originalToUnityScale);
            float invScale = 1f / scale;

            Vector2 move = LegacyInput.GetMovement();
            bool sprint = LegacyInput.GetSprint();

            // Simulate in "original units" then scale to Unity units when writing to Rigidbody2D.
            float xVel = _rb.velocity.x * invScale;
            float xVelAbs = Mathf.Abs(xVel);
            int sign = SignInt(xVel);

            bool hasMove = Mathf.Abs(move.x) > 0.01f;

            // Choose max stage based on sprint button.
            int maxStage = sprint ? runSpeedStage : walkSpeedStage;
            float max = GetArrayValue(walkMaxVelocity, maxStage, 0f);

            // Current speed stage (affects acceleration).
            int stage = GetSpeedStage(xVelAbs, walkMaxVelocity);
            float acc = GetArrayValue(walkAcceleration, stage, 0f) * Mathf.Max(0.01f, accelMultiplier);

            if (_isGrounded && hasMove) {
                int direction = move.x < 0f ? -1 : 1;
                bool reverse = sign != 0 && direction != sign;

                if (reverse && xVelAbs >= skiddingMinimumVelocity) {
                    // Skid: strong decel opposite the current velocity.
                    _isSkidding = true;
                    xVel += (-sign) * (skiddingDeceleration * Mathf.Max(0.01f, decelMultiplier)) * Time.fixedDeltaTime;

                    if (SignInt(xVel) != sign || Mathf.Abs(xVel) < 0.05f) {
                        xVel = 0f;
                        _isSkidding = false;
                    }
                } else {
                    _isSkidding = false;

                    // Prevent overshoot above max in a single step.
                    float maxAccThisStep = Mathf.Abs(max - xVelAbs) / Mathf.Max(0.0001f, Time.fixedDeltaTime);
                    float clampedAcc = Mathf.Clamp(acc, -maxAccThisStep, maxAccThisStep);

                    if (xVelAbs > max) {
                        clampedAcc = -clampedAcc;
                    }

                    xVel += direction * clampedAcc * Time.fixedDeltaTime;

                    // Clamp to max speed for this stage.
                    if (Mathf.Abs(xVel) > max) {
                        xVel = max * SignInt(xVel);
                    }
                }
            } else if (_isGrounded && !hasMove) {
                // Button release deceleration: reduce speed toward 0.
                if (xVelAbs > 0.001f) {
                    float dv = (walkButtonReleaseDeceleration * Mathf.Max(0.01f, decelMultiplier)) * Time.fixedDeltaTime;
                    float newAbs = Mathf.Max(0f, xVelAbs - dv);
                    xVel = newAbs * sign;
                } else {
                    xVel = 0f;
                }
                _isSkidding = false;
            } else {
                // Air control: keep it simple for now (use the same stage-based acceleration but weaker).
                if (hasMove) {
                    int direction = move.x < 0f ? -1 : 1;
                    float airAcc = acc * Mathf.Clamp01(airControlMultiplier);
                    xVel += direction * airAcc * Time.fixedDeltaTime;
                    if (Mathf.Abs(xVel) > max) {
                        xVel = max * SignInt(xVel);
                    }
                }
                _isSkidding = false;
            }

            _rb.velocity = new Vector2(xVel * scale, _rb.velocity.y);
        }

        private void ApplyJump() {
            bool canJump = _coyoteTimer > 0f;
            bool wantsJump = _jumpBufferTimer > 0f;

            if (wantsJump && canJump) {
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;

                float sx = Mathf.Max(0.0001f, originalToUnityScale);
                float sy = Mathf.Max(0.0001f, originalToUnityScale * Mathf.Max(0.01f, verticalScaleMultiplier));

                float xVelAbs = Mathf.Abs(_rb.velocity.x) * (1f / sx);

                // Match original alpha computation (MarioPlayerSystem.HandleJumping).
                float walkStage1 = GetArrayValue(walkMaxVelocity, 1, 0f);
                float alpha = Mathf.Clamp01(xVelAbs - walkStage1 + (walkStage1 * 0.50f));
                float newY = jumpVelocity + Mathf.Lerp(0f, jumpSpeedBonusVelocity, alpha);

                Vector2 v = _rb.velocity;
                v.y = newY * sy;
                _rb.velocity = v;

                _jumpedThisFrame = true;

                SendMessage("OnPlayerJump", SendMessageOptions.DontRequireReceiver);
            }
        }

        private void ApplyGravityOriginal() {
            if (_rb == null) {
                return;
            }

            float sy = Mathf.Max(0.0001f, originalToUnityScale * Mathf.Max(0.01f, verticalScaleMultiplier));
            float invY = 1f / sy;

            float yVel = _rb.velocity.y * invY;

            // Stick to ground when grounded and not jumping.
            if (_isGrounded && !_jumpedThisFrame && yVel <= 0f) {
                _rb.velocity = new Vector2(_rb.velocity.x, stickToGroundVelocity);
                return;
            }

            int stage = GetGravityStage(yVel, gravityVelocity);
            float acc = GetArrayValue(gravityAcceleration, stage, gravityAcceleration.Length > 0 ? gravityAcceleration[gravityAcceleration.Length - 1] : -28.125f);

            // Slow-rise check: if we're in the slow-rise stage and jump isn't held, use the strongest gravity.
            if (stage == 0 && !_jumpHeld) {
                acc = gravityAcceleration[gravityAcceleration.Length - 1];
            }

            yVel += acc * Time.fixedDeltaTime;

            // Terminal velocity clamp.
            if (yVel < terminalVelocity) {
                yVel = terminalVelocity;
            }

            _rb.velocity = new Vector2(_rb.velocity.x, yVel * sy);
        }

        private void UpdateGrounded() {
            if (_col == null) {
                _isGrounded = false;
                return;
            }

            // Use Collider2D.Cast rather than a single ray to avoid false negatives at ledges / during contact offsets.
            _groundFilter.layerMask = groundMask;
            int count = _col.Cast(Vector2.down, _groundFilter, _groundHits, Mathf.Max(0.02f, groundCheckDistance));
            _isGrounded = count > 0;
        }

        private static float FramesToSeconds(int frames) {
            // Reference project is 60fps simulation; convert to seconds for Unity's fixed timestep.
            return Mathf.Max(0f, frames) / 60f;
        }

        private static int GetSpeedStage(float xVelAbs, float[] maxArray) {
            if (maxArray == null || maxArray.Length == 0) {
                return 0;
            }
            float x = xVelAbs - 0.01f;
            for (int i = 0; i < maxArray.Length; i++) {
                if (x <= maxArray[i]) {
                    return i;
                }
            }
            return maxArray.Length - 1;
        }

        private static int GetGravityStage(float yVel, float[] maxArray) {
            if (maxArray == null || maxArray.Length == 0) {
                return 0;
            }
            for (int i = 0; i < maxArray.Length; i++) {
                if (yVel >= maxArray[i]) {
                    return i;
                }
            }
            return maxArray.Length;
        }

        private static float GetArrayValue(float[] arr, int index, float fallback) {
            if (arr == null || arr.Length == 0) {
                return fallback;
            }
            if (index < 0) index = 0;
            if (index >= arr.Length) index = arr.Length - 1;
            return arr[index];
        }

        private static int SignInt(float v) {
            if (v > 0f) return 1;
            if (v < 0f) return -1;
            return 0;
        }

        private void OnGUI() {
            if (!debugHud || _rb == null) {
                return;
            }

            float scale = Mathf.Max(0.0001f, originalToUnityScale);
            float sx = scale;
            float sy = Mathf.Max(0.0001f, originalToUnityScale * Mathf.Max(0.01f, verticalScaleMultiplier));
            float invX = 1f / sx;
            float invY = 1f / sy;

            float xVelAbsOrig = Mathf.Abs(_rb.velocity.x) * invX;
            float yVelOrig = _rb.velocity.y * invY;
            int speedStage = GetSpeedStage(xVelAbsOrig, walkMaxVelocity);
            int gravStage = GetGravityStage(yVelOrig, gravityVelocity);

            string text =
                "NSMB PlayerMotor2D\n" +
                "vx=" + _rb.velocity.x.ToString("F3") + " vy=" + _rb.velocity.y.ToString("F3") + " (unity)\n" +
                "vx=" + (_rb.velocity.x * invX).ToString("F3") + " vy=" + (_rb.velocity.y * invY).ToString("F3") + " (orig)\n" +
                "scaleX=" + sx.ToString("F2") + " scaleY=" + sy.ToString("F2") + "\n" +
                "grounded=" + _isGrounded + " skidding=" + _isSkidding + "\n" +
                "speedStage=" + speedStage + " gravityStage=" + gravStage + "\n" +
                "coyote=" + _coyoteTimer.ToString("F3") + " buffer=" + _jumpBufferTimer.ToString("F3");

            GUI.Label(new Rect(10, 10, 320, 100), text);
        }

        private void OnCollisionEnter2D(Collision2D collision) {
            if (collision == null || collision.collider == null) {
                return;
            }

            // Let enemies own their interaction logic where possible (Koopas have shell/kick states).
            NSMB.Enemies.KoopaEnemy koopaDirect = collision.collider.GetComponent<NSMB.Enemies.KoopaEnemy>();
            if (koopaDirect != null) {
                if (_rb == null) {
                    return;
                }

                // Running into an enemy while moving upward is damage.
                if (_rb.velocity.y > 0.01f) {
                    PlayerHealth phUpKoopa = GetComponent<PlayerHealth>();
                    if (phUpKoopa != null) {
                        phUpKoopa.TakeHit();
                    }
                    return;
                }

                koopaDirect.HandlePlayerCollision(this, collision);
                return;
            }

            NSMB.Enemies.GoombaEnemy goomba = collision.collider.GetComponent<NSMB.Enemies.GoombaEnemy>();
            if (goomba == null) {
                return;
            }

            if (_rb == null) {
                return;
            }

            // Simple stomp rule: player is above enemy and moving downward.
            if (_rb.velocity.y > 0.01f) {
                // Running into enemy while moving upward - treat as damage.
                PlayerHealth phUp = GetComponent<PlayerHealth>();
                if (phUp != null) {
                    phUp.TakeHit();
                }
                return;
            }

            float dy = transform.position.y - collision.collider.transform.position.y;
            if (dy > 0.25f) {
                if (goomba != null) {
                    goomba.TryStomp(this);
                }
            } else {
                PlayerHealth ph = GetComponent<PlayerHealth>();
                if (ph != null) {
                    ph.TakeHit();
                }
            }
        }
    }
}
