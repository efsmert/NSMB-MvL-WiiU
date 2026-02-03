using System;
using System.Collections.Generic;
using UnityEngine;

namespace NSMB.World {
    public static class StageRuntimeBuilder {
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

            BuildTiles(def, root.transform, buildColliders);
            if (buildEntities) {
                BuildEntities(def, root.transform);
            }
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
                layerGo.transform.localPosition = layer.position;
                layerGo.transform.localScale = (layer.scale.sqrMagnitude > 0.0001f) ? layer.scale : Vector3.one;

                // Track per-layer interactive tiles so we can exclude them from the merged colliders.
                HashSet<long> interactiveSolidKeys = null;

                // Build sprites.
                for (int t = 0; t < layer.tiles.Count; t++) {
                    StageTile tile = layer.tiles[t];

                    GameObject tileGo = new GameObject("T_" + tile.x + "_" + tile.y);
                    tileGo.transform.parent = layerGo.transform;

                    // Tilemaps place cell (x,y) at integer coords. With centered pivots, add 0.5.
                    tileGo.transform.localPosition = new Vector3(tile.x + 0.5f, tile.y + 0.5f, 0f);
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

                    // Convert certain solid tiles into interactive block GameObjects (question blocks, etc.)
                    // so they can be bumped from below and animated like the original game.
                    if (IsQuestionBlockTile(layer.resourcesAtlasPath, tile)) {
                        if (interactiveSolidKeys == null) {
                            interactiveSolidKeys = new HashSet<long>();
                        }
                        interactiveSolidKeys.Add(PackTileKey(tile.x, tile.y));

                        BoxCollider2D box = tileGo.AddComponent<BoxCollider2D>();
                        box.size = new Vector2(1f, 1f);

                        tileGo.AddComponent<NSMB.Blocks.BlockBump>();
                        tileGo.AddComponent<NSMB.Blocks.BlockHitDetector>();

                        NSMB.Blocks.QuestionBlockTile qb = tileGo.AddComponent<NSMB.Blocks.QuestionBlockTile>();
                        qb.usedSpriteName = "animation_4";
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

        private static bool IsQuestionBlockTile(string resourcesAtlasPath, StageTile tile) {
            if (!string.Equals(resourcesAtlasPath, NSMB.Content.GameplayAtlasPaths.AnimatedBlocks, StringComparison.InvariantCultureIgnoreCase)) {
                return false;
            }

            // In this project, question block animation frames are animation_0..animation_3.
            int index = tile.spriteIndex;
            if (index >= 0 && index <= 3) {
                return true;
            }

            // Some import paths use spriteName instead of spriteIndex.
            if (!string.IsNullOrEmpty(tile.spriteName)) {
                if (tile.spriteName.StartsWith("animation_", StringComparison.InvariantCultureIgnoreCase)) {
                    int u = tile.spriteName.LastIndexOf('_');
                    if (u >= 0 && u + 1 < tile.spriteName.Length) {
                        int v;
                        if (int.TryParse(tile.spriteName.Substring(u + 1), out v)) {
                            return v >= 0 && v <= 3;
                        }
                    }
                }
            }

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

            go.AddComponent<NSMB.Blocks.BlockBump>();
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
