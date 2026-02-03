using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ImportOriginalStagesMenu {
    private const float FpScale = 1.0f / 65536.0f;

    // Our WiiU project uses 16px tiles at PPU=16 => 1 tile == 1 Unity unit.
    // The original project stores positions in FP where the tilemap is authored at half-scale, so we multiply by 2.
    private const float ImportScale = 2.0f;

    [MenuItem("NSMB/Import/Import Default Grass Stage (Tilemap + Entities)")]
    private static void ImportDefaultGrass() {
        ImportStage(BuildDefaultGrassSpec());
    }

    [MenuItem("NSMB/Import/Import All Stages (Tilemap + Entities)")]
    private static void ImportAllStages() {
        StageImportSpec[] specs = new[] {
            BuildDefaultGrassSpec(),
            BuildDefaultBrickSpec(),
            BuildDefaultPipesSpec(),
            BuildDefaultSnowSpec(),
            BuildDefaultCastleSpec(),
            BuildCustomBeachSpec(),
            BuildCustomJungleSpec(),
            BuildCustomDesertSpec(),
            BuildCustomSkySpec(),
            BuildCustomVolcanoSpec(),
            BuildCustomGhostSpec(),
            BuildCustomBonusSpec(),
        };

        int ok = 0;
        for (int i = 0; i < specs.Length; i++) {
            if (ImportStage(specs[i])) {
                ok++;
            }
        }

        Debug.Log("[NSMB] Imported " + ok + " stage assets into Assets/Resources/NSMB/Levels/ (see Console for any warnings).");
    }

    private sealed class StageImportSpec {
        public string stageKey;
        public string sceneRelPath;
        public string defaultGroundAtlas;
        public Dictionary<string, string> tilemapAtlasByName;
    }

    private static StageImportSpec BuildDefaultGrassSpec() {
        StageImportSpec s = new StageImportSpec();
        s.stageKey = "stage-grassland";
        s.sceneRelPath = "Assets/Scenes/Levels/DefaultGrassLevel.unity";
        s.defaultGroundAtlas = NSMB.Content.GameplayAtlasPaths.Grass;
        s.tilemapAtlasByName = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        s.tilemapAtlasByName["Tilemap_Ground"] = NSMB.Content.GameplayAtlasPaths.Grass;
        return s;
    }

    private static StageImportSpec BuildDefaultBrickSpec() {
        StageImportSpec s = new StageImportSpec();
        s.stageKey = "stage-brick";
        s.sceneRelPath = "Assets/Scenes/Levels/DefaultBrickLevel.unity";
        s.defaultGroundAtlas = NSMB.Content.GameplayAtlasPaths.Bricks;
        s.tilemapAtlasByName = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        s.tilemapAtlasByName["Tilemap_Ground"] = NSMB.Content.GameplayAtlasPaths.Bricks;
        return s;
    }

    private static StageImportSpec BuildDefaultPipesSpec() {
        StageImportSpec s = new StageImportSpec();
        s.stageKey = "stage-pipes";
        s.sceneRelPath = "Assets/Scenes/Levels/DefaultPipes.unity";
        s.defaultGroundAtlas = NSMB.Content.GameplayAtlasPaths.Pipes;
        s.tilemapAtlasByName = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        s.tilemapAtlasByName["Tilemap_Ground"] = NSMB.Content.GameplayAtlasPaths.Pipes;
        return s;
    }

    private static StageImportSpec BuildDefaultSnowSpec() {
        StageImportSpec s = new StageImportSpec();
        s.stageKey = "stage-ice";
        s.sceneRelPath = "Assets/Scenes/Levels/DefaultSnow.unity";
        s.defaultGroundAtlas = NSMB.Content.GameplayAtlasPaths.Snow;
        s.tilemapAtlasByName = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        s.tilemapAtlasByName["Tilemap_Ground"] = NSMB.Content.GameplayAtlasPaths.Snow;
        return s;
    }

    private static StageImportSpec BuildDefaultCastleSpec() {
        StageImportSpec s = new StageImportSpec();
        s.stageKey = "stage-fortress";
        s.sceneRelPath = "Assets/Scenes/Levels/DefaultCastle.unity";
        s.defaultGroundAtlas = NSMB.Content.GameplayAtlasPaths.Castle;
        s.tilemapAtlasByName = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        s.tilemapAtlasByName["Tilemap_Ground"] = NSMB.Content.GameplayAtlasPaths.Castle;
        s.tilemapAtlasByName["Tilemap_Squishy"] = NSMB.Content.GameplayAtlasPaths.Squishy;
        return s;
    }

    private static StageImportSpec BuildCustomBeachSpec() {
        StageImportSpec s = new StageImportSpec();
        s.stageKey = "stage-beach";
        s.sceneRelPath = "Assets/Scenes/Levels/CustomBeach.unity";
        s.defaultGroundAtlas = NSMB.Content.GameplayAtlasPaths.BeachYellow;
        s.tilemapAtlasByName = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        s.tilemapAtlasByName["Tilemap_Ground"] = NSMB.Content.GameplayAtlasPaths.BeachYellow;
        s.tilemapAtlasByName["Tilemap_Background"] = NSMB.Content.GameplayAtlasPaths.BeachBlue;
        s.tilemapAtlasByName["Tilemap_Dark"] = NSMB.Content.GameplayAtlasPaths.Underground;
        return s;
    }

    private static StageImportSpec BuildCustomJungleSpec() {
        StageImportSpec s = new StageImportSpec();
        s.stageKey = "stage-jungle";
        s.sceneRelPath = "Assets/Scenes/Levels/CustomJungle.unity";
        s.defaultGroundAtlas = NSMB.Content.GameplayAtlasPaths.Forest;
        s.tilemapAtlasByName = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        s.tilemapAtlasByName["Tilemap_Ground"] = NSMB.Content.GameplayAtlasPaths.Forest;
        s.tilemapAtlasByName["Tilemap_Background"] = NSMB.Content.GameplayAtlasPaths.Forest;
        return s;
    }

    private static StageImportSpec BuildCustomDesertSpec() {
        StageImportSpec s = new StageImportSpec();
        s.stageKey = "stage-desert";
        s.sceneRelPath = "Assets/Scenes/Levels/CustomDesert.unity";
        s.defaultGroundAtlas = NSMB.Content.GameplayAtlasPaths.Desert;
        s.tilemapAtlasByName = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        s.tilemapAtlasByName["Tilemap_Ground"] = NSMB.Content.GameplayAtlasPaths.Desert;
        s.tilemapAtlasByName["Tilemap_Background"] = NSMB.Content.GameplayAtlasPaths.Desert;
        return s;
    }

    private static StageImportSpec BuildCustomSkySpec() {
        StageImportSpec s = new StageImportSpec();
        s.stageKey = "stage-sky";
        s.sceneRelPath = "Assets/Scenes/Levels/CustomSky.unity";
        s.defaultGroundAtlas = NSMB.Content.GameplayAtlasPaths.SkyMushroom;
        s.tilemapAtlasByName = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        s.tilemapAtlasByName["Tilemap_Ground"] = NSMB.Content.GameplayAtlasPaths.SkyMushroom;
        s.tilemapAtlasByName["Tilemap_Background"] = NSMB.Content.GameplayAtlasPaths.Cloud;
        return s;
    }

    private static StageImportSpec BuildCustomVolcanoSpec() {
        StageImportSpec s = new StageImportSpec();
        s.stageKey = "stage-volcano";
        s.sceneRelPath = "Assets/Scenes/Levels/CustomVolcano.unity";
        s.defaultGroundAtlas = NSMB.Content.GameplayAtlasPaths.Volcano;
        s.tilemapAtlasByName = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        s.tilemapAtlasByName["Tilemap_Ground"] = NSMB.Content.GameplayAtlasPaths.Volcano;
        s.tilemapAtlasByName["Tilemap_Background"] = NSMB.Content.GameplayAtlasPaths.Volcano;
        return s;
    }

    private static StageImportSpec BuildCustomGhostSpec() {
        StageImportSpec s = new StageImportSpec();
        s.stageKey = "stage-ghosthouse";
        s.sceneRelPath = "Assets/Scenes/Levels/CustomGhost.unity";
        s.defaultGroundAtlas = NSMB.Content.GameplayAtlasPaths.Ghosthouse;
        s.tilemapAtlasByName = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        s.tilemapAtlasByName["Tilemap_Ground"] = NSMB.Content.GameplayAtlasPaths.Ghosthouse;
        s.tilemapAtlasByName["Tilemap_DarkBackground"] = NSMB.Content.GameplayAtlasPaths.Ghosthouse;
        return s;
    }

    private static StageImportSpec BuildCustomBonusSpec() {
        StageImportSpec s = new StageImportSpec();
        s.stageKey = "stage-bonus";
        s.sceneRelPath = "Assets/Scenes/Levels/CustomBonus.unity";
        s.defaultGroundAtlas = NSMB.Content.GameplayAtlasPaths.Bonus;
        s.tilemapAtlasByName = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        s.tilemapAtlasByName["Tilemap_Ground"] = NSMB.Content.GameplayAtlasPaths.Bonus;
        s.tilemapAtlasByName["Tilemap_Background"] = NSMB.Content.GameplayAtlasPaths.BonusPillars;
        return s;
    }

    private static bool ImportStage(StageImportSpec spec) {
        string mvLRProjectDir = Directory.GetParent(Application.dataPath).FullName;
        string repoRoot = Directory.GetParent(mvLRProjectDir).FullName;

        string originalProjectDir = Path.Combine(repoRoot, "NSMB-MarioVsLuigi");
        if (!Directory.Exists(originalProjectDir)) {
            Debug.LogError("[NSMB] Original project folder not found: " + originalProjectDir);
            return false;
        }

        string sceneRel = spec.sceneRelPath;
        string sceneAbs = Path.Combine(originalProjectDir, sceneRel.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(sceneAbs)) {
            Debug.LogError("[NSMB] Scene not found: " + sceneAbs);
            return false;
        }

        string stageAbs = FindQuantumMapAssetForScene(originalProjectDir, sceneRel);
        if (string.IsNullOrEmpty(stageAbs) || !File.Exists(stageAbs)) {
            Debug.LogError("[NSMB] Could not find Quantum map/stage asset for scene: " + sceneRel);
            return false;
        }

        List<SceneTilemapData> tilemaps = UnitySceneParsers.ExtractTilemaps(sceneAbs);
        if (tilemaps == null || tilemaps.Count == 0) {
            Debug.LogError("[NSMB] No Tilemap found in: " + sceneAbs);
            return false;
        }

        StageDataInfo stageData = TryGetStageDataInfo(originalProjectDir, stageAbs);
        Vector2 spawn = stageData.spawnPoint;
        if (spawn == Vector2.zero) {
            spawn = UnitySceneParsers.ExtractFirstPlayerSpawn(sceneAbs);
        }
        List<StageEntityData> entities = QuantumMapParsers.ExtractEntities(stageAbs);

        NSMB.World.StageDefinition def = ScriptableObject.CreateInstance<NSMB.World.StageDefinition>();
        def.stageKey = spec.stageKey;
        def.spawnPoint = spawn;
        def.isWrappingLevel = stageData.isWrappingLevel;
        def.cameraMin = stageData.cameraMin;
        def.cameraMax = stageData.cameraMax;

        OriginalSpriteResolver spriteResolver = new OriginalSpriteResolver(originalProjectDir);
        OriginalTileColliderResolver tileColliderResolver = new OriginalTileColliderResolver(originalProjectDir);

        for (int tm = 0; tm < tilemaps.Count; tm++) {
            SceneTilemapData tilemap = tilemaps[tm];
            if (tilemap == null || tilemap.tiles == null || tilemap.tiles.Count == 0) {
                continue;
            }

            Dictionary<string, NSMB.World.StageTileLayer> layersByAtlas = new Dictionary<string, NSMB.World.StageTileLayer>(StringComparer.InvariantCultureIgnoreCase);

            Vector3 layerPos = tilemap.worldPosition * ImportScale;
            Vector3 layerScale = Vector3.Scale(tilemap.worldScale, new Vector3(ImportScale, ImportScale, 1f));

            for (int i = 0; i < tilemap.tiles.Count; i++) {
                SceneTile t = tilemap.tiles[i];

                bool flipX = false;
                bool flipY = false;
                if (t.matrixIndex >= 0 && t.matrixIndex < tilemap.matrices.Count) {
                    SceneMatrix m = tilemap.matrices[t.matrixIndex];
                    flipX = m.e00 < 0f;
                    flipY = m.e11 < 0f;
                }

                string resourcesAtlasPath = null;
                string spriteName = null;

                if (t.spriteIndex >= 0 && t.spriteIndex < tilemap.spriteRefs.Count) {
                    SceneSpriteRef sr = tilemap.spriteRefs[t.spriteIndex];
                    string rp;
                    string sn;
                    if (spriteResolver.TryResolveSprite(sr.guid, sr.fileId, out rp, out sn)) {
                        resourcesAtlasPath = rp;
                        spriteName = sn;
                    }
                }

                if (string.IsNullOrEmpty(resourcesAtlasPath)) {
                    resourcesAtlasPath = ResolveAtlasForTilemap(tilemap.name, spec);
                }
                if (string.IsNullOrEmpty(resourcesAtlasPath)) {
                    resourcesAtlasPath = spec.defaultGroundAtlas;
                }
                if (string.IsNullOrEmpty(resourcesAtlasPath)) {
                    continue;
                }

                bool isSolid = true;
                if (t.tileAssetIndex >= 0 && t.tileAssetIndex < tilemap.tileAssets.Count) {
                    SceneTileAssetRef tr = tilemap.tileAssets[t.tileAssetIndex];
                    if (!string.IsNullOrEmpty(tr.guid)) {
                        int colliderType = tileColliderResolver.GetDefaultColliderType(tr.guid);
                        if (colliderType == 0) {
                            isSolid = false;
                        }
                    }
                }

                NSMB.World.StageTileLayer layer;
                if (!layersByAtlas.TryGetValue(resourcesAtlasPath, out layer)) {
                    layer = new NSMB.World.StageTileLayer();
                    layer.sortingOrder = tilemap.sortingOrder;
                    layer.position = layerPos;
                    layer.scale = layerScale;
                    layer.resourcesAtlasPath = resourcesAtlasPath;

                    layer.name = tilemap.name;
                    if (layersByAtlas.Count > 0) {
                        layer.name = tilemap.name + "_" + ShortAtlasName(resourcesAtlasPath);
                    }

                    layersByAtlas[resourcesAtlasPath] = layer;
                }

                NSMB.World.StageTile outTile = new NSMB.World.StageTile();
                outTile.x = t.x;
                outTile.y = t.y;
                outTile.spriteIndex = t.spriteIndex;
                outTile.spriteName = spriteName;
                outTile.flipX = flipX;
                outTile.flipY = flipY;
                outTile.solid = isSolid;
                layer.tiles.Add(outTile);
            }

            if (layersByAtlas.Count == 0) {
                Debug.LogWarning("[NSMB] No atlas mapping for tilemap " + tilemap.name + " in " + spec.stageKey + "; skipping.");
                continue;
            }

            List<string> keys = new List<string>(layersByAtlas.Keys);
            keys.Sort(StringComparer.InvariantCultureIgnoreCase);
            for (int k = 0; k < keys.Count; k++) {
                def.tileLayers.Add(layersByAtlas[keys[k]]);
            }
        }

        OriginalGenericMoverAssetResolver moverResolver2 = new OriginalGenericMoverAssetResolver(originalProjectDir);

        for (int i = 0; i < entities.Count; i++) {
            StageEntityData e = entities[i];
            if (e.kind == NSMB.World.StageEntityKind.Unknown) {
                continue;
            }
            NSMB.World.StageEntity se = new NSMB.World.StageEntity();
            se.kind = e.kind;
            se.variant = e.variant;
            se.position = e.position * ImportScale;
            se.size = e.size * ImportScale;
            se.colliderOffset = e.colliderOffset * ImportScale;
            se.velocity = e.velocity * ImportScale;
            se.isTrigger = e.isTrigger;
            se.param0 = e.param0;
            se.param1 = e.param1;
            se.param2 = e.param2;
            if (se.kind == NSMB.World.StageEntityKind.BulletBillLauncher) {
                // Radii are in world units from Quantum; match our stage/world scaling.
                se.param1 *= ImportScale;
                se.param2 *= ImportScale;
            }

            if (e.kind == NSMB.World.StageEntityKind.MovingPlatform && e.moverAssetGuidValue != 0) {
                NSMB.World.StagePathNode[] path;
                int loopMode;
                if (moverResolver2.TryLoadPath(e.moverAssetGuidValue, out path, out loopMode)) {
                    se.path = path;
                    se.loopMode = loopMode;
                    se.startOffsetSeconds = (e.startOffsetRaw * FpScale);
                    // Convert path positions to our Unity world scale.
                    if (se.path != null) {
                        for (int p = 0; p < se.path.Length; p++) {
                            NSMB.World.StagePathNode n = se.path[p];
                            n.position = n.position * ImportScale;
                            se.path[p] = n;
                        }
                    }
                }
            }
            def.entities.Add(se);
        }

        string assetPath = "Assets/Resources/NSMB/Levels/" + spec.stageKey + ".asset";
        SaveStageAsset(def, assetPath);
        return true;
    }

    private static void SaveStageAsset(ScriptableObject asset, string assetPath) {
        EnsureFoldersForAssetPath(assetPath);

        ScriptableObject existing = AssetDatabase.LoadAssetAtPath(assetPath, typeof(ScriptableObject)) as ScriptableObject;
        if (existing != null) {
            AssetDatabase.DeleteAsset(assetPath);
        }

        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureFoldersForAssetPath(string assetPath) {
        // assetPath is like "Assets/Resources/NSMB/Levels/stage-grassland.asset"
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/NSMB")) {
            AssetDatabase.CreateFolder("Assets/Resources", "NSMB");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/NSMB/Levels")) {
            AssetDatabase.CreateFolder("Assets/Resources/NSMB", "Levels");
        }
    }

    private static string FindQuantumMapAssetForScene(string originalProjectDir, string sceneRelPath) {
        // Look for a map/stage .asset that declares ScenePath == sceneRelPath and contains MapEntities.
        string mapsRoot = Path.Combine(Path.Combine(Path.Combine(originalProjectDir, "Assets"), "QuantumUser"), Path.Combine("Resources", "AssetObjects"));
        mapsRoot = Path.Combine(mapsRoot, "Maps");
        if (!Directory.Exists(mapsRoot)) {
            return null;
        }

        string[] assets = Directory.GetFiles(mapsRoot, "*.asset", SearchOption.AllDirectories);
        for (int i = 0; i < assets.Length; i++) {
            string path = assets[i];
            // Skip mesh/data helpers for speed.
            if (path.EndsWith("_mesh.asset", StringComparison.InvariantCultureIgnoreCase) ||
                path.EndsWith("StageData.asset", StringComparison.InvariantCultureIgnoreCase)) {
                continue;
            }

            try {
                string text = File.ReadAllText(path);
                if (text.IndexOf("ScenePath: " + sceneRelPath, StringComparison.InvariantCulture) < 0) {
                    continue;
                }
                if (text.IndexOf("MapEntities:", StringComparison.InvariantCulture) < 0) {
                    continue;
                }
                return path;
            } catch (Exception) {
                // ignore unreadable file
            }
        }

        return null;
    }

    private static string ResolveAtlasForTilemap(string tilemapName, StageImportSpec spec) {
        if (spec == null) {
            return null;
        }
        if (spec.tilemapAtlasByName != null && !string.IsNullOrEmpty(tilemapName)) {
            string v;
            if (spec.tilemapAtlasByName.TryGetValue(tilemapName, out v)) {
                return v;
            }
        }

        // Heuristic fallback.
        string n = (tilemapName ?? string.Empty).ToLowerInvariant();
        if (n.Contains("background")) {
            return spec.defaultGroundAtlas;
        }
        if (n.Contains("ground")) {
            return spec.defaultGroundAtlas;
        }
        return spec.defaultGroundAtlas;
    }

    private static string ShortAtlasName(string resourcesAtlasPath) {
        if (string.IsNullOrEmpty(resourcesAtlasPath)) {
            return "atlas";
        }
        int slash = resourcesAtlasPath.LastIndexOf('/');
        if (slash >= 0 && slash + 1 < resourcesAtlasPath.Length) {
            return resourcesAtlasPath.Substring(slash + 1);
        }
        return resourcesAtlasPath;
    }

    private sealed class OriginalSpriteResolver {
        private readonly string _originalProjectDir;

        private Dictionary<string, string> _assetPathByGuid;
        private readonly Dictionary<string, string> _resourcesPathByGuid = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private readonly Dictionary<string, Dictionary<long, string>> _internalIdToNameByGuid = new Dictionary<string, Dictionary<long, string>>(StringComparer.InvariantCultureIgnoreCase);

        public OriginalSpriteResolver(string originalProjectDir) {
            _originalProjectDir = originalProjectDir;
        }

        public bool TryResolveSprite(string spriteGuid, long spriteInternalId, out string resourcesAtlasPath, out string spriteName) {
            resourcesAtlasPath = null;
            spriteName = null;

            if (string.IsNullOrEmpty(spriteGuid) || spriteInternalId == 0) {
                return false;
            }

            resourcesAtlasPath = ResolveResourcesPathForGuid(spriteGuid);
            if (string.IsNullOrEmpty(resourcesAtlasPath)) {
                return false;
            }

            Dictionary<long, string> map = ResolveInternalIdToName(spriteGuid);
            if (map == null) {
                return false;
            }

            string n;
            if (map.TryGetValue(spriteInternalId, out n)) {
                spriteName = n;
                return !string.IsNullOrEmpty(spriteName);
            }

            return false;
        }

        private string ResolveResourcesPathForGuid(string guid) {
            string cached;
            if (_resourcesPathByGuid.TryGetValue(guid, out cached)) {
                return cached;
            }

            string assetPath = ResolveAssetPathForGuid(guid);
            if (string.IsNullOrEmpty(assetPath)) {
                _resourcesPathByGuid[guid] = null;
                return null;
            }

            // Map original Assets/Sprites/.../foo.png -> Resources.Load path: NSMB/Sprites/.../foo
            string normalized = assetPath.Replace('\\', '/');
            string resourcesPath = null;
            const string spritesPrefix = "Assets/Sprites/";
            const string resourcesPrefix = "Assets/Resources/";

            if (normalized.StartsWith(spritesPrefix, StringComparison.InvariantCultureIgnoreCase)) {
                resourcesPath = "NSMB/Sprites/" + normalized.Substring(spritesPrefix.Length);
            } else if (normalized.StartsWith(resourcesPrefix, StringComparison.InvariantCultureIgnoreCase)) {
                resourcesPath = normalized.Substring(resourcesPrefix.Length);
            }

            if (!string.IsNullOrEmpty(resourcesPath)) {
                int dot = resourcesPath.LastIndexOf('.');
                if (dot >= 0) {
                    resourcesPath = resourcesPath.Substring(0, dot);
                }
            }

            _resourcesPathByGuid[guid] = resourcesPath;
            return resourcesPath;
        }

        private Dictionary<long, string> ResolveInternalIdToName(string guid) {
            Dictionary<long, string> cached;
            if (_internalIdToNameByGuid.TryGetValue(guid, out cached)) {
                return cached;
            }

            string assetPath = ResolveAssetPathForGuid(guid);
            if (string.IsNullOrEmpty(assetPath)) {
                _internalIdToNameByGuid[guid] = null;
                return null;
            }

            string metaPath = Path.Combine(_originalProjectDir, assetPath.Replace('/', Path.DirectorySeparatorChar) + ".meta");
            if (!File.Exists(metaPath)) {
                _internalIdToNameByGuid[guid] = null;
                return null;
            }

            Dictionary<long, string> map = ParseInternalIdToNameTable(metaPath);
            _internalIdToNameByGuid[guid] = map;
            return map;
        }

        private string ResolveAssetPathForGuid(string guid) {
            EnsureGuidIndex();

            string p;
            if (_assetPathByGuid != null && _assetPathByGuid.TryGetValue(guid, out p)) {
                return p;
            }

            return null;
        }

        private void EnsureGuidIndex() {
            if (_assetPathByGuid != null) {
                return;
            }

            _assetPathByGuid = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            // Only index sprite/atlas assets (fast enough and matches our tilemap usage).
            string root = Path.Combine(_originalProjectDir, Path.Combine("Assets", "Sprites"));
            if (!Directory.Exists(root)) {
                return;
            }

            string[] metas = Directory.GetFiles(root, "*.meta", SearchOption.AllDirectories);
            for (int i = 0; i < metas.Length; i++) {
                string metaPath = metas[i];
                try {
                    string guid = ReadGuidFromMeta(metaPath);
                    if (string.IsNullOrEmpty(guid)) {
                        continue;
                    }

                    string assetPath = metaPath.Substring(_originalProjectDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (assetPath.EndsWith(".meta", StringComparison.InvariantCultureIgnoreCase)) {
                        assetPath = assetPath.Substring(0, assetPath.Length - ".meta".Length);
                    }

                    assetPath = assetPath.Replace('\\', '/');
                    if (!_assetPathByGuid.ContainsKey(guid)) {
                        _assetPathByGuid[guid] = assetPath;
                    }
                } catch (Exception) {
                    // ignore malformed meta
                }
            }
        }

        private static string ReadGuidFromMeta(string metaPath) {
            // Unity meta contains: guid: xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
            string[] lines = File.ReadAllLines(metaPath);
            for (int i = 0; i < lines.Length; i++) {
                string t = lines[i].Trim();
                if (t.StartsWith("guid:", StringComparison.InvariantCulture)) {
                    return t.Substring("guid:".Length).Trim();
                }
            }
            return null;
        }

        private static Dictionary<long, string> ParseInternalIdToNameTable(string metaPath) {
            Dictionary<long, string> map = new Dictionary<long, string>();
            string[] lines = File.ReadAllLines(metaPath);

            bool inTable = false;
            long pendingId = 0;
            bool havePendingId = false;

            for (int i = 0; i < lines.Length; i++) {
                string t = lines[i].Trim();

                if (!inTable) {
                    if (t.StartsWith("internalIDToNameTable:", StringComparison.InvariantCulture)) {
                        inTable = true;
                    }
                    continue;
                }

                if (t.StartsWith("externalObjects:", StringComparison.InvariantCulture) ||
                    t.StartsWith("spriteSheet:", StringComparison.InvariantCulture) ||
                    t.StartsWith("platformSettings:", StringComparison.InvariantCulture)) {
                    break;
                }

                // Entry looks like:
                // - first:
                //     213: <long>
                //   second: 0 SpriteName
                if (t.StartsWith("213:", StringComparison.InvariantCulture)) {
                    string s = t.Substring("213:".Length).Trim();
                    long id;
                    if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out id)) {
                        pendingId = id;
                        havePendingId = true;
                    }
                    continue;
                }

                if (havePendingId && t.StartsWith("second:", StringComparison.InvariantCulture)) {
                    string name = t.Substring("second:".Length).Trim();
                    // Unity prefixes with "0 " in many metas: "0 SpriteName"
                    int space = name.IndexOf(' ');
                    if (space >= 0) {
                        string maybeNum = name.Substring(0, space).Trim();
                        int dummy;
                        if (int.TryParse(maybeNum, out dummy)) {
                            name = name.Substring(space + 1).Trim();
                        }
                    }

                    if (!string.IsNullOrEmpty(name) && !map.ContainsKey(pendingId)) {
                        map[pendingId] = name;
                    }

                    havePendingId = false;
                    pendingId = 0;
                }
            }

            return map;
        }
    }

    private sealed class OriginalTileColliderResolver {
        private readonly string _originalProjectDir;

        private Dictionary<string, string> _assetPathByGuid;
        private readonly Dictionary<string, int> _defaultColliderByGuid = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

        public OriginalTileColliderResolver(string originalProjectDir) {
            _originalProjectDir = originalProjectDir;
        }

        public int GetDefaultColliderType(string tileAssetGuid) {
            if (string.IsNullOrEmpty(tileAssetGuid)) {
                return 0;
            }

            int cached;
            if (_defaultColliderByGuid.TryGetValue(tileAssetGuid, out cached)) {
                return cached;
            }

            EnsureIndex();

            string rel;
            if (_assetPathByGuid == null || !_assetPathByGuid.TryGetValue(tileAssetGuid, out rel) || string.IsNullOrEmpty(rel)) {
                _defaultColliderByGuid[tileAssetGuid] = 0;
                return 0;
            }

            string abs = Path.Combine(_originalProjectDir, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(abs)) {
                _defaultColliderByGuid[tileAssetGuid] = 0;
                return 0;
            }

            int ct = ParseDefaultColliderType(abs);
            _defaultColliderByGuid[tileAssetGuid] = ct;
            return ct;
        }

        private void EnsureIndex() {
            if (_assetPathByGuid != null) {
                return;
            }

            _assetPathByGuid = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            // Tiles are stored under Resources/Tilemaps/Tiles in the original project.
            string root = Path.Combine(_originalProjectDir, Path.Combine("Assets", Path.Combine("Resources", Path.Combine("Tilemaps", "Tiles"))));
            if (!Directory.Exists(root)) {
                return;
            }

            string[] metas = Directory.GetFiles(root, "*.meta", SearchOption.AllDirectories);
            for (int i = 0; i < metas.Length; i++) {
                string metaPath = metas[i];
                try {
                    string guid = ReadGuidFromMetaLocal(metaPath);
                    if (string.IsNullOrEmpty(guid)) {
                        continue;
                    }

                    string assetPath = metaPath.Substring(_originalProjectDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (assetPath.EndsWith(".meta", StringComparison.InvariantCultureIgnoreCase)) {
                        assetPath = assetPath.Substring(0, assetPath.Length - ".meta".Length);
                    }

                    assetPath = assetPath.Replace('\\', '/');
                    if (!_assetPathByGuid.ContainsKey(guid)) {
                        _assetPathByGuid[guid] = assetPath;
                    }
                } catch (Exception) {
                    // ignore
                }
            }
        }

        private static int ParseDefaultColliderType(string tileAssetAbsPath) {
            // Look for either:
            //   m_DefaultColliderType: <int>
            // or (fallback)
            //   m_ColliderType: <int>
            try {
                string[] lines = File.ReadAllLines(tileAssetAbsPath);
                for (int i = 0; i < lines.Length; i++) {
                    string t = lines[i].Trim();
                    if (t.StartsWith("m_DefaultColliderType:", StringComparison.InvariantCulture)) {
                        return ParseIntAfterColonLocal(t);
                    }
                }

                for (int i = 0; i < lines.Length; i++) {
                    string t = lines[i].Trim();
                    if (t.StartsWith("m_ColliderType:", StringComparison.InvariantCulture)) {
                        return ParseIntAfterColonLocal(t);
                    }
                }
            } catch (Exception) {
                // ignore
            }

            return 0;
        }

        private static string ReadGuidFromMetaLocal(string metaPath) {
            string[] lines = File.ReadAllLines(metaPath);
            for (int i = 0; i < lines.Length; i++) {
                string t = lines[i].Trim();
                if (t.StartsWith("guid:", StringComparison.InvariantCulture)) {
                    return t.Substring("guid:".Length).Trim();
                }
            }
            return null;
        }

        private static int ParseIntAfterColonLocal(string lineTrimmed) {
            int colon = lineTrimmed.IndexOf(':');
            if (colon < 0) {
                return 0;
            }
            string s = lineTrimmed.Substring(colon + 1).Trim();
            int v;
            int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v);
            return v;
        }
    }

    private sealed class OriginalGenericMoverAssetResolver {
        private readonly string _originalProjectDir;
        private Dictionary<long, string> _assetByGuidValue;
        private readonly Dictionary<long, NSMB.World.StagePathNode[]> _pathCache = new Dictionary<long, NSMB.World.StagePathNode[]>();
        private readonly Dictionary<long, int> _loopModeCache = new Dictionary<long, int>();

        public OriginalGenericMoverAssetResolver(string originalProjectDir) {
            _originalProjectDir = originalProjectDir;
        }

        public bool TryLoadPath(long guidValue, out NSMB.World.StagePathNode[] path, out int loopMode) {
            path = null;
            loopMode = 0;

            if (guidValue == 0) {
                return false;
            }

            NSMB.World.StagePathNode[] cached;
            if (_pathCache.TryGetValue(guidValue, out cached)) {
                path = cached;
                int lm;
                if (_loopModeCache.TryGetValue(guidValue, out lm)) {
                    loopMode = lm;
                }
                return path != null && path.Length > 0;
            }

            EnsureIndex();
            string abs;
            if (_assetByGuidValue == null || !_assetByGuidValue.TryGetValue(guidValue, out abs) || string.IsNullOrEmpty(abs)) {
                _pathCache[guidValue] = null;
                _loopModeCache[guidValue] = 0;
                return false;
            }

            NSMB.World.StagePathNode[] nodes;
            int lm2;
            if (!TryParseGenericMoverAsset(abs, out nodes, out lm2)) {
                _pathCache[guidValue] = null;
                _loopModeCache[guidValue] = 0;
                return false;
            }

            _pathCache[guidValue] = nodes;
            _loopModeCache[guidValue] = lm2;
            path = nodes;
            loopMode = lm2;
            return path != null && path.Length > 0;
        }

        private void EnsureIndex() {
            if (_assetByGuidValue != null) {
                return;
            }

            _assetByGuidValue = new Dictionary<long, string>();

            string root = Path.Combine(_originalProjectDir, Path.Combine("Assets", Path.Combine("QuantumUser", Path.Combine("Resources", Path.Combine("AssetObjects", "World")))));
            if (!Directory.Exists(root)) {
                return;
            }

            string[] assets = Directory.GetFiles(root, "*.asset", SearchOption.AllDirectories);
            for (int i = 0; i < assets.Length; i++) {
                string p = assets[i];
                try {
                    long id;
                    if (TryReadIdentifierGuidValue(p, out id) && id != 0) {
                        if (!_assetByGuidValue.ContainsKey(id)) {
                            _assetByGuidValue[id] = p;
                        }
                    }
                } catch (Exception) {
                    // ignore
                }
            }
        }

        private static bool TryParseLongAfterColonLocal(string lineTrimmed, out long value) {
            value = 0;
            int colon = lineTrimmed.IndexOf(':');
            if (colon < 0) {
                return false;
            }
            string s = lineTrimmed.Substring(colon + 1).Trim();
            return long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryReadIdentifierGuidValue(string assetPath, out long guidValue) {
            guidValue = 0;
            string[] lines = File.ReadAllLines(assetPath);

            bool inIdentifier = false;
            bool inGuid = false;
            for (int i = 0; i < lines.Length; i++) {
                string t = lines[i].Trim();
                if (!inIdentifier) {
                    if (t == "Identifier:" || t.EndsWith("Identifier:", StringComparison.InvariantCulture)) {
                        inIdentifier = true;
                    }
                    continue;
                }

                if (!inGuid) {
                    if (t == "Guid:" || t.EndsWith("Guid:", StringComparison.InvariantCulture)) {
                        inGuid = true;
                    }
                    continue;
                }

                if (t.StartsWith("Value:", StringComparison.InvariantCulture)) {
                    string s = t.Substring("Value:".Length).Trim();
                    long v;
                    if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) {
                        guidValue = v;
                        return true;
                    }
                    return false;
                }

                // Stop scanning if we moved past Identifier block.
                if (t.StartsWith("ObjectPath:", StringComparison.InvariantCulture) || t.StartsWith("LoopMode:", StringComparison.InvariantCulture)) {
                    break;
                }
            }

            return false;
        }

        private static bool TryParseGenericMoverAsset(string assetAbsPath, out NSMB.World.StagePathNode[] nodes, out int loopMode) {
            nodes = null;
            loopMode = 0;

            string[] lines = File.ReadAllLines(assetAbsPath);
            bool inPath = false;
            List<NSMB.World.StagePathNode> list = new List<NSMB.World.StagePathNode>();

            NSMB.World.StagePathNode current = new NSMB.World.StagePathNode();
            bool haveNode = false;

            bool inPos = false;
            bool inPX = false;
            bool inPY = false;
            bool inDur = false;

            for (int i = 0; i < lines.Length; i++) {
                string raw = lines[i];
                string t = raw.Trim();

                if (t.StartsWith("ObjectPath:", StringComparison.InvariantCulture)) {
                    inPath = true;
                    continue;
                }

                if (t.StartsWith("LoopMode:", StringComparison.InvariantCulture)) {
                    int v;
                    if (int.TryParse(t.Substring("LoopMode:".Length).Trim(), out v)) {
                        loopMode = v;
                    }
                    continue;
                }

                if (!inPath) {
                    continue;
                }

                if (t.StartsWith("- Position:", StringComparison.InvariantCulture)) {
                    if (haveNode) {
                        list.Add(current);
                    }
                    haveNode = true;
                    current = new NSMB.World.StagePathNode();
                    inPos = true;
                    inPX = false;
                    inPY = false;
                    inDur = false;
                    continue;
                }

                if (!haveNode) {
                    continue;
                }

                if (t == "Position:" || t.EndsWith("Position:", StringComparison.InvariantCulture)) {
                    inPos = true;
                    continue;
                }

                if (inPos) {
                    if (t == "X:" || t.EndsWith("X:", StringComparison.InvariantCulture)) { inPX = true; inPY = false; continue; }
                    if (t == "Y:" || t.EndsWith("Y:", StringComparison.InvariantCulture)) { inPY = true; inPX = false; continue; }
                    if (t.StartsWith("RawValue:", StringComparison.InvariantCulture)) {
                        long rv;
                        if (TryParseLongAfterColonLocal(t, out rv)) {
                            float f = rv * FpScale;
                            if (inPX) {
                                current.position.x = f;
                            } else if (inPY) {
                                current.position.y = f;
                            }
                        }
                        continue;
                    }
                }

                if (t.StartsWith("TravelDuration:", StringComparison.InvariantCulture) || t.EndsWith("TravelDuration:", StringComparison.InvariantCulture)) {
                    inDur = true;
                    continue;
                }
                if (inDur && t.StartsWith("RawValue:", StringComparison.InvariantCulture)) {
                    long rv;
                    if (TryParseLongAfterColonLocal(t, out rv)) {
                        current.travelDurationSeconds = rv * FpScale;
                    }
                    inDur = false;
                    continue;
                }

                if (t.StartsWith("EaseIn:", StringComparison.InvariantCulture)) {
                    int v = 0;
                    int.TryParse(t.Substring("EaseIn:".Length).Trim(), out v);
                    current.easeIn = (v != 0);
                    continue;
                }
                if (t.StartsWith("EaseOut:", StringComparison.InvariantCulture)) {
                    int v = 0;
                    int.TryParse(t.Substring("EaseOut:".Length).Trim(), out v);
                    current.easeOut = (v != 0);
                    continue;
                }

                // End of object path if we hit a new top-level field.
                if (raw.StartsWith("  ", StringComparison.InvariantCulture) == false && t.EndsWith(":", StringComparison.InvariantCulture)) {
                    break;
                }
            }

            if (haveNode) {
                list.Add(current);
            }

            nodes = list.ToArray();
            return nodes.Length > 0;
        }
    }

    private struct StageDataInfo {
        public Vector2 spawnPoint;
        public Vector2 cameraMin;
        public Vector2 cameraMax;
        public bool isWrappingLevel;
    }

    private static StageDataInfo TryGetStageDataInfo(string originalProjectDir, string mapAssetPath) {
        // Find sibling StageData for this map asset and read spawn/camera settings.
        StageDataInfo info = new StageDataInfo();
        info.spawnPoint = Vector2.zero;
        info.cameraMin = Vector2.zero;
        info.cameraMax = Vector2.zero;
        info.isWrappingLevel = false;

        if (string.IsNullOrEmpty(mapAssetPath) || !File.Exists(mapAssetPath)) {
            return info;
        }

        long userAssetId = ParseLongField(mapAssetPath, "UserAsset:", "Value:");
        if (userAssetId == 0) {
            return info;
        }

        string dir = Path.GetDirectoryName(mapAssetPath);
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) {
            return info;
        }

        string[] candidates = Directory.GetFiles(dir, "*StageData.asset", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < candidates.Length; i++) {
            string p = candidates[i];
            try {
                string txt = File.ReadAllText(p);
                if (txt.IndexOf("Guid:", StringComparison.InvariantCulture) < 0) {
                    continue;
                }
                if (txt.IndexOf("Value: " + userAssetId.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCulture) < 0) {
                    continue;
                }

                Vector2? spawn = ParseFpVec2FromBlock(txt, "Spawnpoint:");
                if (spawn.HasValue) {
                    info.spawnPoint = spawn.Value * ImportScale;
                }

                Vector2? camMin = ParseFpVec2FromBlock(txt, "CameraMinPosition:");
                Vector2? camMax = ParseFpVec2FromBlock(txt, "CameraMaxPosition:");
                if (camMin.HasValue && camMax.HasValue) {
                    info.cameraMin = camMin.Value * ImportScale;
                    info.cameraMax = camMax.Value * ImportScale;
                }

                bool? wrap = ParseBoolField(txt, "IsWrappingLevel:");
                if (wrap.HasValue) {
                    info.isWrappingLevel = wrap.Value;
                }

                return info;
            } catch (Exception) {
                // ignore
            }
        }

        return info;
    }

    private static long ParseLongField(string filePath, string anchor, string valuePrefix) {
        try {
            string[] lines = File.ReadAllLines(filePath);
            bool inAnchor = false;
            for (int i = 0; i < lines.Length; i++) {
                string t = lines[i].Trim();
                if (!inAnchor) {
                    if (t == anchor.TrimEnd()) {
                        inAnchor = true;
                    }
                    continue;
                }

                if (t.StartsWith(valuePrefix, StringComparison.InvariantCulture)) {
                    string s = t.Substring(valuePrefix.Length).Trim();
                    long v;
                    if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) {
                        return v;
                    }
                    return 0;
                }
            }
        } catch (Exception) {
            return 0;
        }
        return 0;
    }

    private static Vector2? ParseFpVec2FromBlock(string text, string blockName) {
        // Very small parser for:
        // Spawnpoint:
        //   X:
        //     RawValue: -950272
        //   Y:
        //     RawValue: 147456
        try {
            int idx = text.IndexOf(blockName, StringComparison.InvariantCulture);
            if (idx < 0) {
                return null;
            }
            string tail = text.Substring(idx);

            long? x = TryParseRawValue(tail, "X:");
            long? y = TryParseRawValue(tail, "Y:");
            if (!x.HasValue || !y.HasValue) {
                return null;
            }

            return new Vector2(x.Value * FpScale, y.Value * FpScale);
        } catch (Exception) {
            return null;
        }
    }

    private static long? TryParseRawValue(string text, string axisHeader) {
        int idx = text.IndexOf(axisHeader, StringComparison.InvariantCulture);
        if (idx < 0) return null;
        string tail = text.Substring(idx);
        int rv = tail.IndexOf("RawValue:", StringComparison.InvariantCulture);
        if (rv < 0) return null;
        tail = tail.Substring(rv + "RawValue:".Length).TrimStart();
        int end = 0;
        while (end < tail.Length && (char.IsDigit(tail[end]) || tail[end] == '-' || tail[end] == '+')) {
            end++;
        }
        string num = tail.Substring(0, end).Trim();
        long v;
        if (long.TryParse(num, NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) {
            return v;
        }
        return null;
    }

    private static bool? ParseBoolField(string text, string fieldName) {
        // Field format: IsWrappingLevel: 1
        int idx = text.IndexOf(fieldName, StringComparison.InvariantCulture);
        if (idx < 0) return null;
        string tail = text.Substring(idx);
        int colon = tail.IndexOf(':');
        if (colon < 0) return null;
        tail = tail.Substring(colon + 1).TrimStart();
        int end = 0;
        while (end < tail.Length && (char.IsDigit(tail[end]) || tail[end] == '-' || tail[end] == '+')) {
            end++;
        }
        string num = tail.Substring(0, end).Trim();
        int v;
        if (int.TryParse(num, NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) {
            return v != 0;
        }
        return null;
    }

    private sealed class SceneTilemapData {
        public string name;
        public Vector3 worldPosition;
        public Vector3 worldScale;
        public int sortingOrder;
        public readonly List<SceneTile> tiles = new List<SceneTile>();
        public readonly List<SceneSpriteRef> spriteRefs = new List<SceneSpriteRef>();
        public readonly List<SceneMatrix> matrices = new List<SceneMatrix>();
        public readonly List<SceneTileAssetRef> tileAssets = new List<SceneTileAssetRef>();
    }

    private struct SceneSpriteRef {
        public long fileId;
        public string guid;
    }

    private struct SceneTileAssetRef {
        public long fileId;
        public string guid;
    }

    private struct SceneMatrix {
        public float e00;
        public float e01;
        public float e10;
        public float e11;
    }

    private struct SceneTile {
        public int x;
        public int y;
        public int tileAssetIndex;
        public int spriteIndex;
        public int matrixIndex;
    }

    private struct StageEntityData {
        public NSMB.World.StageEntityKind kind;
        public Vector2 position;
        public int variant;
        public Vector2 size;
        public Vector2 colliderOffset;
        public Vector2 velocity;
        public bool isTrigger;
        public long moverAssetGuidValue;
        public long startOffsetRaw;

        // Optional extra params (see StageDefinition.StageEntity param0/1/2)
        public float param0;
        public float param1;
        public float param2;

        // Bullet bill launcher specifics (raw/ids from Quantum assets)
        public int shootFrames;
        public long minRadiusRaw;
        public long maxRadiusRaw;
        public long bulletPrototypeIdValue;
    }

    private static class UnitySceneParsers {
        private struct TransformInfo {
            public long transformId;
            public long gameObjectId;
            public long parentTransformId;
            public Vector3 localPosition;
            public Vector3 localScale;
        }

        private struct WorldTransform {
            public Vector3 position;
            public Vector3 scale;
        }

        private struct TilemapComponent {
            public long gameObjectId;
            public List<SceneTile> tiles;
            public List<SceneSpriteRef> spriteRefs;
            public List<SceneMatrix> matrices;
            public List<SceneTileAssetRef> tileAssets;
        }

        public static List<SceneTilemapData> ExtractTilemaps(string scenePath) {
            string[] lines = File.ReadAllLines(scenePath);

            Dictionary<long, string> gameObjectNames = new Dictionary<long, string>();
            Dictionary<long, TransformInfo> transformsById = new Dictionary<long, TransformInfo>();
            Dictionary<long, long> transformIdByGameObject = new Dictionary<long, long>();
            Dictionary<long, int> sortingByGameObject = new Dictionary<long, int>();
            List<TilemapComponent> tilemaps = new List<TilemapComponent>();

            ParseBlocks(lines, gameObjectNames, transformsById, transformIdByGameObject, sortingByGameObject, tilemaps);

            if (tilemaps.Count == 0) {
                return new List<SceneTilemapData>();
            }

            List<SceneTilemapData> results = new List<SceneTilemapData>();
            for (int i = 0; i < tilemaps.Count; i++) {
                TilemapComponent tm = tilemaps[i];
                long goId = tm.gameObjectId;

                SceneTilemapData data = new SceneTilemapData();
                data.name = gameObjectNames.ContainsKey(goId) ? gameObjectNames[goId] : "Tilemap";
                data.sortingOrder = sortingByGameObject.ContainsKey(goId) ? sortingByGameObject[goId] : 0;
                data.tiles.AddRange(tm.tiles);
                if (tm.spriteRefs != null) data.spriteRefs.AddRange(tm.spriteRefs);
                if (tm.matrices != null) data.matrices.AddRange(tm.matrices);
                if (tm.tileAssets != null) data.tileAssets.AddRange(tm.tileAssets);

                WorldTransform wt = ResolveWorldTransform(goId, transformsById, transformIdByGameObject);
                data.worldPosition = wt.position;
                data.worldScale = wt.scale;

                results.Add(data);
            }

            // Stable order: by sortingOrder, then name.
            results.Sort(delegate(SceneTilemapData a, SceneTilemapData b) {
                int c = a.sortingOrder.CompareTo(b.sortingOrder);
                if (c != 0) return c;
                return string.Compare(a.name, b.name, StringComparison.InvariantCultureIgnoreCase);
            });

            return results;
        }

        public static Vector2 ExtractFirstPlayerSpawn(string scenePath) {
            // Look for PrefabInstance modifications like:
            // propertyPath: Prototype.Spawnpoint.X.RawValue
            // value: -606208
            string[] lines = File.ReadAllLines(scenePath);

            long? rawX = null;
            long? rawY = null;

            for (int i = 0; i < lines.Length; i++) {
                string line = lines[i];
                if (line.IndexOf("propertyPath: Prototype.Spawnpoint.X.RawValue", StringComparison.InvariantCulture) >= 0) {
                    rawX = TryReadNextValueLong(lines, i);
                } else if (line.IndexOf("propertyPath: Prototype.Spawnpoint.Y.RawValue", StringComparison.InvariantCulture) >= 0) {
                    rawY = TryReadNextValueLong(lines, i);
                }

                if (rawX.HasValue && rawY.HasValue) {
                    break;
                }
            }

            if (!rawX.HasValue || !rawY.HasValue) {
                return Vector2.zero;
            }

            float x = rawX.Value * FpScale * ImportScale;
            float y = rawY.Value * FpScale * ImportScale;
            return new Vector2(x, y);
        }

        private static long? TryReadNextValueLong(string[] lines, int index) {
            for (int j = index + 1; j < Mathf.Min(lines.Length, index + 6); j++) {
                string t = lines[j].Trim();
                if (t.StartsWith("value:", StringComparison.InvariantCulture)) {
                    string s = t.Substring("value:".Length).Trim();
                    long v;
                    if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) {
                        return v;
                    }
                    return null;
                }
            }
            return null;
        }

        private static void ParseBlocks(
            string[] lines,
            Dictionary<long, string> gameObjectNames,
            Dictionary<long, TransformInfo> transformsById,
            Dictionary<long, long> transformIdByGameObject,
            Dictionary<long, int> sortingByGameObject,
            List<TilemapComponent> tilemaps) {

            int i = 0;
            while (i < lines.Length) {
                string line = lines[i];
                if (!line.StartsWith("--- !u!", StringComparison.InvariantCulture)) {
                    i++;
                    continue;
                }

                long fileId = ParseFileId(line);
                int start = i;
                int end = i + 1;
                while (end < lines.Length && !lines[end].StartsWith("--- !u!", StringComparison.InvariantCulture)) {
                    end++;
                }

                string kind = (start + 1 < end) ? lines[start + 1].Trim() : string.Empty;
                if (kind == "GameObject:") {
                    ParseGameObject(fileId, lines, start + 1, end, gameObjectNames);
                } else if (kind == "Transform:") {
                    TransformInfo ti;
                    if (TryParseTransform(fileId, lines, start + 1, end, out ti)) {
                        transformsById[fileId] = ti;
                        transformIdByGameObject[ti.gameObjectId] = fileId;
                    }
                } else if (kind == "Tilemap:") {
                    TilemapComponent tm;
                    if (TryParseTilemap(lines, start + 1, end, out tm)) {
                        tilemaps.Add(tm);
                    }
                } else if (kind == "TilemapRenderer:") {
                    long goId;
                    int sortingOrder;
                    if (TryParseTilemapRenderer(lines, start + 1, end, out goId, out sortingOrder)) {
                        sortingByGameObject[goId] = sortingOrder;
                    }
                }

                i = end;
            }
        }

        private static long ParseFileId(string headerLine) {
            int amp = headerLine.IndexOf('&');
            if (amp < 0) {
                return 0;
            }
            string s = headerLine.Substring(amp + 1).Trim();
            long id;
            if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out id)) {
                return id;
            }
            return 0;
        }

        private static void ParseGameObject(long fileId, string[] lines, int start, int end, Dictionary<long, string> gameObjectNames) {
            for (int i = start; i < end; i++) {
                string t = lines[i].Trim();
                if (t.StartsWith("m_Name:", StringComparison.InvariantCulture)) {
                    string name = t.Substring("m_Name:".Length).Trim();
                    gameObjectNames[fileId] = name;
                    return;
                }
            }
        }

        private static bool TryParseTransform(long transformId, string[] lines, int start, int end, out TransformInfo ti) {
            ti = new TransformInfo();
            ti.transformId = transformId;
            ti.parentTransformId = 0;
            ti.localPosition = Vector3.zero;
            ti.localScale = Vector3.one;

            for (int i = start; i < end; i++) {
                string t = lines[i].Trim();

                if (t.StartsWith("m_GameObject:", StringComparison.InvariantCulture)) {
                    ti.gameObjectId = ParseFileIdFromBraces(t);
                } else if (t.StartsWith("m_LocalPosition:", StringComparison.InvariantCulture)) {
                    ti.localPosition = ParseVector3(t);
                } else if (t.StartsWith("m_LocalScale:", StringComparison.InvariantCulture)) {
                    ti.localScale = ParseVector3(t);
                } else if (t.StartsWith("m_Father:", StringComparison.InvariantCulture)) {
                    ti.parentTransformId = ParseFileIdFromBraces(t);
                }
            }

            return ti.gameObjectId != 0;
        }

        private static WorldTransform ResolveWorldTransform(long gameObjectId, Dictionary<long, TransformInfo> transformsById, Dictionary<long, long> transformIdByGameObject) {
            WorldTransform wt = new WorldTransform();
            wt.position = Vector3.zero;
            wt.scale = Vector3.one;

            long transformId;
            if (!transformIdByGameObject.TryGetValue(gameObjectId, out transformId)) {
                return wt;
            }

            Dictionary<long, WorldTransform> cache = new Dictionary<long, WorldTransform>();
            return ResolveWorldTransformById(transformId, transformsById, cache);
        }

        private static WorldTransform ResolveWorldTransformById(long transformId, Dictionary<long, TransformInfo> transformsById, Dictionary<long, WorldTransform> cache) {
            WorldTransform cached;
            if (cache.TryGetValue(transformId, out cached)) {
                return cached;
            }

            TransformInfo ti;
            if (!transformsById.TryGetValue(transformId, out ti)) {
                WorldTransform wt0;
                wt0.position = Vector3.zero;
                wt0.scale = Vector3.one;
                cache[transformId] = wt0;
                return wt0;
            }

            WorldTransform wt;
            if (ti.parentTransformId != 0) {
                WorldTransform parent = ResolveWorldTransformById(ti.parentTransformId, transformsById, cache);
                wt.scale = Vector3.Scale(parent.scale, ti.localScale);
                wt.position = parent.position + Vector3.Scale(parent.scale, ti.localPosition);
            } else {
                wt.scale = ti.localScale;
                wt.position = ti.localPosition;
            }

            cache[transformId] = wt;
            return wt;
        }

        private static bool TryParseTilemap(string[] lines, int start, int end, out TilemapComponent tm) {
            tm = new TilemapComponent();
            tm.tiles = new List<SceneTile>();
            tm.spriteRefs = new List<SceneSpriteRef>();
            tm.matrices = new List<SceneMatrix>();
            tm.tileAssets = new List<SceneTileAssetRef>();

            bool inTiles = false;
            bool inSpriteArray = false;
            bool inMatrixArray = false;
            bool inTileAssetArray = false;

            SceneTile currentTile = new SceneTile();
            bool haveTile = false;

            bool haveMatrix = false;
            SceneMatrix currentMatrix = new SceneMatrix();
            bool inMatrixData = false;

            for (int i = start; i < end; i++) {
                string raw = lines[i];
                string t = raw.Trim();

                if (t.StartsWith("m_GameObject:", StringComparison.InvariantCulture)) {
                    tm.gameObjectId = ParseFileIdFromBraces(t);
                }

                // Section switches (top-level fields)
                if (t == "m_Tiles:") {
                    inTiles = true;
                    inSpriteArray = false;
                    inMatrixArray = false;
                    inTileAssetArray = false;
                    continue;
                }
                if (t == "m_TileSpriteArray:") {
                    if (haveTile) {
                        tm.tiles.Add(currentTile);
                        haveTile = false;
                    }
                    inTiles = false;
                    inSpriteArray = true;
                    inMatrixArray = false;
                    inTileAssetArray = false;
                    continue;
                }
                if (t == "m_TileMatrixArray:") {
                    if (haveTile) {
                        tm.tiles.Add(currentTile);
                        haveTile = false;
                    }
                    inTiles = false;
                    inSpriteArray = false;
                    inMatrixArray = true;
                    inTileAssetArray = false;
                    continue;
                }
                if (t == "m_TileAssetArray:") {
                    if (haveTile) {
                        tm.tiles.Add(currentTile);
                        haveTile = false;
                    }
                    inTiles = false;
                    inSpriteArray = false;
                    inMatrixArray = false;
                    inTileAssetArray = true;
                    continue;
                }

                // If we hit another top-level field, end current specialized parsing blocks.
                if (raw.StartsWith("  m_", StringComparison.InvariantCulture) &&
                    !raw.StartsWith("  m_Tiles:", StringComparison.InvariantCulture) &&
                    !raw.StartsWith("  m_TileSpriteArray:", StringComparison.InvariantCulture) &&
                    !raw.StartsWith("  m_TileMatrixArray:", StringComparison.InvariantCulture) &&
                    !raw.StartsWith("  m_TileAssetArray:", StringComparison.InvariantCulture)) {
                    inTiles = false;
                    inSpriteArray = false;
                    inMatrixArray = false;
                    inTileAssetArray = false;
                    inMatrixData = false;
                }

                if (inTiles) {
                    if (t.StartsWith("- first:", StringComparison.InvariantCulture)) {
                        if (haveTile) {
                            tm.tiles.Add(currentTile);
                        }
                        haveTile = true;
                        currentTile = new SceneTile();
                        currentTile.tileAssetIndex = -1;
                        currentTile.spriteIndex = -1;
                        currentTile.matrixIndex = 0;
                        ParseFirstCoords(t, out currentTile.x, out currentTile.y);
                    } else if (haveTile && t.StartsWith("m_TileIndex:", StringComparison.InvariantCulture)) {
                        currentTile.tileAssetIndex = ParseIntAfterColon(t);
                    } else if (haveTile && t.StartsWith("m_TileSpriteIndex:", StringComparison.InvariantCulture)) {
                        currentTile.spriteIndex = ParseIntAfterColon(t);
                    } else if (haveTile && t.StartsWith("m_TileMatrixIndex:", StringComparison.InvariantCulture)) {
                        currentTile.matrixIndex = ParseIntAfterColon(t);
                    }
                    continue;
                }

                if (inSpriteArray) {
                    // Each sprite entry includes "m_Data: {fileID: ..., guid: ..., type: 3}"
                    if (t.IndexOf("m_Data:", StringComparison.InvariantCulture) >= 0) {
                        long fileId;
                        string guid;
                        if (TryParseFileIdAndGuidFromInlineMapping(lines, ref i, out fileId, out guid)) {
                            SceneSpriteRef sr;
                            sr.fileId = fileId;
                            sr.guid = guid;
                            tm.spriteRefs.Add(sr);
                        }
                    }
                    continue;
                }

                if (inMatrixArray) {
                    if (t.StartsWith("- m_RefCount:", StringComparison.InvariantCulture)) {
                        if (haveMatrix) {
                            tm.matrices.Add(currentMatrix);
                            haveMatrix = false;
                        }
                        inMatrixData = false;
                        continue;
                    }

                    if (t.StartsWith("m_Data:", StringComparison.InvariantCulture)) {
                        haveMatrix = true;
                        inMatrixData = true;
                        currentMatrix = new SceneMatrix();
                        continue;
                    }

                    if (!inMatrixData || !haveMatrix) {
                        continue;
                    }

                    // Matrix serialization uses e00..e33 entries. We only need 2D components.
                    if (t.StartsWith("e00:", StringComparison.InvariantCulture)) currentMatrix.e00 = ParseFloatAfterColon(t);
                    else if (t.StartsWith("e01:", StringComparison.InvariantCulture)) currentMatrix.e01 = ParseFloatAfterColon(t);
                    else if (t.StartsWith("e10:", StringComparison.InvariantCulture)) currentMatrix.e10 = ParseFloatAfterColon(t);
                    else if (t.StartsWith("e11:", StringComparison.InvariantCulture)) currentMatrix.e11 = ParseFloatAfterColon(t);
                    continue;
                }

                if (inTileAssetArray) {
                    if (t.IndexOf("m_Data:", StringComparison.InvariantCulture) >= 0) {
                        long fileId;
                        string guid;
                        if (TryParseFileIdAndGuidFromInlineMapping(lines, ref i, out fileId, out guid)) {
                            SceneTileAssetRef tr;
                            tr.fileId = fileId;
                            tr.guid = guid;
                            tm.tileAssets.Add(tr);
                        }
                    }
                    continue;
                }
            }

            if (haveTile) {
                tm.tiles.Add(currentTile);
            }
            if (haveMatrix) {
                tm.matrices.Add(currentMatrix);
            }

            return tm.gameObjectId != 0 && tm.tiles.Count > 0;
        }

        private static bool TryParseFileIdAndGuidFromInlineMapping(string[] lines, ref int index, out long fileId, out string guid) {
            fileId = 0;
            guid = string.Empty;

            // Some Unity YAML wraps {fileID: ..., guid: ..., type: 3} across multiple lines.
            string combined = lines[index].Trim();
            int safety = 0;
            while (combined.IndexOf("}", StringComparison.InvariantCulture) < 0 && index + 1 < lines.Length && safety < 6) {
                safety++;
                index++;
                combined += " " + lines[index].Trim();
            }

            int fid = combined.IndexOf("fileID:", StringComparison.InvariantCulture);
            if (fid >= 0) {
                fid += "fileID:".Length;
                string rest = combined.Substring(fid).Trim();
                int comma = rest.IndexOf(',');
                if (comma >= 0) rest = rest.Substring(0, comma);
                long v;
                if (long.TryParse(rest.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) {
                    fileId = v;
                }
            }

            int gid = combined.IndexOf("guid:", StringComparison.InvariantCulture);
            if (gid >= 0) {
                gid += "guid:".Length;
                string rest = combined.Substring(gid).Trim();
                int comma = rest.IndexOf(',');
                if (comma >= 0) rest = rest.Substring(0, comma);
                guid = rest.Trim();
            }

            return fileId != 0 && !string.IsNullOrEmpty(guid);
        }

        private static float ParseFloatAfterColon(string lineTrimmed) {
            int colon = lineTrimmed.IndexOf(':');
            if (colon < 0) {
                return 0f;
            }
            string s = lineTrimmed.Substring(colon + 1).Trim();
            float v;
            float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
            return v;
        }

        private static bool TryParseTilemapRenderer(string[] lines, int start, int end, out long gameObjectId, out int sortingOrder) {
            gameObjectId = 0;
            sortingOrder = 0;

            for (int i = start; i < end; i++) {
                string t = lines[i].Trim();
                if (t.StartsWith("m_GameObject:", StringComparison.InvariantCulture)) {
                    gameObjectId = ParseFileIdFromBraces(t);
                } else if (t.StartsWith("m_SortingOrder:", StringComparison.InvariantCulture)) {
                    sortingOrder = ParseIntAfterColon(t);
                }
            }

            return gameObjectId != 0;
        }

        private static long ParseFileIdFromBraces(string lineTrimmed) {
            // example: m_GameObject: {fileID: 643464983}
            int idx = lineTrimmed.IndexOf("fileID:", StringComparison.InvariantCulture);
            if (idx < 0) {
                return 0;
            }
            idx += "fileID:".Length;
            string rest = lineTrimmed.Substring(idx).Trim();
            int end = rest.IndexOf('}');
            if (end >= 0) {
                rest = rest.Substring(0, end);
            }
            long id;
            if (long.TryParse(rest.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out id)) {
                return id;
            }
            return 0;
        }

        private static Vector3 ParseVector3(string lineTrimmed) {
            // m_LocalPosition: {x: 0, y: 5, z: 0}
            int brace = lineTrimmed.IndexOf('{');
            if (brace < 0) {
                return Vector3.zero;
            }
            string inner = lineTrimmed.Substring(brace + 1);
            int end = inner.IndexOf('}');
            if (end >= 0) {
                inner = inner.Substring(0, end);
            }
            float x = ParseFloatField(inner, "x");
            float y = ParseFloatField(inner, "y");
            float z = ParseFloatField(inner, "z");
            return new Vector3(x, y, z);
        }

        private static float ParseFloatField(string inner, string key) {
            int idx = inner.IndexOf(key + ":", StringComparison.InvariantCulture);
            if (idx < 0) {
                return 0f;
            }
            idx += (key.Length + 1);
            string rest = inner.Substring(idx).Trim();
            int comma = rest.IndexOf(',');
            if (comma >= 0) {
                rest = rest.Substring(0, comma);
            }
            float v;
            float.TryParse(rest.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v);
            return v;
        }

        private static void ParseFirstCoords(string lineTrimmed, out int x, out int y) {
            // - first: {x: -18, y: -13, z: 0}
            int brace = lineTrimmed.IndexOf('{');
            x = 0;
            y = 0;
            if (brace < 0) {
                return;
            }
            string inner = lineTrimmed.Substring(brace + 1);
            int end = inner.IndexOf('}');
            if (end >= 0) {
                inner = inner.Substring(0, end);
            }

            x = (int)ParseFloatField(inner, "x");
            y = (int)ParseFloatField(inner, "y");
        }

        private static int ParseIntAfterColon(string lineTrimmed) {
            int colon = lineTrimmed.IndexOf(':');
            if (colon < 0) {
                return 0;
            }
            string s = lineTrimmed.Substring(colon + 1).Trim();
            int v;
            int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v);
            return v;
        }
    }

    private static class QuantumMapParsers {
        private sealed class RefInfo {
            public string className;
            public long posX;
            public long posY;

            public long velX;
            public long velY;
            public bool bool0;
            public bool bool1;

            public long boxExtX;
            public long boxExtY;
            public long offsetX;
            public long offsetY;
            public bool isTrigger;

            public long assetGuidValue;
            public long startOffsetRaw;

            // BulletBillLauncherPrototype
            public int shootFrames;
            public long minRadiusRaw;
            public long maxRadiusRaw;
            public long bulletPrototypeIdValue;
        }

        public static List<StageEntityData> ExtractEntities(string stageAssetPath) {
            string[] lines = File.ReadAllLines(stageAssetPath);
            List<List<long>> mapEntities = ParseMapEntities(lines);
            Dictionary<long, RefInfo> refsByRid = ParseRefIds(lines);

            List<StageEntityData> entities = new List<StageEntityData>();

            for (int i = 0; i < mapEntities.Count; i++) {
                List<long> comps = mapEntities[i];
                StageEntityData e = new StageEntityData();
                e.kind = NSMB.World.StageEntityKind.Unknown;
                e.variant = 0;
                e.size = Vector2.zero;
                e.colliderOffset = Vector2.zero;
                e.velocity = Vector2.zero;
                e.isTrigger = false;
                e.moverAssetGuidValue = 0;
                e.startOffsetRaw = 0;
                e.param0 = 0f;
                e.param1 = 0f;
                e.param2 = 0f;
                e.shootFrames = 0;
                e.minRadiusRaw = 0;
                e.maxRadiusRaw = 0;
                e.bulletPrototypeIdValue = 0;

                bool hasTransform = false;
                Vector2 pos = Vector2.zero;

                long boxExtX = 0;
                long boxExtY = 0;
                long offX = 0;
                long offY = 0;
                bool hasCollider = false;

                long velX = 0;
                long velY = 0;
                bool hasPlatform = false;
                bool ignoreMovement = false;

                long moverAsset = 0;
                long startOffset = 0;
                bool hasMover = false;

                int shootFrames = 0;
                long minRadiusRaw = 0;
                long maxRadiusRaw = 0;
                long bulletPrototypeIdValue = 0;
                bool hasBulletLauncher = false;

                for (int c = 0; c < comps.Count; c++) {
                    long rid = comps[c];
                    RefInfo info;
                    if (!refsByRid.TryGetValue(rid, out info) || info == null) {
                        continue;
                    }

                    if (info.className == "Transform2DPrototype") {
                        hasTransform = true;
                        pos = new Vector2(info.posX * FpScale, info.posY * FpScale);
                    } else if (info.className == "CoinPrototype") {
                        e.kind = NSMB.World.StageEntityKind.Coin;
                    } else if (info.className == "GoombaPrototype") {
                        e.kind = NSMB.World.StageEntityKind.Goomba;
                    } else if (info.className == "KoopaPrototype") {
                        e.kind = NSMB.World.StageEntityKind.Koopa;
                    } else if (info.className == "BooPrototype") {
                        e.kind = NSMB.World.StageEntityKind.Boo;
                    } else if (info.className == "BobombPrototype") {
                        e.kind = NSMB.World.StageEntityKind.Bobomb;
                    } else if (info.className == "PiranhaPlantPrototype") {
                        e.kind = NSMB.World.StageEntityKind.PiranhaPlant;
                    } else if (info.className == "SpinnerPrototype") {
                        e.kind = NSMB.World.StageEntityKind.Spinner;
                    } else if (info.className == "EnterablePipePrototype") {
                        e.kind = NSMB.World.StageEntityKind.EnterablePipe;
                    } else if (info.className == "MarioBrosPlatformPrototype") {
                        e.kind = NSMB.World.StageEntityKind.MarioBrosPlatform;
                    } else if (info.className == "BreakableObjectPrototype") {
                        e.kind = NSMB.World.StageEntityKind.BreakableBlock;
                    } else if (info.className == "InvisibleBlockPrototype") {
                        e.kind = NSMB.World.StageEntityKind.InvisibleBlock;
                    } else if (info.className == "PhysicsCollider2DPrototype") {
                        hasCollider = true;
                        boxExtX = info.boxExtX;
                        boxExtY = info.boxExtY;
                        offX = info.offsetX;
                        offY = info.offsetY;
                        e.isTrigger = info.isTrigger;
                    } else if (info.className == "MovingPlatformPrototype") {
                        hasPlatform = true;
                        velX = info.velX;
                        velY = info.velY;
                        ignoreMovement = info.bool0;
                    } else if (info.className == "GenericMoverPrototype") {
                        hasMover = true;
                        moverAsset = info.assetGuidValue;
                        startOffset = info.startOffsetRaw;
                    } else if (info.className == "BulletBillLauncherPrototype") {
                        hasBulletLauncher = true;
                        shootFrames = info.shootFrames;
                        minRadiusRaw = info.minRadiusRaw;
                        maxRadiusRaw = info.maxRadiusRaw;
                        bulletPrototypeIdValue = info.bulletPrototypeIdValue;
                        e.kind = NSMB.World.StageEntityKind.BulletBillLauncher;
                    }
                }

                if (hasPlatform && hasTransform) {
                    e.kind = NSMB.World.StageEntityKind.MovingPlatform;

                    // Collider size: BoxExtents are half-extents (FP). Convert to world size (Unity) later via ImportScale.
                    if (hasCollider && boxExtX != 0 && boxExtY != 0) {
                        float sx = (boxExtX * FpScale) * 2f;
                        float sy = (boxExtY * FpScale) * 2f;
                        e.size = new Vector2(sx, sy);
                    }

                    if (!ignoreMovement) {
                        e.velocity = new Vector2(velX * FpScale, velY * FpScale);
                    }

                    if (hasMover) {
                        e.moverAssetGuidValue = moverAsset;
                        e.startOffsetRaw = startOffset;
                    }
                }

                // Basic collider sizing for other entities that have a PhysicsCollider2DPrototype.
                if (e.kind == NSMB.World.StageEntityKind.BulletBillLauncher && hasCollider && boxExtX != 0 && boxExtY != 0) {
                    float sx = (boxExtX * FpScale) * 2f;
                    float sy = (boxExtY * FpScale) * 2f;
                    e.size = new Vector2(sx, sy);
                }

                if (e.kind != NSMB.World.StageEntityKind.Unknown && hasTransform) {
                    e.position = pos;
                    if (hasCollider) {
                        e.colliderOffset = new Vector2(offX * FpScale, offY * FpScale);
                    }

                    if (hasBulletLauncher) {
                        e.shootFrames = shootFrames;
                        e.minRadiusRaw = minRadiusRaw;
                        e.maxRadiusRaw = maxRadiusRaw;
                        e.bulletPrototypeIdValue = bulletPrototypeIdValue;

                        // Map to generic params (import stage will apply ImportScale to radii).
                        e.param0 = (shootFrames > 0) ? (shootFrames / 60f) : 4f;
                        e.param1 = minRadiusRaw * FpScale;
                        e.param2 = maxRadiusRaw * FpScale;
                    }
                    entities.Add(e);
                }
            }

            return entities;
        }

        private static List<List<long>> ParseMapEntities(string[] lines) {
            List<List<long>> entities = new List<List<long>>();

            bool inMapEntities = false;
            List<long> current = null;

            for (int i = 0; i < lines.Length; i++) {
                string t = lines[i].Trim();
                if (!inMapEntities) {
                    if (t == "MapEntities:") {
                        inMapEntities = true;
                    }
                    continue;
                }

                if (t == "references:" || t.StartsWith("references:", StringComparison.InvariantCulture)) {
                    break;
                }

                if (t == "- Components:") {
                    if (current != null && current.Count > 0) {
                        entities.Add(current);
                    }
                    current = new List<long>();
                    continue;
                }

                if (t.StartsWith("- rid:", StringComparison.InvariantCulture)) {
                    long rid;
                    if (TryParseLongAfterColon(t, out rid)) {
                        if (current == null) {
                            current = new List<long>();
                        }
                        current.Add(rid);
                    }
                }
            }

            if (current != null && current.Count > 0) {
                entities.Add(current);
            }

            return entities;
        }

        private static Dictionary<long, RefInfo> ParseRefIds(string[] lines) {
            Dictionary<long, RefInfo> refsByRid = new Dictionary<long, RefInfo>();

            bool inRefIds = false;
            long currentRid = 0;
            RefInfo current = null;

            string className = null;

            bool parseTransformPos = false;
            bool seenPosition = false;
            bool expectX = false;
            bool expectY = false;

            bool parseVelocity = false;
            bool seenVelocity = false;
            bool expectVx = false;
            bool expectVy = false;

            bool parseBoxExtents = false;
            bool seenBoxExtents = false;
            bool expectBx = false;
            bool expectBy = false;
            bool seenOffset = false;
            bool expectOx = false;
            bool expectOy = false;

            bool parseMoverAsset = false;
            bool seenMoverAsset = false;
            bool seenAssetId = false;
            bool parseStartOffset = false;

            bool parseBulletLauncher = false;
            bool parseMinRadius = false;
            bool parseMaxRadius = false;
            bool seenBulletPrototype = false;
            bool seenBulletId = false;

            for (int i = 0; i < lines.Length; i++) {
                string t = lines[i].Trim();

                if (!inRefIds) {
                    if (t == "RefIds:" || t.EndsWith("RefIds:", StringComparison.InvariantCulture)) {
                        inRefIds = true;
                    }
                    continue;
                }

                if (t.StartsWith("- rid:", StringComparison.InvariantCulture)) {
                    // flush previous
                    if (current != null && currentRid != 0) {
                        refsByRid[currentRid] = current;
                    }

                    current = new RefInfo();
                    currentRid = 0;
                    className = null;
                    parseTransformPos = false;
                    seenPosition = false;
                    expectX = false;
                    expectY = false;

                    parseVelocity = false;
                    seenVelocity = false;
                    expectVx = false;
                    expectVy = false;

                    parseBoxExtents = false;
                    seenBoxExtents = false;
                    expectBx = false;
                    expectBy = false;
                    seenOffset = false;
                    expectOx = false;
                    expectOy = false;

                    parseMoverAsset = false;
                    seenMoverAsset = false;
                    seenAssetId = false;
                    parseStartOffset = false;

                    parseBulletLauncher = false;
                    parseMinRadius = false;
                    parseMaxRadius = false;
                    seenBulletPrototype = false;
                    seenBulletId = false;

                    TryParseLongAfterColon(t, out currentRid);
                    continue;
                }

                if (current == null) {
                    continue;
                }

                if (t.StartsWith("type:", StringComparison.InvariantCulture)) {
                    string cls = ExtractClassName(t);
                    current.className = cls;
                    parseTransformPos = (cls == "Transform2DPrototype");
                    parseVelocity = (cls == "MovingPlatformPrototype");
                    parseMoverAsset = (cls == "GenericMoverPrototype");
                    parseBoxExtents = (cls == "PhysicsCollider2DPrototype");
                    parseBulletLauncher = (cls == "BulletBillLauncherPrototype");
                    className = cls;
                    continue;
                }

                // Parse Transform2DPrototype Position
                if (parseTransformPos) {
                    if (t == "Position:" || t.EndsWith("Position:", StringComparison.InvariantCulture)) {
                        seenPosition = true;
                        continue;
                    }

                    if (seenPosition) {
                        if (t == "X:") {
                            expectX = true;
                            continue;
                        }
                        if (t == "Y:") {
                            expectY = true;
                            continue;
                        }

                        if (t.StartsWith("RawValue:", StringComparison.InvariantCulture)) {
                            long v;
                            if (TryParseLongAfterColon(t, out v)) {
                                if (expectX) {
                                    current.posX = v;
                                    expectX = false;
                                } else if (expectY) {
                                    current.posY = v;
                                    expectY = false;
                                }
                            }
                        }
                    }
                }

                // Parse MovingPlatformPrototype Velocity
                if (parseVelocity) {
                    if (t == "Velocity:" || t.EndsWith("Velocity:", StringComparison.InvariantCulture)) {
                        seenVelocity = true;
                        continue;
                    }
                    if (seenVelocity) {
                        if (t == "X:") { expectVx = true; continue; }
                        if (t == "Y:") { expectVy = true; continue; }
                        if (t.StartsWith("RawValue:", StringComparison.InvariantCulture)) {
                            long v;
                            if (TryParseLongAfterColon(t, out v)) {
                                if (expectVx) { current.velX = v; expectVx = false; }
                                else if (expectVy) { current.velY = v; expectVy = false; }
                            }
                        }
                    }
                    if (t.StartsWith("IgnoreMovement:", StringComparison.InvariantCulture) || t.EndsWith("IgnoreMovement:", StringComparison.InvariantCulture)) {
                        // Next line is "Value: 0/1"
                        for (int j = i + 1; j < Mathf.Min(lines.Length, i + 6); j++) {
                            string tt = lines[j].Trim();
                            if (tt.StartsWith("Value:", StringComparison.InvariantCulture)) {
                                int v = 0;
                                int.TryParse(tt.Substring("Value:".Length).Trim(), out v);
                                current.bool0 = (v != 0);
                                break;
                            }
                        }
                    }
                }

                // Parse PhysicsCollider2DPrototype BoxExtents + IsTrigger
                if (parseBoxExtents) {
                    if (t.StartsWith("IsTrigger:", StringComparison.InvariantCulture)) {
                        // In Quantum assets this is usually a direct int (0/1)
                        int v = 0;
                        int.TryParse(t.Substring("IsTrigger:".Length).Trim(), out v);
                        current.isTrigger = (v != 0);
                    }

                    if (t == "BoxExtents:" || t.EndsWith("BoxExtents:", StringComparison.InvariantCulture)) {
                        seenBoxExtents = true;
                        seenOffset = false;
                        continue;
                    }

                    if (t == "PositionOffset:" || t.EndsWith("PositionOffset:", StringComparison.InvariantCulture)) {
                        seenOffset = true;
                        seenBoxExtents = false;
                        continue;
                    }

                    if (seenBoxExtents) {
                        if (t == "X:") { expectBx = true; continue; }
                        if (t == "Y:") { expectBy = true; continue; }
                        if (t.StartsWith("RawValue:", StringComparison.InvariantCulture)) {
                            long v;
                            if (TryParseLongAfterColon(t, out v)) {
                                if (expectBx) { current.boxExtX = v; expectBx = false; }
                                else if (expectBy) { current.boxExtY = v; expectBy = false; }
                            }
                        }
                    }
                    if (seenOffset) {
                        if (t == "X:") { expectOx = true; continue; }
                        if (t == "Y:") { expectOy = true; continue; }
                        if (t.StartsWith("RawValue:", StringComparison.InvariantCulture)) {
                            long v;
                            if (TryParseLongAfterColon(t, out v)) {
                                if (expectOx) { current.offsetX = v; expectOx = false; }
                                else if (expectOy) { current.offsetY = v; expectOy = false; }
                            }
                        }
                    }
                }

                // Parse BulletBillLauncherPrototype data (minimal subset for Wii U backport).
                if (parseBulletLauncher) {
                    if (t.StartsWith("TimeToShootFrames:", StringComparison.InvariantCulture)) {
                        int v = 0;
                        int.TryParse(t.Substring("TimeToShootFrames:".Length).Trim(), out v);
                        if (v > 0) {
                            current.shootFrames = v;
                        }
                    } else if (t.StartsWith("TimeToShoot:", StringComparison.InvariantCulture)) {
                        int v = 0;
                        int.TryParse(t.Substring("TimeToShoot:".Length).Trim(), out v);
                        if (v > 0 && current.shootFrames == 0) {
                            current.shootFrames = v;
                        }
                    }

                    if (t == "MinimumShootRadius:" || t.EndsWith("MinimumShootRadius:", StringComparison.InvariantCulture)) {
                        parseMinRadius = true;
                        parseMaxRadius = false;
                        continue;
                    }
                    if (t == "MaximumShootRadius:" || t.EndsWith("MaximumShootRadius:", StringComparison.InvariantCulture)) {
                        parseMaxRadius = true;
                        parseMinRadius = false;
                        continue;
                    }
                    if ((parseMinRadius || parseMaxRadius) && t.StartsWith("RawValue:", StringComparison.InvariantCulture)) {
                        long v;
                        if (TryParseLongAfterColon(t, out v)) {
                            if (parseMinRadius) current.minRadiusRaw = v;
                            else if (parseMaxRadius) current.maxRadiusRaw = v;
                        }
                        parseMinRadius = false;
                        parseMaxRadius = false;
                        continue;
                    }

                    if (t == "BulletBillPrototype:" || t.EndsWith("BulletBillPrototype:", StringComparison.InvariantCulture)) {
                        seenBulletPrototype = true;
                        seenBulletId = false;
                        continue;
                    }
                    if (seenBulletPrototype) {
                        if (t == "Id:" || t.EndsWith("Id:", StringComparison.InvariantCulture)) {
                            seenBulletId = true;
                            continue;
                        }
                        if (seenBulletId && t.StartsWith("Value:", StringComparison.InvariantCulture)) {
                            long v;
                            if (long.TryParse(t.Substring("Value:".Length).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) {
                                current.bulletPrototypeIdValue = v;
                            }
                            seenBulletPrototype = false;
                            seenBulletId = false;
                        }
                    }
                }

                // Parse GenericMoverPrototype MoverAsset Id Value and StartOffset
                if (parseMoverAsset) {
                    if (t == "MoverAsset:" || t.EndsWith("MoverAsset:", StringComparison.InvariantCulture)) {
                        seenMoverAsset = true;
                        seenAssetId = false;
                        continue;
                    }
                    if (seenMoverAsset) {
                        if (t == "Id:" || t.EndsWith("Id:", StringComparison.InvariantCulture)) {
                            seenAssetId = true;
                            continue;
                        }
                        if (seenAssetId && t.StartsWith("Value:", StringComparison.InvariantCulture)) {
                            long v;
                            if (long.TryParse(t.Substring("Value:".Length).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) {
                                current.assetGuidValue = v;
                            }
                        }
                    }

                    if (t.StartsWith("StartOffset:", StringComparison.InvariantCulture) || t.EndsWith("StartOffset:", StringComparison.InvariantCulture)) {
                        parseStartOffset = true;
                        continue;
                    }
                    if (parseStartOffset && t.StartsWith("RawValue:", StringComparison.InvariantCulture)) {
                        long v;
                        if (TryParseLongAfterColon(t, out v)) {
                            current.startOffsetRaw = v;
                        }
                        parseStartOffset = false;
                    }
                }
            }

            if (current != null && currentRid != 0) {
                refsByRid[currentRid] = current;
            }

            return refsByRid;
        }

        private static bool TryParseLongAfterColon(string lineTrimmed, out long value) {
            value = 0;
            int colon = lineTrimmed.IndexOf(':');
            if (colon < 0) {
                return false;
            }
            string s = lineTrimmed.Substring(colon + 1).Trim();
            return long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static string ExtractClassName(string typeLineTrimmed) {
            // type: {class: Transform2DPrototype, ns: Quantum.Prototypes, asm: Quantum.Engine}
            int idx = typeLineTrimmed.IndexOf("class:", StringComparison.InvariantCulture);
            if (idx < 0) {
                return string.Empty;
            }
            idx += "class:".Length;
            string rest = typeLineTrimmed.Substring(idx).Trim();
            int comma = rest.IndexOf(',');
            if (comma >= 0) {
                rest = rest.Substring(0, comma);
            }
            return rest.Trim();
        }
    }
}
