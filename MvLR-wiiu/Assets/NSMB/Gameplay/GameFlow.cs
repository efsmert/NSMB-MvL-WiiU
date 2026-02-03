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

            if (_menu != null) _menu.SetVisible(true);
            if (_hud != null) _hud.SetVisible(false);
            if (_pause != null) _pause.SetVisible(false);
        }

        public void StartGame() {
            EnsureGameplayWorld(_selectedStageKey);
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

        private void EnsureGameplayWorld(string stageKey) {
            if (_levelRoot != null) {
                Destroy(_levelRoot);
                _levelRoot = null;
            }

            _levelRoot = NSMB.World.LevelRegistry.Spawn(stageKey);
            EnsureBackground(_levelRoot);

            NSMB.World.StageDefinition imported = Resources.Load(typeof(NSMB.World.StageDefinition), "NSMB/Levels/" + stageKey) as NSMB.World.StageDefinition;
            Vector3 spawn = Vector3.zero;
            if (imported != null) {
                spawn = new Vector3(imported.spawnPoint.x, imported.spawnPoint.y, 0f);
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
