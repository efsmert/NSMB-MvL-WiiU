using UnityEngine;

namespace NSMB.Input {
    public static class LegacyInput {
        public static Vector2 GetMovement() {
            float x = UnityEngine.Input.GetAxisRaw("Horizontal");
            float y = UnityEngine.Input.GetAxisRaw("Vertical");
            return new Vector2(x, y);
        }

        public static bool GetJumpDown() {
            return UnityEngine.Input.GetKeyDown(KeyCode.Space);
        }

        public static bool GetJump() {
            return UnityEngine.Input.GetKey(KeyCode.Space);
        }

        public static bool GetSprint() {
            return UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
        }
    }
}

