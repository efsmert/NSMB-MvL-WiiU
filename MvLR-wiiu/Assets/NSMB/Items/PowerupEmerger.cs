using UnityEngine;

namespace NSMB.Items {
    public sealed class PowerupEmerger : MonoBehaviour {
        public float riseDistance = 1.0f;
        public float riseSeconds = 0.35f;

        private Vector3 _start;
        private float _t;
        private Rigidbody2D _rb;
        private Behaviour[] _disableWhileEmerging;

        private void Awake() {
            _start = transform.position;
            _rb = GetComponent<Rigidbody2D>();
            if (_rb != null) {
                _rb.velocity = Vector2.zero;
                _rb.bodyType = RigidbodyType2D.Kinematic;
            }

            // Disable common behaviors that would fight our scripted rise.
            _disableWhileEmerging = GetComponents<Behaviour>();
            for (int i = 0; i < _disableWhileEmerging.Length; i++) {
                Behaviour b = _disableWhileEmerging[i];
                if (b == null || b == this) continue;
                if (b is NSMB.Items.MushroomPowerup || b is NSMB.Items.FireFlowerPowerup) {
                    b.enabled = false;
                }
            }
        }

        private void Update() {
            _t += Time.deltaTime;
            float p = (riseSeconds <= 0.0001f) ? 1f : Mathf.Clamp01(_t / riseSeconds);
            transform.position = _start + new Vector3(0f, Mathf.Lerp(0f, riseDistance, p), 0f);

            if (p >= 1f) {
                Finish();
            }
        }

        private void Finish() {
            if (_rb != null) {
                _rb.bodyType = RigidbodyType2D.Dynamic;
            }

            if (_disableWhileEmerging != null) {
                for (int i = 0; i < _disableWhileEmerging.Length; i++) {
                    Behaviour b = _disableWhileEmerging[i];
                    if (b == null || b == this) continue;
                    if (b is NSMB.Items.MushroomPowerup || b is NSMB.Items.FireFlowerPowerup) {
                        b.enabled = true;
                    }
                }
            }

            Destroy(this);
        }
    }
}

