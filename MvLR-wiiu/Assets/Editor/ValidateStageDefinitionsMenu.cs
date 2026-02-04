using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ValidateStageDefinitionsMenu {
    private sealed class MissingSample {
        public string stageKey;
        public string layerName;
        public int x;
        public int y;
        public string atlas;
        public string spriteName;
        public int spriteIndex;
    }

    [MenuItem("NSMB/Debug/Validate StageDefinitions (Sprites)")]
    private static void Validate() {
        // Unity's FindAssets type filter typically uses the unqualified type name.
        string[] guids = AssetDatabase.FindAssets("t:StageDefinition", new string[] { "Assets/Resources/NSMB/Levels" });
        int totalStages = guids != null ? guids.Length : 0;

        long totalTiles = 0;
        long missingSpriteName = 0;
        long unresolvedSprite = 0;

        Dictionary<string, int> unresolvedCounts = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
        List<MissingSample> missingSamples = new List<MissingSample>();
        List<MissingSample> unresolvedSamples = new List<MissingSample>();

        for (int i = 0; i < totalStages; i++) {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            NSMB.World.StageDefinition def = AssetDatabase.LoadAssetAtPath(path, typeof(NSMB.World.StageDefinition)) as NSMB.World.StageDefinition;
            if (def == null || def.tileLayers == null) {
                continue;
            }

            for (int l = 0; l < def.tileLayers.Count; l++) {
                NSMB.World.StageTileLayer layer = def.tileLayers[l];
                if (layer == null || layer.tiles == null) {
                    continue;
                }

                for (int t = 0; t < layer.tiles.Count; t++) {
                    totalTiles++;
                    NSMB.World.StageTile tile = layer.tiles[t];

                    if (string.IsNullOrEmpty(tile.spriteName)) {
                        // Many imported StageDefinitions intentionally omit spriteName when we can still resolve
                        // the correct sprite via spriteIndex. Only report these if they truly cannot be drawn.
                        if (!TryResolveSpriteByIndex(layer.resourcesAtlasPath, tile.spriteIndex)) {
                            missingSpriteName++;
                            if (missingSamples.Count < 50) {
                                missingSamples.Add(MakeSample(def, layer, tile, layer.resourcesAtlasPath, tile.spriteName));
                            }
                        }
                        continue;
                    }

                    string atlas = layer.resourcesAtlasPath;
                    string spriteName = tile.spriteName;

                    if (!TryResolveSprite(atlas, spriteName)) {
                        unresolvedSprite++;
                        string key = string.Format("{0}|{1}", atlas ?? string.Empty, spriteName ?? string.Empty);
                        int c;
                        unresolvedCounts.TryGetValue(key, out c);
                        unresolvedCounts[key] = c + 1;

                        if (unresolvedSamples.Count < 50) {
                            unresolvedSamples.Add(MakeSample(def, layer, tile, atlas, spriteName));
                        }
                    }
                }
            }
        }

        Debug.Log(string.Format(
            "[NSMB] StageDefinition sprite validation:\n" +
            "- Stages: {0}\n" +
            "- Tiles checked: {1}\n" +
            "- Missing spriteName: {2}\n" +
            "- Unresolved spriteName: {3}",
            totalStages, totalTiles, missingSpriteName, unresolvedSprite));

        if (missingSamples.Count > 0) {
            for (int i = 0; i < missingSamples.Count; i++) {
                MissingSample s = missingSamples[i];
                Debug.LogWarning(string.Format("[NSMB] Missing spriteName: stage={0} layer={1} pos=({2},{3}) atlas={4} spriteIndex={5}",
                    s.stageKey, s.layerName, s.x, s.y, s.atlas, s.spriteIndex));
            }
        }

        if (unresolvedSamples.Count > 0) {
            for (int i = 0; i < unresolvedSamples.Count; i++) {
                MissingSample s = unresolvedSamples[i];
                Debug.LogWarning(string.Format("[NSMB] Unresolved sprite: stage={0} layer={1} pos=({2},{3}) atlas={4} sprite={5}",
                    s.stageKey, s.layerName, s.x, s.y, s.atlas, s.spriteName));
            }
        }

        if (unresolvedCounts.Count > 0) {
            List<KeyValuePair<string, int>> top = new List<KeyValuePair<string, int>>(unresolvedCounts);
            top.Sort(delegate(KeyValuePair<string, int> a, KeyValuePair<string, int> b) { return b.Value.CompareTo(a.Value); });

            int take = Mathf.Min(15, top.Count);
            for (int i = 0; i < take; i++) {
                KeyValuePair<string, int> kv = top[i];
                Debug.LogWarning(string.Format("[NSMB] Top unresolved ({0}x): {1}", kv.Value, kv.Key));
            }
        }
    }

    private static bool TryResolveSprite(string atlas, string spriteName) {
        if (string.IsNullOrEmpty(atlas) || string.IsNullOrEmpty(spriteName)) {
            return false;
        }

        Sprite s = NSMB.Content.ResourceSpriteCache.FindSprite(atlas, spriteName);
        if (s != null) {
            return true;
        }

        if (!spriteName.StartsWith("0 ", StringComparison.InvariantCultureIgnoreCase)) {
            s = NSMB.Content.ResourceSpriteCache.FindSprite(atlas, "0 " + spriteName);
            if (s != null) return true;
        } else {
            string trimmed = spriteName.Substring(2);
            s = NSMB.Content.ResourceSpriteCache.FindSprite(atlas, trimmed);
            if (s != null) return true;
        }

        return false;
    }

    private static bool TryResolveSpriteByIndex(string atlas, int spriteIndex) {
        if (string.IsNullOrEmpty(atlas)) {
            return false;
        }

        Sprite[] sprites = NSMB.Content.ResourceSpriteCache.LoadAllSprites(atlas);
        if (sprites == null || sprites.Length == 0) {
            return false;
        }

        if (spriteIndex >= 0 && spriteIndex < sprites.Length && sprites[spriteIndex] != null) {
            return true;
        }

        // Secondary: try name-based forms like "<atlas>_<index>".
        string atlasName = atlas;
        int slash = atlasName.LastIndexOf('/');
        if (slash >= 0 && slash + 1 < atlasName.Length) {
            atlasName = atlasName.Substring(slash + 1);
        }

        string directName = atlasName + "_" + spriteIndex;
        Sprite direct = NSMB.Content.ResourceSpriteCache.FindSprite(atlas, directName);
        if (direct != null) {
            return true;
        }

        // Final fallback: suffix match.
        string suffix = "_" + spriteIndex;
        for (int i = 0; i < sprites.Length; i++) {
            Sprite s = sprites[i];
            if (s != null && s.name != null && s.name.EndsWith(suffix, StringComparison.InvariantCultureIgnoreCase)) {
                return true;
            }
        }

        return false;
    }

    private static MissingSample MakeSample(NSMB.World.StageDefinition def, NSMB.World.StageTileLayer layer, NSMB.World.StageTile tile, string atlas, string spriteName) {
        MissingSample s = new MissingSample();
        s.stageKey = def != null ? def.stageKey : "unknown";
        s.layerName = layer != null ? layer.name : "layer";
        s.x = tile.x;
        s.y = tile.y;
        s.atlas = atlas;
        s.spriteName = spriteName;
        s.spriteIndex = tile.spriteIndex;
        return s;
    }
}
