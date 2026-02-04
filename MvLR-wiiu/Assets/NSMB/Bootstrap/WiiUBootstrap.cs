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

            // Placeholder sprite (hidden once the 3D model loads).
            SpriteRenderer renderer = player.GetComponent<SpriteRenderer>();
            if (renderer == null) {
                renderer = player.AddComponent<SpriteRenderer>();
            }
            renderer.sprite = CreatePlaceholderSprite();
            renderer.color = Color.white;
            renderer.sortingOrder = 10;

            // Rigidbody2D (also required by PlayerVisualFromOriginal).
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb == null) {
                rb = player.AddComponent<Rigidbody2D>();
            }
            rb.gravityScale = 3f;
            rb.freezeRotation = true;

            BoxCollider2D col = player.GetComponent<BoxCollider2D>();
            if (col == null) {
                col = player.AddComponent<BoxCollider2D>();
            }
            // Match Unity 6 (Quantum) Mario hitbox at Small state: width=0.375*2*scale, height=0.42*scale (scale=2).
            col.size = new Vector2(0.75f, 0.84f);
            col.offset = new Vector2(0f, 0.42f);

            // Movement/visuals.
            if (player.GetComponent<NSMB.Player.PlayerMotor2D>() == null) {
                player.AddComponent<NSMB.Player.PlayerMotor2D>();
            }
            if (player.GetComponent<NSMB.Player.PlayerVisualFromOriginal>() == null) {
                player.AddComponent<NSMB.Player.PlayerVisualFromOriginal>();
            }
            if (player.GetComponent<NSMB.Player.PlayerPowerupState>() == null) {
                player.AddComponent<NSMB.Player.PlayerPowerupState>();
            }
            if (player.GetComponent<NSMB.Player.PlayerSfx>() == null) {
                player.AddComponent<NSMB.Player.PlayerSfx>();
            }
            if (player.GetComponent<NSMB.Player.PlayerHealth>() == null) {
                player.AddComponent<NSMB.Player.PlayerHealth>();
            }
            BindCameraTarget(player.transform);
        }

        public static void EnsurePlayerForFlowAt(Vector3 worldPosition) {
            EnsurePlayerForFlow();
            NSMB.Player.PlayerMotor2D player = Object.FindObjectOfType(typeof(NSMB.Player.PlayerMotor2D)) as NSMB.Player.PlayerMotor2D;
            if (player != null) {
                player.transform.position = worldPosition;
            }
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
            // Texture2D.whiteTexture is 1x1, which makes the sprite effectively invisible in world space.
            // Use a tiny pixel-art placeholder that renders at 1 tile (16px) = 1 world unit.
            const int size = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            // Simple "Mario-ish" block: red cap + blue body.
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    Color c = new Color(0f, 0f, 0f, 0f);

                    // outline
                    if (x == 0 || y == 0 || x == size - 1 || y == size - 1) {
                        c = Color.black;
                    } else if (y >= 11) {
                        c = new Color(0.80f, 0.10f, 0.10f, 1f); // red
                    } else if (y >= 5) {
                        c = new Color(0.10f, 0.30f, 0.80f, 1f); // blue
                    } else {
                        c = new Color(0.95f, 0.80f, 0.65f, 1f); // skin-ish
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply(false, true);

            Rect rect = new Rect(0f, 0f, size, size);
            return Sprite.Create(tex, rect, new Vector2(0.5f, 0.0f), 16f);
        }
    }
}
