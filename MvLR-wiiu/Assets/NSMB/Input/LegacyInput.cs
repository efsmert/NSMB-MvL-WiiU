using UnityEngine;

namespace NSMB.Input {
    public static class LegacyInput {
        public static Vector2 GetMovement() {
            float x = UnityEngine.Input.GetAxisRaw("Horizontal");
            float y = UnityEngine.Input.GetAxisRaw("Vertical");
            return new Vector2(x, y);
        }

        public static bool GetJumpDown() {
            // Keyboard + common gamepad "A" (Unity maps this to JoystickButton0 for most controllers).
            return UnityEngine.Input.GetKeyDown(KeyCode.Space) || UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton0);
        }

        public static bool GetJump() {
            return UnityEngine.Input.GetKey(KeyCode.Space) || UnityEngine.Input.GetKey(KeyCode.JoystickButton0);
        }

        public static bool GetSprint() {
            // Keyboard + common gamepad "B" (JoystickButton1 on most controllers).
            return UnityEngine.Input.GetKey(KeyCode.LeftShift) ||
                   UnityEngine.Input.GetKey(KeyCode.RightShift) ||
                   UnityEngine.Input.GetKey(KeyCode.LeftControl) ||
                   UnityEngine.Input.GetKey(KeyCode.RightControl) ||
                   UnityEngine.Input.GetKey(KeyCode.JoystickButton1);
        }

        public static bool GetPauseDown() {
            return UnityEngine.Input.GetKeyDown(KeyCode.Escape);
        }

        public static bool GetSubmitDown() {
            return UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.Space);
        }

        public static bool GetBackDown() {
            return UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetKeyDown(KeyCode.Backspace);
        }
    }
}
