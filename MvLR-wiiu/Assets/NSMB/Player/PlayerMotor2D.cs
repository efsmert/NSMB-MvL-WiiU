using UnityEngine;
using NSMB.Input;

namespace NSMB.Player {
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerMotor2D : MonoBehaviour {
        public float maxWalkSpeed = 6.5f;
        public float maxSprintSpeed = 9.5f;
        public float acceleration = 70f;
        public float deceleration = 85f;
        public float airAcceleration = 45f;
        public float airDeceleration = 35f;

        public float gravityScale = 3.5f;
        public float jumpVelocity = 11.5f;
        public float jumpCutMultiplier = 0.5f;
        public float coyoteTime = 0.08f;
        public float jumpBufferTime = 0.10f;

        public LayerMask groundMask = -1;
        public float groundCheckDistance = 0.10f;

        private Rigidbody2D _rb;
        private Collider2D _col;

        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _isGrounded;
        private bool _jumpHeld;

        public bool IsGrounded {
            get { return _isGrounded; }
        }

        private void Awake() {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();

            _rb.gravityScale = gravityScale;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void Update() {
            if (LegacyInput.GetJumpDown()) {
                _jumpBufferTimer = jumpBufferTime;
            }

            _jumpHeld = LegacyInput.GetJump();
        }

        private void FixedUpdate() {
            UpdateGrounded();
            TickTimers();
            ApplyHorizontal();
            ApplyJump();
        }

        private void TickTimers() {
            if (_jumpBufferTimer > 0f) {
                _jumpBufferTimer -= Time.fixedDeltaTime;
                if (_jumpBufferTimer < 0f) _jumpBufferTimer = 0f;
            }

            if (_isGrounded) {
                _coyoteTimer = coyoteTime;
            } else if (_coyoteTimer > 0f) {
                _coyoteTimer -= Time.fixedDeltaTime;
                if (_coyoteTimer < 0f) _coyoteTimer = 0f;
            }
        }

        private void ApplyHorizontal() {
            Vector2 move = LegacyInput.GetMovement();
            float desiredSpeed = move.x * (LegacyInput.GetSprint() ? maxSprintSpeed : maxWalkSpeed);

            float accel = _isGrounded ? acceleration : airAcceleration;
            float decel = _isGrounded ? deceleration : airDeceleration;

            float speedDiff = desiredSpeed - _rb.velocity.x;
            float rate = (Mathf.Abs(desiredSpeed) > 0.01f) ? accel : decel;

            float movement = speedDiff * rate;
            float newVx = _rb.velocity.x + (movement * Time.fixedDeltaTime);

            // clamp to prevent overshoot
            if ((speedDiff > 0f && newVx > desiredSpeed) || (speedDiff < 0f && newVx < desiredSpeed)) {
                newVx = desiredSpeed;
            }

            _rb.velocity = new Vector2(newVx, _rb.velocity.y);
        }

        private void ApplyJump() {
            bool canJump = _coyoteTimer > 0f;
            bool wantsJump = _jumpBufferTimer > 0f;

            if (wantsJump && canJump) {
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;

                Vector2 v = _rb.velocity;
                v.y = jumpVelocity;
                _rb.velocity = v;

                SendMessage("OnPlayerJump", SendMessageOptions.DontRequireReceiver);
            }

            // variable jump height (cut)
            if (!_jumpHeld && _rb.velocity.y > 0.01f) {
                _rb.velocity = new Vector2(_rb.velocity.x, _rb.velocity.y * jumpCutMultiplier);
            }
        }

        private void UpdateGrounded() {
            Bounds b = _col.bounds;
            Vector2 origin = new Vector2(b.center.x, b.min.y + 0.01f);
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundMask);
            _isGrounded = hit.collider != null;
        }
    }
}

