using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class PlayerModelImporterTuner : AssetPostprocessor {
    private const string PrefKeyMinimal = "NSMB_PlayerImport_MinimalClips";
    private const string PrefKeyImportMaterials = "NSMB_PlayerImport_ImportMaterials";

    [MenuItem("NSMB/Player Assets/Toggle Minimal FBX Clip Import")]
    private static void ToggleMinimal() {
        bool current = EditorPrefs.GetBool(PrefKeyMinimal, true);
        EditorPrefs.SetBool(PrefKeyMinimal, !current);
        Debug.Log("[NSMB] Minimal FBX clip import is now " + (!current ? "ON" : "OFF") + ". Reimport player FBXs to apply.");
    }

    [MenuItem("NSMB/Player Assets/Toggle Import Materials For Player FBX")]
    private static void ToggleImportMaterials() {
        bool current = EditorPrefs.GetBool(PrefKeyImportMaterials, true);
        EditorPrefs.SetBool(PrefKeyImportMaterials, !current);
        Debug.Log("[NSMB] Player FBX material import is now " + (!current ? "ON" : "OFF") + ". Reimport player FBXs to apply.");
    }

    private void OnPreprocessModel() {
        string assetPath = assetImporter.assetPath.Replace('\\', '/');
        if (!assetPath.StartsWith("Assets/Resources/NSMB/Player/Models/Players/", StringComparison.InvariantCultureIgnoreCase)) {
            return;
        }
        if (!assetPath.EndsWith(".fbx", StringComparison.InvariantCultureIgnoreCase)) {
            return;
        }

        ModelImporter importer = (ModelImporter)assetImporter;

        // Import textures/materials for FBX so we don't end up with a textureless mesh at runtime.
        // (URP/ShaderGraph mats aren't expected here; these are embedded FBX materials + PNGs in the same folder.)
        bool importMaterials = EditorPrefs.GetBool(PrefKeyImportMaterials, true);
        importer.importMaterials = importMaterials;
        if (importMaterials) {
            // Keep search local so FBX textures in the same folder are picked up reliably.
            importer.materialSearch = ModelImporterMaterialSearch.Local;
            importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
        }

        importer.importAnimation = true;
        importer.animationType = ModelImporterAnimationType.Generic;

        bool minimal = EditorPrefs.GetBool(PrefKeyMinimal, true);
        if (!minimal) {
            // Full import (can be very slow on these FBXs).
            return;
        }

        // Minimal import: only bring in the small set of clips we need for locomotion.
        // IMPORTANT: Unity 2017 sometimes imports FBX clips as 0-frame if we rely on defaults.
        // We can pull correct first/last frames from the original Unity-6 .fbx.meta and apply them here.
        ModelImporterClipAnimation[] parsed = TryGetClipAnimationsFromOriginalMeta(assetPath);
        if (parsed != null && parsed.Length > 0) {
            importer.clipAnimations = FilterClips(assetPath, parsed);
            return;
        }

        // Fallback: filter default clips by name.
        ModelImporterClipAnimation[] defaults = importer.defaultClipAnimations;
        if (defaults == null || defaults.Length == 0) {
            return;
        }

        importer.clipAnimations = FilterClips(assetPath, defaults);
    }

    private void OnPreprocessTexture() {
        string assetPath = assetImporter.assetPath.Replace('\\', '/');
        if (!assetPath.StartsWith("Assets/Resources/NSMB/Player/Models/Players/", StringComparison.InvariantCultureIgnoreCase)) {
            return;
        }

        // Avoid filtering/mip bleed on atlas-style player textures. These are meant to be crisp.
        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.anisoLevel = 0;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
    }

    private static ModelImporterClipAnimation[] FilterClips(string assetPath, ModelImporterClipAnimation[] source) {
        if (source == null || source.Length == 0) {
            return source;
        }

        List<ModelImporterClipAnimation> keep = new List<ModelImporterClipAnimation>();
        for (int i = 0; i < source.Length; i++) {
            ModelImporterClipAnimation clip = source[i];
            if (clip == null || string.IsNullOrEmpty(clip.name)) {
                continue;
            }

            if (ShouldKeepClip(assetPath, clip.name)) {
                keep.Add(clip);
            }
        }

        if (keep.Count == 0) {
            keep.Add(source[0]);
        }

        return keep.ToArray();
    }

    private static bool ShouldKeepClip(string assetPath, string clipName) {
        // mario_small_exported.fbx uses "small_model|..._small_model"
        // mario_big_exported.fbx uses "mario_model_mg|..._mario_model_mg"
        string lower = clipName.ToLowerInvariant();

        // Idle / walk
        if (lower.Contains("wait")) return true;
        if (lower.Contains("walk")) return true;

        // Jump / fall
        if (lower.Contains("jump")) return true;
        if (lower.Contains("fall")) return true;

        // If this is a Luigi FBX, names might differ; still keep these basics.
        if (lower.Contains("idle")) return true;

        return false;
    }

    private static ModelImporterClipAnimation[] TryGetClipAnimationsFromOriginalMeta(string dstAssetPath) {
        try {
            // Map:
            //  dst: Assets/Resources/NSMB/Player/Models/Players/<subpath>
            //  src: NSMB-MarioVsLuigi/Assets/Models/Players/<subpath>
            const string dstPrefix = "Assets/Resources/NSMB/Player/Models/Players/";
            if (!dstAssetPath.StartsWith(dstPrefix, StringComparison.InvariantCultureIgnoreCase)) {
                return null;
            }

            string sub = dstAssetPath.Substring(dstPrefix.Length).Replace('/', Path.DirectorySeparatorChar);

            string mvLRProjectDir = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
            string repoRoot = Directory.GetParent(mvLRProjectDir).FullName;
            string originalProjectDir = Path.Combine(repoRoot, "NSMB-MarioVsLuigi");

            string srcFbx = Path.Combine(Path.Combine(Path.Combine(originalProjectDir, "Assets"), "Models"), Path.Combine("Players", sub));
            string srcMeta = srcFbx + ".meta";
            if (!File.Exists(srcMeta)) {
                return null;
            }

            return ParseClipAnimationsFromMeta(File.ReadAllLines(srcMeta));
        } catch {
            return null;
        }
    }

    private sealed class ClipInfo {
        public string name;
        public float firstFrame;
        public float lastFrame;
        public bool loopTime;
        public bool hasName;
        public bool hasFirst;
        public bool hasLast;
    }

    private static ModelImporterClipAnimation[] ParseClipAnimationsFromMeta(string[] lines) {
        if (lines == null || lines.Length == 0) {
            return null;
        }

        int clipAnimationsLine = -1;
        int baseIndent = 0;

        for (int i = 0; i < lines.Length; i++) {
            string line = lines[i];
            if (line == null) continue;
            if (line.IndexOf("clipAnimations:", StringComparison.InvariantCultureIgnoreCase) >= 0) {
                // Be strict: only accept lines that end with "clipAnimations:"
                string trimmed = line.Trim();
                if (string.Equals(trimmed, "clipAnimations:", StringComparison.InvariantCultureIgnoreCase)) {
                    clipAnimationsLine = i;
                    baseIndent = CountLeadingSpaces(line);
                    break;
                }
            }
        }

        if (clipAnimationsLine < 0) {
            return null;
        }

        List<ModelImporterClipAnimation> clips = new List<ModelImporterClipAnimation>();
        ClipInfo current = null;
        int itemIndent = -1;

        for (int i = clipAnimationsLine + 1; i < lines.Length; i++) {
            string line = lines[i];
            if (line == null) continue;

            int indent = CountLeadingSpaces(line);
            if (indent < baseIndent) {
                break;
            }

            string trimmed = line.Trim();

            // Start of a new clip item.
            if (trimmed.StartsWith("-", StringComparison.InvariantCultureIgnoreCase)) {
                if (itemIndent < 0) {
                    itemIndent = indent;
                }
                if (indent == itemIndent) {
                FlushClip(current, clips);
                current = new ClipInfo();
                continue;
                }
            }

            if (current == null) {
                continue;
            }

            // Keys live under the clip item, typically at indent base+4 or more.
            if (itemIndent >= 0 && indent <= itemIndent) {
                continue;
            }
            if (trimmed.StartsWith("name:", StringComparison.InvariantCultureIgnoreCase)) {
                current.name = trimmed.Substring("name:".Length).Trim();
                current.hasName = !string.IsNullOrEmpty(current.name);
                continue;
            }

            if (trimmed.StartsWith("firstFrame:", StringComparison.InvariantCultureIgnoreCase)) {
                float v;
                if (TryParseFloat(trimmed.Substring("firstFrame:".Length).Trim(), out v)) {
                    current.firstFrame = v;
                    current.hasFirst = true;
                }
                continue;
            }

            if (trimmed.StartsWith("lastFrame:", StringComparison.InvariantCultureIgnoreCase)) {
                float v;
                if (TryParseFloat(trimmed.Substring("lastFrame:".Length).Trim(), out v)) {
                    current.lastFrame = v;
                    current.hasLast = true;
                }
                continue;
            }

            if (trimmed.StartsWith("loopTime:", StringComparison.InvariantCultureIgnoreCase)) {
                string v = trimmed.Substring("loopTime:".Length).Trim();
                current.loopTime = string.Equals(v, "1", StringComparison.InvariantCultureIgnoreCase) ||
                                   string.Equals(v, "true", StringComparison.InvariantCultureIgnoreCase);
                continue;
            }
        }

        FlushClip(current, clips);

        return clips.Count > 0 ? clips.ToArray() : null;
    }

    private static void FlushClip(ClipInfo info, List<ModelImporterClipAnimation> outClips) {
        if (info == null || outClips == null) {
            return;
        }
        if (!info.hasName || !info.hasFirst || !info.hasLast) {
            return;
        }
        if (info.lastFrame <= info.firstFrame) {
            return;
        }

        ModelImporterClipAnimation clip = new ModelImporterClipAnimation();
        clip.name = info.name;
        clip.takeName = info.name;
        clip.firstFrame = info.firstFrame;
        clip.lastFrame = info.lastFrame;
        bool forceLoop = info.loopTime || NameImpliesLoop(info.name);
        clip.loopTime = forceLoop;
        outClips.Add(clip);
    }

    private static int CountLeadingSpaces(string line) {
        if (string.IsNullOrEmpty(line)) return 0;
        int count = 0;
        while (count < line.Length && line[count] == ' ') count++;
        return count;
    }

    private static bool TryParseFloat(string s, out float value) {
        // Unity .meta uses invariant formatting.
        return float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    private static bool NameImpliesLoop(string clipName) {
        if (string.IsNullOrEmpty(clipName)) {
            return false;
        }
        string lower = clipName.ToLowerInvariant();
        if (lower.Contains("wait")) return true;
        if (lower.Contains("walk")) return true;
        if (lower.Contains("run")) return true;
        return false;
    }
}
