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
            return;
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

        Vector2 spawn = TryGetSpawnFromStageData(originalProjectDir, stageAbs);
        if (spawn == Vector2.zero) {
            spawn = UnitySceneParsers.ExtractFirstPlayerSpawn(sceneAbs);
        }
        List<StageEntityData> entities = QuantumMapParsers.ExtractEntities(stageAbs);

        NSMB.World.StageDefinition def = ScriptableObject.CreateInstance<NSMB.World.StageDefinition>();
        def.stageKey = spec.stageKey;
        def.spawnPoint = spawn;

        for (int tm = 0; tm < tilemaps.Count; tm++) {
            SceneTilemapData tilemap = tilemaps[tm];
            if (tilemap == null || tilemap.tiles == null || tilemap.tiles.Count == 0) {
                continue;
            }

            string atlasPath = ResolveAtlasForTilemap(tilemap.name, spec);
            if (string.IsNullOrEmpty(atlasPath)) {
                atlasPath = spec.defaultGroundAtlas;
            }
            if (string.IsNullOrEmpty(atlasPath)) {
                Debug.LogWarning("[NSMB] No atlas mapping for tilemap " + tilemap.name + " in " + spec.stageKey + "; skipping.");
                continue;
            }

            NSMB.World.StageTileLayer layer = new NSMB.World.StageTileLayer();
            layer.name = tilemap.name;
            layer.resourcesAtlasPath = atlasPath;
            layer.sortingOrder = tilemap.sortingOrder;
            layer.position = tilemap.worldPosition * ImportScale;
            layer.scale = Vector3.Scale(tilemap.worldScale, new Vector3(ImportScale, ImportScale, 1f));

            for (int i = 0; i < tilemap.tiles.Count; i++) {
                SceneTile t = tilemap.tiles[i];
                NSMB.World.StageTile outTile = new NSMB.World.StageTile();
                outTile.x = t.x;
                outTile.y = t.y;
                outTile.spriteIndex = t.spriteIndex;
                outTile.flipX = t.matrixIndex != 0; // the source mostly uses 0/1 for X mirroring.
                layer.tiles.Add(outTile);
            }

            def.tileLayers.Add(layer);
        }

        for (int i = 0; i < entities.Count; i++) {
            StageEntityData e = entities[i];
            if (e.kind == NSMB.World.StageEntityKind.Unknown) {
                continue;
            }
            NSMB.World.StageEntity se = new NSMB.World.StageEntity();
            se.kind = e.kind;
            se.variant = e.variant;
            se.position = e.position * ImportScale;
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

    private static Vector2 TryGetSpawnFromStageData(string originalProjectDir, string mapAssetPath) {
        // Find sibling StageData for this map asset and read Spawnpoint FP values.
        if (string.IsNullOrEmpty(mapAssetPath) || !File.Exists(mapAssetPath)) {
            return Vector2.zero;
        }

        long userAssetId = ParseLongField(mapAssetPath, "UserAsset:", "Value:");
        if (userAssetId == 0) {
            return Vector2.zero;
        }

        string dir = Path.GetDirectoryName(mapAssetPath);
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) {
            return Vector2.zero;
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
                    return spawn.Value * ImportScale;
                }
            } catch (Exception) {
                // ignore
            }
        }

        return Vector2.zero;
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

    private sealed class SceneTilemapData {
        public string name;
        public Vector3 worldPosition;
        public Vector3 worldScale;
        public int sortingOrder;
        public readonly List<SceneTile> tiles = new List<SceneTile>();
    }

    private struct SceneTile {
        public int x;
        public int y;
        public int spriteIndex;
        public int matrixIndex;
    }

    private struct StageEntityData {
        public NSMB.World.StageEntityKind kind;
        public Vector2 position;
        public int variant;
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

            bool inTiles = false;
            SceneTile current = new SceneTile();
            bool haveCurrent = false;

            for (int i = start; i < end; i++) {
                string raw = lines[i];
                string t = raw.Trim();

                if (t.StartsWith("m_GameObject:", StringComparison.InvariantCulture)) {
                    tm.gameObjectId = ParseFileIdFromBraces(t);
                }

                if (t == "m_Tiles:") {
                    inTiles = true;
                    continue;
                }
                if (!inTiles) {
                    continue;
                }

                // End of tile list when we reach another top-level Tilemap field (2-space indent).
                // Tile entry fields like "m_TileSpriteIndex" are deeper-indented and should not terminate parsing.
                if (raw.StartsWith("  m_", StringComparison.InvariantCulture) && !raw.StartsWith("  m_Tiles:", StringComparison.InvariantCulture)) {
                    if (haveCurrent) {
                        tm.tiles.Add(current);
                        haveCurrent = false;
                    }
                    break;
                }

                if (t.StartsWith("- first:", StringComparison.InvariantCulture)) {
                    if (haveCurrent) {
                        tm.tiles.Add(current);
                    }
                    haveCurrent = true;
                    current = new SceneTile();
                    ParseFirstCoords(t, out current.x, out current.y);
                } else if (haveCurrent && t.StartsWith("m_TileSpriteIndex:", StringComparison.InvariantCulture)) {
                    current.spriteIndex = ParseIntAfterColon(t);
                } else if (haveCurrent && t.StartsWith("m_TileMatrixIndex:", StringComparison.InvariantCulture)) {
                    current.matrixIndex = ParseIntAfterColon(t);
                }
            }

            if (haveCurrent) {
                tm.tiles.Add(current);
            }

            return tm.gameObjectId != 0 && tm.tiles.Count > 0;
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

                bool hasTransform = false;
                Vector2 pos = Vector2.zero;

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
                    }
                }

                if (e.kind != NSMB.World.StageEntityKind.Unknown && hasTransform) {
                    e.position = pos;
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
            bool parseTransformPos = false;
            bool seenPosition = false;
            bool expectX = false;
            bool expectY = false;

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
                    parseTransformPos = false;
                    seenPosition = false;
                    expectX = false;
                    expectY = false;

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
                    continue;
                }

                if (!parseTransformPos) {
                    continue;
                }

                if (t == "Position:" || t.EndsWith("Position:", StringComparison.InvariantCulture)) {
                    seenPosition = true;
                    continue;
                }

                if (!seenPosition) {
                    continue;
                }

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
