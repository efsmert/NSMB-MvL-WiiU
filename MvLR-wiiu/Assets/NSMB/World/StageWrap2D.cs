using System.Collections.Generic;
using UnityEngine;

namespace NSMB.World {
    public sealed class StageWrap2D : MonoBehaviour {
        [Tooltip("Extra distance outside bounds before wrapping triggers (world units).")]
        public float wrapMargin = 0.5f;

        [Tooltip("How often to rescan for dynamic Rigidbody2D objects (seconds).")]
        public float refreshIntervalSeconds = 1.0f;

        private float _leftX;
        private float _rightX;
        private float _minY;
        private float _maxY;
        private float _wrapWidth;
        private float _nextRefreshTime;

        private readonly List<Rigidbody2D> _bodies = new List<Rigidbody2D>(64);
        private NSMB.Camera.CameraFollow2D _cameraFollow;

        private void Start() {
            ResolveBounds();
            ResolveCamera();
            RefreshBodies();
        }

        private void LateUpdate() {
            if (_wrapWidth <= 0.0001f) {
                return;
            }

            if (Time.time >= _nextRefreshTime) {
                _nextRefreshTime = Time.time + Mathf.Max(0.1f, refreshIntervalSeconds);
                RefreshBodies();
                ResolveCamera();
            }

            WrapBodies();
        }

        private void ResolveCamera() {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null) {
                _cameraFollow = null;
                return;
            }

            _cameraFollow = cam.GetComponent<NSMB.Camera.CameraFollow2D>();
        }

        private void ResolveBounds() {
            Bounds bounds;
            if (TryGetStaticColliderBounds(out bounds) || TryGetSpriteBounds(out bounds)) {
                _leftX = bounds.min.x;
                _rightX = bounds.max.x;
                _minY = bounds.min.y;
                _maxY = bounds.max.y;
                _wrapWidth = _rightX - _leftX;
            } else {
                _leftX = 0f;
                _rightX = 0f;
                _minY = 0f;
                _maxY = 0f;
                _wrapWidth = 0f;
            }
        }

        private bool TryGetStaticColliderBounds(out Bounds bounds) {
            bounds = new Bounds();
            bool any = false;

            BoxCollider2D[] cols = GetComponentsInChildren<BoxCollider2D>(true);
            if (cols == null || cols.Length == 0) {
                return false;
            }

            for (int i = 0; i < cols.Length; i++) {
                BoxCollider2D c = cols[i];
                if (c == null) continue;

                Rigidbody2D rb = c.attachedRigidbody;
                if (rb == null || rb.bodyType != RigidbodyType2D.Static) {
                    continue;
                }

                if (!any) {
                    bounds = c.bounds;
                    any = true;
                } else {
                    bounds.Encapsulate(c.bounds);
                }
            }

            return any;
        }

        private bool TryGetSpriteBounds(out Bounds bounds) {
            bounds = new Bounds();
            bool any = false;

            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers == null || renderers.Length == 0) {
                return false;
            }

            for (int i = 0; i < renderers.Length; i++) {
                SpriteRenderer r = renderers[i];
                if (r == null || r.sprite == null) continue;

                if (!any) {
                    bounds = r.bounds;
                    any = true;
                } else {
                    bounds.Encapsulate(r.bounds);
                }
            }

            return any;
        }

        private void RefreshBodies() {
            _bodies.Clear();

            Object[] objs = Object.FindObjectsOfType(typeof(Rigidbody2D));
            if (objs == null || objs.Length == 0) {
                return;
            }

            float yMin = _minY - 8f;
            float yMax = _maxY + 8f;

            for (int i = 0; i < objs.Length; i++) {
                Rigidbody2D rb = objs[i] as Rigidbody2D;
                if (rb == null) continue;
                if (rb.bodyType == RigidbodyType2D.Static) continue;

                // Filter out off-stage rigidbodies (UI helpers, etc.) by vertical range.
                Vector2 p = rb.position;
                if (p.y < yMin || p.y > yMax) {
                    continue;
                }

                _bodies.Add(rb);
            }
        }

        private void WrapBodies() {
            float left = _leftX - wrapMargin;
            float right = _rightX + wrapMargin;
            float width = _wrapWidth;

            if (width <= 0.0001f) {
                return;
            }

            for (int i = 0; i < _bodies.Count; i++) {
                Rigidbody2D rb = _bodies[i];
                if (rb == null) continue;

                Vector2 pos = rb.position;
                float dx = 0f;

                if (pos.x < left) {
                    dx = width;
                } else if (pos.x > right) {
                    dx = -width;
                }

                if (Mathf.Abs(dx) <= 0.0001f) {
                    continue;
                }

                Vector2 next = new Vector2(pos.x + dx, pos.y);
                rb.position = next;

                // If the camera is following this rigidbody (or a child of it), shift the camera as well so
                // it doesn't SmoothDamp across the entire wrap width.
                if (_cameraFollow != null && _cameraFollow.target != null) {
                    NSMB.Player.PlayerMotor2D player = rb.GetComponent<NSMB.Player.PlayerMotor2D>();
                    if (player != null) {
                        NSMB.Player.PlayerMotor2D targetPlayer = _cameraFollow.target.GetComponentInParent<NSMB.Player.PlayerMotor2D>();
                        if (targetPlayer == player) {
                            _cameraFollow.ApplyWrapDelta(dx, 0f);
                        }
                    }
                }
            }
        }
    }
}

