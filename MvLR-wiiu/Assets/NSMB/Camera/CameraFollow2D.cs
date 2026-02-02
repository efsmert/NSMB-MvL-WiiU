using UnityEngine;

namespace NSMB.Camera {
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class CameraFollow2D : MonoBehaviour {
        public Transform target;
        public Vector2 offset = new Vector2(0f, 1f);
        public float smoothTime = 0.12f;

        private Vector3 _velocity;

        private void LateUpdate() {
            if (target == null) {
                return;
            }

            Vector3 desired = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
        }
    }
}

