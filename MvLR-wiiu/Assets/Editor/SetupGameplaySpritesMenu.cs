using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SetupGameplaySpritesMenu {

    [MenuItem("NSMB/Setup Gameplay Sprites (Mirror Atlases To Resources)")]
    private static void MirrorAtlasesToResources() {
        // Copies a minimal set of gameplay atlases from the original Unity-6 project into this project's
        // Resources folder so runtime code can load sprites via Resources.LoadAll<Sprite>().
        //
        // This does NOT copy .meta files; import settings are synced via SyncSpriteImportFromOriginal.

        string mvLRProjectDir = Directory.GetParent(Application.dataPath).FullName;
        string repoRoot = Directory.GetParent(mvLRProjectDir).FullName;
        string originalProjectDir = Path.Combine(repoRoot, "NSMB-MarioVsLuigi");

        // Unity 2017 (.NET 3.5) doesn't support Path.Combine with 4+ args / params string[].
        string originalAtlases = Path.Combine(Path.Combine(Path.Combine(originalProjectDir, "Assets"), "Sprites"), "Atlases");
        if (!Directory.Exists(originalAtlases)) {
            Debug.LogError("[NSMB] Original atlases folder not found: " + originalAtlases);
            return;
        }

        string resourcesAtlases =
            Path.Combine(
                Path.Combine(
                    Path.Combine(
                        Path.Combine(
                            Path.Combine(mvLRProjectDir, "Assets"),
                            "Resources"),
                        "NSMB"),
                    "Sprites"),
                "Atlases");
        if (!Directory.Exists(resourcesAtlases)) {
            Directory.CreateDirectory(resourcesAtlases);
        }

        string[] relativePngs = new[] {
            // Entity
            Path.Combine("Entity", "goomba.png"),
            Path.Combine("Entity", "powerups.png"),

            // Terrain
            Path.Combine("Terrain", "platforms.png"),
            Path.Combine("Terrain", "animated-blocks.png"),
            Path.Combine("Terrain", "dotted-coins.png"),
            Path.Combine("Terrain", "grass.png"),
        };

        List<string> importedAssetPaths = new List<string>();

        for (int i = 0; i < relativePngs.Length; i++) {
            string rel = relativePngs[i];
            string src = Path.Combine(originalAtlases, rel);
            string dst = Path.Combine(resourcesAtlases, rel);

            if (!File.Exists(src)) {
                Debug.LogWarning("[NSMB] Missing original atlas: " + src);
                continue;
            }

            string dstDir = Path.GetDirectoryName(dst);
            if (!Directory.Exists(dstDir)) {
                Directory.CreateDirectory(dstDir);
            }

            File.Copy(src, dst, true);

            string dstAssetPath = ("Assets/Resources/NSMB/Sprites/Atlases/" + rel.Replace('\\', '/'));
            importedAssetPaths.Add(dstAssetPath);
        }

        AssetDatabase.Refresh();

        for (int i = 0; i < importedAssetPaths.Count; i++) {
            AssetDatabase.ImportAsset(importedAssetPaths[i], ImportAssetOptions.ForceUpdate);
        }

        AssetDatabase.Refresh();
        Debug.Log("[NSMB] Mirrored " + importedAssetPaths.Count + " gameplay atlases into Resources.");
    }
}
