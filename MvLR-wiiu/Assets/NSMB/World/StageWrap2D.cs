using System.Collections.Generic;
using UnityEngine;

namespace NSMB.World {
    public sealed class StageWrap2D : MonoBehaviour {
        [Tooltip("Extra distance outside bounds before wrapping triggers (world units).")]
        public float wrapMargin = 0.0f;

        [Tooltip("How often to rescan for dynamic Rigidbody2D objects (seconds).")]
        public float refreshIntervalSeconds = 1.0f;

        private float _leftX;
        private float _rightX;
        private float _minY;
        private float _maxY;
        private float _wrapWidth;
        private float _nextRefreshTime;

        private readonly List<Rigidbody2D> _bodies = new List<Rigidbody2D>(64);
        private readonly List<Transform> _transforms = new List<Transform>(64);
        private NSMB.Camera.CameraFollow2D _cameraFollow;
        private bool _explicitBounds;
        private bool _builtVisualCopies;
        private float _effectiveWrapWidth;

        public float WrapWidth {
            get { return _effectiveWrapWidth; }
        }

        public float LeftX {
            get { return _leftX; }
        }

        private void Start() {
            ResolveBounds();
            ResolveCamera();
            RefreshBodies();
            EnsureVisualCopies();
        }

        private void Update() {
            if (_wrapWidth <= 0.0001f) {
                return;
            }

            WrapWorldIfNeeded();
        }

        private void LateUpdate() {
            if (_wrapWidth <= 0.0001f) {
                return;
            }

            if (Time.time >= _nextRefreshTime) {
                _nextRefreshTime = Time.time + Mathf.Max(0.1f, refreshIntervalSeconds);
                RefreshBodies();
                ResolveCamera();
                EnsureVisualCopies();
            }

            // WrapWorldIfNeeded runs in Update so CameraFollow2D (LateUpdate) sees already-wrapped positions.
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
            if (_explicitBounds && _wrapWidth > 0.0001f) {
                return;
            }

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

            _effectiveWrapWidth = SnapWrapWidth(_wrapWidth);
            _rightX = _leftX + _effectiveWrapWidth;
        }

        public void ConfigureBounds(Vector2 min, Vector2 max) {
            if (min == max) {
                return;
            }

            _explicitBounds = true;
            _leftX = Mathf.Min(min.x, max.x);
            _rightX = Mathf.Max(min.x, max.x);
            _minY = Mathf.Min(min.y, max.y);
            _maxY = Mathf.Max(min.y, max.y);
            _wrapWidth = _rightX - _leftX;
            _effectiveWrapWidth = SnapWrapWidth(_wrapWidth);
            _rightX = _leftX + _effectiveWrapWidth;
        }

        private float SnapWrapWidth(float w) {
            // Snap to the camera's pixel grid to prevent visible 1px jitter when we recenter.
            // Avoid snapping to "whole world units" here; some stages/layers use scaled tiles.
            float snapped = w;
            float pixel = GetPixelSnapUnit();
            if (pixel > 0.0000001f) {
                snapped = Mathf.Round(snapped / pixel) * pixel;
            } else {
                // Fallback: tame float noise without forcing integer units.
                snapped = Mathf.Round(snapped * 1000f) / 1000f;
            }
            return Mathf.Max(0f, snapped);
        }

        private float GetPixelSnapUnit() {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null) {
                return 0f;
            }
            NSMB.Camera.PixelPerfectCameraManual pp = cam.GetComponent<NSMB.Camera.PixelPerfectCameraManual>();
            if (pp != null) {
                return pp.GetPixelSnapUnit();
            }
            if (cam.orthographic && cam.pixelHeight > 0) {
                return (cam.orthographicSize * 2f) / (float)cam.pixelHeight;
            }
            return 0f;
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
            _transforms.Clear();

            float yMin = _minY - 8f;
            float yMax = _maxY + 8f;

            // Prefer wrapping stage entities (coins, enemies, etc.) under our stage root.
            Transform entities = transform.Find("Entities");
            if (entities != null) {
                Rigidbody2D[] rbs = entities.GetComponentsInChildren<Rigidbody2D>(true);
                if (rbs != null) {
                    for (int i = 0; i < rbs.Length; i++) {
                        Rigidbody2D rb = rbs[i];
                        if (rb == null) continue;
                        if (rb.bodyType == RigidbodyType2D.Static) continue;

                        Vector2 p = rb.position;
                        if (p.y < yMin || p.y > yMax) continue;
                        _bodies.Add(rb);
                    }
                }

                // Wrap trigger-only pickups that don't have their own Rigidbody2D (coins, etc.).
                Collider2D[] cols = entities.GetComponentsInChildren<Collider2D>(true);
                if (cols != null) {
                    for (int i = 0; i < cols.Length; i++) {
                        Collider2D c = cols[i];
                        if (c == null) continue;
                        if (c.attachedRigidbody != null) continue;
                        if (!c.isTrigger) continue;

                        Transform tr = c.transform;
                        if (tr == null) continue;
                        Vector3 p3 = tr.position;
                        if (p3.y < yMin || p3.y > yMax) continue;
                        _transforms.Add(tr);
                    }
                }
            }

            // Always include players (live outside the stage root).
            NSMB.Player.PlayerMotor2D[] players = Object.FindObjectsOfType(typeof(NSMB.Player.PlayerMotor2D)) as NSMB.Player.PlayerMotor2D[];
            if (players != null) {
                for (int i = 0; i < players.Length; i++) {
                    NSMB.Player.PlayerMotor2D player = players[i];
                    if (player == null) continue;
                    Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
                    if (prb == null) continue;
                    if (!_bodies.Contains(prb)) {
                        _bodies.Add(prb);
                    }
                }
            }
        }

        private void WrapWorldIfNeeded() {
            Rigidbody2D playerRb = GetPlayerBody();
            if (playerRb == null) {
                return;
            }

            float width = _effectiveWrapWidth;
            if (width <= 0.0001f) {
                return;
            }

            Vector2 ppos = playerRb.position;
            float left = _leftX - wrapMargin;
            float right = _rightX + wrapMargin;

            float dxShift = 0f;
            float pixel = GetPixelSnapUnit();
            float eps = (pixel > 0.0000001f) ? (pixel * 0.51f) : 0.001f;

            // Use exact +/-wrapWidth shifts (already pixel-snapped) to avoid tiny "few pixels" warps caused by
            // modulo math + snapping producing a non-exact delta.
            if (ppos.x < left - eps) {
                dxShift = width;
            } else if (ppos.x > right + eps) {
                dxShift = -width;
            }

            if (Mathf.Abs(dxShift) <= 0.0001f) {
                return;
            }

            ApplyWorldShift(dxShift);
        }

        private Rigidbody2D GetPlayerBody() {
            for (int i = 0; i < _bodies.Count; i++) {
                Rigidbody2D rb = _bodies[i];
                if (rb == null) continue;
                if (rb.GetComponent<NSMB.Player.PlayerMotor2D>() != null) {
                    return rb;
                }
            }

            // Fallback: player can be spawned after StageWrap2D.Start; don't wait for the next refresh tick.
            NSMB.Player.PlayerMotor2D player = Object.FindObjectOfType(typeof(NSMB.Player.PlayerMotor2D)) as NSMB.Player.PlayerMotor2D;
            if (player != null) {
                Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
                if (prb != null) {
                    _bodies.Add(prb);
                    return prb;
                }
            }

            return null;
        }

        private void ApplyWorldShift(float dx) {
            // Ensure we shift by an exact pixel multiple to avoid a one-frame snap discrepancy between camera
            // and sprites when wrapping.
            float pixel = GetPixelSnapUnit();
            if (pixel > 0.0000001f) {
                dx = Mathf.Round(dx / pixel) * pixel;
            }

            for (int i = 0; i < _bodies.Count; i++) {
                Rigidbody2D rb = _bodies[i];
                if (rb == null) continue;
                // Rigidbody interpolation can visually "lerp" across teleports for 1-2 frames.
                // In wrap levels, prefer deterministic/pixel-stable visuals over interpolation smoothness.
                if (rb.interpolation != RigidbodyInterpolation2D.None) {
                    rb.interpolation = RigidbodyInterpolation2D.None;
                }

                Vector2 p = rb.position;
                Vector2 np = new Vector2(p.x + dx, p.y);
                rb.position = np;

                Transform tr = rb.transform;
                if (tr != null) {
                    Vector3 p3 = tr.position;
                    tr.position = new Vector3(np.x, np.y, p3.z);
                }

            }

            for (int i = 0; i < _transforms.Count; i++) {
                Transform tr = _transforms[i];
                if (tr == null) continue;
                Vector3 p3 = tr.position;
                tr.position = new Vector3(p3.x + dx, p3.y, p3.z);
            }

            if (_cameraFollow != null) {
                _cameraFollow.ApplyWrapDelta(dx, 0f);
            } else {
                UnityEngine.Camera cam = UnityEngine.Camera.main;
                if (cam != null) {
                    Transform tr = cam.transform;
                    Vector3 p = tr.position;
                    tr.position = new Vector3(p.x + dx, p.y, p.z);
                }
            }
        }

        private void EnsureVisualCopies() {
            if (_builtVisualCopies) {
                return;
            }

            if (_wrapWidth <= 0.0001f) {
                return;
            }

            // Build visual-only copies of the stage tile layers so the camera can see both ends at once.
            // Gameplay remains single-instance; we "recenter" the world by shifting dynamic objects and camera.
            Transform stageRoot = transform;

            List<Transform> layers = new List<Transform>();
            for (int i = 0; i < stageRoot.childCount; i++) {
                Transform c = stageRoot.GetChild(i);
                if (c == null) continue;
                if (string.Equals(c.name, "Entities", System.StringComparison.InvariantCultureIgnoreCase)) continue;
                if (c.name.IndexOf("_WrapLeft", System.StringComparison.InvariantCultureIgnoreCase) >= 0) continue;
                if (c.name.IndexOf("_WrapRight", System.StringComparison.InvariantCultureIgnoreCase) >= 0) continue;
                layers.Add(c);
            }

            if (layers.Count == 0) {
                return;
            }

            for (int i = 0; i < layers.Count; i++) {
                Transform src = layers[i];
                if (src == null) continue;

                CreateVisualCopy(src, -_effectiveWrapWidth, "_WrapLeft");
                CreateVisualCopy(src, +_effectiveWrapWidth, "_WrapRight");
            }

            _builtVisualCopies = true;
        }

        private void CreateVisualCopy(Transform src, float dx, string suffix) {
            if (src == null) {
                return;
            }

            GameObject copy = Object.Instantiate(src.gameObject);
            copy.name = src.name + suffix;
            // Offset in world-space so scaled layers (e.g. cloud strips) wrap correctly.
            copy.transform.SetParent(transform, true);
            Vector3 srcPos = src.position;
            copy.transform.position = new Vector3(srcPos.x + dx, srcPos.y, srcPos.z);
            copy.transform.rotation = src.rotation;
            copy.transform.localScale = src.localScale;

            // Add mirrors for any tiles that have behaviour-driven visuals (question blocks, bump anim, etc.).
            // We only mirror objects that had MonoBehaviours (pre-strip) to avoid per-tile overhead.
            AddMirrorsForDynamicVisuals(src, copy.transform);
            StripToVisualOnly(copy.transform);
        }

        private static void AddMirrorsForDynamicVisuals(Transform sourceLayer, Transform copyLayer) {
            if (sourceLayer == null || copyLayer == null) {
                return;
            }

            // Collect paths to nodes that have SpriteRenderer + any MonoBehaviour (i.e. visuals can change).
            List<string> mirrorPaths = new List<string>();
            Transform[] all = copyLayer.GetComponentsInChildren<Transform>(true);
            if (all == null) {
                return;
            }

            for (int i = 0; i < all.Length; i++) {
                Transform tr = all[i];
                if (tr == null) continue;

                SpriteRenderer sr = tr.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                MonoBehaviour[] behaviours = tr.GetComponents<MonoBehaviour>();
                if (behaviours == null || behaviours.Length == 0) {
                    continue;
                }

                // Ignore our own helper component if present.
                bool hasNonMirror = false;
                for (int b = 0; b < behaviours.Length; b++) {
                    MonoBehaviour mb = behaviours[b];
                    if (mb == null) continue;
                    if (mb is StageWrapSpriteMirror) continue;
                    hasNonMirror = true;
                    break;
                }

                if (!hasNonMirror) {
                    continue;
                }

                string path = GetRelativePath(copyLayer, tr);
                if (!string.IsNullOrEmpty(path)) {
                    mirrorPaths.Add(path);
                }
            }

            if (mirrorPaths.Count == 0) {
                return;
            }

            // Store mirror intents on the copy layer via a lightweight component list we apply after stripping.
            // We'll attach mirrors by path once the copy has been stripped to visuals only.
            StageWrapMirrorBootstrap boot = copyLayer.gameObject.AddComponent<StageWrapMirrorBootstrap>();
            boot.sourceLayer = sourceLayer;
            boot.paths = mirrorPaths.ToArray();
        }

        private static string GetRelativePath(Transform root, Transform node) {
            if (root == null || node == null) {
                return null;
            }
            if (node == root) {
                return string.Empty;
            }

            List<string> parts = new List<string>();
            Transform cur = node;
            while (cur != null && cur != root) {
                parts.Add(cur.name);
                cur = cur.parent;
            }
            if (cur != root) {
                return null;
            }
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        private static void StripToVisualOnly(Transform root) {
            if (root == null) {
                return;
            }

            // Remove all gameplay components so the wrap copies are purely visual.
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            if (all == null) {
                return;
            }

            for (int i = 0; i < all.Length; i++) {
                Transform tr = all[i];
                if (tr == null) continue;

                MonoBehaviour[] behaviours = tr.GetComponents<MonoBehaviour>();
                if (behaviours != null) {
                    for (int b = 0; b < behaviours.Length; b++) {
                        MonoBehaviour mb = behaviours[b];
                        if (mb != null) {
                            // Keep bootstrap so we can add mirrors after stripping.
                            if (mb is StageWrapMirrorBootstrap) {
                                continue;
                            }
                            // Keep lightweight background scrollers (cloud layers, etc.) on wrap copies.
                            if (mb is ScrollingSpriteLoop2D) {
                                continue;
                            }
                            Object.Destroy(mb);
                        }
                    }
                }

                Collider2D[] cols = tr.GetComponents<Collider2D>();
                if (cols != null) {
                    for (int c = 0; c < cols.Length; c++) {
                        Collider2D col = cols[c];
                        if (col != null) {
                            Object.Destroy(col);
                        }
                    }
                }

                Rigidbody2D rb = tr.GetComponent<Rigidbody2D>();
                if (rb != null) {
                    Object.Destroy(rb);
                }
            }

            // After stripping, materialize mirrors.
            StageWrapMirrorBootstrap[] boots = root.GetComponentsInChildren<StageWrapMirrorBootstrap>(true);
            if (boots != null) {
                for (int i = 0; i < boots.Length; i++) {
                    StageWrapMirrorBootstrap boot = boots[i];
                    if (boot == null) continue;
                    boot.BuildMirrors();
                }
            }
        }

        private sealed class StageWrapMirrorBootstrap : MonoBehaviour {
            public Transform sourceLayer;
            public string[] paths;

            public void BuildMirrors() {
                if (sourceLayer == null || paths == null || paths.Length == 0) {
                    Object.Destroy(this);
                    return;
                }

                for (int i = 0; i < paths.Length; i++) {
                    string path = paths[i];
                    if (string.IsNullOrEmpty(path)) {
                        continue;
                    }

                    Transform copyNode = transform.Find(path);
                    Transform sourceNode = sourceLayer.Find(path);
                    if (copyNode == null || sourceNode == null) {
                        continue;
                    }

                    StageWrapSpriteMirror mirror = copyNode.gameObject.AddComponent<StageWrapSpriteMirror>();
                    mirror.source = sourceNode;
                    mirror.mirrorLocalTransform = true;
                }

                Object.Destroy(this);
            }
        }
    }
}
