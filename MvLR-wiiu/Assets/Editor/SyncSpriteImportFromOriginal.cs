using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class SyncSpriteImportFromOriginal : AssetPostprocessor {
    private static readonly Dictionary<string, ParsedTextureMeta> MetaCache = new Dictionary<string, ParsedTextureMeta>(StringComparer.InvariantCultureIgnoreCase);

    private void OnPreprocessTexture() {
        string assetPath = assetImporter.assetPath.Replace('\\', '/');

        string originalMetaPath = TryGetOriginalMetaPath(assetPath);
        if (string.IsNullOrEmpty(originalMetaPath)) {
            return;
        }

        ParsedTextureMeta meta = GetParsedMeta(originalMetaPath);
        if (meta == null) {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.mipmapEnabled = meta.EnableMipMap;
        importer.alphaIsTransparency = meta.AlphaIsTransparency;
        importer.spritePixelsPerUnit = meta.SpritePixelsToUnits;
        importer.filterMode = meta.FilterMode;
        importer.wrapMode = meta.WrapMode;
        importer.textureCompression = meta.TextureCompression;

        // Pixel-art defaults (tile bleed mitigation): force Point+Clamp+Uncompressed for our atlases.
        // This keeps parity with the Unity 6 project and avoids texture sampling leaking neighboring pixels.
        if (assetPath.IndexOf("/Atlases/Terrain/", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
            assetPath.IndexOf("/Atlases/Entity/", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
            assetPath.IndexOf("/Sprites/Particle/", StringComparison.InvariantCultureIgnoreCase) >= 0) {
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
        // Unity 2017.1 API compatibility: some sprite-related importer knobs moved between
        // TextureImporter and TextureImporterSettings across Unity versions.
        // Apply them via reflection so this remains safe on 2017.1 (no-op if unsupported).
        int extrude = Mathf.Max(0, meta.SpriteExtrude);
        ParsedTextureMeta.TrySetImporterProperty(importer, "spriteExtrude", extrude);
        // Use FullRect meshes for spritesheets to avoid "tight mesh" sampling seams between tiles.
        // SpriteMeshType: 0 = Tight, 1 = FullRect (Unity 2017+).
        ParsedTextureMeta.TrySetImporterProperty(importer, "spriteMeshType", 1);
        ParsedTextureMeta.TrySetTextureSettingsProperty(importer, "spriteExtrude", extrude);
        ParsedTextureMeta.TrySetTextureSettingsProperty(importer, "spriteMeshType", 1);

        if (meta.SpriteMode == 2) {
            importer.spriteImportMode = SpriteImportMode.Multiple;
            if (meta.Sprites != null && meta.Sprites.Length > 0) {
                // Unity 2017 can be stricter about sprite rect bounds. Clamp any slightly-out-of-range rects
                // to avoid "rect lies outside of texture" warnings during import.
                int texW;
                int texH;
                SpriteMetaData[] sprites = CloneSprites(meta.Sprites);
                if (TryGetPngSize(assetPath, out texW, out texH)) {
                    ClampSpriteRects(sprites, texW, texH);
                }
                importer.spritesheet = sprites;
            }
        } else {
            importer.spriteImportMode = SpriteImportMode.Single;
            ApplySingleSpriteSettings(importer, meta.SpriteAlignmentValue, meta.SpritePivot);
        }
    }

    private static SpriteMetaData[] CloneSprites(SpriteMetaData[] sprites) {
        if (sprites == null) {
            return null;
        }
        SpriteMetaData[] cloned = new SpriteMetaData[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) {
            cloned[i] = sprites[i];
        }
        return cloned;
    }

    private static void ClampSpriteRects(SpriteMetaData[] sprites, int texW, int texH) {
        if (sprites == null || sprites.Length == 0) {
            return;
        }
        if (texW <= 0 || texH <= 0) {
            return;
        }

        for (int i = 0; i < sprites.Length; i++) {
            SpriteMetaData smd = sprites[i];
            Rect r = smd.rect;

            float x = Mathf.Clamp(r.x, 0f, Mathf.Max(0f, texW - 1f));
            float y = Mathf.Clamp(r.y, 0f, Mathf.Max(0f, texH - 1f));
            float w = Mathf.Max(1f, r.width);
            float h = Mathf.Max(1f, r.height);

            if (x + w > texW) {
                w = Mathf.Max(1f, texW - x);
            }
            if (y + h > texH) {
                h = Mathf.Max(1f, texH - y);
            }

            smd.rect = new Rect(x, y, w, h);
            sprites[i] = smd;
        }
    }

    private static bool TryGetPngSize(string unityAssetPath, out int width, out int height) {
        width = 0;
        height = 0;

        if (string.IsNullOrEmpty(unityAssetPath)) {
            return false;
        }

        // Only handle PNGs (most of our atlases/particles).
        if (!unityAssetPath.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase)) {
            return false;
        }

        try {
            // Convert "Assets/..." to an absolute path under the current Unity project.
            string rel = unityAssetPath;
            if (rel.StartsWith("Assets/", StringComparison.InvariantCultureIgnoreCase)) {
                rel = rel.Substring("Assets/".Length);
            } else if (rel.StartsWith("Assets\\", StringComparison.InvariantCultureIgnoreCase)) {
                rel = rel.Substring("Assets\\".Length);
            }
            string abs = Path.Combine(Application.dataPath, rel.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
            if (!File.Exists(abs)) {
                return false;
            }

            // PNG header: signature(8), length(4), type(4="IHDR"), data starts at byte 16:
            // width(4 big-endian), height(4 big-endian)
            byte[] bytes = File.ReadAllBytes(abs);
            if (bytes == null || bytes.Length < 24) {
                return false;
            }

            // Signature check
            if (bytes[0] != 137 || bytes[1] != 80 || bytes[2] != 78 || bytes[3] != 71) {
                return false;
            }
            if (bytes[12] != 73 || bytes[13] != 72 || bytes[14] != 68 || bytes[15] != 82) {
                return false;
            }

            width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
            height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];

            return width > 0 && height > 0;
        } catch {
            return false;
        }
    }

        private static void ApplySingleSpriteSettings(TextureImporter importer, int alignment, Vector2 pivot) {
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = alignment;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
        }

    private static string TryGetOriginalMetaPath(string mvlrAssetPath) {
        // Map MvLR paths to the original Unity-6 project paths:
        // - Assets/NSMB/Sprites/Sprites/**  -> NSMB-MarioVsLuigi/Assets/Sprites/**
        // - Assets/NSMB/Sprites/Gizmos/**   -> NSMB-MarioVsLuigi/Assets/Gizmos/**
        // - Assets/Resources/NSMB/UI/**     -> NSMB-MarioVsLuigi/Assets/Sprites/UI/**
        // - Assets/Resources/NSMB/Sprites/**-> NSMB-MarioVsLuigi/Assets/Sprites/**
        const string spritesPrefix = "Assets/NSMB/Sprites/Sprites/";
        const string gizmosPrefix = "Assets/NSMB/Sprites/Gizmos/";
        const string resourcesUiPrefix = "Assets/Resources/NSMB/UI/";
        const string resourcesSpritesPrefix = "Assets/Resources/NSMB/Sprites/";

        string originalRelativeAssetPath;
        if (mvlrAssetPath.StartsWith(spritesPrefix, StringComparison.InvariantCultureIgnoreCase)) {
            originalRelativeAssetPath = "Assets/Sprites/" + mvlrAssetPath.Substring(spritesPrefix.Length);
        } else if (mvlrAssetPath.StartsWith(gizmosPrefix, StringComparison.InvariantCultureIgnoreCase)) {
            originalRelativeAssetPath = "Assets/Gizmos/" + mvlrAssetPath.Substring(gizmosPrefix.Length);
        } else if (mvlrAssetPath.StartsWith(resourcesUiPrefix, StringComparison.InvariantCultureIgnoreCase)) {
            originalRelativeAssetPath = "Assets/Sprites/UI/" + mvlrAssetPath.Substring(resourcesUiPrefix.Length);
        } else if (mvlrAssetPath.StartsWith(resourcesSpritesPrefix, StringComparison.InvariantCultureIgnoreCase)) {
            originalRelativeAssetPath = "Assets/Sprites/" + mvlrAssetPath.Substring(resourcesSpritesPrefix.Length);
        } else {
            return null;
        }

        // Resolve repo root by walking up from this Unity project.
        // Application.dataPath points to ".../MvLR-wiiu/Assets".
        string mvLRProjectDir = Directory.GetParent(Application.dataPath).FullName;
        string repoRoot = Directory.GetParent(mvLRProjectDir).FullName;
        string originalProjectDir = Path.Combine(repoRoot, "NSMB-MarioVsLuigi");
        string originalMetaPath = Path.Combine(originalProjectDir, originalRelativeAssetPath.Replace('/', Path.DirectorySeparatorChar) + ".meta");

        return File.Exists(originalMetaPath) ? originalMetaPath : null;
    }

    private static ParsedTextureMeta GetParsedMeta(string originalMetaPath) {
        ParsedTextureMeta cached;
        if (MetaCache.TryGetValue(originalMetaPath, out cached)) {
            return cached;
        }

        ParsedTextureMeta parsed = ParsedTextureMeta.TryParse(originalMetaPath);
        MetaCache[originalMetaPath] = parsed;
        return parsed;
    }

    private sealed class ParsedTextureMeta {
        public bool EnableMipMap;
        public bool AlphaIsTransparency = true;
        public FilterMode FilterMode = FilterMode.Point;
        public int SpriteMode = 1; // 1 = Single, 2 = Multiple
        public float SpritePixelsToUnits = 100f;
        public TextureWrapMode WrapMode = TextureWrapMode.Clamp;
        public int SpriteExtrude = 1;
        public TextureImporterCompression TextureCompression = TextureImporterCompression.Uncompressed;
        // Unity 2017.1 doesn't reliably expose the SpriteAlignment enum across namespaces in editor code.
        // Alignment value 0 corresponds to Center.
        public int SpriteAlignmentValue = 0;
        public Vector2 SpritePivot = new Vector2(0.5f, 0.5f);
        public SpriteMetaData[] Sprites;

        public static ParsedTextureMeta TryParse(string metaPath) {
            if (string.IsNullOrEmpty(metaPath) || !File.Exists(metaPath)) {
                return null;
            }

            string[] lines;
            try {
                lines = File.ReadAllLines(metaPath);
            } catch {
                return null;
            }

            ParsedTextureMeta meta = new ParsedTextureMeta();

            bool inSpriteSheet = false;
            bool inSpritesList = false;
            int spritesKeyIndent = -1;
            List<SpriteMetaData> sprites = null;
            SpriteMetaData currentSprite = new SpriteMetaData();
            bool hasCurrentSprite = false;

            for (int i = 0; i < lines.Length; i++) {
                string raw = lines[i];
                string line = raw.Trim();

                if (line.Length == 0) {
                    continue;
                }

                if (inSpritesList) {
                    int indent = CountLeadingSpaces(raw);
                    // End of sprites list when we return to the same indentation as the "sprites:" key
                    // (e.g., "outline:", "physicsShape:", "spritePackingTag:", etc).
                    if (spritesKeyIndent >= 0 && indent <= spritesKeyIndent && !line.StartsWith("-", StringComparison.InvariantCulture)) {
                        if (hasCurrentSprite) {
                            sprites.Add(currentSprite);
                            hasCurrentSprite = false;
                            currentSprite = new SpriteMetaData();
                        }
                        inSpritesList = false;
                        // Continue parsing other parts of the meta (do not 'continue' here).
                    } else {
                        // Start of a new SpriteMetaData entry. Only treat list items at the same indentation
                        // level as the "sprites:" key as sprite boundaries; nested lists (e.g. indices) must
                        // not create empty sprite entries.
                        if (indent == spritesKeyIndent && line.StartsWith("- ", StringComparison.InvariantCulture)) {
                            if (hasCurrentSprite) {
                                sprites.Add(currentSprite);
                                currentSprite = new SpriteMetaData();
                            }
                            hasCurrentSprite = true;
                            currentSprite.alignment = 0;
                            currentSprite.pivot = new Vector2(0.5f, 0.5f);
                            currentSprite.border = Vector4.zero;
                            continue;
                        }

                        if (!hasCurrentSprite) {
                            continue;
                        }

                        // Name is typically on its own line in modern Unity metas.
                        if (line.StartsWith("name:", StringComparison.InvariantCultureIgnoreCase)) {
                            string n = line.Substring("name:".Length).Trim();
                            if (!string.IsNullOrEmpty(n)) {
                                currentSprite.name = n;
                            }
                            continue;
                        }

                        Rect rect;
                        if (TryParseInlineRect(line, "rect:", out rect)) {
                            currentSprite.rect = rect;
                            continue;
                        }

                        Vector2 sp;
                        if (TryParseInlineVector2(line, "pivot:", out sp)) {
                            currentSprite.pivot = sp;
                            continue;
                        }

                        Vector4 border;
                        if (TryParseInlineVector4(line, "border:", out border)) {
                            currentSprite.border = border;
                            continue;
                        }

                        int spriteAlign;
                        if (TryParseInt(line, "alignment:", out spriteAlign)) {
                            currentSprite.alignment = spriteAlign;
                            continue;
                        }

                        // Multi-line rect form:
                        // rect:
                        //   serializedVersion: 2
                        //   x: 0
                        //   y: 0
                        //   width: 16
                        //   height: 16
                        if (line == "rect:") {
                            float x = 0, y = 0, w = 0, h = 0;
                            for (int j = i + 1; j < Math.Min(i + 12, lines.Length); j++) {
                                string l2raw = lines[j];
                                string l2 = l2raw.Trim();
                                if (l2.StartsWith("-", StringComparison.InvariantCulture)) break;
                                if (CountLeadingSpaces(l2raw) <= indent) break;

                                float fx, fy, fw, fh;
                                if (TryParseFloat(l2, "x:", out fx)) x = fx;
                                if (TryParseFloat(l2, "y:", out fy)) y = fy;
                                if (TryParseFloat(l2, "width:", out fw)) w = fw;
                                if (TryParseFloat(l2, "height:", out fh)) h = fh;
                            }
                            currentSprite.rect = new Rect(x, y, w, h);
                            continue;
                        }

                        continue;
                    }
                }

                // Top-level scalar values we care about.
                int mip;
                if (TryParseInt(line, "enableMipMap:", out mip)) {
                    meta.EnableMipMap = mip != 0;
                    continue;
                }
                int ait;
                if (TryParseInt(line, "alphaIsTransparency:", out ait)) {
                    meta.AlphaIsTransparency = ait != 0;
                    continue;
                }
                int fm;
                if (TryParseInt(line, "filterMode:", out fm)) {
                    meta.FilterMode = ToFilterMode(fm);
                    continue;
                }
                int wm;
                if (TryParseInt(line, "wrapMode:", out wm)) {
                    meta.WrapMode = ToWrapMode(wm);
                    continue;
                }
                int se;
                if (TryParseInt(line, "spriteExtrude:", out se)) {
                    meta.SpriteExtrude = se;
                    continue;
                }
                int tc;
                if (TryParseInt(line, "textureCompression:", out tc)) {
                    meta.TextureCompression = ToTextureCompression(tc);
                    continue;
                }
                int sm;
                if (TryParseInt(line, "spriteMode:", out sm)) {
                    meta.SpriteMode = sm;
                    continue;
                }
                float ppu;
                if (TryParseFloat(line, "spritePixelsToUnits:", out ppu)) {
                    meta.SpritePixelsToUnits = ppu;
                    continue;
                }
                int align;
                if (TryParseInt(line, "alignment:", out align)) {
                    meta.SpriteAlignmentValue = align;
                    continue;
                }
                Vector2 pivot;
                if (TryParseInlineVector2(line, "spritePivot:", out pivot)) {
                    meta.SpritePivot = pivot;
                    continue;
                }

                // Sprite sheet section.
                if (line == "spriteSheet:") {
                    inSpriteSheet = true;
                    inSpritesList = false;
                    continue;
                }

                if (inSpriteSheet && line == "sprites:") {
                    inSpritesList = true;
                    spritesKeyIndent = CountLeadingSpaces(raw);
                    if (sprites == null) {
                        sprites = new List<SpriteMetaData>();
                    }
                    continue;
                }
            }

            if (sprites != null) {
                if (hasCurrentSprite) {
                    sprites.Add(currentSprite);
                }
                meta.Sprites = sprites.ToArray();
            }

            // Some atlases in the original Unity 6 project keep their slicing data in a different texture meta
            // (e.g. "beach-blue.png.meta" contains "beach-yellow_*" sprite entries while "beach-yellow.png.meta"
            // has an empty spriteSheet). If this texture is Multiple but has no sprites, attempt a sibling meta scan.
            if (meta.SpriteMode == 2 && (meta.Sprites == null || meta.Sprites.Length == 0)) {
                string pngPath = metaPath;
                if (pngPath.EndsWith(".meta", StringComparison.InvariantCultureIgnoreCase)) {
                    pngPath = pngPath.Substring(0, pngPath.Length - ".meta".Length);
                }

                int texW;
                int texH;
                if (TryReadPngSizeFromDisk(pngPath, out texW, out texH) && texW > 0 && texH > 0) {
                    string spritePrefix = Path.GetFileNameWithoutExtension(pngPath) + "_";
                    SpriteMetaData[] inferred = TryFindSpritesInSiblingMetas(metaPath, spritePrefix, texW, texH);
                    if (inferred != null && inferred.Length > 0) {
                        meta.Sprites = inferred;
                    }
                }
            }

            // If the original meta has default filterMode (or missing), force point filtering for sprites.
            if (meta.FilterMode != FilterMode.Point && meta.FilterMode != FilterMode.Bilinear && meta.FilterMode != FilterMode.Trilinear) {
                meta.FilterMode = FilterMode.Point;
            }

            return meta;
        }

        private static bool TryReadPngSizeFromDisk(string absPngPath, out int width, out int height) {
            width = 0;
            height = 0;
            if (string.IsNullOrEmpty(absPngPath) || !File.Exists(absPngPath)) {
                return false;
            }

            try {
                byte[] bytes = File.ReadAllBytes(absPngPath);
                if (bytes == null || bytes.Length < 24) {
                    return false;
                }

                // Signature + IHDR check (same as the helper used for local Unity project assets).
                if (bytes[0] != 137 || bytes[1] != 80 || bytes[2] != 78 || bytes[3] != 71) return false;
                if (bytes[12] != 73 || bytes[13] != 72 || bytes[14] != 68 || bytes[15] != 82) return false;

                width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
                height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
                return width > 0 && height > 0;
            } catch {
                return false;
            }
        }

        private static SpriteMetaData[] TryFindSpritesInSiblingMetas(string originalMetaPath, string spriteNamePrefix, int texW, int texH) {
            if (string.IsNullOrEmpty(originalMetaPath) || string.IsNullOrEmpty(spriteNamePrefix)) {
                return null;
            }

            try {
                string dir = Path.GetDirectoryName(originalMetaPath);
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) {
                    return null;
                }

                string[] metas = Directory.GetFiles(dir, "*.png.meta", SearchOption.TopDirectoryOnly);
                if (metas == null || metas.Length == 0) {
                    return null;
                }

                List<SpriteMetaData> collected = new List<SpriteMetaData>();
                for (int i = 0; i < metas.Length; i++) {
                    string metaPath = metas[i];
                    if (string.Equals(metaPath, originalMetaPath, StringComparison.InvariantCultureIgnoreCase)) {
                        continue;
                    }

                    SpriteMetaData[] sprites = TryParseSpritesOnly(metaPath, spriteNamePrefix);
                    if (sprites == null || sprites.Length == 0) {
                        continue;
                    }

                    for (int s = 0; s < sprites.Length; s++) {
                        SpriteMetaData smd = sprites[s];
                        Rect r = smd.rect;
                        if (r.width <= 0f || r.height <= 0f) {
                            continue;
                        }
                        // Accept if it fits as-is. If it doesn't, we'll attempt a shift after collecting all sprites.
                        collected.Add(smd);
                    }
                }

                if (collected.Count == 0) {
                    return null;
                }

                // If any rects are out of bounds, but the group would fit if shifted (cropped subset), shift them to (0,0).
                float minX = float.MaxValue;
                float minY = float.MaxValue;
                float maxX = float.MinValue;
                float maxY = float.MinValue;
                for (int i = 0; i < collected.Count; i++) {
                    Rect r = collected[i].rect;
                    if (r.x < minX) minX = r.x;
                    if (r.y < minY) minY = r.y;
                    if (r.x + r.width > maxX) maxX = r.x + r.width;
                    if (r.y + r.height > maxY) maxY = r.y + r.height;
                }

                bool fitsAsIs = (minX >= 0f && minY >= 0f && maxX <= texW && maxY <= texH);
                if (!fitsAsIs) {
                    float groupW = maxX - minX;
                    float groupH = maxY - minY;
                    if (groupW <= texW + 0.001f && groupH <= texH + 0.001f) {
                        for (int i = 0; i < collected.Count; i++) {
                            SpriteMetaData smd = collected[i];
                            Rect r = smd.rect;
                            r.x -= minX;
                            r.y -= minY;
                            smd.rect = r;
                            collected[i] = smd;
                        }
                        fitsAsIs = true;
                    }
                }

                if (!fitsAsIs) {
                    // As a last check, filter only those that fit within this texture's bounds.
                    List<SpriteMetaData> filtered = new List<SpriteMetaData>();
                    for (int i = 0; i < collected.Count; i++) {
                        Rect r = collected[i].rect;
                        if (r.x >= 0f && r.y >= 0f && (r.x + r.width) <= texW + 0.001f && (r.y + r.height) <= texH + 0.001f) {
                            filtered.Add(collected[i]);
                        }
                    }
                    collected = filtered;
                }

                if (collected.Count == 0) {
                    return null;
                }

                return collected.ToArray();
            } catch {
                return null;
            }
        }

        private static SpriteMetaData[] TryParseSpritesOnly(string metaPath, string spriteNamePrefix) {
            if (string.IsNullOrEmpty(metaPath) || !File.Exists(metaPath)) {
                return null;
            }

            string[] lines;
            try {
                lines = File.ReadAllLines(metaPath);
            } catch {
                return null;
            }

            bool inSpriteSheet = false;
            bool inSpritesList = false;
            int spritesKeyIndent = -1;
            List<SpriteMetaData> sprites = null;
            SpriteMetaData currentSprite = new SpriteMetaData();
            bool hasCurrentSprite = false;
            bool keepCurrent = false;

            for (int i = 0; i < lines.Length; i++) {
                string raw = lines[i];
                string line = raw.Trim();
                if (line.Length == 0) {
                    continue;
                }

                if (!inSpriteSheet) {
                    if (line == "spriteSheet:") {
                        inSpriteSheet = true;
                    }
                    continue;
                }

                if (!inSpritesList) {
                    if (line == "sprites:") {
                        inSpritesList = true;
                        spritesKeyIndent = CountLeadingSpaces(raw);
                        if (sprites == null) {
                            sprites = new List<SpriteMetaData>();
                        }
                    }
                    continue;
                }

                int indent = CountLeadingSpaces(raw);
                if (spritesKeyIndent >= 0 && indent <= spritesKeyIndent && !line.StartsWith("-", StringComparison.InvariantCulture)) {
                    break;
                }

                if (indent == spritesKeyIndent && line.StartsWith("- ", StringComparison.InvariantCulture)) {
                    if (hasCurrentSprite) {
                        if (keepCurrent) {
                            sprites.Add(currentSprite);
                        }
                        currentSprite = new SpriteMetaData();
                        hasCurrentSprite = false;
                        keepCurrent = false;
                    }
                    hasCurrentSprite = true;
                    currentSprite.alignment = 0;
                    currentSprite.pivot = new Vector2(0.5f, 0.5f);
                    currentSprite.border = Vector4.zero;
                    continue;
                }

                if (!hasCurrentSprite) {
                    continue;
                }

                if (line.StartsWith("name:", StringComparison.InvariantCultureIgnoreCase)) {
                    string n = line.Substring("name:".Length).Trim();
                    currentSprite.name = n;
                    keepCurrent = !string.IsNullOrEmpty(n) && n.StartsWith(spriteNamePrefix, StringComparison.InvariantCultureIgnoreCase);
                    continue;
                }

                if (!keepCurrent) {
                    continue;
                }

                Rect rect;
                if (TryParseInlineRect(line, "rect:", out rect)) {
                    currentSprite.rect = rect;
                    continue;
                }

                Vector2 sp;
                if (TryParseInlineVector2(line, "pivot:", out sp)) {
                    currentSprite.pivot = sp;
                    continue;
                }

                Vector4 border;
                if (TryParseInlineVector4(line, "border:", out border)) {
                    currentSprite.border = border;
                    continue;
                }

                int spriteAlign;
                if (TryParseInt(line, "alignment:", out spriteAlign)) {
                    currentSprite.alignment = spriteAlign;
                    continue;
                }

                if (line == "rect:") {
                    float x = 0, y = 0, w = 0, h = 0;
                    for (int j = i + 1; j < Math.Min(i + 12, lines.Length); j++) {
                        string l2raw = lines[j];
                        string l2 = l2raw.Trim();
                        if (l2.StartsWith("-", StringComparison.InvariantCulture)) break;
                        if (CountLeadingSpaces(l2raw) <= indent) break;

                        float fx, fy, fw, fh;
                        if (TryParseFloat(l2, "x:", out fx)) x = fx;
                        if (TryParseFloat(l2, "y:", out fy)) y = fy;
                        if (TryParseFloat(l2, "width:", out fw)) w = fw;
                        if (TryParseFloat(l2, "height:", out fh)) h = fh;
                    }
                    currentSprite.rect = new Rect(x, y, w, h);
                    continue;
                }
            }

            if (sprites != null && hasCurrentSprite && keepCurrent) {
                sprites.Add(currentSprite);
            }

            if (sprites == null || sprites.Count == 0) {
                return null;
            }

            return sprites.ToArray();
        }

        private static FilterMode ToFilterMode(int filterModeValue) {
            switch (filterModeValue) {
                case 0: return FilterMode.Point;
                case 1: return FilterMode.Bilinear;
                case 2: return FilterMode.Trilinear;
                default: return FilterMode.Point;
            }
        }

        private static TextureWrapMode ToWrapMode(int wrapModeValue) {
            // Unity meta: 0 = Repeat, 1 = Clamp, 2 = Mirror, 3 = MirrorOnce
            switch (wrapModeValue) {
                case 1: return TextureWrapMode.Clamp;
                case 2: return TextureWrapMode.Mirror;
                // Unity 2017.1 compatibility: avoid referencing TextureWrapMode.MirrorOnce directly (may not exist).
                case 3: return (TextureWrapMode)3;
                default: return TextureWrapMode.Repeat;
            }
        }

        private static TextureImporterCompression ToTextureCompression(int v) {
            // Common Unity meta values:
            // 0 = Uncompressed, 1 = Compressed, 2 = CompressedHQ, 3 = CompressedLQ
            switch (v) {
                case 0: return TextureImporterCompression.Uncompressed;
                case 2: return TextureImporterCompression.CompressedHQ;
                case 3: return TextureImporterCompression.CompressedLQ;
                default: return TextureImporterCompression.Compressed;
            }
        }

        public static void TrySetImporterProperty(TextureImporter importer, string propertyName, object value) {
            if (importer == null || string.IsNullOrEmpty(propertyName)) {
                return;
            }

            try {
                var prop = importer.GetType().GetProperty(propertyName);
                if (prop == null || !prop.CanWrite) {
                    return;
                }

                var targetType = prop.PropertyType;
                if (targetType.IsEnum) {
                    object enumValue = System.Enum.ToObject(targetType, value);
                    prop.SetValue(importer, enumValue, null);
                    return;
                }

                if (targetType == typeof(int)) {
                    int intValue = (value is int) ? (int)value : System.Convert.ToInt32(value);
                    prop.SetValue(importer, intValue, null);
                    return;
                }

                if (targetType == typeof(float)) {
                    float floatValue = (value is float) ? (float)value : System.Convert.ToSingle(value);
                    prop.SetValue(importer, floatValue, null);
                    return;
                }

                if (targetType == typeof(bool)) {
                    bool boolValue = (value is bool) ? (bool)value : System.Convert.ToBoolean(value);
                    prop.SetValue(importer, boolValue, null);
                    return;
                }

                prop.SetValue(importer, value, null);
            } catch {
                // Ignore: property doesn't exist or couldn't be assigned in this Unity version.
            }
        }

        public static void TrySetTextureSettingsProperty(TextureImporter importer, string propertyName, object value) {
            if (importer == null || string.IsNullOrEmpty(propertyName)) {
                return;
            }

            try {
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);

                object boxed = settings;
                var prop = boxed.GetType().GetProperty(propertyName);
                if (prop == null || !prop.CanWrite) {
                    return;
                }

                var targetType = prop.PropertyType;
                if (targetType.IsEnum) {
                    object enumValue = System.Enum.ToObject(targetType, value);
                    prop.SetValue(boxed, enumValue, null);
                } else if (targetType == typeof(int)) {
                    int intValue = (value is int) ? (int)value : System.Convert.ToInt32(value);
                    prop.SetValue(boxed, intValue, null);
                } else if (targetType == typeof(float)) {
                    float floatValue = (value is float) ? (float)value : System.Convert.ToSingle(value);
                    prop.SetValue(boxed, floatValue, null);
                } else if (targetType == typeof(bool)) {
                    bool boolValue = (value is bool) ? (bool)value : System.Convert.ToBoolean(value);
                    prop.SetValue(boxed, boolValue, null);
                } else {
                    prop.SetValue(boxed, value, null);
                }

                settings = (TextureImporterSettings)boxed;
                importer.SetTextureSettings(settings);
            } catch {
                // Ignore: property doesn't exist or couldn't be assigned in this Unity version.
            }
        }

        private static bool TryParseInt(string line, string prefix, out int value) {
            value = 0;
            if (!line.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)) {
                return false;
            }
            string raw = line.Substring(prefix.Length).Trim();
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseFloat(string line, string prefix, out float value) {
            value = 0f;
            if (!line.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)) {
                return false;
            }
            string raw = line.Substring(prefix.Length).Trim();
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static string ExtractAfter(string line, string key) {
            int idx = line.IndexOf(key, StringComparison.InvariantCultureIgnoreCase);
            if (idx < 0) return null;
            string raw = line.Substring(idx + key.Length).Trim();
            return raw;
        }

        private static bool TryParseInlineRect(string line, string prefix, out Rect rect) {
            rect = new Rect();
            if (!line.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)) {
                return false;
            }
            int braceStart = line.IndexOf('{');
            int braceEnd = line.IndexOf('}');
            if (braceStart < 0 || braceEnd < 0 || braceEnd <= braceStart) {
                return false;
            }
            string inner = line.Substring(braceStart + 1, braceEnd - braceStart - 1);
            float x, y, w, h;
            if (!TryParseInlineFloat(inner, "x", out x)) return false;
            if (!TryParseInlineFloat(inner, "y", out y)) return false;
            if (!TryParseInlineFloat(inner, "width", out w)) return false;
            if (!TryParseInlineFloat(inner, "height", out h)) return false;
            rect = new Rect(x, y, w, h);
            return true;
        }

        private static bool TryParseInlineVector2(string line, string prefix, out Vector2 vec) {
            vec = Vector2.zero;
            if (!line.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)) {
                return false;
            }
            int braceStart = line.IndexOf('{');
            int braceEnd = line.IndexOf('}');
            if (braceStart < 0 || braceEnd < 0 || braceEnd <= braceStart) {
                return false;
            }
            string inner = line.Substring(braceStart + 1, braceEnd - braceStart - 1);
            float x, y;
            if (!TryParseInlineFloat(inner, "x", out x)) return false;
            if (!TryParseInlineFloat(inner, "y", out y)) return false;
            vec = new Vector2(x, y);
            return true;
        }

        private static int CountLeadingSpaces(string rawLine) {
            if (string.IsNullOrEmpty(rawLine)) {
                return 0;
            }
            int count = 0;
            for (int i = 0; i < rawLine.Length; i++) {
                char c = rawLine[i];
                if (c == ' ') {
                    count++;
                } else {
                    break;
                }
            }
            return count;
        }

        private static bool TryParseInlineVector4(string line, string prefix, out Vector4 vec) {
            vec = Vector4.zero;
            if (!line.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)) {
                return false;
            }
            int braceStart = line.IndexOf('{');
            int braceEnd = line.IndexOf('}');
            if (braceStart < 0 || braceEnd < 0 || braceEnd <= braceStart) {
                return false;
            }
            string inner = line.Substring(braceStart + 1, braceEnd - braceStart - 1);
            float x, y, z, w;
            if (!TryParseInlineFloat(inner, "x", out x)) return false;
            if (!TryParseInlineFloat(inner, "y", out y)) return false;
            if (!TryParseInlineFloat(inner, "z", out z)) return false;
            if (!TryParseInlineFloat(inner, "w", out w)) return false;
            vec = new Vector4(x, y, z, w);
            return true;
        }

        private static bool TryParseInlineFloat(string dict, string key, out float value) {
            value = 0f;
            // Example: "x: 0, y: 0, width: 16, height: 16"
            string[] parts = dict.Split(',');
            for (int i = 0; i < parts.Length; i++) {
                string p = parts[i].Trim();
                if (p.StartsWith(key + ":", StringComparison.InvariantCultureIgnoreCase)) {
                    string raw = p.Substring(key.Length + 1).Trim();
                    return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
                }
            }
            return false;
        }
    }
}
