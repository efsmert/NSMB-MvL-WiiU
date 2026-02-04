using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NSMB.UI {
    // Renders a short HUD string using sprites from a sheet (e.g. NSMB/UI/ui).
    // Intended for pixel-perfect HUD counters like "x 0/10".
    public sealed class HudSpriteText : MonoBehaviour {
        public float scale = 2f;
        public float spacing = 1f;
        public bool rightAligned;

        private UiSpriteStore _sprites;
        private string _sheetPath;
        private readonly List<Image> _glyphs = new List<Image>(32);

        public void Initialize(UiSpriteStore sprites, string sheetPath) {
            _sprites = sprites;
            _sheetPath = sheetPath;
        }

        public void SetText(string text) {
            if (_sprites == null || string.IsNullOrEmpty(_sheetPath)) {
                return;
            }

            if (text == null) {
                text = string.Empty;
            }

            EnsureGlyphCount(text.Length);

            float x = 0f;
            for (int i = 0; i < _glyphs.Count; i++) {
                Image img = _glyphs[i];
                if (img == null) continue;

                if (i >= text.Length) {
                    img.enabled = false;
                    continue;
                }

                char ch = text[i];
                if (ch == ' ') {
                    img.enabled = false;
                    // Keep a visible gap for spaces so numbers don't collapse together.
                    float spaceW = (8f * Mathf.Max(0.01f, scale));
                    if (rightAligned) {
                        x += spaceW + spacing;
                    } else {
                        x += spaceW + spacing;
                    }
                    continue;
                }

                Sprite s = ResolveSprite(ch);
                if (s == null) {
                    img.enabled = false;
                    x += spacing;
                    continue;
                }

                img.enabled = true;
                img.sprite = s;
                img.preserveAspect = true;
                img.raycastTarget = false;

                RectTransform rt = img.rectTransform;
                float anchorX = rightAligned ? 1f : 0f;
                rt.anchorMin = new Vector2(anchorX, 0.5f);
                rt.anchorMax = new Vector2(anchorX, 0.5f);
                rt.pivot = new Vector2(anchorX, 0.5f);

                Vector2 pxSize = s.rect.size * Mathf.Max(0.01f, scale);
                rt.sizeDelta = pxSize;
                if (rightAligned) {
                    x += pxSize.x;
                    rt.anchoredPosition = new Vector2(-x, 0f);
                    x += spacing;
                } else {
                    rt.anchoredPosition = new Vector2(x, 0f);
                    x += pxSize.x + spacing;
                }
            }
        }

        private void EnsureGlyphCount(int count) {
            count = Mathf.Max(0, count);
            while (_glyphs.Count < count) {
                GameObject go = UiRuntimeUtil.CreateUiObject("Glyph", transform);
                Image img = go.AddComponent<Image>();
                img.raycastTarget = false;
                _glyphs.Add(img);
            }
        }

        private Sprite ResolveSprite(char ch) {
            // ui.png contains: hudnumber_0..9, hudnumber_x, hudnumber_slash
            // plus additional small icons like hudnumber_coin, hudnumber_star.
            string name = null;

            if (ch >= '0' && ch <= '9') {
                name = "hudnumber_" + ch;
            } else if (ch == 'x' || ch == 'X') {
                name = "hudnumber_x";
            } else if (ch == '/') {
                name = "hudnumber_slash";
            } else if (ch == ':') {
                name = "hudnumber_colon";
            }

            if (string.IsNullOrEmpty(name)) {
                return null;
            }

            return _sprites.LoadFromSheet(_sheetPath, name);
        }
    }
}
