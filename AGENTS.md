# AGENTS.md — Unity 6 → Unity 2017.1.2p3 Downgrade

## CRITICAL: Read This First

**Project Goal:** Port "New Super Mario Bros DS: Mario VS Luigi" remaster from Unity 6000 to Unity 2017.1.2p3 for Wii U homebrew. Maintain maximum gameplay parity with the original.

### Folder Structure
| Path | Description |
|------|-------------|
| `NSMB-MarioVsLuigi/` | **SOURCE** — Original Unity 6000 project. |
| `MvLR-wiiu/` | **TARGET** — Unity 2017.1.2p3 project you're building. |

### Workflow
1. Find the relevant code in `NSMB-MarioVsLuigi/`
2. Port to `MvLR-wiiu/` with minimal C# 4.0 changes
3. Preserve original logic as closely as possible

### When to Consult UNITY-6.md
- **Starting a new system port** (input, networking, UI, etc.)—check the Feature-to-Code Map
- **Can't locate a file after 2-3 searches**—check Script Responsibilities section
- **Need to understand dependencies**—check Porting Hotspots for API usage locations

Don't read the entire document for small fixes or single-file changes.

---

## Language Constraints (C# 4.0)

Unity 2017.1.2p3 uses **.NET 3.5 / C# 4.0**. Many modern C# features will not compile.

### Quick Reference: What Works

| Feature | Works? | Alternative |
|---------|--------|-------------|
| `var` keyword | ✅ | — |
| LINQ | ✅ | — |
| Lambdas | ✅ | — |
| Generics | ✅ | — |
| `??` null-coalescing | ✅ | — |
| `async/await` | ❌ | `IEnumerator` + `StartCoroutine()` |
| `$"string {x}"` | ❌ | `string.Format("string {0}", x)` |
| `?.` null-conditional | ❌ | `if (x != null) x.Method()` |
| `nameof(x)` | ❌ | `"x"` (literal string) |
| `int X => _x;` | ❌ | `int X { get { return _x; } }` |
| `if (obj is Type t)` | ❌ | `var t = obj as Type; if (t != null)` |
| `(int a, int b)` tuples | ❌ | `out` params or custom struct |
| `out var x` | ❌ | Declare `x` on previous line |
| `new()` target-typed | ❌ | `new List<int>()` (explicit type) |
| Local functions | ❌ | `Func<>` delegate or private method |
| `switch` expressions | ❌ | Traditional `switch` statement |
| `using var` declaration | ❌ | `using (var x = ...) { }` block |
| Auto-property init `= 100` | ❌ | Backing field + constructor |

### Conversion Examples

```csharp
// ❌ String interpolation
string msg = $"Player {name} scored {score}";
// ✅ 
string msg = string.Format("Player {0} scored {1}", name, score);

// ❌ Null-conditional
var health = player?.stats?.health;
// ✅ 
int? health = null;
if (player != null && player.stats != null) 
    health = player.stats.health;

// ❌ Expression-bodied
public int Health => _health;
public void Die() => Destroy(gameObject);
// ✅ 
public int Health { get { return _health; } }
public void Die() { Destroy(gameObject); }

// ❌ Pattern matching
if (obj is Player player) { player.TakeDamage(10); }
// ✅ 
Player player = obj as Player;
if (player != null) { player.TakeDamage(10); }

// ❌ async/await
async Task LoadAsync() {
    await Task.Delay(1000);
    Debug.Log("Done");
}
// ✅ 
IEnumerator LoadAsync() {
    yield return new WaitForSeconds(1f);
    Debug.Log("Done");
}
// Call with: StartCoroutine(LoadAsync());

// ❌ out var
if (int.TryParse(input, out var result)) { }
// ✅ 
int result;
if (int.TryParse(input, out result)) { }

// ❌ Target-typed new
List<int> list = new();
// ✅ 
List<int> list = new List<int>();
```

---

## Systems That Don't Exist in 2017.1

### Input System → Legacy Input

The new Input System (`com.unity.inputsystem`) doesn't exist. Use `UnityEngine.Input`.

```csharp
// ❌ New Input System
using UnityEngine.InputSystem;
public void OnMove(InputValue value) {
    Vector2 move = value.Get<Vector2>();
}

// ✅ Legacy Input (polling in Update)
void Update() {
    float h = Input.GetAxis("Horizontal");
    float v = Input.GetAxis("Vertical");
    Vector2 move = new Vector2(h, v);
    
    if (Input.GetButtonDown("Jump")) Jump();
    if (Input.GetKeyDown(KeyCode.Space)) Jump();
}
```

**Delete:** All `*.inputactions` files, `PlayerInput` components, `InputSystemUIInputModule`  
**Replace with:** `StandaloneInputModule` for UI

### Tilemap → Manual Sprites

Tilemap was added in Unity 2017.2. **Not available in 2017.1.**

```csharp
// ❌ Does not exist
using UnityEngine.Tilemaps;
Tilemap map; TilemapRenderer renderer; Tile tile;

// ✅ Alternative: SpriteRenderer grid
public class TileGrid : MonoBehaviour {
    public Sprite[] sprites;
    public int width, height;
    public float tileSize = 1f;
    private SpriteRenderer[,] tiles;
    
    void CreateGrid() {
        tiles = new SpriteRenderer[width, height];
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                var go = new GameObject("Tile_" + x + "_" + y);
                go.transform.parent = transform;
                go.transform.localPosition = new Vector3(x * tileSize, y * tileSize, 0);
                tiles[x, y] = go.AddComponent<SpriteRenderer>();
            }
        }
    }
}
```

### URP → Built-in Pipeline

URP/SRP don't exist. Use built-in shaders.

| URP Shader | Built-in Replacement |
|------------|---------------------|
| `Universal Render Pipeline/Lit` | `Standard` |
| `Universal Render Pipeline/Unlit` | `Unlit/Texture` |
| `Universal Render Pipeline/Simple Lit` | `Mobile/Diffuse` |

**Delete:** All `*.shadergraph` files, URP assets  
**Remove namespaces:** `UnityEngine.Rendering.Universal`, `UnityEngine.Rendering`

### Pixel Perfect Camera → Manual

```csharp
// ✅ Manual pixel-perfect camera
public class PixelPerfectCamera : MonoBehaviour {
    public int pixelsPerUnit = 16;
    public int referenceHeight = 180;
    
    void Start() {
        var cam = GetComponent<Camera>();
        int scale = Mathf.Max(1, Screen.height / referenceHeight);
        cam.orthographicSize = (float)Screen.height / (scale * pixelsPerUnit * 2f);
    }
}
```

### 2D Animation Package → Frame Animation

Skeletal 2D animation doesn't exist. Use sprite-swap frame animations or third-party (Spine).

---

## Files to Delete

```
DELETE: Packages/                     (entire folder - no Package Manager)
DELETE: *.asmdef                      (Assembly definitions - added in 2017.3)
DELETE: *.asmref
DELETE: *.inputactions
DELETE: *.shadergraph
DELETE: *.shadersubgraph
DELETE: ProjectSettings/PackageManagerSettings.asset
DELETE: ProjectSettings/URPProjectSettings.asset
DELETE: ProjectSettings/BurstAotSettings_*.json
DELETE: Library/                      (regenerates on open)
```

---

## Namespaces to Remove

If you see these imports, the code needs conversion:

```csharp
// ❌ REMOVE — These don't exist in 2017.1
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

---

## API Changes

### UnityWebRequest
```csharp
// ❌ Unity 6
yield return request.SendWebRequest();
if (request.result == UnityWebRequest.Result.Success) { }

// ✅ Unity 2017
yield return request.Send();
if (!request.isError) { }
```

### Attributes
```csharp
// ❌ [ExecuteAlways]     →  ✅ [ExecuteInEditMode]
// ❌ [SerializeReference] →  ✅ [SerializeField] with base class
// ❌ [field: SerializeField] public int X { get; set; }
// ✅ 
[SerializeField] private int _x;
public int X { get { return _x; } set { _x = value; } }
```

---

## Audio: PCM Compression Required

**All audio assets MUST use PCM compression format.** When importing/configuring sounds, always set compression to PCM.

---

## Photon Compatibility

| Product | 2017.1 Support |
|---------|----------------|
| PUN 1.x | ✅ Yes |
| PUN 2 | ❌ No (requires 2017.4+) |
| Quantum | ❌ No (requires 2021+) |

If the source uses PUN 2, downgrade to PUN 1:
```csharp
// ❌ PUN 2
PhotonNetwork.ConnectUsingSettings();
photonView.RPC("Method", RpcTarget.All);

// ✅ PUN 1
PhotonNetwork.ConnectUsingSettings("v1.0");
photonView.RPC("Method", PhotonTargets.All);
```

---

## What Works Without Changes

These systems are stable across both versions:

- **uGUI** (`UnityEngine.UI`): Canvas, Button, Image, Text, InputField, layouts
- **TextMesh Pro**: If vendored in `Assets/` (not via Package Manager)
- **Timeline**: Built into 2017.1 (no Signals/Markers though)
- **Mecanim/Animator**: SetFloat, SetBool, SetTrigger, CrossFade all work
- **Physics2D core**: Rigidbody2D, all colliders, raycasts, bodyType enum
- **SceneManager**: LoadScene, GetActiveScene (not Application.LoadLevel)

---

## Checklist: Before Submitting Changes

- [ ] No C# 6+ syntax (`$""`, `?.`, `=>` properties, pattern matching)
- [ ] No `using` statements for removed namespaces
- [ ] No Package Manager references
- [ ] No `.asmdef` files
- [ ] Audio set to PCM compression
- [ ] Input uses `UnityEngine.Input`, not InputSystem
- [ ] Shaders use built-in, not URP/Shader Graph
- [ ] Tested that code compiles (no red errors in concept)

---

## Self-Improvement

This file is not static. Update it when you learn something that would help future sessions.

### When to Update This File

**Update the code map when:**
- You discover a file location that took multiple searches to find
- A path listed here is wrong or outdated
- You find an important file/folder not mentioned

**Add to Local Norms when:**
- The user corrects you ("don't do X", "always do Y")
- You discover a repo-specific constraint the hard way
- A workaround is needed that isn't obvious

**Add to Gotchas when:**
- A Unity 2017 API behaves unexpectedly
- A conversion pattern has a non-obvious edge case
- Something compiles but fails at runtime

### How to Update

1. Make the edit directly to this AGENTS.md file
2. Keep entries concise—one line if possible
3. Put corrections near the relevant section, or in Local Norms if general
4. Don't ask permission; just update when you learn something useful

### Local Norms

*Add repo-specific rules and corrections here as they're discovered.*

- (none yet)

### Gotchas

*Add non-obvious problems and their solutions here.*

- (none yet)