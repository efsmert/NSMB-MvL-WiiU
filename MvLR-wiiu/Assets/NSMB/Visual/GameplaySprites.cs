using UnityEngine;

namespace NSMB.Visual {
    public static class GameplaySprites {
        private static bool _warnedGoomba;
        private static bool _warnedPowerups;
        private static bool _warnedPlatforms;
        private static bool _warnedAnimatedBlocks;
        private static bool _warnedDottedCoins;

        public static Sprite GetGoombaIdle() {
            Sprite s = NSMB.Content.ResourceSpriteCache.FindSprite(NSMB.Content.GameplayAtlasPaths.Goomba, "goomba_0");
            if (s == null) {
                WarnOnce(ref _warnedGoomba, NSMB.Content.GameplayAtlasPaths.Goomba, "goomba_0");
            }
            return s;
        }

        public static Sprite[] GetGoombaWalkFrames() {
            // Typical walk is first two frames.
            Sprite a = NSMB.Content.ResourceSpriteCache.FindSprite(NSMB.Content.GameplayAtlasPaths.Goomba, "goomba_0");
            Sprite b = NSMB.Content.ResourceSpriteCache.FindSprite(NSMB.Content.GameplayAtlasPaths.Goomba, "goomba_1");
            if (a != null && b != null) {
                return new Sprite[] { a, b };
            }
            if (a == null || b == null) {
                WarnOnce(ref _warnedGoomba, NSMB.Content.GameplayAtlasPaths.Goomba, "goomba_0/goomba_1");
            }
            return NSMB.Content.ResourceSpriteCache.FindSpritesByPrefix(NSMB.Content.GameplayAtlasPaths.Goomba, "goomba_");
        }

        public static Sprite[] GetCoinSpinFrames() {
            Sprite[] frames = NSMB.Content.ResourceSpriteCache.FindSpritesByPrefix(NSMB.Content.GameplayAtlasPaths.DottedCoins, "dotted-coins_");
            if (frames == null || frames.Length == 0) {
                WarnOnce(ref _warnedDottedCoins, NSMB.Content.GameplayAtlasPaths.DottedCoins, "dotted-coins_0..");
                return new Sprite[0];
            }
            return frames;
        }

        public static Sprite GetMushroom() {
            Sprite s = NSMB.Content.ResourceSpriteCache.FindSprite(NSMB.Content.GameplayAtlasPaths.Powerups, "Mushroom");
            if (s == null) {
                WarnOnce(ref _warnedPowerups, NSMB.Content.GameplayAtlasPaths.Powerups, "Mushroom");
            }
            return s;
        }

        public static Sprite GetPlatformTile(int index) {
            Sprite s = NSMB.Content.ResourceSpriteCache.FindSprite(NSMB.Content.GameplayAtlasPaths.Platforms, "platforms_" + index);
            if (s == null) {
                WarnOnce(ref _warnedPlatforms, NSMB.Content.GameplayAtlasPaths.Platforms, "platforms_" + index);
            }
            return s;
        }

        public static Sprite[] GetQuestionBlockFrames() {
            // Original sheet is "animation_0..". We use the first 4 frames.
            Sprite[] all = NSMB.Content.ResourceSpriteCache.FindSpritesByPrefix(NSMB.Content.GameplayAtlasPaths.AnimatedBlocks, "animation_");
            if (all == null || all.Length == 0) {
                WarnOnce(ref _warnedAnimatedBlocks, NSMB.Content.GameplayAtlasPaths.AnimatedBlocks, "animation_0..");
                return new Sprite[0];
            }
            int take = Mathf.Min(4, all.Length);
            Sprite[] frames = new Sprite[take];
            for (int i = 0; i < take; i++) {
                frames[i] = all[i];
            }
            return frames;
        }

        private static void WarnOnce(ref bool warned, string resourcesPath, string spriteName) {
            if (warned) {
                return;
            }
            warned = true;
            #if UNITY_EDITOR
            Debug.LogWarning(string.Format("[NSMB] Missing sliced sprite(s) at Resources.LoadAll(\"{0}\") for \"{1}\". Run NSMB/Resync Sprite Import Settings (From Original).", resourcesPath, spriteName));
            #endif
        }
    }
}
