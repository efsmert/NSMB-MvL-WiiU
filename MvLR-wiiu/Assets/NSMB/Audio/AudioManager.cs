using UnityEngine;

namespace NSMB.Audio {
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioManager : MonoBehaviour {
        private AudioSource _sfxSource;
        [SerializeField] private float _sfxVolume = 1f;

        public float SfxVolume {
            get { return _sfxVolume; }
            set { _sfxVolume = Mathf.Clamp01(value); }
        }

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

        public void PlayOneShot(SoundEffectId soundEffect, float volume, int variant) {
            AudioClip clip = soundEffect.LoadClip(variant);
            PlayOneShot(clip, volume);
        }

        public void PlayOneShot(SoundEffectId soundEffect, float volume) {
            PlayOneShot(soundEffect, volume, 0);
        }

        public void PlayOneShot(SoundEffectId soundEffect) {
            PlayOneShot(soundEffect, 1f, 0);
        }
    }
}
