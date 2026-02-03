using UnityEngine;

namespace NSMB.Visual {
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SimpleSpriteAnimator : MonoBehaviour {
        public Sprite[] frames;
        public float framesPerSecond = 10f;
        public bool loop = true;
        public bool playOnEnable = true;

        private SpriteRenderer _renderer;
        private float _accum;
        private int _index;
        private bool _playing;

        private void Awake() {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable() {
            if (playOnEnable) {
                Play();
            }
        }

        public void Play() {
            _playing = true;
            _accum = 0f;
            _index = 0;
            ApplyFrame(0);
        }

        public void Stop() {
            _playing = false;
        }

        public void SetFrames(Sprite[] newFrames, float fps, bool shouldLoop) {
            frames = newFrames;
            framesPerSecond = fps;
            loop = shouldLoop;
            Play();
        }

        private void Update() {
            if (!_playing || frames == null || frames.Length <= 1 || framesPerSecond <= 0f) {
                return;
            }

            _accum += Time.deltaTime;
            float frameTime = 1f / framesPerSecond;

            while (_accum >= frameTime) {
                _accum -= frameTime;
                _index++;

                if (_index >= frames.Length) {
                    if (loop) {
                        _index = 0;
                    } else {
                        _index = frames.Length - 1;
                        _playing = false;
                    }
                }

                ApplyFrame(_index);
            }
        }

        private void ApplyFrame(int idx) {
            if (_renderer == null || frames == null || frames.Length == 0) {
                return;
            }

            if (idx < 0 || idx >= frames.Length) {
                idx = 0;
            }

            Sprite s = frames[idx];
            if (s != null) {
                _renderer.sprite = s;
            }
        }
    }
}

