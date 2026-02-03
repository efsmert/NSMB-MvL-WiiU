using System.Collections.Generic;
using UnityEngine;

namespace NSMB.Player {
    public enum PlayerCharacter {
        Mario = 0,
        Luigi = 1,
    }

    [RequireComponent(typeof(PlayerMotor2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerVisualFromOriginal : MonoBehaviour {
        public PlayerCharacter character = PlayerCharacter.Mario;
        public bool large = false;
        public int sortingOrder = 20;

        private PlayerMotor2D _motor;
        private Rigidbody2D _rb;
        private Collider2D _col;
        private SpriteRenderer _fallbackSprite;

        private GameObject _modelRoot;
        private Animator _animator;
        private bool _facingRight = true;

        private void Awake() {
            _motor = GetComponent<PlayerMotor2D>();
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();
            _fallbackSprite = GetComponent<SpriteRenderer>();

            TryBuildModel();
        }

        private void LateUpdate() {
            if (_modelRoot == null || _rb == null) {
                return;
            }

            float vx = _rb.velocity.x;
            float vy = _rb.velocity.y;

            if (Mathf.Abs(vx) > 0.05f) {
                _facingRight = vx > 0f;
            }

            // Match original approach: rotate model around Y for facing.
            _modelRoot.transform.localRotation = Quaternion.Euler(0f, _facingRight ? 0f : 180f, 0f);

            if (_animator != null) {
                // These parameter names match the original controller (see MarioPlayerAnimator hashes).
                _animator.SetFloat("velocityX", vx);
                _animator.SetFloat("velocityY", vy);
                _animator.SetFloat("velocityMagnitude", new Vector2(vx, vy).magnitude);
                _animator.SetBool("onGround", _motor != null && _motor.IsGrounded);
                _animator.SetBool("facingRight", _facingRight);
            }
        }

        private void TryBuildModel() {
            string modelPath = GetModelResourcePath(character, large);
            GameObject modelPrefab = Resources.Load(modelPath, typeof(GameObject)) as GameObject;
            if (modelPrefab == null) {
                #if UNITY_EDITOR
                Debug.LogWarning("[NSMB] Player model not found in Resources: " + modelPath + " (run NSMB/Setup Player Assets (Models + Anim From Original))");
                #endif
                return;
            }

            _modelRoot = Instantiate(modelPrefab) as GameObject;
            _modelRoot.name = "Model";
            _modelRoot.transform.parent = transform;
            _modelRoot.transform.localPosition = Vector3.zero;
            _modelRoot.transform.localRotation = Quaternion.identity;
            _modelRoot.transform.localScale = Vector3.one;

            ApplyRendererSorting(_modelRoot, sortingOrder);
            FixMaterials(_modelRoot, GetModelFolderResourcePath(character, large));
            SetupAnimator(_modelRoot);
            ScaleAndAlignToCollider(_modelRoot, _col, large);

            // Hide 2D placeholder sprite if present.
            if (_fallbackSprite != null) {
                _fallbackSprite.sprite = null;
                _fallbackSprite.enabled = false;
                Destroy(_fallbackSprite);
                _fallbackSprite = null;
            }

            // Put the model slightly toward camera to avoid z-fighting with sprites at z=0.
            Vector3 p = _modelRoot.transform.position;
            _modelRoot.transform.position = new Vector3(p.x, p.y, transform.position.z - 0.2f);
        }

        private void SetupAnimator(GameObject root) {
            if (root == null) {
                return;
            }

            _animator = root.GetComponentInChildren<Animator>();
            if (_animator == null) {
                _animator = root.AddComponent<Animator>();
            }

            _animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Unity 6 animator controllers are not reliably backward-compatible with Unity 2017.
            // Prefer a generated 2017-native controller, falling back to any copied controllers if present.
            RuntimeAnimatorController controller = LoadGeneratedController(character, large);
            if (controller == null) {
                controller = LoadCopiedController(character, large);
            }

            _animator.runtimeAnimatorController = controller;

            if (controller == null) {
                #if UNITY_EDITOR
                Debug.LogWarning("[NSMB] No player AnimatorController found. Run NSMB/Player Assets/Build Animator Controllers (From FBX Clips).");
                #endif
            }
        }

        private static RuntimeAnimatorController LoadGeneratedController(PlayerCharacter character, bool large) {
            // Generated via: NSMB/Player Assets/Build Animator Controllers (From FBX Clips)
            if (character != PlayerCharacter.Mario) {
                return null;
            }

            string path = large ? "NSMB/Player/Anim/Generated/LargeMarioGenerated" : "NSMB/Player/Anim/Generated/SmallMarioGenerated";
            return Resources.Load(path, typeof(RuntimeAnimatorController)) as RuntimeAnimatorController;
        }

        private static RuntimeAnimatorController LoadCopiedController(PlayerCharacter character, bool large) {
            // Copied from original (may or may not work in 2017).
            RuntimeAnimatorController baseController = Resources.Load("NSMB/Player/Anim/LargeMario", typeof(RuntimeAnimatorController)) as RuntimeAnimatorController;
            if (baseController != null && character == PlayerCharacter.Mario && large) {
                return baseController;
            }

            string overridePath = GetOverrideControllerResourcePath(character, large);
            if (!string.IsNullOrEmpty(overridePath)) {
                RuntimeAnimatorController oc = Resources.Load(overridePath, typeof(RuntimeAnimatorController)) as RuntimeAnimatorController;
                if (oc != null) {
                    return oc;
                }
            }

            return baseController;
        }

        private static string GetModelFolderResourcePath(PlayerCharacter character, bool large) {
            if (character == PlayerCharacter.Luigi) {
                return large ? "NSMB/Player/Models/Players/luigi_big" : "NSMB/Player/Models/Players/luigi_small";
            }
            return large ? "NSMB/Player/Models/Players/mario_big" : "NSMB/Player/Models/Players/mario_small";
        }

        private static string GetModelResourcePath(PlayerCharacter character, bool large) {
            if (character == PlayerCharacter.Luigi) {
                if (large) {
                    return "NSMB/Player/Models/Players/luigi_big/luigi_big";
                }
                return "NSMB/Player/Models/Players/luigi_small/luigi_small";
            }

            // Mario
            if (large) {
                return "NSMB/Player/Models/Players/mario_big/mario_big_exported";
            }
            return "NSMB/Player/Models/Players/mario_small/mario_small_exported";
        }

        private static string GetOverrideControllerResourcePath(PlayerCharacter character, bool large) {
            if (character == PlayerCharacter.Luigi) {
                return large ? "NSMB/Player/Anim/LargeLuigi" : "NSMB/Player/Anim/SmallLuigi";
            }
            // Mario
            return large ? null : "NSMB/Player/Anim/SmallMario";
        }

        private static void ApplyRendererSorting(GameObject root, int order) {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++) {
                Renderer r = renderers[i];
                if (r == null) continue;

                // Sorting for MeshRenderers exists in 2017 and helps interop with sprites.
                r.sortingLayerName = "Default";
                r.sortingOrder = order;
            }
        }

        private static void FixMaterials(GameObject root, string modelFolderResourcePath) {
            if (root == null) {
                return;
            }

            Shader unlitTexture = Shader.Find("Unlit/Texture");
            Shader unlitTransparent = Shader.Find("Unlit/Transparent");
            if (unlitTexture == null && unlitTransparent == null) {
                return;
            }

            Texture2D[] textures = LoadTextures(modelFolderResourcePath);
            Texture2D primary = PickPrimaryTexture(textures, modelFolderResourcePath);

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++) {
                Renderer r = renderers[i];
                if (r == null) continue;

                Material[] mats = r.sharedMaterials;
                if (mats == null || mats.Length == 0) {
                    // Some imports can produce a renderer with no materials assigned.
                    if (primary != null) {
                        Material fallback = new Material(unlitTexture != null ? unlitTexture : unlitTransparent);
                        fallback.mainTexture = primary;
                        fallback.color = Color.white;
                        r.material = fallback;
                    }
                    continue;
                }

                Material[] newMats = new Material[mats.Length];
                bool changed = false;

                for (int m = 0; m < mats.Length; m++) {
                    Material mat = mats[m];
                    if (mat == null) {
                        continue;
                    }

                    Texture mainTex = mat.mainTexture;
                    if (mainTex == null) {
                        mainTex = mat.GetTexture("_BaseMap");
                    }

                    // If shader is missing/unsupported (URP/ShaderGraph), replace it with a built-in unlit shader.
                    bool shaderBad = (mat.shader == null) || (mat.shader.name != null && mat.shader.name.IndexOf("Universal Render Pipeline", System.StringComparison.InvariantCultureIgnoreCase) >= 0);

                    if (mainTex == null && textures != null && textures.Length > 0) {
                        // Try to guess a texture based on the material name; fall back to first texture.
                        Texture2D guess = GuessTexture(textures, mat.name);
                        if (guess != null) {
                            mainTex = guess;
                        } else if (primary != null) {
                            mainTex = primary;
                        }
                    }

                    // Always force a built-in unlit shader for player models to match the game's 2D look.
                    Shader chosenShader = (unlitTexture != null) ? unlitTexture : unlitTransparent;
                    if (unlitTransparent != null && mainTex != null) {
                        // Heuristic: if texture name hints alpha usage, use transparent.
                        string tn = mainTex.name != null ? mainTex.name.ToLowerInvariant() : "";
                        if (tn.Contains("eye") || tn.Contains("mask")) {
                            chosenShader = unlitTransparent;
                        }
                    }

                    if (shaderBad || mat.shader == null || mat.shader.name == null ||
                        mat.shader.name.IndexOf("Unlit/", System.StringComparison.InvariantCultureIgnoreCase) < 0 ||
                        (mainTex != null && mat.mainTexture != mainTex)) {
                        Material repl = new Material(chosenShader);
                        if (mainTex != null) {
                            repl.mainTexture = mainTex;
                            // Preserve per-material tiling/offset (many FBX imports rely on it).
                            repl.mainTextureScale = mat.mainTextureScale;
                            repl.mainTextureOffset = mat.mainTextureOffset;

                            // Player textures are flipbook atlases (e.g. mario_big.png is 128x1280 = 20 rows of 128x64).
                            // The original ShaderGraph selects the correct slice; in Unity 2017 we mimic this by selecting
                            // the first row/frame via texture scale+offset.
                            ApplyFlipbookIfNeeded(repl, mainTex);
                        }
                        repl.color = Color.white;
                        newMats[m] = repl;
                        changed = true;
                    }
                }

                if (changed) {
                    for (int m = 0; m < newMats.Length; m++) {
                        if (newMats[m] == null) {
                            newMats[m] = mats[m];
                        }
                    }
                    r.materials = newMats;
                }
            }
        }

        private static Texture2D[] LoadTextures(string modelFolderResourcePath) {
            if (string.IsNullOrEmpty(modelFolderResourcePath)) {
                return null;
            }

            UnityEngine.Object[] objs = Resources.LoadAll(modelFolderResourcePath, typeof(Texture2D));
            if (objs == null || objs.Length == 0) {
                return null;
            }

            List<Texture2D> textures = new List<Texture2D>();
            for (int i = 0; i < objs.Length; i++) {
                Texture2D t = objs[i] as Texture2D;
                if (t != null) {
                    textures.Add(t);
                }
            }

            if (textures.Count == 0) {
                return null;
            }

            textures.Sort(delegate(Texture2D a, Texture2D b) {
                string an = (a != null && a.name != null) ? a.name : "";
                string bn = (b != null && b.name != null) ? b.name : "";
                return string.Compare(an, bn, System.StringComparison.InvariantCultureIgnoreCase);
            });

            return textures.ToArray();
        }

        private static Texture2D PickPrimaryTexture(Texture2D[] textures, string modelFolderResourcePath) {
            if (textures == null || textures.Length == 0) {
                return null;
            }

            string key = (modelFolderResourcePath != null) ? modelFolderResourcePath.ToLowerInvariant() : "";
            string prefer = "";
            if (key.Contains("mario_big")) prefer = "mario_big";
            else if (key.Contains("mario_small")) prefer = "mario";
            else if (key.Contains("luigi_big")) prefer = "luigi";
            else if (key.Contains("luigi_small")) prefer = "luigi";

            if (!string.IsNullOrEmpty(prefer)) {
                for (int i = 0; i < textures.Length; i++) {
                    Texture2D t = textures[i];
                    if (t == null || string.IsNullOrEmpty(t.name)) continue;
                    string tn = t.name.ToLowerInvariant();
                    if (tn.Contains(prefer) && !tn.Contains("starman") && !tn.Contains("propeller")) {
                        return t;
                    }
                }
            }

            return textures[0];
        }

        private static Texture2D GuessTexture(Texture2D[] textures, string materialName) {
            if (textures == null || textures.Length == 0) {
                return null;
            }
            if (string.IsNullOrEmpty(materialName)) {
                return null;
            }

            string key = Normalize(materialName);
            if (string.IsNullOrEmpty(key)) {
                return null;
            }

            // Pass 1: contains match.
            for (int i = 0; i < textures.Length; i++) {
                Texture2D t = textures[i];
                if (t == null || string.IsNullOrEmpty(t.name)) continue;
                string tn = Normalize(t.name);
                if (!string.IsNullOrEmpty(tn) && key.IndexOf(tn, System.StringComparison.InvariantCultureIgnoreCase) >= 0) {
                    return t;
                }
                if (!string.IsNullOrEmpty(tn) && tn.IndexOf(key, System.StringComparison.InvariantCultureIgnoreCase) >= 0) {
                    return t;
                }
            }

            // Pass 2: fallback to first.
            return textures[0];
        }

        private static void ApplyFlipbookIfNeeded(Material mat, Texture tex) {
            if (mat == null || tex == null) {
                return;
            }

            Texture2D t = tex as Texture2D;
            if (t == null) {
                return;
            }

            int w = t.width;
            int h = t.height;
            if (w <= 0 || h <= 0) {
                return;
            }

            // Vertical flipbook: mario_big.png is 128x1280 => sliceHeight = w/2 = 64, rows = 20.
            if (h > w && (w % 2) == 0) {
                int sliceHeight = w / 2;
                if (sliceHeight > 0 && (h % sliceHeight) == 0) {
                    int rows = h / sliceHeight;
                    if (rows > 1) {
                        float invRows = 1f / rows;
                        // Choose row 0 from the TOP (Unity UV origin is bottom).
                        mat.mainTextureScale = new Vector2(1f, invRows);
                        mat.mainTextureOffset = new Vector2(0f, 1f - invRows);
                    }
                    return;
                }
            }

            // Horizontal flipbook: mario_eyes.png is 64x32 => sliceWidth = h/2 = 16, cols = 4.
            if (w > h && (h % 2) == 0) {
                int sliceWidth = h / 2;
                if (sliceWidth > 0 && (w % sliceWidth) == 0) {
                    int cols = w / sliceWidth;
                    if (cols > 1) {
                        float invCols = 1f / cols;
                        mat.mainTextureScale = new Vector2(invCols, 1f);
                        mat.mainTextureOffset = Vector2.zero;
                    }
                }
            }
        }

        private static string Normalize(string s) {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.ToLowerInvariant();
            s = s.Replace(" (instance)", "");
            s = s.Replace(" ", "");
            s = s.Replace("_", "");
            s = s.Replace("-", "");
            return s;
        }

        private static void ScaleAndAlignToCollider(GameObject modelRoot, Collider2D col, bool large) {
            if (modelRoot == null) {
                return;
            }

            Bounds b;
            if (!TryGetWorldBounds(modelRoot, out b)) {
                return;
            }

            // Target heights tuned to the 2D collider sizes (1 unit ~= 16px tile).
            float targetHeight = large ? 1.8f : 1.1f;
            if (b.size.y > 0.001f) {
                float scale = targetHeight / b.size.y;
                if (scale > 0.001f && scale < 1000f) {
                    Vector3 ls = modelRoot.transform.localScale;
                    modelRoot.transform.localScale = new Vector3(ls.x * scale, ls.y * scale, ls.z * scale);
                }
            }

            // Recompute after scaling and align centers to the collider center.
            if (!TryGetWorldBounds(modelRoot, out b)) {
                return;
            }

            Vector3 desiredCenter = (col != null) ? col.bounds.center : modelRoot.transform.parent.position;
            Vector3 delta = desiredCenter - b.center;
            modelRoot.transform.position = modelRoot.transform.position + delta;
        }

        private static bool TryGetWorldBounds(GameObject root, out Bounds bounds) {
            bounds = new Bounds(root.transform.position, Vector3.zero);
            Renderer[] rs = root.GetComponentsInChildren<Renderer>(true);
            bool has = false;
            for (int i = 0; i < rs.Length; i++) {
                Renderer r = rs[i];
                if (r == null) continue;
                if (!has) {
                    bounds = r.bounds;
                    has = true;
                } else {
                    bounds.Encapsulate(r.bounds);
                }
            }
            return has;
        }
    }
}
