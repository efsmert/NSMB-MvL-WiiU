# AGENTS.md - Unity 6.0.0 → Unity 2017.1.2p3 Downgrade Guide

**Purpose:** Systematic code transformation rules for backporting a Unity 6.0.0 project to Unity 2017.1.2p3. This document serves as a reference for an AI coding agent to convert the codebase.

**Critical Context:** Unity 2017.1.2p3 (released 2017) predates **7 years** of Unity development. Many modern features simply do not exist. The default scripting environment is **.NET 3.5 / C# 4.0** (experimental .NET 4.6 available but not recommended for compatibility).

---

## Table of Contents

1. [Project Structure Changes](#1-project-structure-changes)
2. [C# Language Features](#2-c-language-features)
3. [Rendering Pipeline](#3-rendering-pipeline)
4. [Input System](#4-input-system)
5. [2D Systems](#5-2d-systems)
6. [UI Systems](#6-ui-systems)
7. [Physics2D](#7-physics2d)
8. [Animation and Timeline](#8-animation-and-timeline)
9. [Scripting API Changes](#9-scripting-api-changes)
10. [Packages to Remove](#10-packages-to-remove)
11. [Photon Compatibility](#11-photon-compatibility)
12. [Transformation Rules Summary](#12-transformation-rules-summary)

---

## 1. Project Structure Changes

### 1.1 Package Manager (DOES NOT EXIST)

**Unity 2017.1 has no Package Manager.** Remove all package infrastructure:

```
DELETE: Packages/manifest.json
DELETE: Packages/packages-lock.json
DELETE: Packages/ folder entirely
```

All package code must be manually imported into `Assets/` as source.

### 1.2 Assembly Definition Files (DO NOT EXIST)

**.asmdef files were introduced in Unity 2017.3** - not available in 2017.1.

```
DELETE: All *.asmdef files
DELETE: All *.asmref files
```

**Compilation behavior in 2017.1:**
- `Assets/Plugins/` → `Assembly-CSharp-firstpass.dll` (compiles first)
- `Assets/Editor/` → `Assembly-CSharp-Editor.dll` (editor only)
- Everything else → `Assembly-CSharp.dll`

### 1.3 ProjectSettings Cleanup

Remove these Unity 6-specific files from `ProjectSettings/`:
```
DELETE: PackageManagerSettings.asset
DELETE: VersionControlSettings.asset
DELETE: MemorySettings.asset
DELETE: BurstAotSettings_*.json
DELETE: URPProjectSettings.asset
DELETE: Any render pipeline settings
```

### 1.4 Library Folder

**Always delete and regenerate:**
```
DELETE: Library/ folder entirely (regenerates on project open)
```

---

## 2. C# Language Features

### 2.1 Version Constraints

| Unity Version | .NET Runtime | C# Version |
|---------------|--------------|------------|
| **Unity 2017.1.2p3** | **.NET 3.5** | **C# 4.0** |
| Unity 6.0.0 | .NET Standard 2.1 | C# 9.0 |

### 2.2 UNSUPPORTED Features (Must Transform)

#### async/await → Coroutines
```csharp
// ❌ UNITY 6 (NOT SUPPORTED)
async Task DoAsync() {
    await Task.Delay(1000);
    Debug.Log("Done");
}

// ✅ UNITY 2017 EQUIVALENT
IEnumerator DoAsync() {
    yield return new WaitForSeconds(1f);
    Debug.Log("Done");
}
// Call with: StartCoroutine(DoAsync());
```

#### Target-typed new expressions
```csharp
// ❌ UNITY 6 (C# 9)
List<int> list = new();
Dictionary<string, int> dict = new();

// ✅ UNITY 2017 EQUIVALENT
List<int> list = new List<int>();
Dictionary<string, int> dict = new Dictionary<string, int>();
```

#### String interpolation
```csharp
// ❌ UNITY 6 (C# 6+)
string msg = $"Player {name} has {score} points";

// ✅ UNITY 2017 EQUIVALENT
string msg = string.Format("Player {0} has {1} points", name, score);
```

#### Null-conditional operator (?.)
```csharp
// ❌ UNITY 6 (C# 6+)
var result = obj?.Method() ?? defaultValue;
string name = player?.stats?.name;

// ✅ UNITY 2017 EQUIVALENT
var result = (obj != null) ? obj.Method() : defaultValue;
string name = (player != null && player.stats != null) ? player.stats.name : null;
```

#### nameof() operator
```csharp
// ❌ UNITY 6 (C# 6+)
throw new ArgumentNullException(nameof(parameter));

// ✅ UNITY 2017 EQUIVALENT
throw new ArgumentNullException("parameter");
```

#### Expression-bodied members
```csharp
// ❌ UNITY 6 (C# 6+)
public int Health => _health;
public void Die() => Destroy(gameObject);

// ✅ UNITY 2017 EQUIVALENT
public int Health { get { return _health; } }
public void Die() { Destroy(gameObject); }
```

#### Pattern matching
```csharp
// ❌ UNITY 6 (C# 7+)
if (obj is Player player) {
    player.TakeDamage(10);
}

// ✅ UNITY 2017 EQUIVALENT
Player player = obj as Player;
if (player != null) {
    player.TakeDamage(10);
}
```

#### Tuple syntax
```csharp
// ❌ UNITY 6 (C# 7+)
(int x, int y) GetPosition() => (10, 20);
var (x, y) = GetPosition();

// ✅ UNITY 2017 EQUIVALENT (out parameters)
void GetPosition(out int x, out int y) { x = 10; y = 20; }
int x, y;
GetPosition(out x, out y);

// OR (custom struct)
struct Position { public int x, y; }
Position GetPosition() { return new Position { x = 10, y = 20 }; }
```

#### out variable declarations
```csharp
// ❌ UNITY 6 (C# 7+)
if (int.TryParse(input, out var result)) { }

// ✅ UNITY 2017 EQUIVALENT
int result;
if (int.TryParse(input, out result)) { }
```

#### Local functions
```csharp
// ❌ UNITY 6 (C# 7+)
void Process() {
    int Validate(int x) => x > 0 ? x : 0;
    var result = Validate(input);
}

// ✅ UNITY 2017 EQUIVALENT
void Process() {
    Func<int, int> validate = delegate(int x) { return x > 0 ? x : 0; };
    var result = validate(input);
}
```

#### Switch expressions
```csharp
// ❌ UNITY 6 (C# 8+)
string GetDayType(int day) => day switch {
    0 or 6 => "Weekend",
    _ => "Weekday"
};

// ✅ UNITY 2017 EQUIVALENT
string GetDayType(int day) {
    switch (day) {
        case 0:
        case 6:
            return "Weekend";
        default:
            return "Weekday";
    }
}
```

#### using declarations
```csharp
// ❌ UNITY 6 (C# 8+)
using var stream = new FileStream(path, FileMode.Open);

// ✅ UNITY 2017 EQUIVALENT
using (var stream = new FileStream(path, FileMode.Open)) {
    // use stream
}
```

#### Auto-property initializers
```csharp
// ❌ UNITY 6 (C# 6+)
public int Health { get; set; } = 100;

// ✅ UNITY 2017 EQUIVALENT
[SerializeField] private int _health = 100;
public int Health { get { return _health; } set { _health = value; } }
```

### 2.3 C# Features Reference Table

| Feature | C# Version | Unity 2017.1 |
|---------|-----------|--------------|
| var keyword | 3.0 | ✅ YES |
| LINQ | 3.0 | ✅ YES |
| Lambda expressions | 3.0 | ✅ YES |
| Generics | 2.0 | ✅ YES |
| Null-coalescing (??) | 2.0 | ✅ YES |
| async/await | 5.0 | ❌ NO |
| String interpolation | 6.0 | ❌ NO |
| Null-conditional (?.) | 6.0 | ❌ NO |
| nameof() | 6.0 | ❌ NO |
| Expression-bodied | 6.0 | ❌ NO |
| Pattern matching | 7.0 | ❌ NO |
| Tuples | 7.0 | ❌ NO |
| out var | 7.0 | ❌ NO |
| Local functions | 7.0 | ❌ NO |
| throw expressions | 7.0 | ❌ NO |
| [field:] target | 7.3 | ❌ NO |
| switch expressions | 8.0 | ❌ NO |
| using declarations | 8.0 | ❌ NO |
| Nullable references | 8.0 | ❌ NO |
| Indices/ranges | 8.0 | ❌ NO |
| Records | 9.0 | ❌ NO |
| Target-typed new | 9.0 | ❌ NO |

---

## 3. Rendering Pipeline

### 3.1 URP Does Not Exist

**URP (Universal Render Pipeline) was introduced in Unity 2019.3.** The SRP (Scriptable Render Pipeline) framework itself started in Unity 2018.1.

**Remove all URP namespaces:**
```csharp
// ❌ DELETE THESE IMPORTS
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
```

**Classes that DO NOT EXIST in 2017:**
- `RenderPipelineAsset`, `RenderPipeline`, `ScriptableRenderContext`
- `UniversalRenderPipelineAsset`, `UniversalAdditionalCameraData`
- `ScriptableRendererFeature`, `ScriptableRenderPass`
- `RenderGraph`, `RTHandle`, `Light2D`, `ShadowCaster2D`

### 3.2 Shader Graph Does Not Exist

**Shader Graph was introduced in Unity 2018.1.**

```
DELETE: All *.shadergraph files
DELETE: All *.shadersubgraph files
```

**All Shader Graph assets must be manually converted to traditional .shader files.**

### 3.3 Shader Conversion

#### URP to Built-in Shader Mapping
| URP Shader | Built-in Equivalent |
|------------|---------------------|
| Universal Render Pipeline/Lit | Standard |
| Universal Render Pipeline/Simple Lit | Mobile/Diffuse |
| Universal Render Pipeline/Unlit | Unlit/Texture |
| Universal Render Pipeline/Particles/Lit | Particles/Standard Surface |
| Universal Render Pipeline/Particles/Unlit | Particles/Alpha Blended |

#### Shader Code Transformation
```hlsl
// ❌ UNITY 6 (URP/HLSL)
HLSLPROGRAM
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
float4 clipPos = TransformObjectToHClip(v.vertex);
ENDHLSL

// ✅ UNITY 2017 (Built-in/CG)
CGPROGRAM
#include "UnityCG.cginc"
sampler2D _MainTex;
half4 color = tex2D(_MainTex, uv);
float4 clipPos = UnityObjectToClipPos(v.vertex);
ENDCG
```

---

## 4. Input System

### 4.1 New Input System Does Not Exist

**com.unity.inputsystem was first released December 2018, requiring Unity 2018.2+.**

**Remove all New Input System code:**
```csharp
// ❌ DELETE THIS NAMESPACE
using UnityEngine.InputSystem;
```

**Classes that DO NOT EXIST:**
- `InputAction`, `InputActionMap`, `InputActionAsset`, `InputActionReference`
- `InputValue`, `InputAction.CallbackContext`
- `PlayerInput`, `PlayerInputManager`
- `Keyboard`, `Mouse`, `Gamepad`, `Touchscreen`
- `InputSystemUIInputModule`

```
DELETE: All *.inputactions asset files
```

### 4.2 Legacy Input Manager API

**Use `UnityEngine.Input` class (available in 2017.1):**

```csharp
// Keyboard
Input.GetKey(KeyCode.Space)          // Key held
Input.GetKeyDown(KeyCode.Space)      // Key pressed this frame
Input.GetKeyUp(KeyCode.Space)        // Key released this frame

// Axes (configured in Edit > Project Settings > Input)
Input.GetAxis("Horizontal")          // Smoothed -1 to 1
Input.GetAxisRaw("Horizontal")       // Raw value
Input.GetButton("Jump")              // Button held
Input.GetButtonDown("Jump")          // Button pressed this frame
Input.GetButtonUp("Jump")            // Button released this frame

// Mouse
Input.GetMouseButton(0)              // Left button held
Input.GetMouseButtonDown(0)          // Left button pressed
Input.mousePosition                  // Screen position (Vector3)
Input.mouseScrollDelta               // Scroll delta (Vector2)

// Touch
Input.touchCount                     // Active touch count
Input.GetTouch(0)                    // Touch struct by index
```

### 4.3 Input Conversion Examples

#### Action Callbacks → Polling
```csharp
// ❌ UNITY 6 (New Input System)
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour {
    public void OnMove(InputValue value) {
        Vector2 moveInput = value.Get<Vector2>();
    }
    
    public void OnJump(InputValue value) {
        if (value.isPressed) Jump();
    }
}

// ✅ UNITY 2017 EQUIVALENT
using UnityEngine;

public class PlayerController : MonoBehaviour {
    void Update() {
        // Movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector2 moveInput = new Vector2(horizontal, vertical);
        HandleMovement(moveInput);
        
        // Jump
        if (Input.GetButtonDown("Jump"))
            Jump();
    }
}
```

#### PlayerInput Component Removal
```csharp
// ❌ UNITY 6 (New Input System)
[RequireComponent(typeof(PlayerInput))]
public class Player : MonoBehaviour {
    void Awake() {
        var playerInput = GetComponent<PlayerInput>();
        playerInput.actions["Jump"].performed += OnJump;
    }
    void OnJump(InputAction.CallbackContext ctx) { }
}

// ✅ UNITY 2017 EQUIVALENT
public class Player : MonoBehaviour {
    void Update() {
        if (Input.GetButtonDown("Jump"))
            OnJump();
    }
    void OnJump() { }
}
```

#### Touch Input Conversion
```csharp
// ❌ UNITY 6 (Enhanced Touch)
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

void OnEnable() {
    EnhancedTouchSupport.Enable();
    Touch.onFingerDown += OnFingerDown;
}

// ✅ UNITY 2017 EQUIVALENT
void Update() {
    for (int i = 0; i < Input.touchCount; i++) {
        Touch touch = Input.GetTouch(i);
        if (touch.phase == TouchPhase.Began) {
            OnFingerDown(touch.position);
        }
    }
}
```

### 4.4 UI Input Module

```csharp
// ❌ UNITY 6
InputSystemUIInputModule

// ✅ UNITY 2017
StandaloneInputModule
```

---

## 5. 2D Systems

### 5.1 Tilemap System (DOES NOT EXIST in 2017.1)

**Tilemap was introduced in Unity 2017.2** - NOT available in 2017.1.2p3.

**Remove all Tilemap code:**
```csharp
// ❌ DELETE THIS NAMESPACE
using UnityEngine.Tilemaps;
```

**Classes that DO NOT EXIST:**
- `Tilemap`, `TilemapRenderer`, `Tile`, `TileBase`, `RuleTile`
- `Grid` component

**Alternative: Manual sprite-based approach:**
```csharp
// ✅ UNITY 2017.1 TILEMAP ALTERNATIVE
public class TileManager : MonoBehaviour {
    public Sprite[] tileSprites;
    public int gridWidth, gridHeight;
    public float tileSize = 1f;
    private SpriteRenderer[,] tiles;
    
    void CreateGrid() {
        tiles = new SpriteRenderer[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                GameObject tileObj = new GameObject("Tile_" + x + "_" + y);
                tileObj.transform.parent = transform;
                tileObj.transform.localPosition = new Vector3(x * tileSize, y * tileSize, 0);
                tiles[x, y] = tileObj.AddComponent<SpriteRenderer>();
            }
        }
    }
    
    public void SetTile(int x, int y, Sprite sprite) {
        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            tiles[x, y].sprite = sprite;
    }
}
```

### 5.2 2D Pixel Perfect (DOES NOT EXIST)

**com.unity.2d.pixel-perfect was introduced in Unity 2018.2.**

**Manual implementation for 2017.1:**
```csharp
// ✅ UNITY 2017 PIXEL PERFECT CAMERA
public class PixelPerfectCameraManual : MonoBehaviour {
    public int pixelsPerUnit = 16;
    public int referenceHeight = 180;
    private Camera cam;
    
    void Start() {
        cam = GetComponent<Camera>();
        UpdateCameraSize();
    }
    
    void UpdateCameraSize() {
        int screenHeight = Screen.height;
        int scale = Mathf.Max(1, screenHeight / referenceHeight);
        float orthoSize = (float)screenHeight / (scale * pixelsPerUnit * 2f);
        cam.orthographicSize = orthoSize;
    }
}
```

### 5.3 2D Animation Package (DOES NOT EXIST)

**com.unity.2d.animation was introduced in Unity 2018.1.**

**Alternatives for 2017.1:**
- Frame-by-frame animation using `Animation` component with sprite swapping
- Third-party: Spine Runtime (if available for 2017)
- Transform-based animation of sprite parts

### 5.4 SpriteRenderer Changes

```csharp
// ❌ UNITY 6 (Added in 2017.3)
spriteRenderer.spriteSortPoint = SpriteSortPoint.Pivot;

// ✅ UNITY 2017.1 ALTERNATIVE
spriteRenderer.sortingLayerName = "Characters";
spriteRenderer.sortingOrder = 5;
```

---

## 6. UI Systems

### 6.1 uGUI (Stable)

**UnityEngine.UI is stable and works identically in both versions.**

Core components available in 2017.1:
- `Canvas`, `CanvasScaler`, `CanvasGroup`
- `Button`, `Image`, `Text`, `RawImage`
- `InputField`, `Toggle`, `Slider`, `Scrollbar`, `Dropdown`
- `HorizontalLayoutGroup`, `VerticalLayoutGroup`, `GridLayoutGroup`
- `ContentSizeFitter`, `AspectRatioFitter`, `LayoutElement`
- `EventSystem`, `GraphicRaycaster`

### 6.2 TextMesh Pro

**TMP was NOT integrated in Unity 2017** - it was a separate Asset Store package.

If project has vendored TMP in `Assets/Extensions/`:
- Keep it as-is (standalone version works in 2017)
- **Namespace is identical:** `using TMPro;`
- May need to regenerate font assets if they have version conflicts

### 6.3 UI Toolkit (DOES NOT EXIST)

**UI Toolkit (UIElements) was introduced in Unity 2019.1.**

```csharp
// ❌ DELETE (DOES NOT EXIST)
using UnityEngine.UIElements;
VisualElement, UIDocument, etc.
```

---

## 7. Physics2D

### 7.1 Core API (Mostly Compatible)

**Physics2D core API is largely compatible.** These features ARE available in 2017.1:

- `Rigidbody2D` with `bodyType` (Dynamic/Kinematic/Static)
- `Rigidbody2D.simulated`
- `Physics2D.Raycast()` with `ContactFilter2D`
- `CompositeCollider2D` (added in Unity 5.6)
- All standard colliders: `BoxCollider2D`, `CircleCollider2D`, `PolygonCollider2D`, `CapsuleCollider2D`

### 7.2 API Changes

```csharp
// ❌ UNITY 6 (List overload - may not exist in 2017)
int Physics2D.Raycast(origin, direction, filter, List<RaycastHit2D> results, distance);

// ✅ UNITY 2017 EQUIVALENT (Array overload)
RaycastHit2D[] results = new RaycastHit2D[10];
int count = Physics2D.Raycast(origin, direction, filter, results, distance);
```

---

## 8. Animation and Timeline

### 8.1 Animator/Mecanim (Stable)

**Mecanim is stable and works identically in both versions.**

```csharp
// ✅ WORKS IN BOTH VERSIONS
animator.SetFloat("Speed", value);
animator.SetBool("IsRunning", true);
animator.SetTrigger("Jump");
animator.Play("StateName");
animator.CrossFade("StateName", 0.1f);
```

### 8.2 Timeline (AVAILABLE in 2017.1)

**Timeline IS available in Unity 2017.1** - it was a flagship feature of that release.

```csharp
// ✅ WORKS IN UNITY 2017.1
using UnityEngine.Playables;
using UnityEngine.Timeline;

PlayableDirector director;
director.Play();
director.Stop();
director.time = 0;
```

---

## 9. Scripting API Changes

### 9.1 Attributes

```csharp
// ❌ UNITY 6 (Added 2018.3)
[ExecuteAlways]

// ✅ UNITY 2017 EQUIVALENT
[ExecuteInEditMode]
```

```csharp
// ❌ UNITY 6 (Added 2019.3)
[SerializeReference] ICommand command;

// ✅ UNITY 2017 EQUIVALENT
[SerializeField] CommandBase command; // Use ScriptableObject base class
```

```csharp
// ❌ UNITY 6 (C# 7.3)
[field: SerializeField] public int Health { get; private set; }

// ✅ UNITY 2017 EQUIVALENT
[SerializeField] private int _health;
public int Health { get { return _health; } private set { _health = value; } }
```

### 9.2 UnityWebRequest

```csharp
// ❌ UNITY 6
yield return request.SendWebRequest();
if (request.result == UnityWebRequest.Result.Success) { }

// ✅ UNITY 2017 EQUIVALENT
yield return request.Send();
if (!request.isError) { }
```

### 9.3 Scene Management

```csharp
// ❌ REMOVED in Unity 6
Application.LoadLevel("SceneName");
Application.loadedLevel;

// ✅ UNITY 2017 (Use SceneManager - available since 5.3)
using UnityEngine.SceneManagement;
SceneManager.LoadScene("SceneName");
SceneManager.GetActiveScene().buildIndex;
```

### 9.4 OnLevelWasLoaded Replacement

```csharp
// ❌ DEPRECATED
void OnLevelWasLoaded(int level) { }

// ✅ UNITY 2017 EQUIVALENT
void OnEnable() {
    SceneManager.sceneLoaded += OnSceneLoaded;
}
void OnDisable() {
    SceneManager.sceneLoaded -= OnSceneLoaded;
}
void OnSceneLoaded(Scene scene, LoadSceneMode mode) { }
```

### 9.5 Application Events

```csharp
// ❌ UNITY 6
Application.wantsToQuit += () => true;
Application.quitting += OnQuitting;

// ✅ UNITY 2017 EQUIVALENT
void OnApplicationQuit() {
    // Handle quit
}
```

---

## 10. Packages to Remove

### 10.1 Complete Removal Required

| Package | Introduced | Action |
|---------|------------|--------|
| com.unity.render-pipelines.universal | 2019.3 | Remove all URP code |
| com.unity.shadergraph | 2018.1 | Convert to .shader files |
| com.unity.inputsystem | 2018.2 | Convert to legacy Input |
| com.unity.2d.tilemap | 2017.2 | Manual sprite grid |
| com.unity.2d.pixel-perfect | 2018.2 | Custom camera script |
| com.unity.2d.animation | 2018.1 | Frame-based animation |
| com.unity.burst | 2018.1 | Remove [BurstCompile] |
| com.unity.collections | 2018.1 | Use managed arrays |
| com.unity.jobs | 2018.1 | Remove Job System code |
| com.unity.mathematics | 2018.1 | Use UnityEngine.Mathf |
| com.unity.entities | 2018.1 | Use MonoBehaviour |
| com.unity.addressables | 2018.2 | Use Resources.Load |

### 10.2 Code Patterns to Remove

```csharp
// ❌ BURST (Remove)
[BurstCompile]
public struct MyJob : IJob { }

// ❌ NATIVE COLLECTIONS (Remove)
NativeArray<float> data;
NativeList<int> list;

// ❌ JOBS (Remove)
public struct MyJob : IJob { }
public struct MyJob : IJobParallelFor { }
JobHandle handle = job.Schedule();

// ❌ MATHEMATICS (Replace)
float3 position;           // → Vector3 position;
quaternion rotation;       // → Quaternion rotation;
math.sin(x);              // → Mathf.Sin(x);
```

---

## 11. Photon Compatibility

### 11.1 Supported Versions

| Photon Product | Unity 2017.1 Support |
|----------------|---------------------|
| **PUN 1.85** | ✅ YES |
| PUN 2 | ❌ NO (requires 2017.4+) |
| Photon Quantum | ❌ NO (requires 2021+) |

### 11.2 If Using PUN 2

Must downgrade to PUN 1.x and convert APIs:

```csharp
// ❌ PUN 2
PhotonNetwork.ConnectUsingSettings();
photonView.RPC("Method", RpcTarget.All);

// ✅ PUN 1
PhotonNetwork.ConnectUsingSettings("v1.0");
photonView.RPC("Method", PhotonTargets.All);
```

---

## 12. Transformation Rules Summary

### 12.1 Regex Patterns for Automated Conversion

```regex
# String interpolation → string.Format
\$"([^"]*)\{(\w+)\}([^"]*)"
→ string.Format("$1{0}$3", $2)

# Expression-bodied properties
public\s+(\w+)\s+(\w+)\s*=>\s*(.+);
→ public $1 $2 { get { return $3; } }

# Null-conditional operator (simple case)
(\w+)\?\.(\w+)
→ ($1 != null) ? $1.$2 : null

# Target-typed new
(\w+<[^>]+>)\s+(\w+)\s*=\s*new\(\);
→ $1 $2 = new $1();

# out var declarations
out\s+var\s+(\w+)
→ out $1
# (Declare variable on previous line)

# nameof
nameof\((\w+)\)
→ "$1"
```

### 12.2 Namespace Removal Checklist

```csharp
// ❌ REMOVE THESE NAMESPACES
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Entities;
```

### 12.3 Critical File Types to Handle

| Extension | Action |
|-----------|--------|
| `.asmdef` | DELETE |
| `.asmref` | DELETE |
| `.shadergraph` | Convert to .shader |
| `.shadersubgraph` | Convert to .shader |
| `.inputactions` | DELETE, configure Input Manager |
| `manifest.json` | DELETE |
| `packages-lock.json` | DELETE |

### 12.4 Component Replacement

| Unity 6 Component | Unity 2017 Replacement |
|-------------------|------------------------|
| `PlayerInput` | Custom MonoBehaviour with Update() polling |
| `InputSystemUIInputModule` | `StandaloneInputModule` |
| `Tilemap` | Custom SpriteRenderer grid manager |
| `TilemapRenderer` | Multiple SpriteRenderers |
| `PixelPerfectCamera` | Custom camera script |
| `Light2D` | Standard Light or sprites |
| `ShadowCaster2D` | N/A (remove) |

---

## Final Notes

1. **Test incrementally** - Convert one system at a time and verify compilation
2. **Delete Library folder** after major changes to force reimport
3. **C# features are the biggest blocker** - Address these first as they affect all files
4. **Tilemap absence is critical** - Plan manual replacement strategy early
5. **Input system conversion is extensive** - Map all actions to Input Manager axes
6. **Shader conversion is manual** - No automated tools for Shader Graph → .shader

**Minimum recommended approach:**
1. Remove all Packages/ folder and .asmdef files
2. Fix all C# language feature errors (expression-bodied, interpolation, etc.)
3. Remove/replace Input System code
4. Remove/replace URP and Shader Graph
5. Remove/replace Tilemap system
6. Fix remaining API differences
7. Regenerate Library and test

## IMPORTANT

**All sounds MUST be in PCM compression format**
1. When setting properties of the sounds, PCM MUST be selected as the compression format, please keep this in mind when porting.

**What folders are what**
0. YOU MUST READ 'UNITY-6.md' - it contains the Unity 6000 project (in step 1 below) layout to further help port stuff down to Unity 2017
1. 'NSMB-MarioVsLuigi/' is the original Unity 6000 project that we are trying to downgrade to Unity 2017.1.2p3. ALWAYS INSPECT CODE / SCRIPTS HERE BEFORE DOWNGRADING SO WE CAN ACHIEVE AS MUCH PARITY AS POSSIBLE. TRY TO KEEP AS MUCH CODE AS CLOSE AS POSSIBLE TO ORIGINAL WHILE KEEPING COMPATIBILITY WITH THE OLDER C# 4.0 LANGUAGE (Unity 2017.1.2p3 only supports C# 4.0)
2. 'MvLR-wiiu/' is the directory containing the Unity 2017.1.2p3 project that we are building now. When you want to port a feature from the original Unity 6000 project, YOU MUST look through 'UNITY-6.md' to find scripts and associated code related to the feature being requested, you MUST try your best to reimplement the code with the fewest needed changes to get it working with the old C# 4.0 limitations.