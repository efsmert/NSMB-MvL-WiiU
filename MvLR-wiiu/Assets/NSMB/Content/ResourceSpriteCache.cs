using System;
using System.Collections.Generic;
using UnityEngine;

namespace NSMB.Content {
    public static class ResourceSpriteCache {
        private static readonly Dictionary<string, Sprite[]> SpriteCache = new Dictionary<string, Sprite[]>(StringComparer.InvariantCultureIgnoreCase);
        private static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>(StringComparer.InvariantCultureIgnoreCase);

        public static Sprite[] LoadAllSprites(string resourcesPath) {
            if (string.IsNullOrEmpty(resourcesPath)) {
                return new Sprite[0];
            }

            Sprite[] cached;
            if (SpriteCache.TryGetValue(resourcesPath, out cached) && cached != null) {
                // Don't "negatively cache" missing sprites in the editor: we frequently import/reslice assets
                // while the editor is running, and we want Resources.LoadAll to see new results.
                #if UNITY_EDITOR
                if (cached.Length > 0) {
                    return cached;
                }
                #else
                return cached;
                #endif
            }

            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcesPath);
            if (sprites != null && sprites.Length > 0) {
                SpriteCache[resourcesPath] = sprites;
                return sprites;
            }

            // Fallback: texture isn't imported as multiple sprites yet. Create a single sprite at runtime.
            Texture2D tex = LoadTexture(resourcesPath);
            if (tex == null) {
                #if !UNITY_EDITOR
                SpriteCache[resourcesPath] = new Sprite[0];
                #endif
                return new Sprite[0];
            }

            Rect rect = new Rect(0f, 0f, tex.width, tex.height);
            Sprite single = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
            Sprite[] singleArr = new Sprite[] { single };
            SpriteCache[resourcesPath] = singleArr;
            return singleArr;
        }

        public static Sprite FindSprite(string resourcesPath, string spriteName) {
            if (string.IsNullOrEmpty(spriteName)) {
                return null;
            }

            Sprite[] sprites = LoadAllSprites(resourcesPath);
            for (int i = 0; i < sprites.Length; i++) {
                Sprite s = sprites[i];
                if (s != null && string.Equals(s.name, spriteName, StringComparison.InvariantCultureIgnoreCase)) {
                    return s;
                }
            }

            return null;
        }

        public static Sprite[] FindSpritesByPrefix(string resourcesPath, string prefix) {
            if (string.IsNullOrEmpty(prefix)) {
                return new Sprite[0];
            }

            Sprite[] sprites = LoadAllSprites(resourcesPath);
            List<Sprite> matches = new List<Sprite>();

            for (int i = 0; i < sprites.Length; i++) {
                Sprite s = sprites[i];
                if (s == null || string.IsNullOrEmpty(s.name)) {
                    continue;
                }

                if (s.name.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)) {
                    matches.Add(s);
                }
            }

            matches.Sort(CompareSpriteByNumericSuffix);
            return matches.ToArray();
        }

        private static int CompareSpriteByNumericSuffix(Sprite a, Sprite b) {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            int ai = ExtractTrailingInt(a.name);
            int bi = ExtractTrailingInt(b.name);
            if (ai != int.MinValue && bi != int.MinValue) {
                return ai.CompareTo(bi);
            }

            return string.Compare(a.name, b.name, StringComparison.InvariantCultureIgnoreCase);
        }

        private static int ExtractTrailingInt(string name) {
            if (string.IsNullOrEmpty(name)) {
                return int.MinValue;
            }

            int underscore = name.LastIndexOf('_');
            if (underscore < 0 || underscore + 1 >= name.Length) {
                return int.MinValue;
            }

            string suffix = name.Substring(underscore + 1);
            int value;
            return int.TryParse(suffix, out value) ? value : int.MinValue;
        }

        private static Texture2D LoadTexture(string resourcesPath) {
            Texture2D cached;
            if (TextureCache.TryGetValue(resourcesPath, out cached) && cached != null) {
                return cached;
            }

            Texture2D tex = Resources.Load(resourcesPath, typeof(Texture2D)) as Texture2D;
            TextureCache[resourcesPath] = tex;
            return tex;
        }
    }
}
