using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class BuildPlayerAnimatorControllerMenu {

    [MenuItem("NSMB/Player Assets/Build Animator Controllers (From FBX Clips)")]
    private static void BuildControllers() {
        // Requires the models to already be copied into:
        // Assets/Resources/NSMB/Player/Models/Players/...
        BuildSmallController();
        BuildBigController();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[NSMB] Built player animator controllers under Assets/Resources/NSMB/Player/Anim/Generated/");
    }

    private static void BuildSmallController() {
        string fbxPath = "Assets/Resources/NSMB/Player/Models/Players/mario_small/mario_small_exported.fbx";
        Dictionary<string, AnimationClip> clips = LoadClipsByName(fbxPath);

        AnimationClip idle = FindClip(clips, "small_model|wait_small_model");
        if (idle == null) idle = FindClipContains(clips, "|wait_small_model");
        if (idle == null) idle = FindClipContains(clips, "small_wait");

        AnimationClip walk = FindClip(clips, "small_model|walk_small_model");
        if (walk == null) walk = FindClipContains(clips, "|walk_small_model");

        AnimationClip jump = FindClip(clips, "small_model|jump_small_model");
        if (jump == null) jump = FindClipContains(clips, "|jump_small_model");

        AnimationClip fall = FindClip(clips, "small_model|m_fall_wait_small_model");
        if (fall == null) fall = FindClipContains(clips, "fall");

        string outPath = EnsureGeneratedFolder() + "/SmallMarioGenerated.controller";
        AnimatorController controller = CreateOrResetController(outPath);
        ConfigureCommonParams(controller);
        BuildLocomotionGraph(controller, idle, walk);
        BuildJumpFall(controller, jump, fall);
    }

    private static void BuildBigController() {
        string fbxPath = "Assets/Resources/NSMB/Player/Models/Players/mario_big/mario_big_exported.fbx";
        Dictionary<string, AnimationClip> clips = LoadClipsByName(fbxPath);

        // mario_big_exported uses names like: "mario_model_mg|..._mario_model_mg"
        AnimationClip idle = FindClipContains(clips, "mario_model_mg|big_wait_mario_model_mg");
        if (idle == null) idle = FindClipContains(clips, "big_wait_mario_model_mg");
        if (idle == null) idle = FindClipContains(clips, "big_wait");

        AnimationClip walk = FindClipContains(clips, "mario_model_mg|big_walk_mario_model_mg");
        if (walk == null) walk = FindClipContains(clips, "big_walk_mario_model_mg");
        if (walk == null) walk = FindClipContains(clips, "big_walk");

        AnimationClip jump = FindClipContains(clips, "mario_model_mg|jump_mario_model_mg");
        if (jump == null) jump = FindClipContains(clips, "|jump_mario_model_mg");
        if (jump == null) jump = FindClipContains(clips, "jump_mario_model_mg");
        if (jump == null) jump = FindClipContains(clips, "|jump");

        AnimationClip fall = FindClipContains(clips, "mario_model_mg|m_fall_wait_mario_model_mg");
        if (fall == null) fall = FindClipContains(clips, "m_fall_wait_mario_model_mg");
        if (fall == null) fall = FindClipContains(clips, "m_fall");
        if (fall == null) fall = FindClipContains(clips, "fall");

        string outPath = EnsureGeneratedFolder() + "/LargeMarioGenerated.controller";
        AnimatorController controller = CreateOrResetController(outPath);
        ConfigureCommonParams(controller);
        BuildLocomotionGraph(controller, idle, walk);
        BuildJumpFall(controller, jump, fall);
    }

    private static void ConfigureCommonParams(AnimatorController controller) {
        EnsureParam(controller, "velocityX", AnimatorControllerParameterType.Float);
        EnsureParam(controller, "velocityY", AnimatorControllerParameterType.Float);
        EnsureParam(controller, "velocityMagnitude", AnimatorControllerParameterType.Float);
        EnsureParam(controller, "onGround", AnimatorControllerParameterType.Bool);
        EnsureParam(controller, "facingRight", AnimatorControllerParameterType.Bool);
    }

    private static void BuildLocomotionGraph(AnimatorController controller, AnimationClip idle, AnimationClip walk) {
        if (controller == null || controller.layers == null || controller.layers.Length == 0) {
            return;
        }

        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine sm = layer.stateMachine;
        sm.states = new ChildAnimatorState[0];
        sm.anyStateTransitions = new AnimatorStateTransition[0];
        sm.entryTransitions = new AnimatorTransition[0];
        sm.stateMachines = new ChildAnimatorStateMachine[0];

        // Locomotion blend (idle <-> walk) driven by velocityMagnitude.
        AnimatorState locomotion = sm.AddState("locomotion", new Vector3(250f, 100f, 0f));

        BlendTree tree;
        locomotion.motion = CreateLocomotionBlendTree(out tree, idle, walk);

        sm.defaultState = locomotion;
    }

    private static void BuildJumpFall(AnimatorController controller, AnimationClip jump, AnimationClip fall) {
        if (controller == null || controller.layers == null || controller.layers.Length == 0) {
            return;
        }

        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine sm = layer.stateMachine;

        AnimatorState locomotion = FindState(sm, "locomotion");
        if (locomotion == null) {
            return;
        }

        if (jump != null) {
            AnimatorState jumpState = sm.AddState("jump", new Vector3(250f, 260f, 0f));
            jumpState.motion = jump;

            AnimatorStateTransition toJump = locomotion.AddTransition(jumpState);
            toJump.hasExitTime = false;
            toJump.duration = 0f;
            toJump.AddCondition(AnimatorConditionMode.IfNot, 0f, "onGround");
            toJump.AddCondition(AnimatorConditionMode.Greater, 0.1f, "velocityY");

            AnimatorStateTransition backToGround = jumpState.AddTransition(locomotion);
            backToGround.hasExitTime = false;
            backToGround.duration = 0f;
            backToGround.AddCondition(AnimatorConditionMode.If, 0f, "onGround");
        }

        if (fall != null) {
            AnimatorState fallState = sm.AddState("fall", new Vector3(520f, 260f, 0f));
            fallState.motion = fall;

            AnimatorStateTransition toFall = locomotion.AddTransition(fallState);
            toFall.hasExitTime = false;
            toFall.duration = 0f;
            toFall.AddCondition(AnimatorConditionMode.IfNot, 0f, "onGround");
            toFall.AddCondition(AnimatorConditionMode.Less, -0.1f, "velocityY");

            AnimatorStateTransition backToGround = fallState.AddTransition(locomotion);
            backToGround.hasExitTime = false;
            backToGround.duration = 0f;
            backToGround.AddCondition(AnimatorConditionMode.If, 0f, "onGround");
        }
    }

    private static Motion CreateLocomotionBlendTree(out BlendTree tree, AnimationClip idle, AnimationClip walk) {
        tree = new BlendTree();
        tree.name = "locomotion";
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = "velocityMagnitude";
        tree.useAutomaticThresholds = false;

        // Fallback clips if missing: Unity will still create the controller, but locomotion will be static.
        if (idle != null) {
            tree.AddChild(idle, 0f);
        }
        if (walk != null) {
            tree.AddChild(walk, 1f);
        }

        if (idle == null && walk == null) {
            // Create an empty placeholder so controller is valid.
            AnimationClip empty = new AnimationClip();
            empty.name = "empty";
            tree.AddChild(empty, 0f);
        }

        return tree;
    }

    private static AnimatorController CreateOrResetController(string assetPath) {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath(assetPath, typeof(AnimatorController)) as AnimatorController;
        if (controller == null) {
            controller = AnimatorController.CreateAnimatorControllerAtPath(assetPath);
        }
        return controller;
    }

    private static void EnsureParam(AnimatorController controller, string name, AnimatorControllerParameterType type) {
        if (controller == null) {
            return;
        }

        AnimatorControllerParameter[] ps = controller.parameters;
        for (int i = 0; i < ps.Length; i++) {
            if (ps[i] != null && string.Equals(ps[i].name, name, StringComparison.InvariantCultureIgnoreCase)) {
                return;
            }
        }

        controller.AddParameter(name, type);
    }

    private static string EnsureGeneratedFolder() {
        string root = "Assets/Resources/NSMB/Player/Anim";
        if (!AssetDatabase.IsValidFolder("Assets/Resources/NSMB/Player")) {
            AssetDatabase.CreateFolder("Assets/Resources/NSMB", "Player");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/NSMB/Player/Anim")) {
            AssetDatabase.CreateFolder("Assets/Resources/NSMB/Player", "Anim");
        }
        if (!AssetDatabase.IsValidFolder(root + "/Generated")) {
            AssetDatabase.CreateFolder(root, "Generated");
        }
        return root + "/Generated";
    }

    private static Dictionary<string, AnimationClip> LoadClipsByName(string assetPath) {
        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>(StringComparer.InvariantCultureIgnoreCase);

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (assets == null || assets.Length == 0) {
            Debug.LogWarning("[NSMB] No assets at path: " + assetPath);
            return clips;
        }

        for (int i = 0; i < assets.Length; i++) {
            AnimationClip clip = assets[i] as AnimationClip;
            if (clip == null) {
                continue;
            }

            // Ignore import preview clips.
            if (clip.name != null && clip.name.StartsWith("__preview__", StringComparison.InvariantCultureIgnoreCase)) {
                continue;
            }

            // Some imported clips can end up as 0 frames. Skip them so we don't build controllers from empty clips.
            if (clip.length <= 0.001f) {
                continue;
            }

            if (!clips.ContainsKey(clip.name)) {
                clips.Add(clip.name, clip);
            }
        }

        if (clips.Count == 0) {
            Debug.LogWarning("[NSMB] No AnimationClips found in: " + assetPath);
        }

        return clips;
    }

    private static AnimationClip FindClip(Dictionary<string, AnimationClip> clips, string exactName) {
        if (clips == null) return null;
        AnimationClip clip;
        return clips.TryGetValue(exactName, out clip) ? clip : null;
    }

    private static AnimationClip FindClipContains(Dictionary<string, AnimationClip> clips, string contains) {
        if (clips == null || string.IsNullOrEmpty(contains)) {
            return null;
        }
        foreach (KeyValuePair<string, AnimationClip> kv in clips) {
            if (kv.Key != null && kv.Key.IndexOf(contains, StringComparison.InvariantCultureIgnoreCase) >= 0) {
                return kv.Value;
            }
        }
        return null;
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
}
