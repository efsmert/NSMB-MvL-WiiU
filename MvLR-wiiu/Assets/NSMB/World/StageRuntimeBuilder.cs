using System;
using System.Collections.Generic;
using UnityEngine;

namespace NSMB.World {
    public static class StageRuntimeBuilder {
        public static void Build(StageDefinition def, Transform parent) {
            if (def == null || parent == null) {
                return;
            }

            GameObject root = new GameObject("Stage_" + (string.IsNullOrEmpty(def.stageKey) ? "Unknown" : def.stageKey));
            root.transform.parent = parent;
            root.transform.localPosition = Vector3.zero;
            root.transform.localScale = Vector3.one;

            BuildTiles(def, root.transform);
            BuildEntities(def, root.transform);
        }

        private static void BuildTiles(StageDefinition def, Transform stageRoot) {
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

                // Build sprites.
                for (int t = 0; t < layer.tiles.Count; t++) {
                    StageTile tile = layer.tiles[t];

                    GameObject tileGo = new GameObject("T_" + tile.x + "_" + tile.y);
                    tileGo.transform.parent = layerGo.transform;

                    // Tilemaps place cell (x,y) at integer coords. With centered pivots, add 0.5.
                    tileGo.transform.localPosition = new Vector3(tile.x + 0.5f, tile.y + 0.5f, 0f);
                    tileGo.transform.localScale = tile.flipX ? new Vector3(-1f, 1f, 1f) : Vector3.one;

                    SpriteRenderer sr = tileGo.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = layer.sortingOrder;
                    sr.color = Color.white;
                    sr.sprite = TryResolveTileSprite(layer.resourcesAtlasPath, tile.spriteIndex);
                }

                // Colliders: only for layers that look like ground for now.
                if (LooksLikeSolidGroundLayer(layer.name, layer.resourcesAtlasPath)) {
                    BuildMergedColliders(layer, layerGo.transform);
                }
            }
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

            // We rely on imported sprite names like "grass_0", "platforms_3", etc.
            string atlasName = resourcesAtlasPath;
            int slash = atlasName.LastIndexOf('/');
            if (slash >= 0 && slash < atlasName.Length - 1) {
                atlasName = atlasName.Substring(slash + 1);
            }

            string spriteName = atlasName + "_" + spriteIndex;
            return NSMB.Content.ResourceSpriteCache.FindSprite(resourcesAtlasPath, spriteName);
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

        private static void BuildMergedColliders(StageTileLayer layer, Transform layerTransform) {
            // Very simple greedy merge of solid cells into rectangles (good enough for now).
            int minX, maxX, minY, maxY;
            if (!GetTileBounds(layer.tiles, out minX, out maxX, out minY, out maxY)) {
                return;
            }

            int width = (maxX - minX) + 1;
            int height = (maxY - minY) + 1;
            bool[,] solid = new bool[width, height];

            for (int i = 0; i < layer.tiles.Count; i++) {
                StageTile t = layer.tiles[i];
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

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.8f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.color = Color.white;
            sr.sortingOrder = 0;

            go.AddComponent<NSMB.Enemies.GoombaEnemy>();
        }
    }
}

