using UnityEngine;

namespace NSMB.World {
    public sealed class StageWrapSpriteMirror : MonoBehaviour {
        public Transform source;
        public bool mirrorLocalTransform = true;

        private SpriteRenderer _sr;
        private SpriteRenderer _sourceSr;

        private void Awake() {
            _sr = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate() {
            if (source == null) {
                return;
            }

            if (mirrorLocalTransform) {
                transform.localPosition = source.localPosition;
                transform.localRotation = source.localRotation;
                transform.localScale = source.localScale;
            } else {
                transform.position = source.position;
                transform.rotation = source.rotation;
                transform.localScale = source.lossyScale;
            }

            if (_sr == null) {
                _sr = GetComponent<SpriteRenderer>();
            }
            if (_sr == null) {
                return;
            }

            if (_sourceSr == null) {
                _sourceSr = source.GetComponent<SpriteRenderer>();
            }
            if (_sourceSr == null) {
                return;
            }

            _sr.sprite = _sourceSr.sprite;
            _sr.color = _sourceSr.color;
            _sr.flipX = _sourceSr.flipX;
            _sr.flipY = _sourceSr.flipY;
            _sr.sortingLayerID = _sourceSr.sortingLayerID;
            _sr.sortingOrder = _sourceSr.sortingOrder;
            _sr.enabled = _sourceSr.enabled;
        }
    }
}

