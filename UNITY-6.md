# NSMB-MarioVsLuigi (Unity 6 / Unity 6000) - Project Structure Map

This document describes the **original Unity 6** project located at `NSMB-MarioVsLuigi/` (the source project to be downgraded/reimplemented elsewhere).

## Quick Facts

- **Unity editor version:** `6000.0.48f1` (from `NSMB-MarioVsLuigi/ProjectSettings/ProjectVersion.txt`)
- **Render pipeline:** **URP** (`com.unity.render-pipelines.universal = 17.0.4`)
  - Active pipeline asset: `NSMB-MarioVsLuigi/Assets/PipelineAsset.asset`
  - URP global settings: `NSMB-MarioVsLuigi/Assets/UniversalRenderPipelineGlobalSettings.asset`
- **Input:** **New Input System** (`com.unity.inputsystem = 1.14.0`)
  - InputActions asset: `NSMB-MarioVsLuigi/Assets/Controls.inputactions`
  - Generated C# wrapper: `NSMB-MarioVsLuigi/Assets/Controls.cs`
- **Networking / deterministic sim:** Photon stack (Photon Realtime + **Photon Quantum**)
- **Rough size indicators (file counts):**
  - C#: **581** files
  - Prefabs: **127**
  - Scenes: **19**
  - Shader Graphs: **19** (`.shadergraph`) + **1** (`.shadersubgraph`)
  - Tile assets (Resources Tilemaps): **561** (`.asset`)
  - Audio: **163** (`.ogg`) + **3** (`.wav`)

## Top-Level Layout (`NSMB-MarioVsLuigi/`)

- `.git/`, `.github/` - VCS + GitHub config (issues/workflows, etc.)
- `Assets/` - All Unity content (game code, prefabs, sprites, scenes, URP assets, Photon/Quantum SDK content)
- `Packages/` - UPM dependencies
- `ProjectSettings/` - Unity project settings (includes URP, ShaderGraph, Burst, Input, etc.)
- `Quantum.Dotnet/` - Separate .NET solution for Quantum runner/simulation tooling (see below)
- Root `.sln` / `.csproj` files - Unity-generated/SDK project files for IDE tooling
- `README.md`, `LINKS.md` - project description + distribution links

## Packages / Dependencies (`NSMB-MarioVsLuigi/Packages/manifest.json`)

Key dependencies to be aware of:

- Rendering: `com.unity.render-pipelines.universal = 17.0.4`
- Input: `com.unity.inputsystem = 1.14.0`
- 2D: `com.unity.2d.tilemap`, `com.unity.2d.tilemap.extras`, `com.unity.2d.animation`, `com.unity.2d.pixel-perfect`
- Performance: `com.unity.burst = 1.8.24`
- Timeline: `com.unity.timeline = 1.8.8`
- JSON: `com.unity.nuget.newtonsoft-json = 3.2.1`
- Misc (Git URL deps): `com.dbrizov.naughtyattributes`, `com.jimmycushnie.jimmysunityutilities`, `com.jimmycushnie.noisynodes`

## Project Settings (`NSMB-MarioVsLuigi/ProjectSettings/`)

Notable settings files present:

- `GraphicsSettings.asset` - sets `m_CustomRenderPipeline` (URP pipeline asset) and URP global settings mapping
- `URPProjectSettings.asset`, `ShaderGraphSettings.asset` - URP/ShaderGraph related
- `InputManager.asset` - legacy input manager still exists (but project primarily uses Input System)
- `BurstAotSettings_*.json`, `CommonBurstAotSettings.json` - Burst configuration (multi-platform)
- `MemorySettings.asset`, `PackageManagerSettings.asset`, `VersionControlSettings.asset` - Unity 6-era settings
- `EditorBuildSettings.asset` - scenes included in builds (see "Scenes")

## Assets Overview (`NSMB-MarioVsLuigi/Assets/`)

### Scenes (`Assets/Scenes/`)

- Top-level:
  - `Assets/Scenes/Intro.unity`
  - `Assets/Scenes/MainMenu.unity`
- Levels:
  - `Assets/Scenes/Levels/DefaultGrassLevel.unity`
  - `Assets/Scenes/Levels/DefaultBrickLevel.unity`
  - `Assets/Scenes/Levels/DefaultCastle.unity`
  - `Assets/Scenes/Levels/DefaultPipes.unity`
  - `Assets/Scenes/Levels/DefaultSnow.unity`
  - Custom: `CustomJungle`, `CustomSky`, `CustomVolcano`, `CustomBonus`, `CustomDesert`, `CustomBeach`, `CustomGhost`
  - Unofficial/debug: `Assets/Scenes/Levels/unofficial/*`
- Template:
  - `Assets/Scenes/Template/LevelTemplate.unity`

Build scenes listed in `ProjectSettings/EditorBuildSettings.asset` (order matters):

1. `Assets/Scenes/Intro.unity`
2. `Assets/Scenes/MainMenu.unity`
3. `Assets/Scenes/Levels/DefaultGrassLevel.unity`
4. `Assets/Scenes/Levels/DefaultBrickLevel.unity`
5. `Assets/Scenes/Levels/DefaultCastle.unity`
6. `Assets/Scenes/Levels/DefaultPipes.unity`
7. `Assets/Scenes/Levels/DefaultSnow.unity`
8. `Assets/Scenes/Levels/CustomJungle.unity`
9. `Assets/Scenes/Levels/CustomSky.unity`
10. `Assets/Scenes/Levels/CustomVolcano.unity`
11. `Assets/Scenes/Levels/CustomBonus.unity`
12. `Assets/Scenes/Levels/CustomDesert.unity`
13. `Assets/Scenes/Levels/CustomBeach.unity`
14. `Assets/Scenes/Levels/CustomGhost.unity`

### Common Content Folders (non-code)

- `Assets/Animations/` - animation clips/controllers grouped by `Enemy/`, `Menu/`, `Player/`, `Powerup/`, `UI/`, `WorldElements/`
- `Assets/Materials/` - materials split into `2d/` and `3d/`
- `Assets/Models/` - 3D models grouped by item/character themes (e.g. `Players/`, `HammerModels/`, `blue_shell/`, etc.)
- `Assets/Resources/Fonts/` - font assets used by UI/TMP
- `Assets/Gizmos/` - editor gizmo textures/definitions (Unity editor only)

### Game Code (`Assets/Scripts/`)

Primary gameplay/UI code is organized under `Assets/Scripts/` and largely uses the `NSMB.*` namespace.

Top-level folders (with rough C# counts):

- `Assets/Scripts/UI/` (~141) - biggest area: menus, HUD, prompts, options, localization UI, replay UI, scoreboard, etc.
- `Assets/Scripts/Entity/` (~27) - entities (player/enemies/world elements), gameplay logic
- `Assets/Scripts/Utilities/` (~19) - shared helpers/components/extensions
- `Assets/Scripts/Sound/` (~7) - sound system glue (loads/plays `SoundData` etc.)
- `Assets/Scripts/Particles/` (~6)
- `Assets/Scripts/Quantum/` (~6) - Unity<->Quantum integration glue (input collection etc.)
- `Assets/Scripts/Replay/` (~5)
- `Assets/Scripts/Background/` (~4)
- `Assets/Scripts/Editor/` (~4) - editor tooling scripts
- `Assets/Scripts/Networking/` (~4) - higher-level networking glue (Photon/rooms/etc.)
- `Assets/Scripts/Camera/` (~3)
- `Assets/Scripts/Chat/` (~2)
- `Assets/Scripts/Tiles/` (~2) - tilemap/tile helpers

Namespace families observed (non-exhaustive):

- `NSMB.*`
  - `NSMB.Entities.*` (Player/Enemies/World/CoinItems)
  - `NSMB.UI.*` (Intro/MainMenu/Pause/Game/Options/Translation/RTL/etc.)
  - `NSMB.Quantum.*`, `NSMB.Networking.*`, `NSMB.Sound.*`, `NSMB.Tiles.*`

### Deterministic Simulation + Photon Quantum (`Assets/Photon/` + `Assets/QuantumUser/`)

This project contains a Photon + Quantum SDK layout plus "game-specific Quantum user code".

- `Assets/Photon/`
  - `PhotonLibs/` - Photon client libs (incl. `websocket-sharp.dll`, `PhotonClient.dll`, etc.)
  - `PhotonRealtime/` - Photon Realtime C# sources + asmdef
  - `Quantum/` - Quantum runtime/editor tooling, codegen, assemblies (LiteNetLib, Quantum.Engine, etc.)
- `Assets/QuantumUser/` - game-specific Quantum layer
  - `Editor/` - Quantum editor integration + codegen hooks
  - `Resources/`
    - `AssetObjects/` - ScriptableObject-style definitions grouped by domain:
      - `Characters/`, `Gamemodes/`, `Items/`, `Maps/`, `Music/`, `Palettes/`, `Projectile/`, `Teams/`, `World/`
    - `EntityPrototypes/` - prefabs used as Quantum entity prototypes:
      - `Coins/`, `Enemy/`, `Items/`, `Player/`, `Projectile/`, `World/`
  - `Simulation/`
    - `Generated/` - generated Quantum simulation code
    - `Helpers/` - helpers for sim integration
    - `NSMB/` - game simulation logic grouped by systems:
      - `Camera/`, `Debug/`, `Entity/`, `Enums/`, `Gamemode/`, `Logic/`, `Map/`, `Modding/`, `Room/`, `Utils/`
  - `View/Generated/` - generated Quantum view layer glue

Quantum-specific asset types present:

- `.qtn` (~40) - Quantum schema/networked state definitions
- `.qprototype` (~43) - Quantum prototypes

### Input System (`Assets/Controls.inputactions`)

Action maps and actions (summary from `Controls.inputactions`):

- Map: **Player** (5 actions)
  - `Movement` (Value), `Jump`, `Sprint`, `Powerup Action`, `Reserve Item`
- Map: **UI** (13 actions)
  - `Navigate`, `Submit`, `Cancel`, `Pause`, `Scoreboard`, `Next`, `Previous`
  - plus pointer-style actions prefixed with `!` (`!Point`, `!Click`, `!ScrollWheel`, etc.)
  - plus `!SpectatePlayerByIndex`
- Map: **Replay** (3 actions): `ZoomIn`, `ZoomOut`, `Reset`
- Map: **Debug** (2 actions): `FPS Monitor`, `Toggle HUD`

### Tilemaps / Level Tiles (`Assets/Resources/Tilemaps/`)

This is a large content area with Tilemap assets stored under `Resources`:

- `Assets/Resources/Tilemaps/Palettes/` - palette assets
- `Assets/Resources/Tilemaps/Tiles/` - tile assets grouped by tilesets (e.g. `BeachBlue`, `Basic`, `Animated`, etc.)

File types inside `Assets/Resources/Tilemaps/`:

- `.asset` (561) - tiles/palettes/ScriptableObjects used by Tilemap tooling
- `.prefab` (13) - prefabs used for tilemap-related content

### Sprites (`Assets/Sprites/`)

- `Assets/Sprites/Atlases/` - sprite atlas assets (includes `.spriteatlas`)
- `Assets/Sprites/Level Backgrounds/`
- `Assets/Sprites/Particle/`
- `Assets/Sprites/UI/` - includes a `.svg` logo asset used by README badge/image

### Shaders (`Assets/Shaders/` + TMP shader graphs)

- `Assets/Shaders/2D/`, `Assets/Shaders/3D/`, `Assets/Shaders/UI/`
- Primarily **Shader Graph**:
  - `.shadergraph` (15 within `Assets/Shaders/` + additional TMP shadergraphs under `Assets/Extensions/TextMesh Pro/Shaders/`)
  - `.shadersubgraph` (1)
  - A few `.shader` (3) exist (built-in style shader files)

### Audio (`Assets/Resources/Sound/`)

- Audio appears under `Assets/Resources/Sound/` with subfolders like `character/`, `enemy/`, `music/`, etc.
- Formats present:
  - Mostly `.ogg` (163 files)
  - A small number of `.wav` (3 files)

### ScriptableObjects (`Assets/ScriptableObjects/`)

- `Assets/ScriptableObjects/SoundData/` - sound configuration assets (12)
- `Assets/ScriptableObjects/TextValidators/` - text/format validators (6)

### StreamingAssets (`Assets/StreamingAssets/`)

- `Assets/StreamingAssets/lang/` - translation JSON files (example present: `pirate.json`)
- `Assets/StreamingAssets/replays/` - replay folders (`favorite/`, `saved/`, `temp/`) as on-disk storage locations

### Extensions / Third-Party Content (`Assets/Extensions/`)

- `ArabicSupport/` - RTL/Arabic text support
- `Graphy - Ultimate Stats Monitor/` - Graphy runtime/editor (has asmdefs)
- `StandaloneFileBrowser/` - file picker with platform DLLs
- `TextMesh Pro/` - TMP content vendored under Extensions (includes URP/HDRP shadergraphs)
- `WebGLTemplates/` - templates for WebGL builds

### Plugins (`Assets/Plugins/` + managed DLLs)

Native plugin binaries (Discord Game SDK):

- `Assets/Plugins/x86*/discord_game_sdk.*` and `Assets/Plugins/aarch64/discord_game_sdk.dylib`

Managed DLL samples (outside `Assets/Plugins/`):

- `Assets/Photon/PhotonLibs/**/PhotonClient.dll`
- `Assets/Photon/Quantum/Assemblies/*.dll` (Quantum.Engine/Deterministic/Log/LiteNetLib, etc.)
- `Assets/Extensions/StandaloneFileBrowser/Plugins/*.dll` (Mono.Posix, System.Windows.Forms, etc.)

## Assembly Definition Files (Unity compilation units)

Unity 6 project uses asmdefs/asmrefs:

- `.asmdef` (8) - e.g. Graphy, Photon Realtime, Quantum runtime/editor, Photon WebSocket
- `.asmref` (4) - QuantumUser references into Quantum assemblies

## Porting Hotspots (Modern APIs used in code)

These are **where** modern Unity 6 systems are referenced in C# (useful for a downgrade/rewrite pass):

- New Input System (`UnityEngine.InputSystem`) is widely referenced (example files):
  - `Assets/Controls.cs`, `Assets/Scripts/Quantum/InputCollector.cs`, `Assets/Scripts/UI/*` (rebind UI, pause menu, etc.)
- Tilemap API (`UnityEngine.Tilemaps`) appears in:
  - `Assets/QuantumUser/Simulation/NSMB/Map/StageTile.cs`
  - `Assets/Scripts/Tiles/SiblingRuleTile.cs`
  - `Assets/Scripts/Tiles/TilemapAnimator.cs`
  - `Assets/Scripts/Quantum/Editor/VersusStageBaker.cs`
  - `Assets/Scripts/Entity/World Elements/BlockBumpAnimator.cs`
- URP namespaces (`UnityEngine.Rendering.Universal`) appear in:
  - `Assets/Scripts/GlobalController.cs`
  - `Assets/Scripts/Entity/Powerup/ProjectileAnimator.cs`
- Some `Unity.Collections` / `Unity.Jobs` usage exists via Quantum runtime/editor integration:
  - `Assets/Photon/Quantum/Runtime/QuantumTaskRunnerJobs.cs` (Jobs + Collections)
  - plus a handful of files in `Assets/QuantumUser/Simulation/*`

## Quantum.Dotnet (`NSMB-MarioVsLuigi/Quantum.Dotnet/`)

Separate .NET solution (outside Unity `Assets/`) used by Quantum tooling:

- `Quantum.Dotnet.sln`
- `Lib/Debug`, `Lib/Release`
- `Quantum.Runner.Dotnet/`
- `Quantum.Simulation.Dotnet/` (contains `bin/`, `obj/`)

## Feature-to-Code Map (Porting Guide)

Use this section to quickly locate the **original Unity 6 implementation** of a feature. Paths are relative to `NSMB-MarioVsLuigi/`.

### Bootstrap / Global state
- App/global orchestration: `Assets/Scripts/GlobalController.cs`
- Player settings/options storage + access: `Assets/Scripts/Settings.cs`
- Discord integration glue: `Assets/Scripts/DiscordController.cs` + native plugin binaries under `Assets/Plugins/`
- Rumble: `Assets/Scripts/RumbleManager.cs`
- Build metadata + editor build steps: `Assets/Scripts/BuildInfo.cs`, `Assets/Scripts/Editor/BuildInfoProcessor.cs`, `Assets/Scripts/Editor/BuildScript.cs`, `Assets/Scripts/Editor/WebGLPreprocessBuild.cs`

### Scenes / Level loading
- Scene loading into gameplay/simulation: `Assets/Scripts/Quantum/MvLSceneLoader.cs`
- Stage context bridging Unity scene to sim: `Assets/Scripts/Quantum/StageContext.cs`
- Entity view pooling (Unity presentation): `Assets/Scripts/Quantum/MarioVsLuigiEntityViewPool.cs`

### Input (New Input System + rebind UI)
- InputActions + generated wrapper: `Assets/Controls.inputactions`, `Assets/Controls.cs`
- Input collection -> Quantum: `Assets/Scripts/Quantum/InputCollector.cs`
- UI rebinding / EventSystem glue: `Assets/Scripts/UI/EventSystemRebindableControls.cs`
- Options rebind flow (UI): `Assets/Scripts/UI/Options/CompositeRebindPrompt.cs`, `Assets/Scripts/UI/Options/RebindCompositeOption.cs`, `Assets/Scripts/UI/Options/RebindPauseOption*.cs`

### Networking / Rooms / Updates
- High-level networking glue: `Assets/Scripts/Networking/NetworkHandler.cs`
- Auth + certificates: `Assets/Scripts/Networking/AuthenticationHandler.cs`, `Assets/Scripts/Networking/MvLCertificateHandler.cs`
- Update check: `Assets/Scripts/Networking/UpdateChecker.cs`
- Photon Realtime SDK sources: `Assets/Photon/PhotonRealtime/`
- Photon Quantum runtime/editor: `Assets/Photon/Quantum/`
- Game lobby/room commands (Quantum simulation): `Assets/QuantumUser/Simulation/NSMB/Room/*`

### Deterministic gameplay (Quantum simulation - authoritative logic)
- Core game rules + loop: `Assets/QuantumUser/Simulation/NSMB/Logic/GameLogicSystem.cs`, `Assets/QuantumUser/Simulation/NSMB/Logic/GameRules.cs`
- Gamemodes: `Assets/QuantumUser/Simulation/NSMB/Gamemode/*`
- Player: `Assets/QuantumUser/Simulation/NSMB/Entity/Player/MarioPlayer.cs`, `Assets/QuantumUser/Simulation/NSMB/Entity/Player/MarioPlayerSystem.cs`
- Enemies: `Assets/QuantumUser/Simulation/NSMB/Entity/Enemy/*/*System.cs`
- Projectiles: `Assets/QuantumUser/Simulation/NSMB/Entity/Projectile/*`
- Powerups: `Assets/QuantumUser/Simulation/NSMB/Entity/Powerup/PowerupSystem.cs`
- Tiles/stage interactions (simulation side): `Assets/QuantumUser/Simulation/NSMB/Map/*` and `Assets/QuantumUser/Simulation/NSMB/Map/Tile/*`

### Unity presentation (Animators / Views)
- Player visuals: `Assets/Scripts/Entity/Player/MarioPlayerAnimator.cs`
- Enemy visuals: `Assets/Scripts/Entity/Enemy/*Animator.cs`
- World visuals: `Assets/Scripts/Entity/World Elements/*Animator.cs`
- Powerup/projectile visuals: `Assets/Scripts/Entity/Powerup/*Animator.cs`
- Generic view glue: `Assets/Scripts/Entity/WrappingEntityView.cs`, `Assets/Scripts/Entity/MaterialDuplicator.cs`, `Assets/Scripts/Entity/FreezeZ.cs`

### UI (menus, HUD, in-game overlays)
- Main menu root: `Assets/Scripts/UI/MainMenu/MainMenuCanvas.cs`, `Assets/Scripts/UI/MainMenu/MenuGameStateHandler.cs`
- Room list: `Assets/Scripts/UI/MainMenu/RoomList/*`
- In-room menu: `Assets/Scripts/UI/MainMenu/InRoom/*`
- Prompts (join/create/settings/update/replay actions): `Assets/Scripts/UI/MainMenu/Prompts/*`
- Intro/loading: `Assets/Scripts/UI/Intro/IntroManager.cs`, `Assets/Scripts/UI/Loading/*`
- Pause: `Assets/Scripts/UI/Pause/PauseMenuManager.cs`
- In-game HUD/root: `Assets/Scripts/UI/Game/MasterCanvas.cs`, `Assets/Scripts/UI/Game/UIUpdater.cs`
- Scoreboard: `Assets/Scripts/UI/Game/Scoreboard/*`
- Results: `Assets/Scripts/UI/Game/Results/*`
- Replay UI: `Assets/Scripts/UI/Game/Replay/*`

### Audio / Music
- Music: `Assets/Scripts/Sound/MusicManager.cs`, `Assets/Scripts/Sound/LoopingMusicPlayer.cs`
- SFX: `Assets/Scripts/Sound/SfxManager.cs`, `Assets/Scripts/Sound/LoopingSoundPlayer.cs`
- Sound data container type: `Assets/Scripts/Sound/SoundData.cs`
- Simulation sound enum IDs: `Assets/QuantumUser/Simulation/NSMB/Enums/SoundEffect.cs`

### Replay system
- Replay storage/parsing: `Assets/Scripts/Replay/*`
- Replay UI: `Assets/Scripts/UI/Game/Replay/*` and `Assets/Scripts/UI/MainMenu/Replays/*`
- On-disk replay folders: `Assets/StreamingAssets/replays/`

### Localization / RTL
- Translation runtime: `Assets/Scripts/UI/Translation/TranslationManager.cs` + `Assets/Scripts/UI/Translation/*`
- RTL UI transforms: `Assets/Scripts/UI/RTL/*`
- Arabic shaping support: `Assets/Extensions/ArabicSupport/ArabicSupport.cs`
- Language JSON assets: `Assets/StreamingAssets/lang/*`

### Tilemaps / stage authoring tools
- Tile behaviors/tools (Unity side): `Assets/Scripts/Tiles/SiblingRuleTile.cs`, `Assets/Scripts/Tiles/TilemapAnimator.cs`
- Stage baking (editor): `Assets/Scripts/Quantum/Editor/VersusStageBaker.cs`
- Simulation stage runtime: `Assets/QuantumUser/Simulation/NSMB/Map/StageSystem.cs`, `Assets/QuantumUser/Simulation/NSMB/Map/StageTile*.cs`, `Assets/QuantumUser/Simulation/NSMB/Map/VersusStageData.cs`

### Cameras / background
- Camera behaviour: `Assets/Scripts/Camera/*` (Unity) + `Assets/QuantumUser/Simulation/NSMB/Camera/*` (simulation)
- Background parallax: `Assets/Scripts/Background/*`

## Script Responsibilities (Detailed)

This section is generated from code scanning (types, base classes, Unity message methods, and notable dependencies) to help quickly locate the correct script cluster when porting a feature.

<!-- AUTO:SCRIPT_RESPONSIBILITIES_START -->
### Script folder map (folder-level only)

This is the simplified view: **folder -> what the scripts in that folder do overall**.

### Input

- `Assets/Controls.inputactions` - New Input System bindings (action maps: Player/UI/Replay/Debug).
- `Assets/Controls.cs` - generated C# wrapper over `Controls.inputactions`.

### App/client runtime (`Assets/Scripts/`)

- `Assets/Scripts/` (root) - global bootstrap managers (settings, global controller, rumble, Discord integration, build info).
- `Assets/Scripts/Background/` - background/parallax movement and looping helpers used in levels.
- `Assets/Scripts/Camera/` - Unity camera behaviour (resolution scaling, camera animation, secondary camera tracking).
- `Assets/Scripts/Chat/` - client-side chat UI/data glue (pairs with Quantum room chat commands).
- `Assets/Scripts/Editor/` - Unity editor-only build tooling and preprocess scripts.
- `Assets/Scripts/Networking/` - client networking glue (Photon connection/auth/certs, update checks, ping updates).
- `Assets/Scripts/Particles/` - particle helpers, number popups, particle-driven audio.
- `Assets/Scripts/Quantum/` - Unity<->Quantum integration (scene bootstrap, input collection, stage context, view pooling).
  - `Assets/Scripts/Quantum/Editor/` - editor-time stage baking/tools (Unity-side).
- `Assets/Scripts/Replay/` - replay file format + parsing + runtime replay management.
- `Assets/Scripts/Sound/` - music/SFX runtime managers and audio preloading.
- `Assets/Scripts/Tiles/` - Unity Tilemap helpers (rules/animation) used for stage authoring/baking.
- `Assets/Scripts/Utilities/` - shared helpers and small components used across the project.
  - `Assets/Scripts/Utilities/Components/` - attachable helper components.
  - `Assets/Scripts/Utilities/Extensions/` - extension methods used across code.
- `Assets/Scripts/Entity/` - Unity-side presentation layer for entities/world (visuals, animators, view sync).
  - `Assets/Scripts/Entity/Enemy/` - enemy visual controllers.
  - `Assets/Scripts/Entity/Player/` - player visual controllers.
  - `Assets/Scripts/Entity/Powerup/` - powerup/projectile visual controllers.
  - `Assets/Scripts/Entity/World Elements/` - world element visuals (blocks, coins, platforms, liquids, etc.).
- `Assets/Scripts/UI/` - all uGUI UI code (menus, HUD, pause, prompts, replay UI, translation/RTL).
  - `Assets/Scripts/UI/Elements/` - reusable UI components (clickables, focus helpers, validators, toggles).
  - `Assets/Scripts/UI/Intro/` - intro scene UI/flow.
  - `Assets/Scripts/UI/Loading/` - loading scene UI/flow.
  - `Assets/Scripts/UI/Pause/` - pause menu UI/flow.
  - `Assets/Scripts/UI/Options/` - options menu UI (includes input rebinding).
    - `Assets/Scripts/UI/Options/Loaders/` - bind persisted settings to UI controls.
  - `Assets/Scripts/UI/Game/` - in-game HUD root + in-match UI.
    - `Assets/Scripts/UI/Game/Chat/` - in-game chat UI.
    - `Assets/Scripts/UI/Game/Replay/` - in-game replay viewer UI + tabs.
    - `Assets/Scripts/UI/Game/Results/` - end-of-match results UI.
    - `Assets/Scripts/UI/Game/Scoreboard/` - scoreboard UI.
    - `Assets/Scripts/UI/Game/Track/` - track/minimap/icon UI.
  - `Assets/Scripts/UI/MainMenu/` - main menu root orchestration and submenus.
    - `Assets/Scripts/UI/MainMenu/About/` - credits/about pages.
    - `Assets/Scripts/UI/MainMenu/Header/` - header controls (back button, styling).
    - `Assets/Scripts/UI/MainMenu/Main/` - landing/home submenu (news board, version display).
    - `Assets/Scripts/UI/MainMenu/RoomList/` - room browser/list UI.
    - `Assets/Scripts/UI/MainMenu/Replays/` - replay list UI (main menu).
    - `Assets/Scripts/UI/MainMenu/Prompts/` - prompt stack (create/join/errors/update/replay prompts).
      - `Assets/Scripts/UI/MainMenu/Prompts/GameSettings/` - game settings prompt UI (rules/stage panels).
    - `Assets/Scripts/UI/MainMenu/InRoom/` - in-room/lobby submenu panels and lobby glue.
      - `Assets/Scripts/UI/MainMenu/InRoom/Player/` - player list UI and per-player controls.
      - `Assets/Scripts/UI/MainMenu/InRoom/Profile/` - profile UI (palette/team/nickname widgets).
      - `Assets/Scripts/UI/MainMenu/InRoom/Room/` - room settings UI (changeable rules/stage/gamemode) + lobby chat widgets.
  - `Assets/Scripts/UI/Translation/` - translation manager + translatable TMP wrappers.
  - `Assets/Scripts/UI/RTL/` - right-to-left UI transform helpers.

### Game-specific deterministic simulation + assets (`Assets/QuantumUser/`)

- `Assets/QuantumUser/Resources/AssetObjects/` - Quantum asset objects (characters, gamemodes, items, maps, music, palettes, etc.).
- `Assets/QuantumUser/Resources/EntityPrototypes/` - Quantum entity prototype prefabs (player/enemy/item/world/projectile prototypes).
- `Assets/QuantumUser/Simulation/NSMB/` - authoritative deterministic gameplay implementation.
  - `Assets/QuantumUser/Simulation/NSMB/Logic/` - match flow and game rules.
  - `Assets/QuantumUser/Simulation/NSMB/Gamemode/` - gamemode logic + gamemode assets.
  - `Assets/QuantumUser/Simulation/NSMB/Entity/` - players/enemies/items/projectiles/physics/interactions.
  - `Assets/QuantumUser/Simulation/NSMB/Map/` - stage runtime (tiles, liquids, movers, baked stage data).
  - `Assets/QuantumUser/Simulation/NSMB/Room/` - lobby/room state and command messages.
  - `Assets/QuantumUser/Simulation/NSMB/Camera/` - simulation camera systems/controllers.
  - `Assets/QuantumUser/Simulation/NSMB/Enums/` - deterministic enums (sound/particle IDs).
  - `Assets/QuantumUser/Simulation/NSMB/Debug/` - simulation debug commands/systems.
  - `Assets/QuantumUser/Simulation/NSMB/Modding/` - addon/mod definition assets.
  - `Assets/QuantumUser/Simulation/NSMB/Utils/` - simulation utilities.
- `Assets/QuantumUser/Editor/CodeGen/` - Quantum codegen integration (Unity editor side).
- `Assets/QuantumUser/Simulation/Generated/` - generated Quantum simulation glue (typically not hand-edited).
- `Assets/QuantumUser/Simulation/Helpers/` - Quantum simulation helper glue.
- `Assets/QuantumUser/View/Generated/` - generated Quantum Unity view glue (typically not hand-edited).

### Photon/Quantum SDK (third-party runtime + tooling) (`Assets/Photon/`)

- `Assets/Photon/PhotonLibs/` - Photon client libs and support code (including WebSocket implementation).
- `Assets/Photon/PhotonRealtime/` - Photon Realtime SDK sources.
- `Assets/Photon/Quantum/` - Photon Quantum SDK integration.
  - `Assets/Photon/Quantum/Runtime/` - Unity runtime integration (runner, entity view updater, input adapters, profiling, etc.).
  - `Assets/Photon/Quantum/Editor/` - Unity editor tooling.
  - `Assets/Photon/Quantum/Simulation/` - Quantum simulation-side SDK code.

### Extensions (vendored third-party) (`Assets/Extensions/`)

- `Assets/Extensions/ArabicSupport/` - Arabic/RTL text shaping support.
- `Assets/Extensions/TextMesh Pro/` - TMP assets/shaders (includes URP/HDRP TMP shader graphs).
- `Assets/Extensions/Graphy - Ultimate Stats Monitor/` - Graphy performance HUD + editor tooling.
- `Assets/Extensions/StandaloneFileBrowser/` - cross-platform file browser API + platform plugins.
- `Assets/Extensions/WebGLTemplates/` - WebGL build templates.
<!-- AUTO:SCRIPT_RESPONSIBILITIES_END -->


