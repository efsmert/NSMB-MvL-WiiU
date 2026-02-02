using UnityEngine;
using UnityEngine.UI;

namespace NSMB.UI {
    public sealed class HudController : MonoBehaviour {
        private GameObject _root;
        private UiSpriteStore _sprites;
        private Text _coins;
        private Text _score;
        private Font _font;

        public void Initialize(UiSpriteStore sprites) {
            _sprites = sprites;
            _font = UiRuntimeUtil.LoadFontOrDefault("Fonts/TTFs/numbers");
            BuildIfNeeded();
        }

        public void SetVisible(bool visible) {
            if (_root != null) {
                _root.SetActive(visible);
            }
        }

        private void Update() {
            if (_root == null || !_root.activeSelf) {
                return;
            }

            NSMB.Gameplay.GameManager gm = NSMB.Gameplay.GameManager.Instance;
            if (gm == null) {
                return;
            }

            if (_coins != null) {
                _coins.text = "x " + gm.Coins;
            }
            if (_score != null) {
                _score.text = gm.Score.ToString();
            }
        }

        private void BuildIfNeeded() {
            if (_root != null) {
                return;
            }

            _root = UiRuntimeUtil.CreateUiObject("HUD", transform);
            RectTransform rootRt = _root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            Sprite panelSprite = _sprites.LoadSingle("NSMB/UI/Menu/Elements/rounded-rect-5px");
            Image panel = UiRuntimeUtil.CreateImage(_root.transform, "Panel", panelSprite);
            panel.type = Image.Type.Sliced;
            panel.color = new Color(1f, 1f, 1f, 0.75f);
            RectTransform panelRt = panel.rectTransform;
            panelRt.anchorMin = new Vector2(0f, 1f);
            panelRt.anchorMax = new Vector2(0f, 1f);
            panelRt.pivot = new Vector2(0f, 1f);
            panelRt.anchoredPosition = new Vector2(16f, -16f);
            panelRt.sizeDelta = new Vector2(360f, 84f);

            Sprite coinSprite = _sprites.LoadSingle("NSMB/UI/ministar");
            Image coin = UiRuntimeUtil.CreateImage(panel.transform, "CoinIcon", coinSprite);
            RectTransform coinRt = coin.rectTransform;
            coinRt.anchorMin = new Vector2(0f, 0.5f);
            coinRt.anchorMax = new Vector2(0f, 0.5f);
            coinRt.pivot = new Vector2(0f, 0.5f);
            coinRt.anchoredPosition = new Vector2(12f, 0f);
            coinRt.sizeDelta = new Vector2(48f, 48f);

            _coins = UiRuntimeUtil.CreateText(panel.transform, "CoinsText", _font, 30, TextAnchor.MiddleLeft, Color.black);
            RectTransform coinsRt = _coins.rectTransform;
            coinsRt.anchorMin = new Vector2(0f, 0.5f);
            coinsRt.anchorMax = new Vector2(0f, 0.5f);
            coinsRt.pivot = new Vector2(0f, 0.5f);
            coinsRt.anchoredPosition = new Vector2(72f, 0f);
            coinsRt.sizeDelta = new Vector2(140f, 40f);
            _coins.text = "x 0";

            Text scoreLabel = UiRuntimeUtil.CreateText(panel.transform, "ScoreLabel", UiRuntimeUtil.LoadFontOrDefault("Fonts/TTFs/PixelMplus12-Bold"), 18, TextAnchor.UpperRight, Color.black);
            RectTransform scoreLabelRt = scoreLabel.rectTransform;
            scoreLabelRt.anchorMin = new Vector2(1f, 1f);
            scoreLabelRt.anchorMax = new Vector2(1f, 1f);
            scoreLabelRt.pivot = new Vector2(1f, 1f);
            scoreLabelRt.anchoredPosition = new Vector2(-10f, -8f);
            scoreLabelRt.sizeDelta = new Vector2(120f, 22f);
            scoreLabel.text = "SCORE";

            _score = UiRuntimeUtil.CreateText(panel.transform, "ScoreText", _font, 26, TextAnchor.LowerRight, Color.black);
            RectTransform scoreRt = _score.rectTransform;
            scoreRt.anchorMin = new Vector2(1f, 0f);
            scoreRt.anchorMax = new Vector2(1f, 0f);
            scoreRt.pivot = new Vector2(1f, 0f);
            scoreRt.anchoredPosition = new Vector2(-10f, 8f);
            scoreRt.sizeDelta = new Vector2(200f, 32f);
            _score.text = "0";
        }
    }
}
