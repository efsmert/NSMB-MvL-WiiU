using UnityEngine;

namespace NSMB.Enemies {
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class BulletBillProjectile : MonoBehaviour {
        public int direction = 1;
        public float speed = 6f;
        public float lifetimeSeconds = 8f;

        private Rigidbody2D _rb;
        private float _t;
        private SpriteRenderer _sr;

        private void Awake() {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb != null) {
                _rb.gravityScale = 0f;
                _rb.freezeRotation = true;
            }

            Transform graphics;
            NSMB.Visual.SimpleSpriteAnimator anim;
            Unity6EnemyPrototypes.ApplyBulletBill(gameObject, out graphics, out _sr, out anim);
            if (_sr != null) {
                _sr.flipX = (direction < 0);
            }
        }

        private void FixedUpdate() {
            if (_rb != null) {
                Vector2 v = _rb.velocity;
                v.x = direction * speed;
                v.y = 0f;
                _rb.velocity = v;
            } else {
                transform.position += new Vector3(direction * speed * Time.fixedDeltaTime, 0f, 0f);
            }
            if (_sr != null) {
                _sr.flipX = (direction < 0);
            }
        }

        private void Update() {
            _t += Time.deltaTime;
            if (lifetimeSeconds > 0f && _t >= lifetimeSeconds) {
                Destroy(gameObject);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision) {
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other) {
            Destroy(gameObject);
        }
    }
}
