using UnityEngine;
using UnityEngine.UI;

namespace NSMB.UI {
    public sealed class HudBootstrap : MonoBehaviour {
        private Text _coinsText;
        private Text _scoreText;

        private void Start() {
            Build();
        }

        private void Update() {
            NSMB.Gameplay.GameManager gm = NSMB.Gameplay.GameManager.Instance;
            if (gm == null) {
                return;
            }

            if (_coinsText != null) {
                _coinsText.text = "Coins: " + gm.Coins;
            }
            if (_scoreText != null) {
                _scoreText.text = "Score: " + gm.Score;
            }
        }

        private void Build() {
            if (FindObjectOfType(typeof(Canvas)) != null) {
                return;
            }

            GameObject canvasGo = new GameObject("Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            _coinsText = CreateLabel(canvasGo.transform, "CoinsText", new Vector2(10f, -10f));
            _scoreText = CreateLabel(canvasGo.transform, "ScoreText", new Vector2(10f, -30f));
        }

        private Text CreateLabel(Transform parent, string name, Vector2 anchoredPos) {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
            t.fontSize = 18;
            t.color = Color.white;
            t.alignment = TextAnchor.UpperLeft;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(300f, 25f);

            return t;
        }
    }
}

