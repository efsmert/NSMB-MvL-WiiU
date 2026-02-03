using UnityEngine;

namespace NSMB.Audio {
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioManager : MonoBehaviour {
        private AudioSource _sfxSource;
        private AudioSource _musicSource;
        [SerializeField] private float _sfxVolume = 1f;
        [SerializeField] private float _musicVolume = 0.8f;
        private string _currentMusicPath;

        public float SfxVolume {
            get { return _sfxVolume; }
            set { _sfxVolume = Mathf.Clamp01(value); }
        }

        public float MusicVolume {
            get { return _musicVolume; }
            set {
                _musicVolume = Mathf.Clamp01(value);
                if (_musicSource != null) {
                    _musicSource.volume = _musicVolume;
                }
            }
        }

        private void Awake() {
            _sfxSource = GetComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.loop = false;

            // Separate music source so one-shots don't cut off the loop.
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.volume = Mathf.Clamp01(_musicVolume);
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

        public void PlayMusic(AudioClip clip, float volume, bool loop) {
            if (_musicSource == null) {
                return;
            }

            if (clip == null) {
                StopMusic();
                return;
            }

            if (_musicSource.clip == clip && _musicSource.isPlaying) {
                _musicSource.loop = loop;
                _musicSource.volume = Mathf.Clamp01(volume) * Mathf.Clamp01(_musicVolume);
                return;
            }

            _musicSource.Stop();
            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.volume = Mathf.Clamp01(volume) * Mathf.Clamp01(_musicVolume);
            _musicSource.Play();
        }

        public void PlayMusicResources(string resourcesPath, float volume, bool loop) {
            if (string.IsNullOrEmpty(resourcesPath)) {
                StopMusic();
                return;
            }

            // Avoid reloading/restarting the same track on repeated EnterMenu calls.
            if (string.Equals(_currentMusicPath, resourcesPath) && _musicSource != null && _musicSource.isPlaying) {
                _musicSource.loop = loop;
                _musicSource.volume = Mathf.Clamp01(volume) * Mathf.Clamp01(_musicVolume);
                return;
            }

            AudioClip clip = Resources.Load(resourcesPath) as AudioClip;
            _currentMusicPath = resourcesPath;
            PlayMusic(clip, volume, loop);
        }

        public void StopMusic() {
            _currentMusicPath = null;
            if (_musicSource != null) {
                _musicSource.Stop();
                _musicSource.clip = null;
            }
        }
    }
}
