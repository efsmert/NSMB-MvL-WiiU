using UnityEngine;

namespace NSMB.Player {
    [RequireComponent(typeof(PlayerMotor2D))]
    public sealed class PlayerSfx : MonoBehaviour {
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
                _audio.PlayOneShot(NSMB.Audio.SoundEffectId.Player_Sound_Jump, jumpVolume);
            }
        }
    }
}
