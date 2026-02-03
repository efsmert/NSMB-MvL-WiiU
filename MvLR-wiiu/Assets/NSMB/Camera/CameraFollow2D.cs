using UnityEngine;

namespace NSMB.Camera {
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class CameraFollow2D : MonoBehaviour {
        public Transform target;
        public Vector2 offset = new Vector2(0f, 1f);
        public float smoothTime = 0.12f;

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
        }

        public void SetBounds(Vector2 min, Vector2 max) {
            boundsMin = min;
            boundsMax = max;
            clampToBounds = (min != max);
        }
    }
}
