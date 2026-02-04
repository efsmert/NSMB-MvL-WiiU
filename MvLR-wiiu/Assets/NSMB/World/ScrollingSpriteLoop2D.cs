using UnityEngine;

namespace NSMB.World {
    // Simple horizontal loop/scroller used for background layers (cloud strips, etc.).
    // Kept on StageWrap2D visual copies so wrap layers animate consistently.
    public sealed class ScrollingSpriteLoop2D : MonoBehaviour {
        [Tooltip("Scroll speed in world units per second. Negative scrolls left.")]
        public float speed = -0.1f;

        [Tooltip("Repeat period (world units). Usually the sprite's world width.")]
        public float tileWorldWidth = 1f;

        private Vector3 _startLocalPos;
        private float _timeOffset;

        private void Awake() {
            _startLocalPos = transform.localPosition;
            // De-correlate layers/copies so they don't all phase-align at (0,0).
            _timeOffset = Random.value * 1000f;
        }

        private void Update() {
            if (tileWorldWidth <= 0.0001f) {
                return;
            }

            // We apply offsets in local space, but the authored values are in world units.
            // Convert by the current world scale so scaled layers (e.g. big clouds) scroll at
            // the expected world speed and wrap with the correct repeat width.
            float scaleX = Mathf.Abs(transform.lossyScale.x);
            if (scaleX <= 0.0001f) {
                scaleX = 1f;
            }

            float localSpeed = speed / scaleX;
            float localTileWidth = tileWorldWidth / scaleX;

            float t = (Time.time + _timeOffset) * localSpeed;
            float offset;
            if (t >= 0f) {
                offset = Mathf.Repeat(t, localTileWidth);
            } else {
                offset = -Mathf.Repeat(-t, localTileWidth);
            }

            transform.localPosition = new Vector3(_startLocalPos.x + offset, _startLocalPos.y, _startLocalPos.z);
        }
    }
}
