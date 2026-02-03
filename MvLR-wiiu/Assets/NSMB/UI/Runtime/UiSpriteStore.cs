using System;
using System.Collections.Generic;
using UnityEngine;

namespace NSMB.UI {
    public sealed class UiSpriteStore {
        private readonly Dictionary<string, Sprite> _singleSprites = new Dictionary<string, Sprite>(StringComparer.InvariantCultureIgnoreCase);
        private readonly Dictionary<string, Sprite> _namedSprites = new Dictionary<string, Sprite>(StringComparer.InvariantCultureIgnoreCase);

        public Sprite LoadSingle(string resourcePath) {
            Sprite cached;
            if (_singleSprites.TryGetValue(resourcePath, out cached)) {
                #if UNITY_EDITOR
                if (cached != null) {
                    return cached;
                }
                #else
                return cached;
                #endif
            }

            Sprite sprite = Resources.Load(resourcePath) as Sprite;
            if (sprite == null) {
                // If the texture import settings aren't Sprite yet (common during porting),
                // fall back to loading the Texture2D and creating a Sprite at runtime.
                Texture2D tex = Resources.Load(resourcePath) as Texture2D;
                if (tex != null) {
                    tex.filterMode = FilterMode.Point;
                    Rect rect = new Rect(0f, 0f, tex.width, tex.height);
                    sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
                }
            }
            #if UNITY_EDITOR
            // Don't negatively cache missing sprites while iterating on imports.
            if (sprite != null) {
                _singleSprites[resourcePath] = sprite;
            }
            #else
            _singleSprites[resourcePath] = sprite;
            #endif
            return sprite;
        }

        public Sprite LoadFromSheet(string sheetResourcePath, string spriteName) {
            string key = sheetResourcePath + "@" + spriteName;

            Sprite cached;
            if (_namedSprites.TryGetValue(key, out cached)) {
                #if UNITY_EDITOR
                if (cached != null) {
                    return cached;
                }
                #else
                return cached;
                #endif
            }

            Sprite[] sprites = Resources.LoadAll<Sprite>(sheetResourcePath);
            Sprite found = null;
            if (sprites != null) {
                for (int i = 0; i < sprites.Length; i++) {
                    Sprite s = sprites[i];
                    if (s != null && string.Equals(s.name, spriteName, StringComparison.InvariantCultureIgnoreCase)) {
                        found = s;
                        break;
                    }
                }
            }

            #if UNITY_EDITOR
            if (found != null) {
                _namedSprites[key] = found;
            }
            #else
            _namedSprites[key] = found;
            #endif
            return found;
        }
    }
}
