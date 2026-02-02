using UnityEngine;

namespace NSMB.Blocks {
    public sealed class SpawnMushroomOnBump : MonoBehaviour {
        public Vector2 spawnOffset = new Vector2(0f, 1.2f);

        private bool _used;

        private void OnEnable() {
            _used = false;
        }

        // Called by BlockBump via SendMessage
        private void OnBumped() {
            if (_used) {
                return;
            }
            _used = true;

            Vector3 spawnPos = transform.position + new Vector3(spawnOffset.x, spawnOffset.y, 0f);

            GameObject mush = new GameObject("Mushroom");
            mush.transform.position = spawnPos;

            Rigidbody2D rb = mush.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.5f;
            rb.freezeRotation = true;

            CircleCollider2D col = mush.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;
            col.isTrigger = false;

            SpriteRenderer sr = mush.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePlaceholderSprite();
            sr.color = new Color(1f, 0.2f, 0.2f, 1f);
            sr.sortingOrder = 0;

            mush.AddComponent<NSMB.Items.MushroomPowerup>();
        }

        private static Sprite CreatePlaceholderSprite() {
            Texture2D tex = Texture2D.whiteTexture;
            Rect rect = new Rect(0f, 0f, tex.width, tex.height);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            return Sprite.Create(tex, rect, pivot, 100f);
        }
    }
}

