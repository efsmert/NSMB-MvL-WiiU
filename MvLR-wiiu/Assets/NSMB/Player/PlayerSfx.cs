using UnityEngine;

namespace NSMB.Player {
    [RequireComponent(typeof(PlayerMotor2D))]
    public sealed class PlayerSfx : MonoBehaviour {
        public string jumpSfxResourcesPath = "NSMB/AudioClips/Resources/Sound/player/jump";
        public float jumpVolume = 0.9f;

        private NSMB.Audio.AudioManager _audio;

        private void Start() {
            NSMB.Core.GameRoot root = NSMB.Core.GameRoot.Instance;
            if (root != null) {
                _audio = root.GetComponent<NSMB.Audio.AudioManager>();
            }
        }

        private void OnPlayerJump() {
            if (_audio != null) {
                _audio.PlayResourcesOneShot(jumpSfxResourcesPath, jumpVolume);
            }
        }
    }
}

