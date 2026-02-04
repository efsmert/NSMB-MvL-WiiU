using UnityEngine;

	namespace NSMB.Camera {
	    [RequireComponent(typeof(UnityEngine.Camera))]
	    public sealed class PixelPerfectCameraManual : MonoBehaviour {
	        public int pixelsPerUnit = 16;
	        public int referenceHeight = 180;

	        private UnityEngine.Camera _cam;
	        private int _lastW = -1;
	        private int _lastH = -1;
	        private int _currentScale = 1;

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
	            if (_cam == null || pixelsPerUnit <= 0 || referenceHeight <= 0) {
	                return;
	            }

	            // Use camera pixel rect rather than Screen.* to correctly handle aspect-limited Game view
	            // (letterboxing) and multi-display setups.
	            int pixelH = Mathf.Max(1, _cam.pixelHeight);
	            int pixelW = Mathf.Max(1, _cam.pixelWidth);
	            _lastW = pixelW;
	            _lastH = pixelH;

	            int scale = Mathf.Max(1, pixelH / referenceHeight);
	            _currentScale = Mathf.Max(1, scale);
	            float orthoSize = (float)pixelH / (_currentScale * pixelsPerUnit * 2f);
	            _cam.orthographicSize = orthoSize;
	        }

	        public float GetPixelSnapUnit() {
	            if (_cam == null || !_cam.orthographic) {
	                return 0f;
	            }
	            // Exact world-space size of a screen pixel for this camera.
	            float h = Mathf.Max(1f, (float)_cam.pixelHeight);
	            return (_cam.orthographicSize * 2f) / h;
	        }
	    }
	}
