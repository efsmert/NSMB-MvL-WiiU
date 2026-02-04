using UnityEngine;

namespace NSMB.Camera {
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class CameraFollow2D : MonoBehaviour {
        public Transform target;
        public Vector2 offset = new Vector2(0f, 1f);
        public float smoothTime = 0.12f;

        [Header("Pixel Snapping")]
        public bool pixelSnap = true;

        public bool clampToBounds;
        public Vector2 boundsMin;
        public Vector2 boundsMax;

        private Vector3 _velocity;

        private void LateUpdate() {
            if (target == null) {
                return;
            }

            Vector3 desired = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
            Vector3 next = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);

            if (clampToBounds && boundsMin != boundsMax) {
                UnityEngine.Camera cam = GetComponent<UnityEngine.Camera>();
                if (cam != null && cam.orthographic) {
                    float halfH = cam.orthographicSize;
                    float halfW = halfH * cam.aspect;

                    float minX = boundsMin.x + halfW;
                    float maxX = boundsMax.x - halfW;
                    float minY = boundsMin.y + halfH;
                    float maxY = boundsMax.y - halfH;

                    if (minX <= maxX) next.x = Mathf.Clamp(next.x, minX, maxX);
                    if (minY <= maxY) next.y = Mathf.Clamp(next.y, minY, maxY);
                }
            }

            transform.position = next;

	            if (pixelSnap) {
	                UnityEngine.Camera cam = GetComponent<UnityEngine.Camera>();
	                if (cam != null && cam.orthographic && cam.pixelHeight > 0) {
                        float unit = 0f;
                        PixelPerfectCameraManual pp = cam.GetComponent<PixelPerfectCameraManual>();
                        if (pp != null) {
                            unit = pp.GetPixelSnapUnit();
                        }
                        if (unit <= 0f) {
	                        unit = (cam.orthographicSize * 2f) / (float)cam.pixelHeight;
                        }
	                    if (unit > 0f) {
	                        Vector3 snapped = transform.position;
	                        snapped.x = Mathf.Round(snapped.x / unit) * unit;
	                        snapped.y = Mathf.Round(snapped.y / unit) * unit;
	                        transform.position = snapped;
	                    }
	                }
	            }
	        }

        public void SetBounds(Vector2 min, Vector2 max) {
            boundsMin = min;
            boundsMax = max;
            clampToBounds = (min != max);
        }

        public void ApplyWrapDelta(float dx, float dy) {
            if (Mathf.Abs(dx) <= 0.0001f && Mathf.Abs(dy) <= 0.0001f) {
                return;
            }

            transform.position = new Vector3(transform.position.x + dx, transform.position.y + dy, transform.position.z);
            _velocity = Vector3.zero;
        }
    }
}
