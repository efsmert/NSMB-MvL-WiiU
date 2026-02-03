using UnityEngine;

namespace NSMB.World {
    public sealed class TestLevelBootstrap : MonoBehaviour {
        private bool _built;

        private void Start() {
            if (_built) {
                return;
            }
            _built = true;

            BuildGround();
            BuildPlatform(new Vector2(2f, 1.5f), new Vector2(3f, 0.5f));
            BuildPlatform(new Vector2(-3f, 2.5f), new Vector2(2f, 0.5f));
            BuildPlatform(new Vector2(0f, 4f), new Vector2(4f, 0.5f));

            BuildCoins();
            BuildMoreBlocks();
            BuildEnemies();

            // Test scene convenience: spawn the player if the menu-driven flow isn't being used.
            NSMB.WiiU.WiiUBootstrap.EnsurePlayerForFlow();
        }

        private void BuildGround() {
            Sprite tile = NSMB.Visual.GameplaySprites.GetPlatformTile(0);
            BuildTiledStatic("Ground", new Vector2(0f, -2.5f), new Vector2(40f, 1f), tile, -10);
        }

        private void BuildPlatform(Vector2 position, Vector2 size) {
            Sprite tile = NSMB.Visual.GameplaySprites.GetPlatformTile(1);
            BuildTiledStatic("Platform", position, size, tile, -5);
        }

        private void BuildCoins() {
            BuildCoin(new Vector2(0f, -1.2f));
            BuildCoin(new Vector2(2f, 2.4f));
            BuildCoin(new Vector2(-3f, 3.4f));
            BuildCoin(new Vector2(0f, 5.0f));

            BuildBumpBlock(new Vector2(1.5f, 0.5f), true);
        }

        private void BuildMoreBlocks() {
            BuildBumpBlock(new Vector2(-1.5f, 0.5f), false);
            BuildBumpBlock(new Vector2(-0.5f, 0.5f), false);
            BuildBumpBlock(new Vector2(0.5f, 0.5f), false);
        }

        private void BuildEnemies() {
            BuildGoomba(new Vector2(4.0f, -1.4f));
            BuildGoomba(new Vector2(-4.5f, -1.4f));
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

        private void BuildBumpBlock(Vector2 position, bool spawnsMushroom) {
            GameObject go = new GameObject(spawnsMushroom ? "QuestionBlock" : "Block");
            go.transform.parent = transform;
            go.transform.position = new Vector3(position.x, position.y, 0f);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.color = Color.white;
            sr.sortingOrder = 0;

            go.AddComponent<NSMB.Blocks.BlockBump>();
            go.AddComponent<NSMB.Blocks.BlockHitDetector>();

            if (spawnsMushroom) {
                Sprite[] frames = NSMB.Visual.GameplaySprites.GetQuestionBlockFrames();
                if (frames != null && frames.Length > 0) {
                    sr.sprite = frames[0];
                    NSMB.Visual.SimpleSpriteAnimator anim = go.AddComponent<NSMB.Visual.SimpleSpriteAnimator>();
                    anim.SetFrames(frames, 8f, true);
                }
            } else {
                Sprite brick = NSMB.Visual.GameplaySprites.GetPlatformTile(2);
                if (brick != null) {
                    sr.sprite = brick;
                }
            }

            if (spawnsMushroom) {
                go.AddComponent<NSMB.Blocks.SpawnMushroomOnBump>();
            }
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

            // Create child SpriteRenderers to tile a single sprite across a box.
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
