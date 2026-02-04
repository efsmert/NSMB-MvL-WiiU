using System.Collections.Generic;
using UnityEngine;

namespace NSMB.World {
    // Mirrors an instantiated Transform hierarchy from a source hierarchy (by relative path),
    // with an optional offset applied to the root local position.
    public sealed class TransformHierarchyMirror : MonoBehaviour {
        public Transform sourceRoot;
        public Vector3 rootLocalPositionOffset;

        private Transform _targetRoot;
        private readonly List<Transform> _targets = new List<Transform>(128);
        private readonly List<Transform> _sources = new List<Transform>(128);

        private void Awake() {
            _targetRoot = transform;
            RebuildMap();
        }

        private void LateUpdate() {
            if (sourceRoot == null || _targetRoot == null) {
                return;
            }

            // Root with offset.
            _targetRoot.localPosition = sourceRoot.localPosition + rootLocalPositionOffset;
            _targetRoot.localRotation = sourceRoot.localRotation;
            _targetRoot.localScale = sourceRoot.localScale;

            for (int i = 0; i < _targets.Count; i++) {
                Transform t = _targets[i];
                Transform s = _sources[i];
                if (t == null || s == null) continue;

                t.localPosition = s.localPosition;
                t.localRotation = s.localRotation;
                t.localScale = s.localScale;
            }
        }

        public void RebuildMap() {
            _targets.Clear();
            _sources.Clear();

            if (sourceRoot == null || _targetRoot == null) {
                return;
            }

            Transform[] allTargets = _targetRoot.GetComponentsInChildren<Transform>(true);
            if (allTargets == null) {
                return;
            }

            for (int i = 0; i < allTargets.Length; i++) {
                Transform t = allTargets[i];
                if (t == null) continue;
                if (t == _targetRoot) continue;

                string rel = GetRelativePath(_targetRoot, t);
                if (rel == null) continue;
                Transform s = sourceRoot.Find(rel);
                if (s == null) continue;

                _targets.Add(t);
                _sources.Add(s);
            }
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
    }
}

