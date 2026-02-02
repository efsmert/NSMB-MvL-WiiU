using UnityEngine;

namespace NSMB.Audio {
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioManager : MonoBehaviour {
        private AudioSource _sfxSource;
        [SerializeField] private float _sfxVolume = 1f;

        private void Awake() {
            _sfxSource = GetComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.loop = false;
        }

        public void PlayOneShot(AudioClip clip, float volume) {
            if (clip == null) {
                return;
            }
            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume) * Mathf.Clamp01(_sfxVolume));
        }

        public void PlayOneShot(AudioClip clip) {
            PlayOneShot(clip, 1f);
        }

        public void PlayResourcesOneShot(string resourcesPath, float volume) {
            if (string.IsNullOrEmpty(resourcesPath)) {
                return;
            }

            AudioClip clip = Resources.Load(resourcesPath) as AudioClip;
            PlayOneShot(clip, volume);
        }

        public void PlayResourcesOneShot(string resourcesPath) {
            PlayResourcesOneShot(resourcesPath, 1f);
        }
    }
}
