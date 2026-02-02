using UnityEngine;
using UnityEngine.UI;

namespace NSMB.UI {
    public sealed class UIRootBootstrap : MonoBehaviour {
        private bool _built;

        private void Start() {
            Build();
        }

        private void Build() {
            if (_built) {
                return;
            }
            _built = true;

            Canvas canvas = FindObjectOfType(typeof(Canvas)) as Canvas;
            if (canvas == null) {
                GameObject canvasGo = new GameObject("NSMB_UI");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.matchWidthOrHeight = 0.5f;

                canvasGo.AddComponent<GraphicRaycaster>();
                DontDestroyOnLoad(canvasGo);
            }

            EnsureEventSystem();

            UiSpriteStore sprites = new UiSpriteStore();

            MenuController menu = canvas.gameObject.GetComponent<MenuController>();
            if (menu == null) {
                menu = canvas.gameObject.AddComponent<MenuController>();
            }
            menu.Initialize(sprites);

            HudController hud = canvas.gameObject.GetComponent<HudController>();
            if (hud == null) {
                hud = canvas.gameObject.AddComponent<HudController>();
            }
            hud.Initialize(sprites);

            PauseMenuController pause = canvas.gameObject.GetComponent<PauseMenuController>();
            if (pause == null) {
                pause = canvas.gameObject.AddComponent<PauseMenuController>();
            }
            pause.Initialize(sprites);

            NSMB.Gameplay.GameFlow flow = NSMB.Gameplay.GameFlow.Instance;
            if (flow != null) {
                flow.BindUi(menu, hud, pause);
            }
        }

        private static void EnsureEventSystem() {
            if (FindObjectOfType(typeof(UnityEngine.EventSystems.EventSystem)) != null) {
                return;
            }

            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Object.DontDestroyOnLoad(es);
        }
    }
}
