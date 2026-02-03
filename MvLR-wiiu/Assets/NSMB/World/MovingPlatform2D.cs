using UnityEngine;

namespace NSMB.World {
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class MovingPlatform2D : MonoBehaviour {
        // If `path` is provided, the platform follows it; otherwise it moves at constant velocity.
        public Vector2 velocity;
        public StagePathNode[] path;
        public int loopMode; // 0=Clamp, 1=Loop, 2=PingPong (matches GenericMoverAsset)
        public float startOffsetSeconds;

        private Rigidbody2D _rb;
        private Vector2 _startPosition;
        private float _time;

        private void Awake() {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Start() {
            _startPosition = _rb.position;
            _time = startOffsetSeconds;
        }

        private void FixedUpdate() {
            if (_rb == null) {
                return;
            }

            if (path != null && path.Length >= 2) {
                float dt = Time.fixedDeltaTime;
                float t0 = _time;
                float t1 = _time + dt;
                Vector2 p0 = SamplePathPosition(t0);
                Vector2 p1 = SamplePathPosition(t1);
                Vector2 delta = p1 - p0;
                _rb.MovePosition(_rb.position + delta);
                _time = t1;
            } else {
                // Constant velocity in Unity units/sec.
                Vector2 delta = velocity * Time.fixedDeltaTime;
                _rb.MovePosition(_rb.position + delta);
            }
        }

        private Vector2 SamplePathPosition(float sampleSeconds) {
            if (path == null || path.Length == 0) {
                return _startPosition;
            }

            float total = 0f;
            for (int i = 0; i < path.Length; i++) {
                total += Mathf.Max(0f, path[i].travelDurationSeconds);
            }

            if (total <= 0.0001f) {
                return _startPosition + path[0].position;
            }

            float t = sampleSeconds;
            if (loopMode == 1) { // Loop
                t = Mathf.Repeat(t, total);
            } else if (loopMode == 0) { // Clamp
                t = Mathf.Clamp(t, 0f, total);
            } else if (loopMode == 2) { // PingPong
                t = Mathf.Repeat(t, total * 2f);
                if (t > total) {
                    t = (total * 2f) - t;
                }
            }

            for (int i = 0; i < path.Length; i++) {
                StagePathNode current = path[i];
                StagePathNode next = path[(i + 1) % path.Length];
                float seg = Mathf.Max(0f, current.travelDurationSeconds);

                if (t > seg) {
                    t -= seg;
                    continue;
                }

                float alpha = (seg <= 0.0001f) ? 1f : (t / seg);
                if (next.easeIn && next.easeOut) {
                    alpha = EaseInOut(alpha);
                } else if (next.easeIn) {
                    alpha = EaseIn(alpha);
                } else if (next.easeOut) {
                    alpha = EaseOut(alpha);
                }

                Vector2 local = Vector2.Lerp(current.position, next.position, alpha);
                return _startPosition + local;
            }

            return _startPosition;
        }

        private static float EaseIn(float t) {
            return t * t;
        }

        private static float EaseOut(float t) {
            float u = 1f - t;
            return 1f - (u * u);
        }

        private static float EaseInOut(float t) {
            // Smoothstep-like.
            return t * t * (3f - 2f * t);
        }
    }
}

