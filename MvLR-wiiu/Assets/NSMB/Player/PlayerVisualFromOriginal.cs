using System.Collections;
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
        private enum EyeState {
            Normal = 0,
            HalfBlink = 1,
            FullBlink = 2,
            Death = 3,
        }

        public PlayerCharacter character = PlayerCharacter.Mario;
        public bool large = false;
        public int sortingOrder = 20;
        // The exported player FBXs face the camera by default; yaw them so the character faces right in 2D.
        public float yawOffsetDegrees = 90f;
        [Tooltip("Scales the imported 3D model relative to the current hitbox height. Use to fine-tune parity.")]
        public float visualHeightMultiplier = 1f;

        // Unity 6 reference (PlayerMario/PlayerLuigi prefabs):
        // - Small model root scale: 0.03
        // - Large (Mushroom) model root scale: 0.054
        // - Root Y offset: -0.03
        //
        // NOTE: In Unity 2017, FBX import scales can vary between machines. Prefer fitting to the active hitbox height
        // at runtime, with these values as a fallback if we can't compute renderer bounds.
        private const float FallbackSmallModelRootScale = 0.03f;
        private const float FallbackLargeModelRootScale = 0.054f;
        private const float FallbackModelRootYOffset = -0.03f;

        private PlayerMotor2D _motor;
        private Rigidbody2D _rb;
        private Collider2D _col;
	        private SpriteRenderer _fallbackSprite;

	        private GameObject _modelRoot;
	        private Animator _animator;
	        private bool _facingRight = true;
	        private float _nextControllerRetryTime;
	        private bool _builtLarge;
	        private float _nextSizeRebuildTime;
	        private readonly List<Material> _eyeMaterials = new List<Material>(4);
	        private Coroutine _blinkCoroutine;
	        private EyeState _eyeState = EyeState.Normal;
	        private GameObject _wrapEchoLeft;
	        private GameObject _wrapEchoRight;
	        private NSMB.World.StageWrap2D _wrapStage;
	        private float _wrapWidth;
	        private float _nextWrapScanTime;

        private void Awake() {
            _motor = GetComponent<PlayerMotor2D>();
            _rb = GetComponent<Rigidbody2D>();
	            _col = GetComponent<Collider2D>();
	            _fallbackSprite = GetComponent<SpriteRenderer>();
	            _builtLarge = large;
	        }

	        private void Start() {
	            // Build after all Awake() calls have run so PlayerPowerupState has applied the correct hitbox size.
	            TryBuildModel();
	            _builtLarge = large;
	            EnsureBlinkRoutine();
	            EnsureWrapEchos();
	        }

	        private void OnEnable() {
	            EnsureBlinkRoutine();
	            EnsureWrapEchos();
	        }

	        private void OnDisable() {
	            if (_blinkCoroutine != null) {
	                StopCoroutine(_blinkCoroutine);
	                _blinkCoroutine = null;
	            }
	            DestroyWrapEchos();
	        }

	        private void LateUpdate() {
	            if (_rb == null) {
	                return;
	            }

            if (large != _builtLarge && Time.time >= _nextSizeRebuildTime) {
                _nextSizeRebuildTime = Time.time + 0.10f;
                RebuildForSize();
                return;
            }

            if (_modelRoot == null) {
                return;
            }

	            // If the controller wasn't imported yet when the player spawned (common when assets are still importing),
	            // retry to attach the generated controller so we don't get stuck in a wrong/empty state.
	            if (_animator != null) {
                RuntimeAnimatorController cur = _animator.runtimeAnimatorController;
                bool needs = (cur == null) || (cur.animationClips == null) || (cur.animationClips.Length == 0) ||
                             (cur.animationClips.Length == 1 && cur.animationClips[0] != null &&
                              string.Equals(cur.animationClips[0].name, "empty", System.StringComparison.InvariantCultureIgnoreCase));
                if (needs && Time.time >= _nextControllerRetryTime) {
                    _nextControllerRetryTime = Time.time + 1.0f;
                    RuntimeAnimatorController controller = LoadGeneratedController(character, large);
                    if (controller != null) {
                        _animator.runtimeAnimatorController = controller;
                        // reset playback
                        _animator.Rebind();
                    }
                }
            }

            float vx = _rb.velocity.x;
            float vy = _rb.velocity.y;

            if (Mathf.Abs(vx) > 0.05f) {
                _facingRight = vx > 0f;
            }

            // Match original approach: rotate model around Y for facing.
            float yaw = yawOffsetDegrees + (_facingRight ? 0f : 180f);
            _modelRoot.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

	            if (_animator != null) {
	                // These parameter names match the original controller (see MarioPlayerAnimator hashes).
	                _animator.SetFloat("velocityX", vx);
	                _animator.SetFloat("velocityY", vy);
	                _animator.SetFloat("velocityMagnitude", new Vector2(vx, vy).magnitude);
	                _animator.SetBool("onGround", _motor != null && _motor.IsGrounded);
	                _animator.SetBool("facingRight", _facingRight);
	            }

	            UpdateWrapEchos();
	        }

	        private void RebuildForSize() {
		            if (_modelRoot != null) {
		                Destroy(_modelRoot);
		                _modelRoot = null;
		                _animator = null;
		            }

		            _eyeMaterials.Clear();
		            _eyeState = EyeState.Normal;
		            DestroyWrapEchos();
		            TryBuildModel();
		            _builtLarge = large;
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

            // Create a yaw root so we can rotate for facing without overwriting the authored
            // pitch/roll of the imported FBX hierarchy.
            _modelRoot = new GameObject("Model");
            _modelRoot.transform.parent = transform;
            _modelRoot.transform.localPosition = Vector3.zero;
            _modelRoot.transform.localRotation = Quaternion.identity;
            _modelRoot.transform.localScale = Vector3.one;

            GameObject content = Instantiate(modelPrefab) as GameObject;
            if (content == null) {
                Destroy(_modelRoot);
                _modelRoot = null;
                return;
            }

 	            content.name = "ModelContent";
 	            content.transform.parent = _modelRoot.transform;
 	            content.transform.localRotation = Quaternion.identity;
 	            content.transform.localPosition = Vector3.zero;
 	            content.transform.localScale = Vector3.one;
 
 	            // Match the Unity 6 prefabs as closely as possible:
 	            // - Small model root scale: 0.03
 	            // - Large (Mushroom) model root scale: 0.054
 	            // - Root Y offset: -0.03
 	            //
 	            // IMPORTANT: the Unity 6 project is authored in a half-scale world (tile = 0.5 Unity units).
 	            // This port uses a 1-unit-per-tile world, so we apply a 2x visual scale to preserve parity.
 	            // NOTE: PlayerMotor2D.originalToUnityScale is a physics conversion factor; do not use it here.
 	            const float VisualWorldScaleFromOriginal = 2f;
 	            float multiplier = Mathf.Max(0.01f, visualHeightMultiplier);
 	            float fallbackScale = (large ? FallbackLargeModelRootScale : FallbackSmallModelRootScale) * VisualWorldScaleFromOriginal * multiplier;
 	            content.transform.localPosition = new Vector3(0f, FallbackModelRootYOffset * VisualWorldScaleFromOriginal, 0f);
 	            content.transform.localScale = new Vector3(fallbackScale, fallbackScale, fallbackScale);
	            // NOTE: Keep the authored Unity 6 scale as the primary source of truth.
	            // FBX bounds can vary across Unity 2017 import settings (and SkinnedMeshRenderer bounds
	            // can be unreliable until animation updates), so auto-fitting can cause dramatic size jumps.

		            ApplyRendererSorting(content, sortingOrder);
		            FixMaterials(content, GetModelFolderResourcePath(character, large));
		            RefreshEyeMaterials(content);
		            SetEyeState(EyeState.Normal);
		            EnsureBlinkRoutine();
		            SetupAnimator(content);

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

	        private void EnsureBlinkRoutine() {
	            if (!isActiveAndEnabled) {
	                return;
	            }
	            if (_blinkCoroutine != null) {
	                return;
	            }
	            _blinkCoroutine = StartCoroutine(BlinkRoutine());
	        }

	        private IEnumerator BlinkRoutine() {
	            WaitForSeconds blinkStep = new WaitForSeconds(0.1f);
	            while (true) {
	                float wait = 3f + Random.value * 6f;
	                yield return new WaitForSeconds(wait);

	                // If the model was rebuilt mid-wait, we might not have eye materials yet.
	                if (_eyeMaterials == null || _eyeMaterials.Count == 0) {
	                    continue;
	                }

	                SetEyeState(EyeState.HalfBlink);
	                yield return blinkStep;
	                SetEyeState(EyeState.FullBlink);
	                yield return blinkStep;
	                SetEyeState(EyeState.HalfBlink);
	                yield return blinkStep;
	                SetEyeState(EyeState.Normal);
	            }
	        }

	        private void RefreshEyeMaterials(GameObject modelContentRoot) {
	            _eyeMaterials.Clear();
	            if (modelContentRoot == null) {
	                return;
	            }

	            Renderer[] renderers = modelContentRoot.GetComponentsInChildren<Renderer>(true);
	            HashSet<int> seen = new HashSet<int>();
	            for (int i = 0; i < renderers.Length; i++) {
	                Renderer r = renderers[i];
	                if (r == null) continue;

	                Material[] mats = r.materials;
	                if (mats == null) continue;

	                for (int m = 0; m < mats.Length; m++) {
	                    Material mat = mats[m];
	                    if (mat == null) continue;

	                    bool isEyeMaterial = false;
	                    Shader sh = mat.shader;
	                    if (sh != null && !string.IsNullOrEmpty(sh.name) &&
	                        sh.name.IndexOf("NSMB/UnlitEyesFlipbook", System.StringComparison.InvariantCultureIgnoreCase) >= 0) {
	                        isEyeMaterial = true;
	                    } else if (mat.HasProperty("_EyeState") && mat.HasProperty("_EyeGrid")) {
	                        isEyeMaterial = true;
	                    }

	                    if (!isEyeMaterial) continue;

	                    int id = mat.GetInstanceID();
	                    if (seen.Contains(id)) continue;
	                    seen.Add(id);
	                    _eyeMaterials.Add(mat);
	                }
	            }
	        }

	        private void SetEyeState(EyeState state) {
	            _eyeState = state;
	            float v = (float)(int)state;
	            for (int i = 0; i < _eyeMaterials.Count; i++) {
	                Material mat = _eyeMaterials[i];
	                if (mat == null) continue;
	                if (mat.HasProperty("_EyeState")) {
	                    mat.SetFloat("_EyeState", v);
	                }
	                // Keep compatibility with any older shaders that used a non-underscored property.
	                if (mat.HasProperty("EyeState")) {
	                    mat.SetFloat("EyeState", v);
	                }
	            }
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
            if (controller != null) {
                // Ensure state machine bindings are refreshed when swapping controllers at runtime.
                _animator.Rebind();
                _animator.Update(0f);
            }

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

 	            // Player visuals are rendered in a mostly 2D scene with no reliable lighting setup.
 	            // Use unlit shaders for parity/consistency. Prefer single-sided rendering; the Mario meshes are
 	            // authored for proper depth occlusion, and double-sided rendering can make limbs look "see-through"
 	            // when submeshes overlap.
 	            Shader opaque = Shader.Find("NSMB/UnlitBaseMapOpaque");
 	            if (opaque == null) {
 	                opaque = Shader.Find("NSMB/UnlitBaseMapOpaqueDoubleSided");
 	            }
	            if (opaque == null) {
	                opaque = Shader.Find("Unlit/Texture");
	            }
	            Shader cutout = Shader.Find("NSMB/UnlitBaseMapCutout");
                Shader cutoutDoubleSided = Shader.Find("NSMB/UnlitBaseMapCutoutDoubleSided");
                Shader eyesShader = Shader.Find("NSMB/UnlitEyesFlipbook");
	            if (cutout == null) {
	                cutout = Shader.Find("Transparent/Cutout/Diffuse");
	            }
	            if (opaque == null && cutout == null) {
	                return;
	            }

	            Texture2D[] textures = LoadTextures(modelFolderResourcePath);
	            Texture2D primary = PickPrimaryTexture(textures, modelFolderResourcePath);

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++) {
                Renderer r = renderers[i];
                if (r == null) continue;
                bool rendererHasUv2 = RendererHasUv2(r);
	
                float uvRangeY = 1f;
                TryGetRendererUvRangeY(r, out uvRangeY);
                Material[] mats = r.sharedMaterials;
	                if (mats == null || mats.Length == 0) {
	                    // Some imports can produce a renderer with no materials assigned.
	                    if (primary != null && opaque != null) {
	                        Material fallbackMat = new Material(opaque);
	                        ApplyMaterialTextureAndUv(fallbackMat, primary, Vector2.one, Vector2.zero);
	                        ApplyMaterialColors(fallbackMat);
	                        r.materials = new Material[] { fallbackMat };
	                    }
	                    continue;
	                }

                Material[] newMats = new Material[mats.Length];

	                for (int m = 0; m < mats.Length; m++) {
	                    Material mat = mats[m];
	                    if (mat == null) {
	                        continue;
	                    }
 
	                    bool nameSaysEye = (!string.IsNullOrEmpty(mat.name) &&
	                                       (mat.name.IndexOf("eye", System.StringComparison.InvariantCultureIgnoreCase) >= 0));
                        bool hasEyeProps = mat.HasProperty("_EyeState") || mat.HasProperty("EyeState") ||
                                           (mat.HasProperty("_EyeGrid") && mat.HasProperty("_UseUv2"));
                        bool texSaysEye = false;
	                    bool wantsCutout = nameSaysEye || hasEyeProps;

 	                    // Unity 2017 can spam the Console when probing non-texture properties. Avoid accessing
 	                    // `mat.mainTexture`/`mat.mainTextureScale` here (they can resolve to missing/invalid
 	                    // shader properties depending on the imported material/shader).
 	                    //
 	                    // Prefer the legacy Standard property, then fall back to guessing from the model folder.
 	                    Texture mainTex = SafeGetTexture(mat, "_MainTex");
                        Texture2D sourceTex2D = mainTex as Texture2D;
                        if (sourceTex2D != null && !string.IsNullOrEmpty(sourceTex2D.name)) {
                            texSaysEye = sourceTex2D.name.IndexOf("eye", System.StringComparison.InvariantCultureIgnoreCase) >= 0;
                            if (texSaysEye) {
                                wantsCutout = true;
                            }
                        }

	                    if (textures != null && textures.Length > 0) {
	                        // Enforce stable texture selection for Unity 2017:
	                        // - eye materials always use the dedicated eyes texture (when present)
	                        // - everything else always uses the primary body texture (mario_big.png / luigi.png)
	                        bool isEye = wantsCutout;

	                        if (isEye) {
	                            Texture2D eyes = null;
	                            for (int ti = 0; ti < textures.Length; ti++) {
	                                Texture2D tt = textures[ti];
	                                if (tt == null || string.IsNullOrEmpty(tt.name)) continue;
	                                if (tt.name.IndexOf("eyes", System.StringComparison.InvariantCultureIgnoreCase) >= 0) {
	                                    eyes = tt;
	                                    break;
	                                }
	                            }
	                            if (eyes != null) {
	                                mainTex = eyes;
	                            } else if (primary != null) {
	                                mainTex = primary;
	                            }
	                        } else if (primary != null) {
	                            mainTex = primary;
	                        }
	                    }

                        Texture2D mainTex2D = mainTex as Texture2D;
	                        if (mainTex2D != null) {
	                            // Enforce pixel-art sampling; helps reduce shimmering and color bleed on 3D models.
	                            mainTex2D.filterMode = FilterMode.Point;
	
	                            // Keep textures clamped; the eye shader wraps UVs into 0..1 (frac) when needed.
	                            mainTex2D.wrapMode = TextureWrapMode.Clamp;
	                        }

	                     Vector2 scale = Vector2.one;
	                     Vector2 offset = Vector2.zero;
	                     TryGetTextureScaleOffset(mat, "_MainTex", out scale, out offset);

                        // Unity 6 player textures are often tall flipbook atlases (stacked frames).
                        // In Unity 2017 we don't have the original shader/controller driving the UV row, so pick a
                        // stable default frame by slicing to the top row when:
                        // - this isn't an eye material, and
                        // - UVs appear to use full 0..1 range (rangeY large), and
                        // - the incoming material doesn't already specify a transform.
                        if (!wantsCutout && mainTex != null) {
                            Vector2 fbScale;
                            Vector2 fbOffset;
                            if (TryGetFlipbookTransform(mainTex, out fbScale, out fbOffset)) {
                                bool defaultTransform = (Mathf.Abs(scale.x - 1f) < 0.0001f &&
                                                         Mathf.Abs(scale.y - 1f) < 0.0001f &&
                                                         Mathf.Abs(offset.x) < 0.0001f &&
                                                         Mathf.Abs(offset.y) < 0.0001f);
                                if (defaultTransform && uvRangeY > 0.2f) {
                                    scale = fbScale;
                                    offset = fbOffset;
                                }
                            }
                        }

	                    // Always standardize to our unlit shaders so the character renders consistently
	                    // regardless of scene lighting, while still keeping the original textures/UVs.
	                    Shader targetShader = null;
                        if (wantsCutout) {
                            // Eyes use a packed sprite sheet in the Unity 6 project, selected by EyeState
                            // in the original shader. In Unity 2017 we emulate this by sampling from a 2D
                            // sheet with an index (defaulting to 0 = open).
                            targetShader = (eyesShader != null) ? eyesShader : ((cutoutDoubleSided != null) ? cutoutDoubleSided : cutout);
                        } else {
                            targetShader = opaque;
                        }
	                    if (targetShader == null) {
	                        targetShader = opaque != null ? opaque : cutout;
	                    }
	                    if (targetShader == null) continue;

	                    Material repl = new Material(targetShader);
	                    if (mainTex != null) {
	                        ApplyMaterialTextureAndUv(repl, mainTex, scale, offset);
	                    }
	                    if (wantsCutout && repl.HasProperty("_Cutoff")) {
	                        try {
	                            float c = mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.5f;
	                            repl.SetFloat("_Cutoff", c);
	                        } catch {
	                            repl.SetFloat("_Cutoff", 0.5f);
	                        }
	                    }
	                        if (wantsCutout && eyesShader != null && targetShader == eyesShader) {
                            // Unity 6 imports mario_eyes.png as a flipbook with columns=4, rows=1 (Texture2DArray).
                            // In Unity 2017 we emulate that with a 4x1 sheet.
                            if (repl.HasProperty("_EyeGrid")) {
                                repl.SetVector("_EyeGrid", new Vector4(4f, 1f, 0f, 0f));
                            }
                            if (repl.HasProperty("_EyeState")) {
                                repl.SetFloat("_EyeState", 0f);
                            }
	                            if (repl.HasProperty("_UseUv2")) {
	                                repl.SetFloat("_UseUv2", rendererHasUv2 ? 1f : 0f);
	                            }
	                            if (repl.HasProperty("_RowFromTop")) {
	                                // Top row in the PNG contains the eye states; default to indexing from the top.
	                                repl.SetFloat("_RowFromTop", 1f);
	                            }
                                if (repl.HasProperty("_Cutoff")) {
                                    // Eye textures are authored as opaque; avoid full discard from import/cutoff drift.
                                    repl.SetFloat("_Cutoff", 0.01f);
                                }
                                // Ensure eyes render on top of surrounding geometry/background.
                                repl.renderQueue = 5000;
	                        }
	                        ApplyMaterialColors(repl);
	                        newMats[m] = repl;
	                    }

                for (int m = 0; m < newMats.Length; m++) {
                    if (newMats[m] == null) {
                        newMats[m] = mats[m];
                    }
                }
                r.materials = newMats;
            }
        }

	        private bool TryFitModelToHitbox(GameObject content, float fallbackScale) {
	            if (content == null) {
	                return false;
	            }

            if (_col == null) {
                return false;
            }

            float desiredHeight = _col.bounds.size.y * Mathf.Max(0.01f, visualHeightMultiplier);
            if (desiredHeight <= 0.0001f) {
                return false;
            }

            Bounds bounds;
            if (!TryGetRendererBounds(content, out bounds)) {
                return false;
            }

	            float modelHeight = bounds.size.y;
	            if (modelHeight <= 0.0001f) {
	                return false;
	            }

	            float currentScale = content.transform.localScale.x;
	            if (currentScale <= 0.000001f || float.IsNaN(currentScale) || float.IsInfinity(currentScale)) {
	                currentScale = 1f;
	            }

	            float scale = currentScale * (desiredHeight / modelHeight);
	            if (scale <= 0.000001f || float.IsNaN(scale) || float.IsInfinity(scale)) {
	                return false;
	            }

            // Guard against bogus bounds during import producing extreme scales.
            // Use the authored Unity 6 scale as a sanity check band.
            if (fallbackScale > 0.000001f) {
                float min = fallbackScale * 0.25f;
                float max = fallbackScale * 4.0f;
                if (scale < min || scale > max) {
                    return false;
                }
            }

            content.transform.localScale = new Vector3(scale, scale, scale);

            // Recompute bounds after scaling and align model feet to the player's feet (transform origin).
            Bounds bounds2;
            if (TryGetRendererBounds(content, out bounds2)) {
                float feetY = transform.position.y;
                float dy = feetY - bounds2.min.y;
                if (Mathf.Abs(dy) > 0.0001f) {
                    Vector3 lp = content.transform.localPosition;
                    content.transform.localPosition = new Vector3(lp.x, lp.y + dy, lp.z);
                }
            }

            return true;
        }

        private static bool TryGetRendererBounds(GameObject root, out Bounds bounds) {
            bounds = new Bounds();
            if (root == null) {
                return false;
            }

            // Prefer mesh-based bounds (stable for SkinnedMeshRenderer in older Unity versions).
            if (TryGetMeshBounds(root, out bounds)) {
                return true;
            }

            // Fallback to renderer bounds.
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) {
                return false;
            }

            bool any = false;
            for (int i = 0; i < renderers.Length; i++) {
                Renderer r = renderers[i];
                if (r == null) {
                    continue;
                }

                if (!any) {
                    bounds = r.bounds;
                    any = true;
                } else {
                    bounds.Encapsulate(r.bounds);
                }
            }

            return any;
        }

        private static bool TryGetMeshBounds(GameObject root, out Bounds bounds) {
            bounds = new Bounds();
            if (root == null) {
                return false;
            }

            bool any = false;

            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            if (meshFilters != null) {
                for (int i = 0; i < meshFilters.Length; i++) {
                    MeshFilter mf = meshFilters[i];
                    if (mf == null || mf.sharedMesh == null) continue;
                    Bounds wb = TransformBounds(mf.transform.localToWorldMatrix, mf.sharedMesh.bounds);
                    if (!any) {
                        bounds = wb;
                        any = true;
                    } else {
                        bounds.Encapsulate(wb);
                    }
                }
            }

            SkinnedMeshRenderer[] skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (skinned != null) {
                for (int i = 0; i < skinned.Length; i++) {
                    SkinnedMeshRenderer smr = skinned[i];
                    if (smr == null || smr.sharedMesh == null) continue;
                    Bounds wb = TransformBounds(smr.transform.localToWorldMatrix, smr.sharedMesh.bounds);
                    if (!any) {
                        bounds = wb;
                        any = true;
                    } else {
                        bounds.Encapsulate(wb);
                    }
                }
            }

            return any;
        }

        private static Bounds TransformBounds(Matrix4x4 m, Bounds b) {
            Vector3 c = b.center;
            Vector3 e = b.extents;

            Vector3[] pts = new Vector3[8];
            pts[0] = new Vector3(c.x - e.x, c.y - e.y, c.z - e.z);
            pts[1] = new Vector3(c.x + e.x, c.y - e.y, c.z - e.z);
            pts[2] = new Vector3(c.x - e.x, c.y + e.y, c.z - e.z);
            pts[3] = new Vector3(c.x + e.x, c.y + e.y, c.z - e.z);
            pts[4] = new Vector3(c.x - e.x, c.y - e.y, c.z + e.z);
            pts[5] = new Vector3(c.x + e.x, c.y - e.y, c.z + e.z);
            pts[6] = new Vector3(c.x - e.x, c.y + e.y, c.z + e.z);
            pts[7] = new Vector3(c.x + e.x, c.y + e.y, c.z + e.z);

            Vector3 p0 = m.MultiplyPoint3x4(pts[0]);
            Bounds wb = new Bounds(p0, Vector3.zero);
            for (int i = 1; i < pts.Length; i++) {
                wb.Encapsulate(m.MultiplyPoint3x4(pts[i]));
            }
            return wb;
        }

        private static Texture SafeGetTexture(Material mat, string propertyName) {
            if (mat == null || string.IsNullOrEmpty(propertyName)) {
                return null;
            }

            try {
                // IMPORTANT: In Unity 2017, calling GetTexture/GetTextureScale/GetTextureOffset on a missing property
                // logs an error to the Console even if it doesn't throw. Guard with HasProperty to avoid log spam.
                if (!mat.HasProperty(propertyName)) {
                    return null;
                }

                return mat.GetTexture(propertyName);
            } catch {
                // Ignore: some shaders may have the property name but not as a texture property.
            }

            return null;
        }

        private static bool TryGetTextureScaleOffset(Material mat, string propertyName, out Vector2 scale, out Vector2 offset) {
            scale = Vector2.one;
            offset = Vector2.zero;

            if (mat == null || string.IsNullOrEmpty(propertyName)) {
                return false;
            }

            try {
                if (!mat.HasProperty(propertyName)) {
                    return false;
                }

                scale = mat.GetTextureScale(propertyName);
                offset = mat.GetTextureOffset(propertyName);
                return true;
            } catch {
                return false;
            }
        }

        private static void ApplyMaterialTextureAndUv(Material mat, Texture tex, Vector2 scale, Vector2 offset) {
            if (mat == null || tex == null) {
                return;
            }

            if (mat.HasProperty("_BaseMap")) {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTextureScale("_BaseMap", scale);
                mat.SetTextureOffset("_BaseMap", offset);
            }
            if (mat.HasProperty("_MainTex")) {
                mat.SetTexture("_MainTex", tex);
                mat.SetTextureScale("_MainTex", scale);
                mat.SetTextureOffset("_MainTex", offset);
            }

            // Keep the generic accessors consistent for built-in shaders.
            mat.mainTexture = tex;
            mat.mainTextureScale = scale;
            mat.mainTextureOffset = offset;
        }

	        private static void ApplyMaterialColors(Material mat) {
	            if (mat == null) {
	                return;
	            }

            if (mat.HasProperty("_BaseColor")) {
                mat.SetColor("_BaseColor", Color.white);
            }
            if (mat.HasProperty("_Color")) {
                mat.SetColor("_Color", Color.white);
            }
	            mat.color = Color.white;
	        }

	        private static void ForceOpaque(Material mat) {
	            if (mat == null) {
	                return;
	            }

	            // Best-effort: if this is a Standard-like shader, force opaque rendering to avoid
	            // transparent self-sorting artifacts ("see-through" limbs).
	            try {
	                if (mat.HasProperty("_Mode")) {
	                    mat.SetFloat("_Mode", 0f);
	                }
	                if (mat.HasProperty("_SrcBlend")) {
	                    mat.SetInt("_SrcBlend", 1);
	                }
	                if (mat.HasProperty("_DstBlend")) {
	                    mat.SetInt("_DstBlend", 0);
	                }
	                if (mat.HasProperty("_ZWrite")) {
	                    mat.SetInt("_ZWrite", 1);
	                }
	            } catch {
	                // ignore
	            }

	            mat.DisableKeyword("_ALPHATEST_ON");
	            mat.DisableKeyword("_ALPHABLEND_ON");
	            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
	            mat.renderQueue = -1;
	        }

	        private static Texture2D[] LoadTextures(string modelFolderResourcePath) {
	            if (string.IsNullOrEmpty(modelFolderResourcePath)) {
	                return null;
	            }

	            List<Texture2D> textures = new List<Texture2D>();

	            UnityEngine.Object[] objs = Resources.LoadAll(modelFolderResourcePath, typeof(Texture2D));
	            if (objs == null || objs.Length == 0) {
	                objs = null;
	            }

	            if (objs != null) {
	                for (int i = 0; i < objs.Length; i++) {
	                    Texture2D t = objs[i] as Texture2D;
	                    if (t != null) {
	                        textures.Add(t);
	                    }
	                }
	            }

	            // Cross-load paired folders for parity with Unity 6:
	            // - Small folders may not include the main body texture (fallback to *_big).
	            // - Big folders may not include the shared eye texture (fallback to *_small).
	            string fallbackBig = null;
	            string fallbackSmall = null;
	            if (modelFolderResourcePath.IndexOf("_small", System.StringComparison.InvariantCultureIgnoreCase) >= 0) {
	                fallbackBig = modelFolderResourcePath.Replace("_small", "_big");
	            }
	            if (modelFolderResourcePath.IndexOf("_big", System.StringComparison.InvariantCultureIgnoreCase) >= 0) {
	                fallbackSmall = modelFolderResourcePath.Replace("_big", "_small");
	            }

	            if (!string.IsNullOrEmpty(fallbackBig) &&
	                !string.Equals(fallbackBig, modelFolderResourcePath, System.StringComparison.InvariantCultureIgnoreCase)) {
	                UnityEngine.Object[] objs2 = Resources.LoadAll(fallbackBig, typeof(Texture2D));
	                if (objs2 != null) {
	                    for (int i = 0; i < objs2.Length; i++) {
	                        Texture2D t = objs2[i] as Texture2D;
	                        if (t != null) {
	                            textures.Add(t);
	                        }
	                    }
	                }
	            }

	            if (!string.IsNullOrEmpty(fallbackSmall) &&
	                !string.Equals(fallbackSmall, modelFolderResourcePath, System.StringComparison.InvariantCultureIgnoreCase)) {
	                UnityEngine.Object[] objs2 = Resources.LoadAll(fallbackSmall, typeof(Texture2D));
	                if (objs2 != null) {
	                    for (int i = 0; i < objs2.Length; i++) {
	                        Texture2D t = objs2[i] as Texture2D;
	                        if (t != null) {
	                            textures.Add(t);
	                        }
	                    }
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
	                    if (tn.Contains(prefer) &&
	                        !tn.Contains("starman") &&
	                        !tn.Contains("propeller") &&
	                        !tn.Contains("mask") &&
	                        !tn.Contains("legmask") &&
	                        !tn.Contains("eyes")) {
	                        return t;
	                    }
	                }
	            }

	            // Fallback: first non-mask, non-variant texture.
	            for (int i = 0; i < textures.Length; i++) {
	                Texture2D t = textures[i];
	                if (t == null || string.IsNullOrEmpty(t.name)) continue;
	                string tn = t.name.ToLowerInvariant();
	                if (tn.Contains("starman") || tn.Contains("propeller") || tn.Contains("mask") || tn.Contains("legmask")) {
	                    continue;
	                }
	                return t;
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

        private static bool TryGetFlipbookTransform(Texture tex, out Vector2 scale, out Vector2 offset) {
            scale = Vector2.one;
            offset = Vector2.zero;

            Texture2D t = tex as Texture2D;
            if (t == null) {
                return false;
            }

            int w = t.width;
            int h = t.height;
            if (w <= 0 || h <= 0) {
                return false;
            }

            // Only apply flipbook slicing for tall textures (body). Do NOT touch eyes; their UVs are already authored.
            if (h <= w) {
                return false;
            }

            // Prefer half-height rows (rows = h / (w/2)). mario_big.png is 128x1280 => 20 rows of 64px.
            int rows = 0;
            if ((w % 2) == 0) {
                int frameH = w / 2;
                if (frameH > 0 && (h % frameH) == 0) {
                    rows = h / frameH;
                }
            }
            if (rows <= 1 && (h % w) == 0) {
                // Fallback to square rows if present.
                rows = h / w;
            }

            if (rows <= 1) {
                return false;
            }

            float invRows = 1f / rows;
            scale = new Vector2(1f, invRows);
            // Default to the TOP row (this matches the default "PowerupState=0" visuals in the Unity 6 shader).
            offset = new Vector2(0f, 1f - invRows);
            return true;
        }

        private static bool TryGetRendererUvRangeY(Renderer r, out float rangeY) {
            rangeY = 1f;
            if (r == null) {
                return false;
            }

            Mesh mesh = null;
            SkinnedMeshRenderer smr = r as SkinnedMeshRenderer;
            if (smr != null) {
                mesh = smr.sharedMesh;
            } else {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf != null) {
                    mesh = mf.sharedMesh;
                }
            }

            if (mesh == null) {
                return false;
            }

            Vector2[] uv = null;
            try {
                uv = mesh.uv;
            } catch {
                uv = null;
            }

            if (uv == null || uv.Length == 0) {
                return false;
            }

            float minY = uv[0].y;
            float maxY = uv[0].y;
            for (int i = 1; i < uv.Length; i++) {
                float y = uv[i].y;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            rangeY = Mathf.Abs(maxY - minY);
            return true;
        }

        private static bool RendererHasUv2(Renderer r) {
            if (r == null) {
                return false;
            }

            Mesh mesh = null;
            SkinnedMeshRenderer smr = r as SkinnedMeshRenderer;
            if (smr != null) {
                mesh = smr.sharedMesh;
            } else {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf != null) {
                    mesh = mf.sharedMesh;
                }
            }

            if (mesh == null) {
                return false;
            }

            Vector2[] uv2 = null;
            try {
                uv2 = mesh.uv2;
            } catch {
                uv2 = null;
            }
            if (uv2 == null || uv2.Length == 0) {
                return false;
            }

            // Ignore empty/generated channels that contain only zeroes.
            for (int i = 0; i < uv2.Length; i++) {
                if (Mathf.Abs(uv2[i].x) > 0.0001f || Mathf.Abs(uv2[i].y) > 0.0001f) {
                    return true;
                }
            }

            return false;
        }

        private void EnsureWrapEchos() {
            _nextWrapScanTime = 0f;
            UpdateWrapEchos();
        }

        private void UpdateWrapEchos() {
            if (_modelRoot == null) {
                DestroyWrapEchos();
                return;
            }

            // Scan infrequently; StageWrap2D exists only in wrapping stages.
            if (_wrapStage == null || Time.time >= _nextWrapScanTime) {
                _nextWrapScanTime = Time.time + 1.0f;
                _wrapStage = Object.FindObjectOfType(typeof(NSMB.World.StageWrap2D)) as NSMB.World.StageWrap2D;
            }

            if (_wrapStage == null || _wrapStage.WrapWidth <= 0.0001f) {
                DestroyWrapEchos();
                return;
            }

            float w = _wrapStage.WrapWidth;
            if (_wrapEchoLeft == null || _wrapEchoRight == null || Mathf.Abs(w - _wrapWidth) > 0.001f) {
                BuildWrapEchos(w);
                return;
            }

            // Keep offsets up to date if wrap width changes (rare) without needing a rebuild.
            NSMB.World.TransformHierarchyMirror left = _wrapEchoLeft.GetComponent<NSMB.World.TransformHierarchyMirror>();
            if (left != null) {
                left.rootLocalPositionOffset = new Vector3(-_wrapWidth, 0f, 0f);
            }

            NSMB.World.TransformHierarchyMirror right = _wrapEchoRight.GetComponent<NSMB.World.TransformHierarchyMirror>();
            if (right != null) {
                right.rootLocalPositionOffset = new Vector3(_wrapWidth, 0f, 0f);
            }
        }

        private void BuildWrapEchos(float wrapWidth) {
            DestroyWrapEchos();

            if (_modelRoot == null || wrapWidth <= 0.0001f) {
                return;
            }

            _wrapWidth = wrapWidth;
            _wrapEchoLeft = CreateWrapEcho("Model_WrapLeft", -wrapWidth);
            _wrapEchoRight = CreateWrapEcho("Model_WrapRight", wrapWidth);
        }

        private GameObject CreateWrapEcho(string name, float dx) {
            if (_modelRoot == null) {
                return null;
            }

            GameObject echo = Instantiate(_modelRoot);
            echo.name = name;
            echo.SetActive(false);

            Transform src = _modelRoot.transform;
            Transform dst = echo.transform;
            dst.parent = src.parent;
            dst.localPosition = src.localPosition + new Vector3(dx, 0f, 0f);
            dst.localRotation = src.localRotation;
            dst.localScale = src.localScale;

            NSMB.World.TransformHierarchyMirror mirror = echo.AddComponent<NSMB.World.TransformHierarchyMirror>();
            mirror.sourceRoot = src;
            mirror.rootLocalPositionOffset = new Vector3(dx, 0f, 0f);

            StripEchoToVisualOnly(echo.transform);
            mirror.RebuildMap();

            echo.SetActive(true);
            return echo;
        }

        private static void StripEchoToVisualOnly(Transform root) {
            if (root == null) {
                return;
            }

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            if (all == null) {
                return;
            }

            for (int i = 0; i < all.Length; i++) {
                Transform tr = all[i];
                if (tr == null) continue;

                // Remove scripts (mirror is the only one we keep).
                MonoBehaviour[] behaviours = tr.GetComponents<MonoBehaviour>();
                if (behaviours != null) {
                    for (int b = 0; b < behaviours.Length; b++) {
                        MonoBehaviour mb = behaviours[b];
                        if (mb == null) continue;
                        if (mb is NSMB.World.TransformHierarchyMirror) continue;
                        Object.Destroy(mb);
                    }
                }

                // Remove physics / interactions.
                Collider2D[] cols2d = tr.GetComponents<Collider2D>();
                if (cols2d != null) {
                    for (int c = 0; c < cols2d.Length; c++) {
                        Collider2D col = cols2d[c];
                        if (col != null) {
                            Object.Destroy(col);
                        }
                    }
                }

                Rigidbody2D rb2d = tr.GetComponent<Rigidbody2D>();
                if (rb2d != null) {
                    Object.Destroy(rb2d);
                }

                Animator anim = tr.GetComponent<Animator>();
                if (anim != null) {
                    Object.Destroy(anim);
                }

                Animation legacyAnim = tr.GetComponent<Animation>();
                if (legacyAnim != null) {
                    Object.Destroy(legacyAnim);
                }

                AudioSource audio = tr.GetComponent<AudioSource>();
                if (audio != null) {
                    Object.Destroy(audio);
                }
            }
        }

        private void DestroyWrapEchos() {
            if (_wrapEchoLeft != null) {
                Destroy(_wrapEchoLeft);
                _wrapEchoLeft = null;
            }
            if (_wrapEchoRight != null) {
                Destroy(_wrapEchoRight);
                _wrapEchoRight = null;
            }

            _wrapWidth = 0f;
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

    }
}
