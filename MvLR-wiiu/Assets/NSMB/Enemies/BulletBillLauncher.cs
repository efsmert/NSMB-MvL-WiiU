using UnityEngine;

namespace NSMB.Enemies {
    public sealed class BulletBillLauncher : MonoBehaviour {
        public float fireIntervalSeconds = 4f;
        public float minimumShootRadius = 0.0f;
        public float maximumShootRadius = 9.0f;
        public Vector2 bulletSpawnOffset = Vector2.zero;

        public float bulletSpeed = 6.0f;
        public float bulletLifetimeSeconds = 8.0f;

        private float _cooldown;

        private void Update() {
            if (fireIntervalSeconds <= 0f) {
                return;
            }

            if (_cooldown > 0f) {
                _cooldown -= Time.deltaTime;
                if (_cooldown > 0f) {
                    return;
                }
                _cooldown = 0f;
            }

            NSMB.Player.PlayerMotor2D player = Object.FindObjectOfType(typeof(NSMB.Player.PlayerMotor2D)) as NSMB.Player.PlayerMotor2D;
            if (player == null) {
                _cooldown = fireIntervalSeconds;
                return;
            }

            Vector2 from = (Vector2)transform.position;
            Vector2 to = (Vector2)player.transform.position;
            float d = Vector2.Distance(from, to);
            if (d < minimumShootRadius || d > maximumShootRadius) {
                return;
            }

            SpawnBullet(to.x < from.x ? -1 : 1);
            _cooldown = fireIntervalSeconds;
        }

        private void SpawnBullet(int dir) {
            GameObject go = new GameObject("BulletBill");
            go.transform.position = transform.position + (Vector3)bulletSpawnOffset;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.75f, 0.5f);
            col.isTrigger = false;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 0;
            sr.color = Color.white;
            sr.sprite = NSMB.Content.ResourceSpriteCache.FindSprite(NSMB.Content.GameplayAtlasPaths.BulletBill, "bullet-bill_0");

            BulletBillProjectile proj = go.AddComponent<BulletBillProjectile>();
            proj.direction = dir;
            proj.speed = bulletSpeed;
            proj.lifetimeSeconds = bulletLifetimeSeconds;
        }
    }
}

