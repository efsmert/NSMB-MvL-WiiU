using UnityEngine;

namespace NSMB.World {
    // Simple second level to prove stage selection + level loading.
    public sealed class FlatLevelBootstrap : MonoBehaviour {
        private bool _built;

        private void Start() {
            if (_built) return;
            _built = true;

            BuildGround();
            BuildStairs();
            BuildCoins();
            BuildEnemies();
        }

        private void BuildGround() {
            Sprite tile = NSMB.Visual.GameplaySprites.GetPlatformTile(0);
            BuildTiledStatic("Ground", new Vector2(0f, -2.5f), new Vector2(60f, 1f), tile, -10);
        }

        private void BuildStairs() {
            for (int i = 0; i < 8; i++) {
                Sprite tile = NSMB.Visual.GameplaySprites.GetPlatformTile(2);
                BuildTiledStatic("Step_" + i, new Vector2(6f + i * 1.0f, -1.5f + i * 0.5f), new Vector2(1f, 1f), tile, -5);
            }
        }

        private void BuildCoins() {
            for (int i = 0; i < 10; i++) {
                BuildCoin(new Vector2(-6f + i * 1.5f, 0.5f));
            }
        }

        private void BuildEnemies() {
            BuildGoomba(new Vector2(3f, -1.4f));
            BuildGoomba(new Vector2(10f, -1.4f));
        }

        private void BuildCoin(Vector2 position) {
            GameObject go = new GameObject("Coin");
            go.transform.parent = transform;
            go.transform.position = new Vector3(position.x, position.y, 0f);

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.25f;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.color = Color.white;
            sr.sortingOrder = 0;

            go.AddComponent<NSMB.Items.CoinPickup>();
        }

        private void BuildGoomba(Vector2 position) {
            GameObject go = new GameObject("Goomba");
            go.transform.parent = transform;
            go.transform.position = new Vector3(position.x, position.y, 0f);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.5f;
            rb.freezeRotation = true;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.8f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.color = Color.white;
            sr.sortingOrder = 0;

            go.AddComponent<NSMB.Enemies.GoombaEnemy>();
        }

        private void BuildTiledStatic(string name, Vector2 position, Vector2 size, Sprite tile, int sortingOrder) {
            GameObject go = new GameObject(name);
            go.transform.parent = transform;
            go.transform.position = new Vector3(position.x, position.y, 0f);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            if (tile == null) {
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = CreatePlaceholderSprite();
                sr.color = new Color(0.45f, 0.45f, 0.45f, 1f);
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = size;
                sr.sortingOrder = sortingOrder;
                return;
            }

            Vector2 tileSize = tile.bounds.size;
            if (tileSize.x <= 0f || tileSize.y <= 0f) {
                tileSize = new Vector2(1f, 1f);
            }

            int tilesX = Mathf.Max(1, Mathf.CeilToInt(size.x / tileSize.x));
            int tilesY = Mathf.Max(1, Mathf.CeilToInt(size.y / tileSize.y));

            float startX = position.x - (size.x * 0.5f);
            float startY = position.y - (size.y * 0.5f);

            for (int y = 0; y < tilesY; y++) {
                for (int x = 0; x < tilesX; x++) {
                    GameObject child = new GameObject("Tile_" + x + "_" + y);
                    child.transform.parent = go.transform;

                    float px = startX + (x * tileSize.x) + (tileSize.x * 0.5f);
                    float py = startY + (y * tileSize.y) + (tileSize.y * 0.5f);
                    child.transform.position = new Vector3(px, py, 0f);

                    SpriteRenderer sr = child.AddComponent<SpriteRenderer>();
                    sr.sprite = tile;
                    sr.color = Color.white;
                    sr.sortingOrder = sortingOrder;
                }
            }
        }

        private static Sprite CreatePlaceholderSprite() {
            Texture2D tex = Texture2D.whiteTexture;
            Rect rect = new Rect(0f, 0f, tex.width, tex.height);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            return Sprite.Create(tex, rect, pivot, 100f, 0, SpriteMeshType.FullRect, new Vector4(0.5f, 0.5f, 0.5f, 0.5f));
        }
    }
}
