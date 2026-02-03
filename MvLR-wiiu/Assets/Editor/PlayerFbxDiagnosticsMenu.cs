using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PlayerFbxDiagnosticsMenu {
    [MenuItem("NSMB/Player Assets/Diagnostics/Log Imported FBX Clips")]
    private static void LogImportedFbxClips() {
        LogFbx("Assets/Resources/NSMB/Player/Models/Players/mario_small/mario_small_exported.fbx");
        LogFbx("Assets/Resources/NSMB/Player/Models/Players/mario_big/mario_big_exported.fbx");
        LogFbx("Assets/Resources/NSMB/Player/Models/Players/luigi_small/luigi_small.fbx");
        LogFbx("Assets/Resources/NSMB/Player/Models/Players/luigi_big/luigi_big.fbx");
    }

    private static void LogFbx(string assetPath) {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (assets == null || assets.Length == 0) {
            Debug.LogWarning("[NSMB] FBX not found or has no sub-assets: " + assetPath);
            return;
        }

        int clips = 0;
        int empty = 0;
        List<string> examples = new List<string>();

        for (int i = 0; i < assets.Length; i++) {
            AnimationClip clip = assets[i] as AnimationClip;
            if (clip == null) continue;
            if (clip.name != null && clip.name.StartsWith("__preview__", StringComparison.InvariantCultureIgnoreCase)) continue;

            clips++;
            if (clip.length <= 0.001f) empty++;
            if (examples.Count < 6) {
                examples.Add(clip.name + " len=" + clip.length.ToString("0.###") + " loop=" + clip.isLooping);
            }
        }

        Debug.Log("[NSMB] FBX " + assetPath + " subassets=" + assets.Length + " clips=" + clips + " empty=" + empty +
                  (examples.Count > 0 ? ("\n  " + string.Join("\n  ", examples.ToArray())) : ""));
    }
}

