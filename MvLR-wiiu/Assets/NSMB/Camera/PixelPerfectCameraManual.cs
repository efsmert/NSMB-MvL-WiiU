using UnityEngine;

namespace NSMB.Camera {
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class PixelPerfectCameraManual : MonoBehaviour {
        public int pixelsPerUnit = 16;
        public int referenceHeight = 180;

        private UnityEngine.Camera _cam;
        private int _lastW = -1;
        private int _lastH = -1;

        private void Awake() {
            _cam = GetComponent<UnityEngine.Camera>();
        }

        private void Start() {
            UpdateCameraSize();
        }

        private void Update() {
            if (Screen.width != _lastW || Screen.height != _lastH) {
                UpdateCameraSize();
            }
        }

        private void UpdateCameraSize() {
            _lastW = Screen.width;
            _lastH = Screen.height;

            if (_cam == null || pixelsPerUnit <= 0 || referenceHeight <= 0) {
                return;
            }

            int screenHeight = Screen.height;
            int scale = Mathf.Max(1, screenHeight / referenceHeight);
            float orthoSize = (float)screenHeight / (scale * pixelsPerUnit * 2f);
            _cam.orthographicSize = orthoSize;
        }
    }
}

