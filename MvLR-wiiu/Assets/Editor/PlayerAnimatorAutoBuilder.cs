using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public sealed class PlayerAnimatorAutoBuilder : AssetPostprocessor {
    private static bool _scheduled;
    private static bool _building;

    static PlayerAnimatorAutoBuilder() {
        // Run once on editor load; if FBXs aren't imported yet, we'll also trigger on their import via AssetPostprocessor.
        ScheduleIfNeeded();
        // Also run when entering play mode, so you don't have to click any menu item.
        EditorApplication.playmodeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged() {
        // Try to run just before entering play mode (or as early as possible) so controllers are ready at runtime.
        if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying) {
            ScheduleIfNeeded();
        }
    }

    private static void ScheduleIfNeeded() {
        if (_scheduled) {
            return;
        }
        _scheduled = true;
        EditorApplication.delayCall += delegate {
            _scheduled = false;
            TryBuildIfMissingOrEmpty();
        };
    }

    private static void TryBuildIfMissingOrEmpty() {
        if (_building) {
            return;
        }

        // Rebuild if generated controllers are missing, empty, or incompatible (e.g. missing BlendTree locomotion).
        string smallPath = "Assets/Resources/NSMB/Player/Anim/Generated/SmallMarioGenerated.controller";
        string bigPath = "Assets/Resources/NSMB/Player/Anim/Generated/LargeMarioGenerated.controller";

        AnimatorController small = AssetDatabase.LoadAssetAtPath(smallPath, typeof(AnimatorController)) as AnimatorController;
        AnimatorController big = AssetDatabase.LoadAssetAtPath(bigPath, typeof(AnimatorController)) as AnimatorController;

        if (IsControllerReady(small) && IsLargeControllerReady(big)) {
            return;
        }

        // If FBX clips aren't imported yet, defer (otherwise we'd just regenerate "empty" again).
        if (!HasAnyUsefulClips("Assets/Resources/NSMB/Player/Models/Players/mario_small/mario_small_exported.fbx") ||
            !HasAnyUsefulClips("Assets/Resources/NSMB/Player/Models/Players/mario_big/mario_big_exported.fbx")) {
            ScheduleIfNeeded();
            return;
        }

        _building = true;
        try {
            BuildPlayerAnimatorControllerMenu.BuildControllersForImport();
        } catch (Exception ex) {
            Debug.LogWarning("[NSMB] Auto-build player controllers failed: " + ex.Message);
        } finally {
            _building = false;
        }
    }

    private static bool IsControllerReady(AnimatorController controller) {
        if (controller == null) {
            return false;
        }

        // Parameters must exist for runtime to drive the controller.
        if (!HasParam(controller, "velocityMagnitude") || !HasParam(controller, "velocityY") || !HasParam(controller, "onGround")) {
            return false;
        }

        // Locomotion state must exist and have a serialized BlendTree motion (otherwise locomotion won't animate after reload).
        if (controller.layers == null || controller.layers.Length == 0 || controller.layers[0].stateMachine == null) {
            return false;
        }

        AnimatorState locomotion = FindState(controller.layers[0].stateMachine, "locomotion");
        if (locomotion == null) {
            return false;
        }
        if (locomotion.motion == null) {
            return false;
        }
        if (!(locomotion.motion is BlendTree)) {
            return false;
        }

        AnimationClip[] clips = controller.animationClips;
        if (clips == null || clips.Length == 0) {
            return false;
        }

        bool hasWait = false;
        bool hasWalk = false;
        for (int i = 0; i < clips.Length; i++) {
            AnimationClip c = clips[i];
            if (c == null || string.IsNullOrEmpty(c.name)) continue;
            string n = c.name.ToLowerInvariant();
            if (!hasWait && n.Contains("wait")) hasWait = true;
            if (!hasWalk && n.Contains("walk")) hasWalk = true;
        }

        // Controllers with only jump/fall should be rebuilt.
        return hasWait && hasWalk;
    }

    private static bool IsLargeControllerReady(AnimatorController controller) {
        if (!IsControllerReady(controller)) {
            return false;
        }

        // Unity 6 parity: the large (Mushroom) state uses "wait_mario_model_mg"/"walk_mario_model_mg".
        // The "big_wait_*"/"big_walk_*" clips are used by MegaMushroom states.
        AnimationClip[] clips = controller.animationClips;
        bool hasNormalWait = false;
        bool hasNormalWalk = false;
        if (clips != null) {
            for (int i = 0; i < clips.Length; i++) {
                AnimationClip c = clips[i];
                if (c == null || string.IsNullOrEmpty(c.name)) continue;
                string n = c.name.ToLowerInvariant();
                if (!hasNormalWait && n.Contains("wait_mario_model_mg")) hasNormalWait = true;
                if (!hasNormalWalk && n.Contains("walk_mario_model_mg")) hasNormalWalk = true;
            }
        }

        return hasNormalWait && hasNormalWalk;
    }

    private static bool HasParam(AnimatorController controller, string name) {
        if (controller == null || string.IsNullOrEmpty(name)) {
            return false;
        }
        AnimatorControllerParameter[] ps = controller.parameters;
        for (int i = 0; i < ps.Length; i++) {
            if (ps[i] != null && string.Equals(ps[i].name, name, StringComparison.InvariantCultureIgnoreCase)) {
                return true;
            }
        }
        return false;
    }

    private static AnimatorState FindState(AnimatorStateMachine sm, string name) {
        if (sm == null || string.IsNullOrEmpty(name)) return null;
        ChildAnimatorState[] states = sm.states;
        for (int i = 0; i < states.Length; i++) {
            AnimatorState s = states[i].state;
            if (s != null && string.Equals(s.name, name, StringComparison.InvariantCultureIgnoreCase)) {
                return s;
            }
        }
        return null;
    }

    private static bool HasAnyUsefulClips(string fbxPath) {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        if (assets == null || assets.Length == 0) {
            return false;
        }
        for (int i = 0; i < assets.Length; i++) {
            AnimationClip clip = assets[i] as AnimationClip;
            if (clip == null) continue;
            if (clip.name != null && clip.name.StartsWith("__preview__", StringComparison.InvariantCultureIgnoreCase)) continue;
            if (clip.length <= 0.001f) continue;
            string n = clip.name != null ? clip.name.ToLowerInvariant() : "";
            if (n.Contains("wait") || n.Contains("walk") || n.Contains("jump")) {
                return true;
            }
        }
        return false;
    }

    // Called automatically by Unity when assets import.
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
        if (importedAssets == null || importedAssets.Length == 0) {
            return;
        }

        for (int i = 0; i < importedAssets.Length; i++) {
            string p = importedAssets[i];
            if (string.IsNullOrEmpty(p)) continue;

            if (p.IndexOf("Assets/Resources/NSMB/Player/Models/Players/mario_small/mario_small_exported.fbx", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                p.IndexOf("Assets/Resources/NSMB/Player/Models/Players/mario_big/mario_big_exported.fbx", StringComparison.InvariantCultureIgnoreCase) >= 0) {
                ScheduleIfNeeded();
                return;
            }
        }
    }
}
