using System;
using UnityEditor;
using UnityEngine;

public static class SpriteImportDiagnosticsMenu {

    [MenuItem("NSMB/Diagnostics/Log Sprite Slice Counts")]
    private static void LogSliceCounts() {
        string[] paths = new[] {
            "Assets/Resources/NSMB/UI/Menu/menubg.png",
            "Assets/Resources/NSMB/UI/ui.png",

            "Assets/Resources/NSMB/Sprites/Atlases/Entity/goomba.png",
            "Assets/Resources/NSMB/Sprites/Atlases/Entity/powerups.png",

            "Assets/Resources/NSMB/Sprites/Atlases/Terrain/platforms.png",
            "Assets/Resources/NSMB/Sprites/Atlases/Terrain/animated-blocks.png",
            "Assets/Resources/NSMB/Sprites/Atlases/Terrain/dotted-coins.png",
            "Assets/Resources/NSMB/Sprites/Atlases/Terrain/grass.png",
        };

        for (int i = 0; i < paths.Length; i++) {
            string path = paths[i];
            if (!System.IO.File.Exists(path)) {
                Debug.LogWarning("[NSMB] Missing file: " + path);
                continue;
            }

            UnityEngine.Object[] reps = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
            int spriteCount = 0;
            string firstSprite = null;

            if (reps != null) {
                for (int r = 0; r < reps.Length; r++) {
                    Sprite s = reps[r] as Sprite;
                    if (s != null) {
                        spriteCount++;
                        if (firstSprite == null) {
                            firstSprite = s.name;
                        }
                    }
                }
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            string mode = importer != null ? importer.spriteImportMode.ToString() : "Unknown";
            float ppu = importer != null ? importer.spritePixelsPerUnit : 0f;

            Debug.Log(string.Format("[NSMB] {0} -> sprites: {1}, first: {2}, mode: {3}, PPU: {4}",
                path, spriteCount, firstSprite ?? "(none)", mode, ppu));
        }
    }
}

