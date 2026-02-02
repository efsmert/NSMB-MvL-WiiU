# NSMB-MvL-WiiU Port Map (Unity 6 → Unity 2017.1.2p3)

This repo contains two Unity projects:

- `NSMB-MarioVsLuigi/` (Unity `6000.0.48f1`): original game project (Quantum + URP + New Input System + Tilemap + ShaderGraph).
- `MvLR-wiiu/` (Unity `2017.1.2p3`): Wii U / Unity 2017 target project (this is where we rebuild/port).

Unity 2017.1 constraints (hard):

- No Package Manager, no `.asmdef/.asmref`
- No URP / SRP, no Shader Graph
- No New Input System (`*.inputactions`, `UnityEngine.InputSystem`)
- No Tilemap system (`UnityEngine.Tilemaps` is 2017.2+)
- C# 4.0 / .NET 3.5 (no `async/await`, tuples, pattern matching, `??=`, etc.)

## Why `NSMB-MarioVsLuigi/` won’t open in Unity 2017.1

Even before script compile errors, Unity 2017.1 can crash on modern serialized asset formats (notably nested prefab data and newer scene/prefab YAML). So the “port” must be a **re-implementation** that uses the original project as a **spec/reference source**.

## Original project architecture (`NSMB-MarioVsLuigi/Assets`)

### Major subsystems (folders)

- `Scripts/Quantum/`:
  - Quantum view components, scene loading (`MvLSceneLoader`), input collection (`InputCollector`), stage context (`StageContext`).
- `Scripts/Entity/`:
  - View/Animator layers for simulation entities:
    - `Player/` (`MarioPlayerAnimator`, `GoldBlockAnimator`)
    - `Enemy/` (`GoombaAnimator`, `KoopaAnimator`, `BooAnimator`, `BobombAnimator`, `BulletBillAnimator`, `PiranhaPlantAnimator`)
    - `Powerup/` (`CoinItemAnimator`, `ProjectileAnimator`)
    - `World Elements/` (`BlockBumpAnimator`, `CoinAnimator`, `SpinnerAnimator`, `StarCoinAnimator`, etc.)
- `Scripts/UI/`:
  - Main menu, room UI, pause/options, replay UI, in-game HUD.
  - Notable: `UI/Game/NdsResolutionHandler.cs` (NDS-style rendertexture scaling).
- `Scripts/Sound/`:
  - `MusicManager`, `SfxManager`, looping sound players, preloaders.
  - Sound IDs are driven by `SoundEffect` in `Assets/QuantumUser/Simulation/NSMB/Enums/SoundEffect.cs` via `Resources.Load("Sound/...")`.
- `Scripts/Tiles/`:
  - Tilemap runtime animation and rules (not portable to 2017.1 due to missing Tilemap + Quantum).
- `Scripts/Networking/`:
  - Photon Realtime + Quantum networking (uses modern C#; deferred for now).
- `Resources/`:
  - `Sound/` (AudioClips), `Prefabs/`, `Tilemaps/`, `Data/`, `Fonts/`.

### Game content entrypoints

- `Scripts/GlobalController.cs`: main singleton root (settings, audio mixer, UI linking, scene callbacks).
- Scenes:
  - `Assets/Scenes/MainMenu.unity`
  - `Assets/Scenes/Intro.unity`
  - `Assets/Scenes/Levels/*`

## Target project approach (`MvLR-wiiu/`)

We rebuild the game in Unity 2017.1 with:

- Built-in rendering pipeline (`SpriteRenderer`, `Sprite/Default` or custom CG shaders).
- Legacy input (`UnityEngine.Input` / Input Manager).
- No Tilemap: manual sprite-grid + colliders (or handcrafted level prefabs).
- Determinism/networking deferred; focus on local gameplay parity first.

### Current baseline systems (already in `MvLR-wiiu/Assets/NSMB`)

- Bootstrap: `Assets/NSMB/Bootstrap/WiiUBootstrap.cs`
- Player movement: `Assets/NSMB/Player/PlayerMotor2D.cs`
- Camera follow: `Assets/NSMB/Camera/CameraFollow2D.cs`
- Basic pickups/blocks:
  - `Assets/NSMB/Items/CoinPickup.cs`
  - `Assets/NSMB/Blocks/BlockBump.cs`, `BlockHitDetector.cs`, `SpawnMushroomOnBump.cs`
- Audio:
  - `Assets/NSMB/Audio/AudioManager.cs`
  - Editor import enforcement: `Assets/Editor/EnforcePcmAudioImport.cs` (**PCM required** per `AGENTS.md`).

## Highest priority port tasks (to reach visual parity)

1. Sprite import parity:
   - Apply Point filtering, disable mipmaps, preserve sprite slicing/pivots.
   - Prefer automated import rules driven from the original project’s `.meta` where possible.
2. Camera / presentation:
   - Pixel-perfect scaling + aspect handling (NDS mode-like behavior).
3. Level system (no Tilemap):
   - Implement a level format compatible with Unity 2017.1 (sprite-grid + colliders).
4. Core gameplay loop:
   - Game states, HUD, scoring, coins, powerups, enemies, win conditions.
5. Menus/options:
   - Main menu, character/team selection, stage select, pause menu.
6. Networking (later):
   - Decide between Photon PUN 1.x or a custom solution compatible with 2017.1.

