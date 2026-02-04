using UnityEngine;

namespace NSMB.Gameplay {
    public enum GameFlowState {
        Menu = 0,
        InGame = 1,
        Paused = 2,
    }

    public sealed class GameFlow : MonoBehaviour {
        private static GameFlow _instance;
        public static GameFlow Instance { get { return _instance; } }

        private GameFlowState _state = GameFlowState.Menu;
        public GameFlowState State { get { return _state; } }

        private NSMB.UI.MenuController _menu;
        private NSMB.UI.HudController _hud;
        private NSMB.UI.PauseMenuController _pause;
        private GameObject _levelRoot;
        private GameObject _menuBackdrop;
        private GameObject _menuCameraTarget;
        private string _selectedStageKey = "stage-grassland";

        private void Awake() {
            if (_instance != null && _instance != this) {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void BindUi(NSMB.UI.MenuController menu, NSMB.UI.HudController hud, NSMB.UI.PauseMenuController pause) {
            _menu = menu;
            _hud = hud;
            _pause = pause;

            EnterMenu();
        }

        private void Update() {
            if (_state == GameFlowState.InGame) {
                if (NSMB.Input.LegacyInput.GetPauseDown()) {
                    EnterPaused();
                }
            } else if (_state == GameFlowState.Paused) {
                if (NSMB.Input.LegacyInput.GetPauseDown()) {
                    EnterInGame();
                }
            }
        }

        public void EnterMenu() {
            _state = GameFlowState.Menu;
            Time.timeScale = 1f;

            EnsureMenuBackdrop();
            EnsureMenuMusic();

            if (_menu != null) _menu.SetVisible(true);
            if (_hud != null) _hud.SetVisible(false);
            if (_pause != null) _pause.SetVisible(false);
        }

        public void StartGame() {
            DestroyMenuBackdrop();
            EnsureGameplayWorld(_selectedStageKey);
            StopMenuMusic();
            EnterInGame();
        }

        public void StartGame(string stageKey) {
            _selectedStageKey = stageKey;
            StartGame();
        }

        public void EnterInGame() {
            _state = GameFlowState.InGame;
            Time.timeScale = 1f;

            if (_menu != null) _menu.SetVisible(false);
            if (_hud != null) _hud.SetVisible(true);
            if (_pause != null) _pause.SetVisible(false);
        }

        public void EnterPaused() {
            _state = GameFlowState.Paused;
            Time.timeScale = 0f;

            if (_pause != null) _pause.SetVisible(true);
        }

        public void QuitToMenu() {
            Time.timeScale = 1f;

            // For now, just hide gameplay objects (keep it simple/fast in 2017.1).
            NSMB.Player.PlayerMotor2D player = Object.FindObjectOfType(typeof(NSMB.Player.PlayerMotor2D)) as NSMB.Player.PlayerMotor2D;
            if (player != null) {
                Destroy(player.gameObject);
            }

            if (_levelRoot != null) {
                Destroy(_levelRoot);
                _levelRoot = null;
            }

            EnterMenu();
        }

        private void EnsureMenuBackdrop() {
            if (_menuBackdrop != null) {
                return;
            }

            // Use a stage as an animated backdrop for the main menu (matches original feel better than static UI sprites).
            // Keep it lightweight: tiles only, no entities/colliders.
            _menuBackdrop = new GameObject("MenuBackdrop");

            string stageKey = "stage-fortress";
            NSMB.World.StageDefinition def = Resources.Load<NSMB.World.StageDefinition>("NSMB/Levels/" + stageKey);
            if (def == null) {
                stageKey = "stage-grassland";
                def = Resources.Load<NSMB.World.StageDefinition>("NSMB/Levels/" + stageKey);
            }

            if (def != null) {
                NSMB.World.StageRuntimeBuilder.Build(def, _menuBackdrop.transform, false, false);

                UnityEngine.Camera cam = UnityEngine.Camera.main;
                if (cam != null) {
                    NSMB.Camera.CameraFollow2D follow = cam.GetComponent<NSMB.Camera.CameraFollow2D>();
                    if (follow != null) {
                        if (_menuCameraTarget == null) {
                            _menuCameraTarget = new GameObject("MenuCameraTarget");
                        }

                        Vector2 min = def.cameraMin;
                        Vector2 max = def.cameraMax;
                        Vector3 targetPos;
                        if (def.isWrappingLevel) {
                            follow.clampToBounds = false;
                            targetPos = new Vector3(def.spawnPoint.x, def.spawnPoint.y, 0f);
                        } else if (min != max) {
                            targetPos = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, 0f);
                            follow.SetBounds(min, max);
                        } else {
                            targetPos = new Vector3(def.spawnPoint.x, def.spawnPoint.y, 0f);
                            follow.clampToBounds = false;
                        }

                        _menuCameraTarget.transform.position = targetPos;
                        follow.target = _menuCameraTarget.transform;
                        follow.offset = Vector2.zero;
                        follow.smoothTime = 0.10f;
                    }
                }
            }
        }

        private void DestroyMenuBackdrop() {
            if (_menuBackdrop != null) {
                Destroy(_menuBackdrop);
                _menuBackdrop = null;
            }

            if (_menuCameraTarget != null) {
                Destroy(_menuCameraTarget);
                _menuCameraTarget = null;
            }
        }

        private static void EnsureMenuMusic() {
            NSMB.Core.GameRoot root = NSMB.Core.GameRoot.Instance;
            if (root == null) return;

            NSMB.Audio.AudioManager audio = root.GetComponent<NSMB.Audio.AudioManager>();
            if (audio == null) return;

            audio.PlayMusicResources("NSMB/AudioClips/Resources/Sound/music/mainmenu", 0.75f, true);
        }

        private static void StopMenuMusic() {
            NSMB.Core.GameRoot root = NSMB.Core.GameRoot.Instance;
            if (root == null) return;

            NSMB.Audio.AudioManager audio = root.GetComponent<NSMB.Audio.AudioManager>();
            if (audio == null) return;

            audio.StopMusic();
        }

        private void EnsureGameplayWorld(string stageKey) {
            if (_levelRoot != null) {
                Destroy(_levelRoot);
                _levelRoot = null;
            }

            _levelRoot = NSMB.World.LevelRegistry.Spawn(stageKey);
            // Background is now built as part of StageRuntimeBuilder for imported stages.
            EnsureBackground(_levelRoot);

            NSMB.World.StageDefinition imported = Resources.Load<NSMB.World.StageDefinition>("NSMB/Levels/" + stageKey);
            Vector3 spawn = Vector3.zero;
            if (imported != null) {
                spawn = new Vector3(imported.spawnPoint.x, imported.spawnPoint.y, 0f);
            }

            // Camera bounds from stage definition (if any).
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam != null) {
                NSMB.Camera.CameraFollow2D follow = cam.GetComponent<NSMB.Camera.CameraFollow2D>();
                if (follow != null) {
                    if (imported != null && imported.cameraMin != imported.cameraMax) {
                        follow.SetBounds(imported.cameraMin, imported.cameraMax);

                        // Wrapping stages still need vertical clamping (to avoid showing "below the world"),
                        // but horizontal clamping would fight StageWrap2D's recentering.
                        follow.clampY = true;
                        follow.clampX = !(imported != null && imported.isWrappingLevel);
                    } else {
                        follow.clampToBounds = false;
                    }
                }
            }

            NSMB.Player.PlayerMotor2D existing = Object.FindObjectOfType(typeof(NSMB.Player.PlayerMotor2D)) as NSMB.Player.PlayerMotor2D;
            if (existing == null) {
                // Use bootstrap helper to create a player the same way.
                NSMB.WiiU.WiiUBootstrap.EnsurePlayerForFlowAt(spawn);
            } else if (imported != null) {
                existing.transform.position = spawn;
            }
        }

        private static void EnsureBackground(GameObject levelRoot) {
            if (levelRoot == null) {
                return;
            }

            // If the stage already spawned a background (preferred path), don't add the fallback gradient.
            Transform stage = null;
            for (int i = 0; i < levelRoot.transform.childCount; i++) {
                Transform c = levelRoot.transform.GetChild(i);
                if (c != null && c.name.StartsWith("Stage_", System.StringComparison.InvariantCultureIgnoreCase)) {
                    stage = c;
                    break;
                }
            }
            if (stage != null) {
                Transform existingBg = stage.Find("Background");
                if (existingBg != null) {
                    return;
                }
            }

            // Gameplay background should be a subtle dark gradient. (The "uibackground" asset in this repo
            // is a diagonal white stripe used by some menus, so do not use it here.)
            Sprite[] bgSprites = NSMB.Content.ResourceSpriteCache.LoadAllSprites("NSMB/UI/bggradient");
            Sprite bg = (bgSprites != null && bgSprites.Length > 0) ? bgSprites[0] : null;
            if (bg == null) {
                return;
            }

            Transform existing = levelRoot.transform.Find("Background");
            GameObject go;
            if (existing != null) {
                go = existing.gameObject;
            } else {
                go = new GameObject("Background");
                go.transform.parent = levelRoot.transform;
                go.transform.localPosition = Vector3.zero;
            }

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) {
                sr = go.AddComponent<SpriteRenderer>();
            }
            sr.sprite = bg;
            sr.sortingOrder = -1000;
            sr.color = Color.white;

            // Scale to roughly fill the camera view (we keep it simple for 2017.1).
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam != null && cam.orthographic) {
                float height = cam.orthographicSize * 2f;
                float width = height * cam.aspect;

                Vector2 size = bg.bounds.size;
                if (size.x > 0f && size.y > 0f) {
                    go.transform.localScale = new Vector3(width / size.x, height / size.y, 1f);
                }
            }
        }
    }
}
