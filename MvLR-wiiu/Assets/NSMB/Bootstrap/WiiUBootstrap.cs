using UnityEngine;

namespace NSMB.WiiU {
    public sealed class WiiUBootstrap : MonoBehaviour {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad() {
            EnsureCamera();
            EnsureGameRoot();
        }

        private static void EnsureGameRoot() {
            NSMB.Core.GameRoot root = Object.FindObjectOfType(typeof(NSMB.Core.GameRoot)) as NSMB.Core.GameRoot;
            if (root != null) {
                return;
            }

            GameObject go = new GameObject("NSMB");
            go.AddComponent<NSMB.Core.GameRoot>();
        }

        private static void EnsureCamera() {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null) {
                cam = Object.FindObjectOfType(typeof(UnityEngine.Camera)) as UnityEngine.Camera;
            }

            if (cam == null) {
                GameObject go = new GameObject("Main Camera");
                cam = go.AddComponent<UnityEngine.Camera>();
                go.tag = "MainCamera";
                go.transform.position = new Vector3(0f, 0f, -10f);
                go.AddComponent<AudioListener>();
            }

            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.10f, 0.14f, 1f);

            NSMB.Camera.CameraFollow2D follow = cam.GetComponent<NSMB.Camera.CameraFollow2D>();
            if (follow == null) {
                follow = cam.gameObject.AddComponent<NSMB.Camera.CameraFollow2D>();
            }

            if (cam.GetComponent<NSMB.Camera.PixelPerfectCameraManual>() == null) {
                cam.gameObject.AddComponent<NSMB.Camera.PixelPerfectCameraManual>();
            }
        }

        public static void EnsurePlayerForFlow() {
            NSMB.Player.PlayerMotor2D existing = Object.FindObjectOfType(typeof(NSMB.Player.PlayerMotor2D)) as NSMB.Player.PlayerMotor2D;
            if (existing != null) {
                BindCameraTarget(existing.transform);
                return;
            }

            GameObject player = new GameObject("Player");
            player.transform.position = Vector3.zero;

            var renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = CreatePlaceholderSprite();
            renderer.sortingOrder = 0;

            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;

            BoxCollider2D col = player.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.9f);

            player.AddComponent<NSMB.Player.PlayerMotor2D>();
            player.AddComponent<NSMB.Player.PlayerSfx>();
            player.AddComponent<NSMB.Player.PlayerHealth>();
            BindCameraTarget(player.transform);
        }

        private static void BindCameraTarget(Transform target) {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null) {
                return;
            }

            NSMB.Camera.CameraFollow2D follow = cam.GetComponent<NSMB.Camera.CameraFollow2D>();
            if (follow != null) {
                follow.target = target;
            }
        }

        private static Sprite CreatePlaceholderSprite() {
            Texture2D tex = Texture2D.whiteTexture;
            Rect rect = new Rect(0f, 0f, tex.width, tex.height);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            return Sprite.Create(tex, rect, pivot, 100f);
        }
    }
}
