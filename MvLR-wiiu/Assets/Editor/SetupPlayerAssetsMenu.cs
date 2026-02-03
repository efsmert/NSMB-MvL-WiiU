using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SetupPlayerAssetsMenu {

    [MenuItem("NSMB/Player Assets/Copy Models (From Original)")]
    private static void CopyModelsOnly() {
        CopyPlayerAssets(copyModels: true, copyAnimControllers: false, copyWhichModels: CopyWhichModels.All);
    }

    [MenuItem("NSMB/Player Assets/Copy Mario Small Model (From Original)")]
    private static void CopyMarioSmallOnly() {
        CopyPlayerAssets(copyModels: true, copyAnimControllers: false, copyWhichModels: CopyWhichModels.MarioSmall);
    }

    [MenuItem("NSMB/Player Assets/Copy Mario Big Model (From Original)")]
    private static void CopyMarioBigOnly() {
        CopyPlayerAssets(copyModels: true, copyAnimControllers: false, copyWhichModels: CopyWhichModels.MarioBig);
    }

    [MenuItem("NSMB/Player Assets/Copy Luigi Small Model (From Original)")]
    private static void CopyLuigiSmallOnly() {
        CopyPlayerAssets(copyModels: true, copyAnimControllers: false, copyWhichModels: CopyWhichModels.LuigiSmall);
    }

    [MenuItem("NSMB/Player Assets/Copy Luigi Big Model (From Original)")]
    private static void CopyLuigiBigOnly() {
        CopyPlayerAssets(copyModels: true, copyAnimControllers: false, copyWhichModels: CopyWhichModels.LuigiBig);
    }

    [MenuItem("NSMB/Player Assets/Copy Anim Controllers (From Original, Experimental)")]
    private static void CopyAnimControllersOnly() {
        CopyPlayerAssets(copyModels: false, copyAnimControllers: true, copyWhichModels: CopyWhichModels.None);
    }

    [MenuItem("NSMB/Player Assets/Copy Models + Anim (From Original)")]
    private static void CopyModelsAndAnim() {
        CopyPlayerAssets(copyModels: true, copyAnimControllers: true, copyWhichModels: CopyWhichModels.All);
    }

    [MenuItem("NSMB/Player Assets/Cleanup Copied Player Resources")]
    private static void CleanupCopiedPlayerResources() {
        const string root = "Assets/Resources/NSMB/Player";
        if (!AssetDatabase.IsValidFolder(root)) {
            Debug.Log("[NSMB] Nothing to clean (missing " + root + ")");
            return;
        }

        if (!EditorUtility.DisplayDialog("Cleanup Player Resources",
                "This will delete:\n\n" + root + "\n\nUnity will need to reimport assets. Continue?",
                "Delete", "Cancel")) {
            return;
        }

        if (!AssetDatabase.DeleteAsset(root)) {
            Debug.LogError("[NSMB] Failed to delete " + root);
        } else {
            AssetDatabase.Refresh();
            Debug.Log("[NSMB] Deleted " + root);
        }
    }

    private enum CopyWhichModels {
        None = 0,
        MarioSmall = 1,
        MarioBig = 2,
        LuigiSmall = 3,
        LuigiBig = 4,
        All = 5,
    }

    private static void CopyPlayerAssets(bool copyModels, bool copyAnimControllers, CopyWhichModels copyWhichModels) {
        string mvLRProjectDir = Directory.GetParent(Application.dataPath).FullName;
        string repoRoot = Directory.GetParent(mvLRProjectDir).FullName;
        string originalProjectDir = Path.Combine(repoRoot, "NSMB-MarioVsLuigi");

        string srcAssets = Path.Combine(originalProjectDir, "Assets");
        if (!Directory.Exists(srcAssets)) {
            Debug.LogError("[NSMB] Original project not found: " + originalProjectDir);
            return;
        }

        string dstResourcesRoot =
            Path.Combine(
                Path.Combine(
                    Path.Combine(mvLRProjectDir, "Assets"),
                    "Resources"),
                Path.Combine("NSMB", "Player"));

        EnsureDir(dstResourcesRoot);

        AssetDatabase.StartAssetEditing();
        try {
            if (copyModels) {
                // Copy player models (skip .meta and any .blend sources to reduce import risk in Unity 2017).
                if (copyWhichModels == CopyWhichModels.All || copyWhichModels == CopyWhichModels.MarioSmall) {
                    CopyFolderFiltered(
                        Path.Combine(Path.Combine(Path.Combine(srcAssets, "Models"), "Players"), "mario_small"),
                        Path.Combine(dstResourcesRoot, Path.Combine("Models", "Players/mario_small")),
                        // Keep this minimal: Unity 6 .anim files are often not backward-compatible with Unity 2017.
                        new[] { ".fbx", ".png", ".mask" },
                        new[] { ".blend", ".blend1", ".bak" },
                        includeMetaFiles: false);
                }

                if (copyWhichModels == CopyWhichModels.All || copyWhichModels == CopyWhichModels.MarioBig) {
                    CopyFolderFiltered(
                        Path.Combine(Path.Combine(Path.Combine(srcAssets, "Models"), "Players"), "mario_big"),
                        Path.Combine(dstResourcesRoot, Path.Combine("Models", "Players/mario_big")),
                        new[] { ".fbx", ".png", ".mask" },
                        new[] { ".blend", ".blend1", ".bak" },
                        includeMetaFiles: false);

                    // Prefer the older .fbx.bak payload if present (it can import more reliably in Unity 2017).
                    string srcBak = Path.Combine(Path.Combine(Path.Combine(srcAssets, "Models"), "Players"), Path.Combine("mario_big", "mario_big_exported.fbx.bak"));
                    string dstFbx = Path.Combine(dstResourcesRoot, Path.Combine("Models", Path.Combine("Players", Path.Combine("mario_big", "mario_big_exported.fbx"))));
                    if (File.Exists(srcBak)) {
                        CopyFileIfExists(srcBak, dstFbx);
                    }
                }

                if (copyWhichModels == CopyWhichModels.All || copyWhichModels == CopyWhichModels.LuigiSmall) {
                    CopyFolderFiltered(
                        Path.Combine(Path.Combine(Path.Combine(srcAssets, "Models"), "Players"), "luigi_small"),
                        Path.Combine(dstResourcesRoot, Path.Combine("Models", "Players/luigi_small")),
                        new[] { ".fbx", ".png", ".mask" },
                        new[] { ".blend", ".blend1", ".bak" },
                        includeMetaFiles: false);
                }

                if (copyWhichModels == CopyWhichModels.All || copyWhichModels == CopyWhichModels.LuigiBig) {
                    CopyFolderFiltered(
                        Path.Combine(Path.Combine(Path.Combine(srcAssets, "Models"), "Players"), "luigi_big"),
                        Path.Combine(dstResourcesRoot, Path.Combine("Models", "Players/luigi_big")),
                        new[] { ".fbx", ".png", ".mask" },
                        new[] { ".blend", ".blend1", ".bak" },
                    includeMetaFiles: false);
                }
            }

            if (copyAnimControllers) {
                // Copy animator controllers / overrides. These may not be fully backward-compatible with 2017,
                // so keep them optional.
                EnsureDir(Path.Combine(dstResourcesRoot, "Anim"));
                CopyFileIfExists(Path.Combine(Path.Combine(Path.Combine(srcAssets, "Animations"), "Player"), "LargeMario.controller"),
                    Path.Combine(dstResourcesRoot, Path.Combine("Anim", "LargeMario.controller")));

                CopyFileIfExists(Path.Combine(Path.Combine(Path.Combine(srcAssets, "Animations"), "Player"), "SmallMario.overrideController"),
                    Path.Combine(dstResourcesRoot, Path.Combine("Anim", "SmallMario.overrideController")));

                CopyFileIfExists(Path.Combine(Path.Combine(Path.Combine(srcAssets, "Animations"), "Player"), "SmallLuigi.overrideController"),
                    Path.Combine(dstResourcesRoot, Path.Combine("Anim", "SmallLuigi.overrideController")));

                CopyFileIfExists(Path.Combine(Path.Combine(Path.Combine(srcAssets, "Animations"), "Player"), "LargeLuigi.overrideController"),
                    Path.Combine(dstResourcesRoot, Path.Combine("Anim", "LargeLuigi.overrideController")));
            }
        } finally {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.Refresh();
        Debug.Log("[NSMB] Player assets copied. Unity will now import them (this can take several minutes in Unity 2017, especially FBX rigs).");
    }

    private static void CopyFolderFiltered(string srcDir, string dstDir, string[] includeExts, string[] excludeExts, bool includeMetaFiles) {
        if (!Directory.Exists(srcDir)) {
            Debug.LogWarning("[NSMB] Missing folder: " + srcDir);
            return;
        }

        EnsureDir(dstDir);

        HashSet<string> include = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        for (int i = 0; i < includeExts.Length; i++) {
            include.Add(includeExts[i]);
        }

        HashSet<string> exclude = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        for (int i = 0; i < excludeExts.Length; i++) {
            exclude.Add(excludeExts[i]);
        }

        string[] files = Directory.GetFiles(srcDir);
        for (int i = 0; i < files.Length; i++) {
            string srcFile = files[i];
            string ext = Path.GetExtension(srcFile);
            if (!string.IsNullOrEmpty(ext)) {
                if (exclude.Contains(ext)) {
                    continue;
                }
                if (!include.Contains(ext) && !(includeMetaFiles && string.Equals(ext, ".meta", StringComparison.InvariantCultureIgnoreCase))) {
                    continue;
                }
            }

            // Skip .blend/.bak metas too if present.
            if (srcFile.EndsWith(".blend.meta", StringComparison.InvariantCultureIgnoreCase) ||
                srcFile.EndsWith(".blend1.meta", StringComparison.InvariantCultureIgnoreCase) ||
                srcFile.EndsWith(".bak.meta", StringComparison.InvariantCultureIgnoreCase)) {
                continue;
            }

            string dstFile = Path.Combine(dstDir, Path.GetFileName(srcFile));
            File.Copy(srcFile, dstFile, true);
        }
    }

    private static void CopyFileIfExists(string src, string dst) {
        if (!File.Exists(src)) {
            Debug.LogWarning("[NSMB] Missing file: " + src);
            return;
        }
        EnsureDir(Path.GetDirectoryName(dst));
        File.Copy(src, dst, true);
    }

    private static void EnsureDir(string dir) {
        if (string.IsNullOrEmpty(dir)) {
            return;
        }
        if (!Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }
    }
}
