using System.Collections.Generic;
using UnityEngine;

namespace NSMB.Audio {
    public enum SoundEffectId : byte {
        Enemy_Generic_Stomp = 2,
        Enemy_Shell_Kick = 8,
        Player_Sound_Collision = 17,
        Player_Sound_Jump = 24,
        Player_Sound_PowerupCollect = 16,

        UI_Decide = 81,
        UI_Back = 82,
        UI_Cursor = 83,
        UI_WindowClose = 85,
        UI_WindowOpen = 86,

        World_Block_Bump = 68,
        World_Block_Break = 67,
        World_Block_Powerup = 69,
        World_Coin_Collect = 70,
    }

    public static class SoundEffectIdExtensions {
        private static readonly Dictionary<SoundEffectId, string> Paths = new Dictionary<SoundEffectId, string> {
            { SoundEffectId.Enemy_Generic_Stomp, "enemy/stomp" },
            { SoundEffectId.Enemy_Shell_Kick, "enemy/shell_kick" },
            { SoundEffectId.Player_Sound_Collision, "player/collision" },
            { SoundEffectId.Player_Sound_Jump, "player/jump" },
            { SoundEffectId.Player_Sound_PowerupCollect, "player/powerup" },

            { SoundEffectId.UI_Decide, "ui/decide" },
            { SoundEffectId.UI_Back, "ui/back" },
            { SoundEffectId.UI_Cursor, "ui/cursor" },
            { SoundEffectId.UI_WindowClose, "ui/windowclosed" },
            { SoundEffectId.UI_WindowOpen, "ui/windowopen" },

            { SoundEffectId.World_Block_Bump, "world/block_bump" },
            { SoundEffectId.World_Block_Break, "world/block_break" },
            { SoundEffectId.World_Block_Powerup, "world/block_powerup" },
            { SoundEffectId.World_Coin_Collect, "world/coin_collect" },
        };

        private static readonly Dictionary<string, AudioClip> ClipCache = new Dictionary<string, AudioClip>();

        public static string GetResourcesPath(this SoundEffectId sfx, int variant) {
            string basePath;
            if (!Paths.TryGetValue(sfx, out basePath)) {
                basePath = "ui/error";
            }

            // Audio is stored under: Assets/Resources/NSMB/AudioClips/Resources/Sound/...
            // So the Resources.Load path must include: "NSMB/AudioClips/Resources/" + "Sound/..."
            string name = "NSMB/AudioClips/Resources/Sound/" + basePath;
            if (variant > 0) {
                name = name + "_" + variant;
            }
            return name;
        }

        public static AudioClip LoadClip(this SoundEffectId sfx, int variant) {
            string path = sfx.GetResourcesPath(variant);

            AudioClip cached;
            if (ClipCache.TryGetValue(path, out cached)) {
                return cached;
            }

            AudioClip clip = Resources.Load(path) as AudioClip;
            ClipCache[path] = clip;
            return clip;
        }
    }
}
