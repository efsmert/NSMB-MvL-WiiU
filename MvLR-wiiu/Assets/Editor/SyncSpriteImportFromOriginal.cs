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

        if (meta.SpriteMode == 2) {
            importer.spriteImportMode = SpriteImportMode.Multiple;
            if (meta.Sprites != null && meta.Sprites.Length > 0) {
                importer.spritesheet = meta.Sprites;
            }
        } else {
            importer.spriteImportMode = SpriteImportMode.Single;
            ApplySingleSpriteSettings(importer, meta.SpriteAlignmentValue, meta.SpritePivot);
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
                        if (line.StartsWith("- ", StringComparison.InvariantCulture)) {
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

            // If the original meta has default filterMode (or missing), force point filtering for sprites.
            if (meta.FilterMode != FilterMode.Point && meta.FilterMode != FilterMode.Bilinear && meta.FilterMode != FilterMode.Trilinear) {
                meta.FilterMode = FilterMode.Point;
            }

            return meta;
        }

        private static FilterMode ToFilterMode(int filterModeValue) {
            switch (filterModeValue) {
                case 0: return FilterMode.Point;
                case 1: return FilterMode.Bilinear;
                case 2: return FilterMode.Trilinear;
                default: return FilterMode.Point;
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
