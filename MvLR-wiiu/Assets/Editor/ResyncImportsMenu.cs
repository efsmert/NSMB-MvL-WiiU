using System;
using System.Collections.Generic;
using UnityEditor;

public static class ResyncImportsMenu {

    [MenuItem("NSMB/Resync Sprite Import Settings (From Original)")]
    private static void ResyncSprites() {
        string[] roots = new[] {
            "Assets/NSMB/Sprites/Sprites",
            "Assets/NSMB/Sprites/Gizmos",
            "Assets/Resources/NSMB/UI",
            "Assets/Resources/NSMB/Sprites",
        };

        List<string> texturePaths = new List<string>();

        for (int r = 0; r < roots.Length; r++) {
            string root = roots[r];
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { root });
            for (int i = 0; i < guids.Length; i++) {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.IsNullOrEmpty(path)) {
                    texturePaths.Add(path);
                }
            }
        }

        texturePaths.Sort(StringComparer.InvariantCultureIgnoreCase);

        for (int i = 0; i < texturePaths.Count; i++) {
            AssetDatabase.ImportAsset(texturePaths[i], ImportAssetOptions.ForceUpdate);
        }

        AssetDatabase.Refresh();
    }

    [MenuItem("NSMB/Reimport Audio As PCM")]
    private static void ReimportAudioAsPcm() {
        string[] roots = new[] {
            "Assets/Resources",
        };

        List<string> audioPaths = new List<string>();

        for (int r = 0; r < roots.Length; r++) {
            string root = roots[r];
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { root });
            for (int i = 0; i < guids.Length; i++) {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.IsNullOrEmpty(path)) {
                    audioPaths.Add(path);
                }
            }
        }

        audioPaths.Sort(StringComparer.InvariantCultureIgnoreCase);

        for (int i = 0; i < audioPaths.Count; i++) {
            AssetDatabase.ImportAsset(audioPaths[i], ImportAssetOptions.ForceUpdate);
        }

        AssetDatabase.Refresh();
    }
}
