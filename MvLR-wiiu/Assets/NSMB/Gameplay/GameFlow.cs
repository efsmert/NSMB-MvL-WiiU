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
            EnsureGameplayWorld();
            EnterInGame();
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

            NSMB.World.TestLevelBootstrap level = Object.FindObjectOfType(typeof(NSMB.World.TestLevelBootstrap)) as NSMB.World.TestLevelBootstrap;
            if (level != null) {
                Destroy(level.gameObject);
            }

            EnterMenu();
        }

        private static void EnsureGameplayWorld() {
            if (Object.FindObjectOfType(typeof(NSMB.World.TestLevelBootstrap)) == null) {
                GameObject go = new GameObject("TestLevel");
                go.AddComponent<NSMB.World.TestLevelBootstrap>();
            }

            NSMB.Player.PlayerMotor2D existing = Object.FindObjectOfType(typeof(NSMB.Player.PlayerMotor2D)) as NSMB.Player.PlayerMotor2D;
            if (existing == null) {
                // Use bootstrap helper to create a player the same way.
                NSMB.WiiU.WiiUBootstrap.EnsurePlayerForFlow();
            }
        }
    }
}
