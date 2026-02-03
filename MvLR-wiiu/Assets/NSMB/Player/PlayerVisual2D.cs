using UnityEngine;

namespace NSMB.Player {
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlayerVisual2D : MonoBehaviour {
        private SpriteRenderer _sr;

        private void Awake() {
            _sr = GetComponent<SpriteRenderer>();
            EnsureVisible();
        }

        private void EnsureVisible() {
            if (_sr == null) {
                return;
            }

            // If some other system replaces the sprite later, keep it.
            if (_sr.sprite != null && _sr.sprite.texture != null && _sr.sprite.texture.width > 1 && _sr.sprite.texture.height > 1) {
                return;
            }

            // Keep sorting above terrain by default.
            if (_sr.sortingOrder < 5) {
                _sr.sortingOrder = 10;
            }

            // Ensure we have something visible even if the bootstrap sprite is missing.
            if (_sr.sprite == null || _sr.sprite.texture == Texture2D.whiteTexture) {
                _sr.sprite = CreateFallbackSprite();
            }
            _sr.color = Color.white;
        }

        private static Sprite CreateFallbackSprite() {
            const int size = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    Color c;
                    if (x == 0 || y == 0 || x == size - 1 || y == size - 1) {
                        c = Color.black;
                    } else if (y >= 11) {
                        c = new Color(0.80f, 0.10f, 0.10f, 1f);
                    } else {
                        c = new Color(0.95f, 0.80f, 0.65f, 1f);
                    }
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.0f), 16f);
        }
    }
}

