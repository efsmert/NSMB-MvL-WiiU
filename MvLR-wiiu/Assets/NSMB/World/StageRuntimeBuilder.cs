using System;
using System.Collections.Generic;
using UnityEngine;

namespace NSMB.World {
    public static class StageRuntimeBuilder {
        private const float PixelsPerUnit = 16f;
        private const float PixelUnit = 1f / PixelsPerUnit;
        private const float TileVisualOverlap = 1.001f;
        private const int BackgroundSortingOrder = -2000;
        private const int ForegroundSortingOrder = BackgroundSortingOrder + 100;
        // Clouds render above the background sky gradient, but below the foreground bushes layer.
        private const int CloudBigSortingOrder = BackgroundSortingOrder + 20;
        private const int CloudSmallSortingOrder = BackgroundSortingOrder + 30;
        private const float BackgroundZ = 10f;
        private const float ForegroundZ = BackgroundZ - 0.5f;
        private const float CloudBigZ = BackgroundZ - 0.15f;
        private const float CloudSmallZ = BackgroundZ - 0.10f;
        private const float BackgroundForegroundYOffsetFactor = 0.20f;

        private static readonly Dictionary<string, Sprite> _backgroundSpriteCache = new Dictionary<string, Sprite>(StringComparer.InvariantCultureIgnoreCase);

        public static void Build(StageDefinition def, Transform parent) {
            Build(def, parent, true, true);
        }

        public static void Build(StageDefinition def, Transform parent, bool buildEntities, bool buildColliders) {
            if (def == null || parent == null) {
                return;
            }

            GameObject root = new GameObject("Stage_" + (string.IsNullOrEmpty(def.stageKey) ? "Unknown" : def.stageKey));
            root.transform.parent = parent;
            root.transform.localPosition = Vector3.zero;
            root.transform.localScale = Vector3.one;

            BuildBackground(def, root.transform);
            BuildTiles(def, root.transform, buildColliders);
            if (buildEntities) {
                BuildEntities(def, root.transform);
                EnsurePitKillZone(def, root.transform);
            }

            if (def.isWrappingLevel) {
                StageWrap2D wrap = root.AddComponent<StageWrap2D>();

                Vector2 min;
                Vector2 max;
                if (def.wrapMin != def.wrapMax) {
                    wrap.ConfigureBounds(def.wrapMin, def.wrapMax);
                } else if (TryGetWrappingWorldBounds(def, out min, out max) || TryGetStageTileWorldBounds(def, out min, out max)) {
                    wrap.ConfigureBounds(min, max);
                } else if (def.cameraMin != def.cameraMax) {
                    // Fallback: use imported camera clamp bounds if tile bounds are unavailable.
                    wrap.ConfigureBounds(def.cameraMin, def.cameraMax);
                }
            }
        }

        private static void EnsurePitKillZone(StageDefinition def, Transform stageRoot) {
            if (def == null || stageRoot == null) {
                return;
            }

            PitKillZone2D existing = stageRoot.GetComponent<PitKillZone2D>();
            if (existing == null) {
                existing = stageRoot.gameObject.AddComponent<PitKillZone2D>();
            }

            float baseY = (def.cameraMin != def.cameraMax) ? Mathf.Min(def.cameraMin.y, def.cameraMax.y) : 0f;
            // Roughly matches the Unity 6 feel: falling below the visible play space kills you.
            existing.killY = baseY - 8f;
        }

        private static void BuildBackground(StageDefinition def, Transform stageRoot) {
            if (def == null || stageRoot == null) {
                return;
            }

            string bgName = GetBackgroundNameForStage(def.stageKey);
            if (string.IsNullOrEmpty(bgName)) {
                return;
            }

            string resourcePath = "NSMB/LevelBackgrounds/" + bgName;
            TryApplyCameraClearColor(bgName);

            // Determine horizontal tiling span.
            float left = 0f;
            float right = 0f;
            bool hasBounds = false;
            if (def.wrapMin != def.wrapMax) {
                left = Mathf.Min(def.wrapMin.x, def.wrapMax.x);
                right = Mathf.Max(def.wrapMin.x, def.wrapMax.x);
                hasBounds = true;
            } else {
                Vector2 min;
                Vector2 max;
                if (TryGetWrappingWorldBounds(def, out min, out max) || TryGetStageTileWorldBounds(def, out min, out max)) {
                    left = min.x;
                    right = max.x;
                    hasBounds = true;
                }
            }
            if (!hasBounds || right <= left + 0.0001f) {
                // Fallback span: a few screens wide.
                left = -32f;
                right = 32f;
            }

            float baseY = (def.cameraMin != def.cameraMax) ? Mathf.Min(def.cameraMin.y, def.cameraMax.y) : 0f;

            GameObject bgRoot = new GameObject("Background");
            bgRoot.transform.parent = stageRoot;
            bgRoot.transform.localPosition = new Vector3(0f, 0f, 0f);
            bgRoot.transform.localScale = Vector3.one;

            Sprite backSprite;
            Sprite foreSprite;
            if (TryGetCompositeBackgroundSprites(resourcePath, bgName, out backSprite, out foreSprite) && backSprite != null && foreSprite != null) {
                Transform backLayer = new GameObject("Back").transform;
                backLayer.parent = bgRoot.transform;
                backLayer.localPosition = Vector3.zero;
                backLayer.localScale = Vector3.one;

                BuildBackgroundTiled(backSprite, backLayer, left, right, baseY, BackgroundZ, BackgroundSortingOrder, "BG_");

                float camMinY = baseY;
                float camMaxY = (def.cameraMin != def.cameraMax) ? Mathf.Max(def.cameraMin.y, def.cameraMax.y) : (baseY + 9f);
                float camCenterHintY = (def.cameraMin != def.cameraMax) ? Mathf.Clamp(def.spawnPoint.y, camMinY, camMaxY) : def.spawnPoint.y;
                float orthoHalfHeight = GetMainCameraOrthoHalfHeight();
                TryBuildScrollingClouds(bgRoot.transform, bgName, left, right, camMinY, camMaxY, camCenterHintY, orthoHalfHeight);

                Transform foregroundLayer = new GameObject("Foreground").transform;
                foregroundLayer.parent = bgRoot.transform;
                foregroundLayer.localPosition = Vector3.zero;
                foregroundLayer.localScale = Vector3.one;

                float foreY = baseY - (foreSprite.bounds.size.y * BackgroundForegroundYOffsetFactor);
                BuildBackgroundTiled(foreSprite, foregroundLayer, left, right, foreY, ForegroundZ, ForegroundSortingOrder, "FG_");
                return;
            }

            Sprite sprite = GetOrCreateBackgroundSprite(resourcePath);
            if (sprite == null) {
                return;
            }

            BuildBackgroundTiled(sprite, bgRoot.transform, left, right, baseY, BackgroundZ, BackgroundSortingOrder, "BG_");
            float camMinYFallback = baseY;
            float camMaxYFallback = (def.cameraMin != def.cameraMax) ? Mathf.Max(def.cameraMin.y, def.cameraMax.y) : (baseY + 9f);
            float camCenterHintYFallback = (def.cameraMin != def.cameraMax) ? Mathf.Clamp(def.spawnPoint.y, camMinYFallback, camMaxYFallback) : def.spawnPoint.y;
            float orthoHalfHeightFallback = GetMainCameraOrthoHalfHeight();
            TryBuildScrollingClouds(bgRoot.transform, bgName, left, right, camMinYFallback, camMaxYFallback, camCenterHintYFallback, orthoHalfHeightFallback);
        }

        private static void TryBuildScrollingClouds(Transform bgRoot, string bgName, float left, float right, float camMinY, float camMaxY, float camCenterHintY, float orthoHalfHeight) {
            if (bgRoot == null || string.IsNullOrEmpty(bgName)) {
                return;
            }

            // Unity 6 grass/sky stages have 2 scrolling cloud layers.
            bool wantsClouds =
                string.Equals(bgName, "grass-sky", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(bgName, "sky-bg", StringComparison.InvariantCultureIgnoreCase);

            if (!wantsClouds) {
                return;
            }

            // Avoid duplicating if a background prefab already spawned clouds.
            if (bgRoot.Find("Clouds") != null) {
                return;
            }

            // Use the shared clouds texture from level backgrounds.
            const string cloudsResourcePath = "NSMB/LevelBackgrounds/clouds";
            // Always create a FullRect sprite for tiling (avoids Unity warning when drawMode=Tiled).
            Sprite cloudSprite = GetOrCreateRuntimeSpriteFromTexture(cloudsResourcePath, cloudsResourcePath + "|ppu100|center|repeat|fullrect", new Vector2(0.5f, 0.5f), 100f, TextureWrapMode.Repeat);
            if (cloudSprite == null) {
                return;
            }

            float spriteWidth = cloudSprite.bounds.size.x;
            // Match Unity 6 style distribution by repeating a small deterministic pattern over a scroll period.
            float smallPeriod = spriteWidth * 3.0f;
            float bigPeriod = spriteWidth * 4.0f;

            float margin = Mathf.Max(smallPeriod, bigPeriod) * 2f;
            float l = left - margin;
            float r = right + margin;
            float centerX = (l + r) * 0.5f;

            // Place clouds relative to the *starting camera view* (camera center + ortho size),
            // because StageDefinition camera bounds represent the camera *center* clamp, not the top edge.
            // This makes clouds sit near the top of the screen across different pixel-perfect scales.
            if (orthoHalfHeight <= 0.0001f) {
                orthoHalfHeight = 5f;
            }

            float viewTopY = camCenterHintY + orthoHalfHeight;
            float maxViewTopY = camMaxY + orthoHalfHeight;
            float minViewTopY = camMinY + orthoHalfHeight;

            float maxY = maxViewTopY - 0.05f;
            float minY = minViewTopY + Mathf.Max(0.5f, orthoHalfHeight * 0.35f);

            // Big row is near the very top; small row sits mid-high (closer to Unity 6 screenshot).
            float bigYOffset = Mathf.Max(0.85f, orthoHalfHeight * 0.16f);
            float smallYOffset = Mathf.Max(2.50f, orthoHalfHeight * 0.48f);
            float bigY = Mathf.Clamp(viewTopY - bigYOffset, minY, maxY);
            float smallY = Mathf.Clamp(viewTopY - smallYOffset, minY, maxY);

            // Keep small below big even under clamping.
            if (smallY > bigY - 0.20f) {
                smallY = bigY - 0.20f;
            }

            Transform cloudsRoot = new GameObject("Clouds").transform;
            cloudsRoot.parent = bgRoot;
            cloudsRoot.localPosition = Vector3.zero;
            cloudsRoot.localScale = Vector3.one;

            // Small clouds (fainter, slower) - matches Unity 6 defaults.
            Transform small = new GameObject("SmallClouds").transform;
            small.parent = cloudsRoot;
            small.position = new Vector3(centerX, smallY, CloudSmallZ);
            small.localScale = Vector3.one;
            ScrollingSpriteLoop2D smallScroll = small.gameObject.AddComponent<ScrollingSpriteLoop2D>();
            smallScroll.speed = -0.1f;
            smallScroll.tileWorldWidth = smallPeriod;

            // Pattern: two loose rows of small clouds.
            BuildCloudPatternStrip(
                cloudSprite,
                small,
                l,
                r,
                centerX,
                1f,
                smallPeriod,
                new float[] { 0.08f, 0.32f, 0.58f, 0.84f },
                new float[] { 0.00f, -0.18f, -0.06f, -0.24f },
                new bool[] { false, true, false, true },
                CloudSmallSortingOrder,
                0.27058825f
            );

            // Big clouds (stronger, faster) - scaled up.
            Transform big = new GameObject("BigClouds").transform;
            big.parent = cloudsRoot;
            big.position = new Vector3(centerX, bigY, CloudBigZ);
            big.localScale = new Vector3(2f, 2f, 1f);
            ScrollingSpriteLoop2D bigScroll = big.gameObject.AddComponent<ScrollingSpriteLoop2D>();
            bigScroll.speed = -0.2f;
            bigScroll.tileWorldWidth = bigPeriod;

            // Pattern: top row of large puffs (fewer, more opaque).
            BuildCloudPatternStrip(
                cloudSprite,
                big,
                l,
                r,
                centerX,
                2f,
                bigPeriod,
                new float[] { 0.18f, 0.64f },
                new float[] { 0.00f, -0.10f },
                new bool[] { false, true },
                CloudBigSortingOrder,
                0.8352941f
            );
        }

        private static void BuildCloudPatternStrip(Sprite sprite, Transform layerRoot, float left, float right, float centerX, float scaleX, float periodWorld, float[] patternXFracs, float[] patternYOffsets, bool[] patternFlipX, int sortingOrder, float alpha) {
            if (sprite == null || layerRoot == null) {
                return;
            }

            if (patternXFracs == null || patternYOffsets == null || patternXFracs.Length == 0 || patternYOffsets.Length == 0) {
                return;
            }

            int patternCount = Mathf.Min(patternXFracs.Length, patternYOffsets.Length);
            if (patternFlipX != null) {
                patternCount = Mathf.Min(patternCount, patternFlipX.Length);
            }

            float span = Mathf.Max(0.01f, right - left);
            int cellCount = Mathf.Max(1, Mathf.CeilToInt(span / Mathf.Max(0.01f, periodWorld)) + 4);

            float invScaleX = 1f / Mathf.Max(0.01f, scaleX);

            for (int cell = 0; cell < cellCount; cell++) {
                float cellStartX = left + (cell * periodWorld);

                for (int p = 0; p < patternCount; p++) {
                    float worldX = cellStartX + (patternXFracs[p] * periodWorld);
                    float localX = (worldX - centerX) * invScaleX;
                    float localY = patternYOffsets[p] * invScaleX;

                    GameObject go = new GameObject("C_" + cell + "_" + p);
                    go.transform.parent = layerRoot;
                    go.transform.localPosition = new Vector3(localX, localY, 0f);
                    go.transform.localScale = Vector3.one;

                    SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    sr.sortingOrder = sortingOrder;
                    sr.color = new Color(1f, 1f, 1f, alpha);
                    if (patternFlipX != null) {
                        sr.flipX = patternFlipX[p];
                    }
                }
            }
        }

        private static void TryApplyCameraClearColor(string bgName) {
            if (string.IsNullOrEmpty(bgName)) {
                return;
            }

            Color clearColor;
            if (!TryGetCameraClearColorForBackground(bgName, out clearColor)) {
                return;
            }

            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null) {
                cam = UnityEngine.Object.FindObjectOfType(typeof(UnityEngine.Camera)) as UnityEngine.Camera;
            }
            if (cam == null) {
                return;
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = clearColor;
        }

        private static float GetMainCameraOrthoHalfHeight() {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null) {
                cam = UnityEngine.Object.FindObjectOfType(typeof(UnityEngine.Camera)) as UnityEngine.Camera;
            }
            if (cam == null) {
                return 5f;
            }
            if (!cam.orthographic) {
                return 5f;
            }
            return Mathf.Max(0.01f, cam.orthographicSize);
        }

        private static bool TryGetCameraClearColorForBackground(string bgName, out Color clearColor) {
            // Match Unity 6 camera clear color to the top row of the gradient for these skies,
            // so any visible "gap" above the background sprite blends seamlessly.
            if (string.Equals(bgName, "grass-sky", StringComparison.InvariantCultureIgnoreCase)) {
                clearColor = new Color(32f / 255f, 152f / 255f, 248f / 255f, 1f);
                return true;
            }
            if (string.Equals(bgName, "sky-bg", StringComparison.InvariantCultureIgnoreCase)) {
                clearColor = new Color(40f / 255f, 160f / 255f, 248f / 255f, 1f);
                return true;
            }

            clearColor = Color.black;
            return false;
        }

        private static void ApplyTintAlpha(Transform root, float a) {
            if (root == null) {
                return;
            }

            SpriteRenderer[] srs = root.GetComponentsInChildren<SpriteRenderer>(true);
            if (srs == null) {
                return;
            }

            for (int i = 0; i < srs.Length; i++) {
                SpriteRenderer sr = srs[i];
                if (sr == null) continue;
                Color c = sr.color;
                c.a = a;
                sr.color = c;
            }
        }

        private static void BuildBackgroundTiled(Sprite sprite, Transform parent, float left, float right, float baseY, float z, int sortingOrder, string namePrefix) {
            if (sprite == null || parent == null) {
                return;
            }

            float spriteWidth = sprite.bounds.size.x;
            if (spriteWidth <= 0.0001f) {
                return;
            }

            int count = Mathf.Max(1, Mathf.CeilToInt((right - left) / spriteWidth) + 2);
            for (int i = 0; i < count; i++) {
                GameObject go = new GameObject(namePrefix + i);
                go.transform.parent = parent;
                float x = left + (i * spriteWidth);
                go.transform.position = new Vector3(x, baseY, z);

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = sortingOrder;
                sr.color = Color.white;
            }
        }

        private static bool TryGetCompositeBackgroundSprites(string resourcePath, string baseName, out Sprite back, out Sprite foreground) {
            back = null;
            foreground = null;

            if (string.IsNullOrEmpty(resourcePath) || string.IsNullOrEmpty(baseName)) {
                return false;
            }

            Sprite[] slices = Resources.LoadAll<Sprite>(resourcePath);
            if (slices == null || slices.Length < 2) {
                return false;
            }

            string backName = baseName + "_0";
            string foreName = baseName + "_1";

            Sprite backSrc = null;
            Sprite foreSrc = null;
            for (int i = 0; i < slices.Length; i++) {
                Sprite s = slices[i];
                if (s == null) continue;
                if (backSrc == null && string.Equals(s.name, backName, StringComparison.InvariantCultureIgnoreCase)) {
                    backSrc = s;
                } else if (foreSrc == null && string.Equals(s.name, foreName, StringComparison.InvariantCultureIgnoreCase)) {
                    foreSrc = s;
                }
                if (backSrc != null && foreSrc != null) break;
            }

            if (backSrc == null || foreSrc == null) {
                return false;
            }

            back = GetOrCreateBackgroundSliceSprite(resourcePath, backSrc);
            foreground = GetOrCreateBackgroundSliceSprite(resourcePath, foreSrc);
            return (back != null && foreground != null);
        }

        private static Sprite GetOrCreateBackgroundSliceSprite(string resourcePath, Sprite sourceSprite) {
            if (string.IsNullOrEmpty(resourcePath) || sourceSprite == null) {
                return null;
            }

            string cacheKey = resourcePath + "|slice|" + sourceSprite.name;
            Sprite cached;
            if (_backgroundSpriteCache.TryGetValue(cacheKey, out cached) && cached != null) {
                return cached;
            }

            Texture2D tex = sourceSprite.texture;
            if (tex == null) {
                return null;
            }

            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Rect rect = sourceSprite.rect;
            // Use a bottom pivot so the slice sits on cameraMin.y like Unity 6.
            Sprite sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            _backgroundSpriteCache[cacheKey] = sprite;
            return sprite;
        }

        private static Sprite GetOrCreateRuntimeSpriteFromTexture(string resourcePath, string cacheKey, Vector2 pivot) {
            return GetOrCreateRuntimeSpriteFromTexture(resourcePath, cacheKey, pivot, PixelsPerUnit, TextureWrapMode.Clamp);
        }

        private static Sprite GetOrCreateRuntimeSpriteFromTexture(string resourcePath, string cacheKey, Vector2 pivot, float pixelsPerUnit, TextureWrapMode wrapMode) {
            if (string.IsNullOrEmpty(resourcePath) || string.IsNullOrEmpty(cacheKey)) {
                return null;
            }

            Sprite cached;
            if (_backgroundSpriteCache.TryGetValue(cacheKey, out cached) && cached != null) {
                return cached;
            }

            Texture2D tex = Resources.Load(resourcePath, typeof(Texture2D)) as Texture2D;
            if (tex == null) {
                Sprite s = Resources.Load(resourcePath, typeof(Sprite)) as Sprite;
                if (s != null) {
                    tex = s.texture;
                }
            }
            if (tex == null) {
                return null;
            }

            tex.filterMode = FilterMode.Point;
            tex.wrapMode = wrapMode;

            Rect rect = new Rect(0f, 0f, tex.width, tex.height);
            Sprite sprite = Sprite.Create(tex, rect, pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
            _backgroundSpriteCache[cacheKey] = sprite;
            return sprite;
        }

        private static Sprite GetOrCreateBackgroundSprite(string resourcePath) {
            if (string.IsNullOrEmpty(resourcePath)) {
                return null;
            }

            Sprite cached;
            if (_backgroundSpriteCache.TryGetValue(resourcePath, out cached) && cached != null) {
                return cached;
            }

            // Some background textures are imported as "Multiple" sprites (split into top/bottom slices).
            // Loading the first sprite is unstable (ordering isn't guaranteed) and can yield a cropped result.
            // Always create a full-rect sprite from the underlying texture at the correct PPU.
            Texture2D tex = null;
            UnityEngine.Object asset = Resources.Load(resourcePath);
            if (asset != null) {
                Texture2D asTex = asset as Texture2D;
                if (asTex != null) {
                    tex = asTex;
                } else {
                    Sprite asSprite = asset as Sprite;
                    if (asSprite != null) {
                        tex = asSprite.texture;
                    }
                }
            }

            if (tex == null) {
                Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
                if (sprites != null && sprites.Length > 0 && sprites[0] != null) {
                    tex = sprites[0].texture;
                }
            }

            if (tex == null) {
                return null;
            }

            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Rect rect = new Rect(0f, 0f, tex.width, tex.height);
            // Use a bottom pivot so the background sits on cameraMin.y like the original.
            Sprite sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            _backgroundSpriteCache[resourcePath] = sprite;
            return sprite;
        }

        private static string GetBackgroundNameForStage(string stageKey) {
            string key = stageKey ?? string.Empty;
            key = key.ToLowerInvariant();

            if (key.IndexOf("beach", StringComparison.InvariantCultureIgnoreCase) >= 0) return "beach-bg";
            if (key.IndexOf("bonus", StringComparison.InvariantCultureIgnoreCase) >= 0) return "bonus-bg";
            if (key.IndexOf("brick", StringComparison.InvariantCultureIgnoreCase) >= 0) return "cave-bg";
            if (key.IndexOf("fortress", StringComparison.InvariantCultureIgnoreCase) >= 0) return "castle-bg";
            if (key.IndexOf("ghost", StringComparison.InvariantCultureIgnoreCase) >= 0) return "ghosthouse-bg";
            if (key.IndexOf("ice", StringComparison.InvariantCultureIgnoreCase) >= 0) return "snow-bg";
            if (key.IndexOf("jungle", StringComparison.InvariantCultureIgnoreCase) >= 0) return "jungle-bg";
            if (key.IndexOf("pipes", StringComparison.InvariantCultureIgnoreCase) >= 0) return "pipe-bg";
            if (key.IndexOf("sky", StringComparison.InvariantCultureIgnoreCase) >= 0) return "sky-bg";
            if (key.IndexOf("snow", StringComparison.InvariantCultureIgnoreCase) >= 0) return "snow-bg";
            if (key.IndexOf("volcano", StringComparison.InvariantCultureIgnoreCase) >= 0) return "volcano-bg";

            // Default.
            return "grass-sky";
        }

        private static bool TryGetWrappingWorldBounds(StageDefinition def, out Vector2 min, out Vector2 max) {
            // Prefer the "main" tilemap width for wrapping (matches Unity 6 StageData TileDimensions),
            // rather than the union of all tile layers (which may include out-of-bounds decoration layers).
            min = Vector2.zero;
            max = Vector2.zero;

            if (def == null || def.tileLayers == null || def.tileLayers.Count == 0) {
                return false;
            }

            bool any = false;

            for (int i = 0; i < def.tileLayers.Count; i++) {
                StageTileLayer layer = def.tileLayers[i];
                if (layer == null || layer.tiles == null || layer.tiles.Count == 0) {
                    continue;
                }

                string n = layer.name ?? "";
                bool isGround = (n.IndexOf("Tilemap_Ground", StringComparison.InvariantCultureIgnoreCase) >= 0);
                if (!isGround) {
                    continue;
                }

                Vector2 lmin;
                Vector2 lmax;
                if (!TryGetLayerTileWorldBounds(layer, out lmin, out lmax)) {
                    continue;
                }

                if (!any) {
                    min = lmin;
                    max = lmax;
                    any = true;
                } else {
                    min.x = Mathf.Min(min.x, lmin.x);
                    min.y = Mathf.Min(min.y, lmin.y);
                    max.x = Mathf.Max(max.x, lmax.x);
                    max.y = Mathf.Max(max.y, lmax.y);
                }
            }

            return any && (max.x > min.x + 0.0001f);
        }

        private static bool TryGetLayerTileWorldBounds(StageTileLayer layer, out Vector2 min, out Vector2 max) {
            min = Vector2.zero;
            max = Vector2.zero;
            if (layer == null || layer.tiles == null || layer.tiles.Count == 0) {
                return false;
            }

            Vector3 layerScale = (layer.scale.sqrMagnitude > 0.0001f) ? layer.scale : Vector3.one;
            float legacyFactor = GetLegacyTileLayerScaleFactor(layerScale);
            Vector3 layerPos = layer.position * legacyFactor;
            layerScale = NormalizeLegacyTileLayerScale(layerScale);

            float sx = Mathf.Abs(layerScale.x);
            float sy = Mathf.Abs(layerScale.y);
            if (sx <= 0.0001f || sy <= 0.0001f) {
                return false;
            }

            bool any = false;

            for (int t = 0; t < layer.tiles.Count; t++) {
                StageTile tile = layer.tiles[t];

                float cx = layerPos.x + (tile.x + 0.5f) * layerScale.x;
                float cy = layerPos.y + (tile.y + 0.5f) * layerScale.y;

                float halfW = sx * 0.5f;
                float halfH = sy * 0.5f;

                float tx0 = cx - halfW;
                float tx1 = cx + halfW;
                float ty0 = cy - halfH;
                float ty1 = cy + halfH;

                if (!any) {
                    min = new Vector2(Mathf.Min(tx0, tx1), Mathf.Min(ty0, ty1));
                    max = new Vector2(Mathf.Max(tx0, tx1), Mathf.Max(ty0, ty1));
                    any = true;
                } else {
                    min.x = Mathf.Min(min.x, Mathf.Min(tx0, tx1));
                    min.y = Mathf.Min(min.y, Mathf.Min(ty0, ty1));
                    max.x = Mathf.Max(max.x, Mathf.Max(tx0, tx1));
                    max.y = Mathf.Max(max.y, Mathf.Max(ty0, ty1));
                }
            }

            return any && (max.x > min.x + 0.0001f);
        }

        private static void BuildTiles(StageDefinition def, Transform stageRoot, bool buildColliders) {
            if (def.tileLayers == null) {
                return;
            }

            for (int i = 0; i < def.tileLayers.Count; i++) {
                StageTileLayer layer = def.tileLayers[i];
                if (layer == null || layer.tiles == null || layer.tiles.Count == 0) {
                    continue;
                }

                GameObject layerGo = new GameObject(string.IsNullOrEmpty(layer.name) ? ("TileLayer_" + i) : layer.name);
                layerGo.transform.parent = stageRoot;

                Vector3 layerScale = (layer.scale.sqrMagnitude > 0.0001f) ? layer.scale : Vector3.one;
                float legacyFactor = GetLegacyTileLayerScaleFactor(layerScale);
                layerGo.transform.localPosition = layer.position * legacyFactor;
                layerGo.transform.localScale = NormalizeLegacyTileLayerScale(layerScale);

                // Snap tile layers to the pixel grid to avoid 1px seams due to sub-pixel offsets.
                layerGo.transform.localPosition = SnapVector(layerGo.transform.localPosition, PixelUnit);

                // Track per-layer interactive tiles so we can exclude them from the merged colliders.
                HashSet<long> interactiveSolidKeys = null;

                // Build sprites.
                for (int t = 0; t < layer.tiles.Count; t++) {
                    StageTile tile = layer.tiles[t];

                    GameObject tileGo = new GameObject("T_" + tile.x + "_" + tile.y);
                    tileGo.transform.parent = layerGo.transform;

                    // Tilemaps place cell (x,y) at integer coords. With centered pivots, add 0.5.
                    tileGo.transform.localPosition = new Vector3(tile.x + 0.5f, tile.y + 0.5f, 0f);
                    tileGo.transform.localPosition = SnapVector(tileGo.transform.localPosition, PixelUnit);
                    float sx = tile.flipX ? -1f : 1f;
                    float sy = tile.flipY ? -1f : 1f;
                    tileGo.transform.localScale = new Vector3(sx, sy, 1f);

                    SpriteRenderer sr = tileGo.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = layer.sortingOrder;
                    sr.color = Color.white;
                    if (!string.IsNullOrEmpty(tile.spriteName)) {
                        sr.sprite = NSMB.Content.ResourceSpriteCache.FindSprite(layer.resourcesAtlasPath, tile.spriteName);
                        if (sr.sprite == null && tile.spriteName.Length > 0 && !char.IsDigit(tile.spriteName[0])) {
                            sr.sprite = NSMB.Content.ResourceSpriteCache.FindSprite(layer.resourcesAtlasPath, "0 " + tile.spriteName);
                        }
                    }
                    if (sr.sprite == null) {
                        sr.sprite = TryResolveTileSprite(layer.resourcesAtlasPath, tile.spriteIndex);
                    }
                    if (sr.sprite != null) {
                        // Runtime safety net: enforce pixel-art sampling to reduce seams on hardware/players
                        // where importer settings haven't been applied yet.
                        Texture2D tex = sr.sprite.texture;
                        if (tex != null) {
                            tex.filterMode = FilterMode.Point;
                            tex.wrapMode = TextureWrapMode.Clamp;
                        }
                    }

                    // Convert certain solid tiles into interactive block GameObjects (question blocks, etc.)
                    // so they can be bumped from below and animated like the original game.
                    int animatedBlocksIndex;
                    bool isAnimatedBlocksTile = TryGetAnimatedBlocksIndex(layer.resourcesAtlasPath, tile, out animatedBlocksIndex);
                    bool isInteractive = (tile.interactionKind != NSMB.World.StageTileInteractionKind.None) ||
                                         (isAnimatedBlocksTile && IsInteractiveBlockAnimatedIndex(animatedBlocksIndex));

                    // Small visual overlap helps hide thin seams if a tile sprite has a 1px transparent edge.
                    // Never apply to interactive blocks (their colliders must remain exactly 1x1).
                    if (!isInteractive) {
                        tileGo.transform.localScale = new Vector3(sx * TileVisualOverlap, sy * TileVisualOverlap, 1f);
                    }
                    if (isInteractive) {
                        if (interactiveSolidKeys == null) {
                            interactiveSolidKeys = new HashSet<long>();
                        }
                        interactiveSolidKeys.Add(PackTileKey(tile.x, tile.y));

                        BoxCollider2D box = tileGo.AddComponent<BoxCollider2D>();
                        box.size = new Vector2(1f, 1f);

                        NSMB.Blocks.BlockBump bump = tileGo.AddComponent<NSMB.Blocks.BlockBump>();
                        // Let the tile behavior pick sounds (coin/powerup/break) for parity with the Unity 6 logic.
                        bump.playBumpSfx = false;
                        tileGo.AddComponent<NSMB.Blocks.BlockHitDetector>();

                        NSMB.Blocks.InteractiveBlockTile ib = tileGo.AddComponent<NSMB.Blocks.InteractiveBlockTile>();
                        ib.interactionKind = tile.interactionKind;
                        ib.breakingRules = tile.breakingRules;
                        ib.bumpIfNotBroken = tile.bumpIfNotBroken;
                        ib.usedAtlasPath = tile.usedAtlasPath;
                        ib.usedSpriteName = tile.usedSpriteName;
                        ib.smallPowerup = tile.smallPowerup;
                        ib.largePowerup = tile.largePowerup;

                        // Question blocks (yellow) are animation_0..animation_3 in the Unity 6 atlas.
                        ib.isQuestionBlockVisual = isAnimatedBlocksTile && animatedBlocksIndex >= 0 && animatedBlocksIndex <= 3;
                    }
                }

                // Colliders: only for layers that look like ground for now.
                if (buildColliders && LooksLikeSolidGroundLayer(layer.name, layer.resourcesAtlasPath)) {
                    List<StageTile> solidTiles = FilterSolidTiles(layer.tiles);
                    if (interactiveSolidKeys != null && interactiveSolidKeys.Count > 0) {
                        solidTiles = ExcludeTilesByKey(solidTiles, interactiveSolidKeys);
                    }
                    if (solidTiles.Count > 0) {
                        BuildMergedColliders(solidTiles, layerGo.transform);
                    }
                }
            }
        }

        private static bool TryGetAnimatedBlocksIndex(string resourcesAtlasPath, StageTile tile, out int index) {
            index = -1;
            if (!string.Equals(resourcesAtlasPath, NSMB.Content.GameplayAtlasPaths.AnimatedBlocks, StringComparison.InvariantCultureIgnoreCase)) {
                return false;
            }

            // Prefer spriteName, because Unity 6 Tilemap serializes a separate sprite index table that may not be stable.
            if (!string.IsNullOrEmpty(tile.spriteName) && tile.spriteName.StartsWith("animation_", StringComparison.InvariantCultureIgnoreCase)) {
                int u = tile.spriteName.LastIndexOf('_');
                if (u >= 0 && u + 1 < tile.spriteName.Length) {
                    int v;
                    if (int.TryParse(tile.spriteName.Substring(u + 1), out v)) {
                        index = v;
                        return true;
                    }
                }
            }

            // Fallback: use spriteIndex directly (some older imported assets used this).
            if (tile.spriteIndex >= 0) {
                index = tile.spriteIndex;
                return true;
            }

            return false;
        }

        private static bool IsInteractiveBlockAnimatedIndex(int animatedBlocksIndex) {
            // Derived from Unity 6 tile assets under Assets/Resources/Tilemaps/Tiles:
            // - YellowPowerup/YellowCoin use animation_0..3 (question animation).
            // - BrownBrick uses base animation_4.
            // - BlueBrick uses base animation_16.
            // - GrayBrick uses base animation_24.
            // We treat these as bumpable blocks for parity; full "contents" behavior is ported separately.
            if (animatedBlocksIndex >= 0 && animatedBlocksIndex <= 3) return true;
            if (animatedBlocksIndex == 4) return true;
            if (animatedBlocksIndex == 16) return true;
            if (animatedBlocksIndex == 24) return true;
            return false;
        }

        private static long PackTileKey(int x, int y) {
            return (((long)x) << 32) ^ (uint)y;
        }

        private static List<StageTile> ExcludeTilesByKey(List<StageTile> tiles, HashSet<long> keys) {
            if (tiles == null || tiles.Count == 0 || keys == null || keys.Count == 0) {
                return tiles;
            }

            List<StageTile> filtered = new List<StageTile>(tiles.Count);
            for (int i = 0; i < tiles.Count; i++) {
                StageTile t = tiles[i];
                if (!keys.Contains(PackTileKey(t.x, t.y))) {
                    filtered.Add(t);
                }
            }
            return filtered;
        }

        private static List<StageTile> FilterSolidTiles(List<StageTile> tiles) {
            if (tiles == null || tiles.Count == 0) {
                return new List<StageTile>();
            }

            bool anyTaggedSolid = false;
            for (int i = 0; i < tiles.Count; i++) {
                if (tiles[i].solid) {
                    anyTaggedSolid = true;
                    break;
                }
            }

            // Backwards-compat: older imported stage assets won't have the `solid` field serialized,
            // which means it will default to false for all tiles. In that case treat everything as solid.
            if (!anyTaggedSolid) {
                return new List<StageTile>(tiles);
            }

            List<StageTile> solid = new List<StageTile>();
            for (int i = 0; i < tiles.Count; i++) {
                if (tiles[i].solid) {
                    solid.Add(tiles[i]);
                }
            }
            return solid;
        }

        private static void BuildEntities(StageDefinition def, Transform stageRoot) {
            if (def.entities == null) {
                return;
            }

            GameObject entitiesRoot = new GameObject("Entities");
            entitiesRoot.transform.parent = stageRoot;
            entitiesRoot.transform.localPosition = Vector3.zero;
            entitiesRoot.transform.localScale = Vector3.one;

            for (int i = 0; i < def.entities.Count; i++) {
                StageEntity e = def.entities[i];
                switch (e.kind) {
                    case StageEntityKind.Coin:
                        SpawnCoin(entitiesRoot.transform, e.position);
                        break;
                    case StageEntityKind.Goomba:
                        SpawnGoomba(entitiesRoot.transform, e.position);
                        break;
                    case StageEntityKind.Koopa:
                        SpawnKoopa(entitiesRoot.transform, e.position);
                        break;
                    case StageEntityKind.BreakableBlock:
                        SpawnBreakableBlock(entitiesRoot.transform, e.position);
                        break;
                    case StageEntityKind.InvisibleBlock:
                        SpawnInvisibleBlock(entitiesRoot.transform, e.position);
                        break;
                    case StageEntityKind.MovingPlatform:
                        SpawnMovingPlatform(entitiesRoot.transform, e);
                        break;
                    case StageEntityKind.BulletBillLauncher:
                        SpawnBulletBillLauncher(entitiesRoot.transform, e);
                        break;
                    case StageEntityKind.PiranhaPlant:
                        SpawnPiranhaPlant(entitiesRoot.transform, e.position);
                        break;
                    case StageEntityKind.Boo:
                        SpawnBoo(entitiesRoot.transform, e.position);
                        break;
                    case StageEntityKind.Bobomb:
                        SpawnBobomb(entitiesRoot.transform, e.position);
                        break;
                    case StageEntityKind.Spinner:
                        SpawnSpinner(entitiesRoot.transform, e.position);
                        break;
                    case StageEntityKind.EnterablePipe:
                        SpawnEnterablePipe(entitiesRoot.transform, e);
                        break;
                    case StageEntityKind.MarioBrosPlatform:
                        SpawnMarioBrosPlatform(entitiesRoot.transform, e);
                        break;
                    default:
                        // Unknown/unsupported - skip (importer should warn).
                        break;
                }
            }
        }

        private static Vector3 SnapVector(Vector3 v, float step) {
            if (step <= 0f) {
                return v;
            }
            float x = Mathf.Round(v.x / step) * step;
            float y = Mathf.Round(v.y / step) * step;
            float z = Mathf.Round(v.z / step) * step;
            return new Vector3(x, y, z);
        }

        private static Sprite TryResolveTileSprite(string resourcesAtlasPath, int spriteIndex) {
            if (string.IsNullOrEmpty(resourcesAtlasPath)) {
                return null;
            }

            Sprite[] sprites = NSMB.Content.ResourceSpriteCache.LoadAllSprites(resourcesAtlasPath);
            if (sprites == null || sprites.Length == 0) {
                return null;
            }

            // Primary: Tilemap m_TileSpriteIndex is an index into Unity's imported sprites list for this texture.
            if (spriteIndex >= 0 && spriteIndex < sprites.Length && sprites[spriteIndex] != null) {
                return sprites[spriteIndex];
            }

            // Secondary: some tools/paths in this repo use naming like "grass_0", "platforms_3", etc.
            string atlasName = resourcesAtlasPath;
            int slash = atlasName.LastIndexOf('/');
            if (slash >= 0 && slash < atlasName.Length - 1) {
                atlasName = atlasName.Substring(slash + 1);
            }

            string spriteName = atlasName + "_" + spriteIndex;
            Sprite direct = NSMB.Content.ResourceSpriteCache.FindSprite(resourcesAtlasPath, spriteName);
            if (direct != null) {
                return direct;
            }

            // Fallback: some sheets in the original project use different prefixes but still end with "_<index>".
            for (int i = 0; i < sprites.Length; i++) {
                Sprite s = sprites[i];
                if (s == null || string.IsNullOrEmpty(s.name)) {
                    continue;
                }
                int suffix = ExtractTrailingInt(s.name);
                if (suffix == spriteIndex) {
                    return s;
                }
            }

            // Final fallback: treat spriteIndex as an index into the imported spritesheet list order.
            if (spriteIndex >= 0 && spriteIndex < sprites.Length) {
                return sprites[spriteIndex];
            }

            return sprites[0];
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

        private static bool LooksLikeSolidGroundLayer(string layerName, string atlasPath) {
            string n = (layerName ?? string.Empty).ToLowerInvariant();
            if (n.Contains("ground") || n.Contains("solid") || n.Contains("terrain")) {
                return true;
            }

            // DefaultGrassLevel uses Tilemap_Ground.
            if (n.Contains("tilemap_ground")) {
                return true;
            }

            // If the atlas is grass/platform tiles, treat it as solid for now.
            if (!string.IsNullOrEmpty(atlasPath)) {
                string a = atlasPath.ToLowerInvariant();
                if (a.Contains("/terrain/grass") || a.EndsWith("/grass")) return true;
                if (a.Contains("/terrain/platforms") || a.EndsWith("/platforms")) return true;
            }

            return false;
        }

        private static Vector3 NormalizeLegacyTileLayerScale(Vector3 s) {
            // Backwards-compat for imported StageDefinitions created before the importer normalized Tilemap scales.
            // Unity 6 levels commonly have Tilemap transforms at scale 0.5; our runtime assumes 1 unit per tile.
            // If we keep the 0.5 scale, tiles are half-size and all actors (player/enemies/items) look too big.
            //
            // New imports should already be normalized; this only applies to existing StageDefinition assets.
            if (Mathf.Abs(s.x - 1f) <= 0.0005f && Mathf.Abs(s.y - 1f) <= 0.0005f) {
                return Vector3.one;
            }

            if (Mathf.Abs(s.x - 0.5f) <= 0.0005f && Mathf.Abs(s.y - 0.5f) <= 0.0005f) {
                return Vector3.one;
            }

            if (Mathf.Abs(s.x - 0.25f) <= 0.0005f && Mathf.Abs(s.y - 0.25f) <= 0.0005f) {
                return Vector3.one;
            }

            return s;
        }

        private static float GetLegacyTileLayerScaleFactor(Vector3 s) {
            // Legacy imported stage assets stored raw Unity 6 Tilemap transforms (scale 0.5 / 0.25) without
            // applying the port's ImportScale (2x / 4x). To keep old assets playable without reimporting,
            // scale both the layer scale AND its position by this factor.
            if (Mathf.Abs(s.x - 0.5f) <= 0.0005f && Mathf.Abs(s.y - 0.5f) <= 0.0005f) {
                return 2f;
            }
            if (Mathf.Abs(s.x - 0.25f) <= 0.0005f && Mathf.Abs(s.y - 0.25f) <= 0.0005f) {
                return 4f;
            }
            return 1f;
        }

        private static bool TryGetStageTileWorldBounds(StageDefinition def, out Vector2 min, out Vector2 max) {
            min = Vector2.zero;
            max = Vector2.zero;
            if (def == null || def.tileLayers == null || def.tileLayers.Count == 0) {
                return false;
            }

            bool any = false;

            for (int i = 0; i < def.tileLayers.Count; i++) {
                StageTileLayer layer = def.tileLayers[i];
                if (layer == null || layer.tiles == null || layer.tiles.Count == 0) {
                    continue;
                }

                Vector3 layerScale = (layer.scale.sqrMagnitude > 0.0001f) ? layer.scale : Vector3.one;
                float legacyFactor = GetLegacyTileLayerScaleFactor(layerScale);
                Vector3 layerPos = layer.position * legacyFactor;
                layerScale = NormalizeLegacyTileLayerScale(layerScale);

                float sx = Mathf.Abs(layerScale.x);
                float sy = Mathf.Abs(layerScale.y);
                if (sx <= 0.0001f || sy <= 0.0001f) {
                    continue;
                }

                for (int t = 0; t < layer.tiles.Count; t++) {
                    StageTile tile = layer.tiles[t];

                    float cx = layerPos.x + (tile.x + 0.5f) * layerScale.x;
                    float cy = layerPos.y + (tile.y + 0.5f) * layerScale.y;

                    float halfW = sx * 0.5f;
                    float halfH = sy * 0.5f;

                    float tx0 = cx - halfW;
                    float tx1 = cx + halfW;
                    float ty0 = cy - halfH;
                    float ty1 = cy + halfH;

                    if (!any) {
                        min = new Vector2(Mathf.Min(tx0, tx1), Mathf.Min(ty0, ty1));
                        max = new Vector2(Mathf.Max(tx0, tx1), Mathf.Max(ty0, ty1));
                        any = true;
                    } else {
                        min.x = Mathf.Min(min.x, Mathf.Min(tx0, tx1));
                        min.y = Mathf.Min(min.y, Mathf.Min(ty0, ty1));
                        max.x = Mathf.Max(max.x, Mathf.Max(tx0, tx1));
                        max.y = Mathf.Max(max.y, Mathf.Max(ty0, ty1));
                    }
                }
            }

            return any && (max.x > min.x + 0.0001f);
        }

        private static void BuildMergedColliders(List<StageTile> tiles, Transform layerTransform) {
            // Very simple greedy merge of solid cells into rectangles (good enough for now).
            int minX, maxX, minY, maxY;
            if (!GetTileBounds(tiles, out minX, out maxX, out minY, out maxY)) {
                return;
            }

            int width = (maxX - minX) + 1;
            int height = (maxY - minY) + 1;
            bool[,] solid = new bool[width, height];

            for (int i = 0; i < tiles.Count; i++) {
                StageTile t = tiles[i];
                solid[t.x - minX, t.y - minY] = true;
            }

            GameObject collidersGo = new GameObject("Colliders");
            collidersGo.transform.parent = layerTransform;
            collidersGo.transform.localPosition = Vector3.zero;
            collidersGo.transform.localScale = Vector3.one;

            Rigidbody2D rb = collidersGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            bool[,] used = new bool[width, height];

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    if (!solid[x, y] || used[x, y]) {
                        continue;
                    }

                    // Extend horizontally.
                    int runX = x;
                    while (runX < width && solid[runX, y] && !used[runX, y]) {
                        runX++;
                    }
                    int runWidth = runX - x;

                    // Extend vertically as long as all cells in the run are solid.
                    int runY = y + 1;
                    while (runY < height) {
                        bool ok = true;
                        for (int rx = x; rx < x + runWidth; rx++) {
                            if (!solid[rx, runY] || used[rx, runY]) {
                                ok = false;
                                break;
                            }
                        }
                        if (!ok) {
                            break;
                        }
                        runY++;
                    }
                    int runHeight = runY - y;

                    // Mark used.
                    for (int yy = y; yy < y + runHeight; yy++) {
                        for (int xx = x; xx < x + runWidth; xx++) {
                            used[xx, yy] = true;
                        }
                    }

                    BoxCollider2D col = collidersGo.AddComponent<BoxCollider2D>();

                    float w = runWidth;
                    float h = runHeight;
                    float cx = (minX + x) + (w * 0.5f);
                    float cy = (minY + y) + (h * 0.5f);
                    col.size = new Vector2(w, h);
                    col.offset = new Vector2(cx, cy);
                }
            }
        }

        private static bool GetTileBounds(List<StageTile> tiles, out int minX, out int maxX, out int minY, out int maxY) {
            minX = 0;
            maxX = 0;
            minY = 0;
            maxY = 0;
            if (tiles == null || tiles.Count == 0) {
                return false;
            }

            minX = maxX = tiles[0].x;
            minY = maxY = tiles[0].y;
            for (int i = 1; i < tiles.Count; i++) {
                StageTile t = tiles[i];
                if (t.x < minX) minX = t.x;
                if (t.x > maxX) maxX = t.x;
                if (t.y < minY) minY = t.y;
                if (t.y > maxY) maxY = t.y;
            }
            return true;
        }

        private static void SpawnCoin(Transform parent, Vector2 pos) {
            GameObject go = new GameObject("Coin");
            go.transform.parent = parent;
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.25f;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.color = Color.white;
            sr.sortingOrder = 0;

            go.AddComponent<NSMB.Items.CoinPickup>();
        }

        private static void SpawnGoomba(Transform parent, Vector2 pos) {
            GameObject go = new GameObject("Goomba");
            go.transform.parent = parent;
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.5f;
            rb.freezeRotation = true;

            go.AddComponent<BoxCollider2D>();

            go.AddComponent<NSMB.Enemies.GoombaEnemy>();
        }

        private static void SpawnKoopa(Transform parent, Vector2 pos) {
            GameObject go = new GameObject("Koopa");
            go.transform.parent = parent;
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.5f;
            rb.freezeRotation = true;

            go.AddComponent<BoxCollider2D>();

            go.AddComponent<NSMB.Enemies.KoopaEnemy>();
        }

        private static void SpawnBreakableBlock(Transform parent, Vector2 pos) {
            GameObject go = new GameObject("BreakableBlock");
            go.transform.parent = parent;
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.color = Color.white;
            sr.sortingOrder = 0;
            sr.sprite = NSMB.Visual.GameplaySprites.GetPlatformTile(2);

            NSMB.Blocks.BlockBump bump = go.AddComponent<NSMB.Blocks.BlockBump>();
            bump.playBumpSfx = false;
            go.AddComponent<NSMB.Blocks.BlockHitDetector>();
            go.AddComponent<NSMB.Blocks.BreakableBlock>();
        }

        private static void SpawnInvisibleBlock(Transform parent, Vector2 pos) {
            GameObject go = new GameObject("InvisibleBlock");
            go.transform.parent = parent;
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);

            // Invisible by default.
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 1f, 1f, 0f);
            sr.sortingOrder = 0;

            go.AddComponent<NSMB.Blocks.BlockBump>();
            go.AddComponent<NSMB.Blocks.BlockHitDetector>();

            // When bumped, show a placeholder sprite so it's obvious it exists.
            go.AddComponent<RevealOnBump>();
        }

        private static void SpawnMovingPlatform(Transform parent, StageEntity e) {
            GameObject go = new GameObject("MovingPlatform");
            go.transform.parent = parent;
            go.transform.position = new Vector3(e.position.x, e.position.y, 0f);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            Vector2 size = (e.size.sqrMagnitude > 0.0001f) ? e.size : new Vector2(2f, 0.5f);
            col.size = size;
            if (e.colliderOffset.sqrMagnitude > 0.000001f) {
                col.offset = e.colliderOffset;
            }
            col.isTrigger = e.isTrigger;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.color = Color.white;
            sr.sortingOrder = 0;
            sr.sprite = NSMB.Visual.GameplaySprites.GetPlatformTile(0);

            MovingPlatform2D mp = go.AddComponent<MovingPlatform2D>();
            mp.velocity = e.velocity;
            mp.path = e.path;
            mp.loopMode = e.loopMode;
            mp.startOffsetSeconds = e.startOffsetSeconds;
        }

        private static void SpawnBulletBillLauncher(Transform parent, StageEntity e) {
            GameObject go = new GameObject("BulletBillLauncher");
            go.transform.parent = parent;
            go.transform.position = new Vector3(e.position.x, e.position.y, 0f);

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            Vector2 size = (e.size.sqrMagnitude > 0.0001f) ? e.size : new Vector2(0.5f, 1f);
            col.size = size;
            if (e.colliderOffset.sqrMagnitude > 0.000001f) {
                col.offset = e.colliderOffset;
            }
            col.isTrigger = e.isTrigger;

            // Visuals are optional; keep it lightweight and avoid relying on Unity UI components.
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 0;
            sr.color = Color.white;
            sr.sprite = NSMB.Content.ResourceSpriteCache.FindSprite(NSMB.Content.GameplayAtlasPaths.BulletBill, "bullet-bill_0");

            NSMB.Enemies.BulletBillLauncher launcher = go.AddComponent<NSMB.Enemies.BulletBillLauncher>();
            launcher.fireIntervalSeconds = (e.param0 > 0.01f) ? e.param0 : 4f;
            launcher.minimumShootRadius = Mathf.Max(0f, e.param1);
            launcher.maximumShootRadius = (e.param2 > 0.01f) ? e.param2 : 9f;
            launcher.bulletSpawnOffset = e.colliderOffset;
        }

        private static void SpawnPiranhaPlant(Transform parent, Vector2 pos) {
            GameObject go = new GameObject("PiranhaPlant");
            go.transform.parent = parent;
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            // Collider: simple bite box.
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            go.AddComponent<NSMB.Enemies.PiranhaPlantEnemy>();
        }

        private static void SpawnBoo(Transform parent, Vector2 pos) {
            GameObject go = new GameObject("Boo");
            go.transform.parent = parent;
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            go.AddComponent<NSMB.Enemies.BooEnemy>();
        }

        private static void SpawnBobomb(Transform parent, Vector2 pos) {
            GameObject go = new GameObject("Bobomb");
            go.transform.parent = parent;
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.5f;
            rb.freezeRotation = true;

            go.AddComponent<BoxCollider2D>();

            go.AddComponent<NSMB.Enemies.BobombEnemy>();
        }

        private static void SpawnSpinner(Transform parent, Vector2 pos) {
            // Placeholder hazard: damages player on touch. Original uses a custom "spinner" entity.
            GameObject go = new GameObject("Spinner");
            go.transform.parent = parent;
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.45f;
            col.isTrigger = true;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            sr.sortingOrder = 0;

            go.AddComponent<NSMB.World.DamageOnTouch>();
        }

        private static void SpawnEnterablePipe(Transform parent, StageEntity e) {
            // Placeholder solid pipe collision. Warp/pipe logic is not yet ported.
            GameObject go = new GameObject("EnterablePipe");
            go.transform.parent = parent;
            go.transform.position = new Vector3(e.position.x, e.position.y, 0f);

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            Vector2 size = (e.size.sqrMagnitude > 0.0001f) ? e.size : new Vector2(1f, 2f);
            col.size = size;
            if (e.colliderOffset.sqrMagnitude > 0.000001f) {
                col.offset = e.colliderOffset;
            }
            col.isTrigger = false;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 1f, 1f, 0.0f);
            sr.sortingOrder = 0;
        }

        private static void SpawnMarioBrosPlatform(Transform parent, StageEntity e) {
            // Placeholder static platform. Original has special bounce behavior; port later.
            GameObject go = new GameObject("MarioBrosPlatform");
            go.transform.parent = parent;
            go.transform.position = new Vector3(e.position.x, e.position.y, 0f);

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            Vector2 size = (e.size.sqrMagnitude > 0.0001f) ? e.size : new Vector2(3f, 0.5f);
            col.size = size;
            if (e.colliderOffset.sqrMagnitude > 0.000001f) {
                col.offset = e.colliderOffset;
            }
            col.isTrigger = false;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.color = Color.white;
            sr.sortingOrder = 0;
            sr.sprite = NSMB.Visual.GameplaySprites.GetPlatformTile(0);
        }

        private sealed class RevealOnBump : MonoBehaviour {
            private bool _revealed;

            private void OnBumped() {
                if (_revealed) return;
                _revealed = true;

                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null) {
                    sr.sprite = NSMB.Visual.GameplaySprites.GetPlatformTile(2);
                    sr.color = Color.white;
                }
            }
        }
    }
}
