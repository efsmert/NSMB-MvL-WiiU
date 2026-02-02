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
        }

        private void BuildGround() {
            BuildStaticBox("Ground", new Vector2(0f, -2.5f), new Vector2(40f, 1f), new Color(0.25f, 0.7f, 0.3f, 1f));
        }

        private void BuildPlatform(Vector2 position, Vector2 size) {
            BuildStaticBox("Platform", position, size, new Color(0.45f, 0.45f, 0.45f, 1f));
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

        private void BuildCoin(Vector2 position) {
            GameObject go = new GameObject("Coin");
            go.transform.parent = transform;
            go.transform.position = new Vector3(position.x, position.y, 0f);

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.25f;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePlaceholderSprite();
            sr.color = new Color(1f, 0.9f, 0.2f, 1f);
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
            sr.sprite = CreatePlaceholderSprite();
            sr.color = spawnsMushroom ? new Color(1f, 0.75f, 0.1f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f);
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(1f, 1f);
            sr.sortingOrder = 0;

            NSMB.Blocks.BlockBump bump = go.AddComponent<NSMB.Blocks.BlockBump>();
            go.AddComponent<NSMB.Blocks.BlockHitDetector>();

            if (spawnsMushroom) {
                go.AddComponent<NSMB.Blocks.SpawnMushroomOnBump>();
            }
        }

        private void BuildStaticBox(string name, Vector2 position, Vector2 size, Color color) {
            GameObject go = new GameObject(name);
            go.transform.parent = transform;
            go.transform.position = new Vector3(position.x, position.y, 0f);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePlaceholderSprite();
            sr.color = color;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.sortingOrder = -10;
        }

        private static Sprite CreatePlaceholderSprite() {
            Texture2D tex = Texture2D.whiteTexture;
            Rect rect = new Rect(0f, 0f, tex.width, tex.height);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            return Sprite.Create(tex, rect, pivot, 100f, 0, SpriteMeshType.FullRect, new Vector4(0.5f, 0.5f, 0.5f, 0.5f));
        }

    }
}
