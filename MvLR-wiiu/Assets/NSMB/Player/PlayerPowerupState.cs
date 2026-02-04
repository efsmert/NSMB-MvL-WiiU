using UnityEngine;

namespace NSMB.Player {
    public enum PlayerPowerupStateId : byte {
        Small = 0,
        Mushroom = 1,
        FireFlower = 2,
    }

    public sealed class PlayerPowerupState : MonoBehaviour {
        // From Unity 6 (Quantum) MarioPlayerPhysicsInfo + MarioPlayerSystem.HandleHitbox:
        // - Half width extents are constant: 0.1875
        // - Small hitbox height: 0.42
        // - Large hitbox height: 0.82
        private const float OriginalHalfWidth = 0.1875f;
        private const float OriginalSmallHeight = 0.42f;
        private const float OriginalLargeHeight = 0.82f;

        [SerializeField] private PlayerPowerupStateId _state = PlayerPowerupStateId.Small;

        public PlayerPowerupStateId State {
            get { return _state; }
        }

        public bool IsLarge {
            get { return _state >= PlayerPowerupStateId.Mushroom; }
        }

        public void CollectMushroom() {
            if (_state < PlayerPowerupStateId.Mushroom) {
                SetState(PlayerPowerupStateId.Mushroom);
            }
        }

        public void CollectFireFlower() {
            if (_state < PlayerPowerupStateId.FireFlower) {
                SetState(PlayerPowerupStateId.FireFlower);
            }
        }

        private void Awake() {
            SyncVisual();
            ApplyHitbox(true);
        }

        private void SetState(PlayerPowerupStateId s) {
            _state = s;

            ApplyHitbox(true);
            PlayerVisualFromOriginal visual = GetComponent<PlayerVisualFromOriginal>();
            if (visual != null) {
                // Visual controller chooses models/controllers based on this flag.
                visual.large = IsLarge;
            }
        }

        private void SyncVisual() {
            PlayerVisualFromOriginal visual = GetComponent<PlayerVisualFromOriginal>();
            if (visual != null) {
                visual.large = IsLarge;
            }
        }

        private void ApplyHitbox(bool preserveFeet) {
            BoxCollider2D box = GetComponent<BoxCollider2D>();
            if (box == null) {
                return;
            }

            float scale = 2f;
            PlayerMotor2D motor = GetComponent<PlayerMotor2D>();
            if (motor != null && motor.originalToUnityScale > 0.01f) {
                scale = motor.originalToUnityScale;
            }

            float height = (IsLarge ? OriginalLargeHeight : OriginalSmallHeight) * scale;
            float width = (OriginalHalfWidth * 2f) * scale;

            float beforeMinY = preserveFeet ? box.bounds.min.y : 0f;

            box.size = new Vector2(width, height);
            // Bottom-anchored like the original: collider origin at feet.
            box.offset = new Vector2(0f, height * 0.5f);

            if (preserveFeet) {
                float afterMinY = box.bounds.min.y;
                float dy = beforeMinY - afterMinY;
                if (Mathf.Abs(dy) > 0.0001f) {
                    Vector3 p = transform.position;
                    transform.position = new Vector3(p.x, p.y + dy, p.z);
                }
            }
        }
    }
}
