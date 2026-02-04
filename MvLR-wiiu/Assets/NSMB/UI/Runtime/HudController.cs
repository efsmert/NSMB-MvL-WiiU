using UnityEngine;
using UnityEngine.UI;

namespace NSMB.UI {
    public sealed class HudController : MonoBehaviour {
        private GameObject _root;
        private UiSpriteStore _sprites;
        private HudSpriteText _starsText;
        private HudSpriteText _coinsText;
        private RectTransform _trackStar;

        public void Initialize(UiSpriteStore sprites) {
            _sprites = sprites;
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

            if (_coinsText != null) {
                _coinsText.SetText(string.Format("x {0}/{1}", gm.Coins, gm.CoinRequirement));
            }
            if (_starsText != null) {
                _starsText.SetText(string.Format("x {0}/{1}", gm.Stars, gm.StarRequirement));
            }

            // Track: keep the star centered for now (parity hook; later tie to actual track position).
            if (_trackStar != null) {
                _trackStar.anchoredPosition = new Vector2(0f, _trackStar.anchoredPosition.y);
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

            // Top-left: stars requirement (star icon + "x 0/10")
            GameObject left = UiRuntimeUtil.CreateUiObject("StarsGroup", _root.transform);
            RectTransform leftRt = left.GetComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0f, 1f);
            leftRt.anchorMax = new Vector2(0f, 1f);
            leftRt.pivot = new Vector2(0f, 1f);
            leftRt.anchoredPosition = new Vector2(18f, -18f);
            leftRt.sizeDelta = new Vector2(320f, 64f);

            Sprite starIcon = _sprites.LoadFromSheet("NSMB/UI/ui", "hud_star");
            if (starIcon == null) {
                starIcon = _sprites.LoadSingle("NSMB/UI/ministar");
            }
            Image star = UiRuntimeUtil.CreateImage(left.transform, "StarIcon", starIcon);
            SetNativeSizeScaled(star, 2f);
            RectTransform starRt = star.rectTransform;
            starRt.anchorMin = new Vector2(0f, 0.5f);
            starRt.anchorMax = new Vector2(0f, 0.5f);
            starRt.pivot = new Vector2(0f, 0.5f);
            starRt.anchoredPosition = new Vector2(0f, -2f);

            GameObject starsTextGo = UiRuntimeUtil.CreateUiObject("StarsText", left.transform);
            RectTransform starsTextRt = starsTextGo.GetComponent<RectTransform>();
            starsTextRt.anchorMin = new Vector2(0f, 0.5f);
            starsTextRt.anchorMax = new Vector2(0f, 0.5f);
            starsTextRt.pivot = new Vector2(0f, 0.5f);
            starsTextRt.anchoredPosition = new Vector2(48f, -2f);
            _starsText = starsTextGo.AddComponent<HudSpriteText>();
            _starsText.scale = 2f;
            _starsText.spacing = 1f;
            _starsText.rightAligned = false;
            _starsText.Initialize(_sprites, "NSMB/UI/ui");

            // Top-right: coins requirement (coin icon + "x 0/8")
            GameObject right = UiRuntimeUtil.CreateUiObject("CoinsGroup", _root.transform);
            RectTransform rightRt = right.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(1f, 1f);
            rightRt.anchorMax = new Vector2(1f, 1f);
            rightRt.pivot = new Vector2(1f, 1f);
            rightRt.anchoredPosition = new Vector2(-18f, -18f);
            rightRt.sizeDelta = new Vector2(320f, 64f);

            Sprite coinIcon = _sprites.LoadFromSheet("NSMB/UI/ui", "hudnumber_coin");
            Image coin = UiRuntimeUtil.CreateImage(right.transform, "CoinIcon", coinIcon);
            SetNativeSizeScaled(coin, 2f);
            RectTransform coinRt = coin.rectTransform;
            coinRt.anchorMin = new Vector2(1f, 0.5f);
            coinRt.anchorMax = new Vector2(1f, 0.5f);
            coinRt.pivot = new Vector2(1f, 0.5f);
            coinRt.anchoredPosition = new Vector2(0f, -2f);

            GameObject coinsTextGo = UiRuntimeUtil.CreateUiObject("CoinsText", right.transform);
            RectTransform coinsTextRt = coinsTextGo.GetComponent<RectTransform>();
            coinsTextRt.anchorMin = new Vector2(1f, 0.5f);
            coinsTextRt.anchorMax = new Vector2(1f, 0.5f);
            coinsTextRt.pivot = new Vector2(1f, 0.5f);
            coinsTextRt.anchoredPosition = new Vector2(-48f, -2f);
            _coinsText = coinsTextGo.AddComponent<HudSpriteText>();
            _coinsText.scale = 2f;
            _coinsText.spacing = 1f;
            _coinsText.rightAligned = true;
            _coinsText.Initialize(_sprites, "NSMB/UI/ui");

            // Top center: track bar + star marker.
            GameObject track = UiRuntimeUtil.CreateUiObject("Track", _root.transform);
            RectTransform trackRt = track.GetComponent<RectTransform>();
            trackRt.anchorMin = new Vector2(0.5f, 1f);
            trackRt.anchorMax = new Vector2(0.5f, 1f);
            trackRt.pivot = new Vector2(0.5f, 1f);
            trackRt.anchoredPosition = new Vector2(0f, -18f);
            trackRt.sizeDelta = new Vector2(760f, 40f);

            Sprite trackMid = _sprites.LoadFromSheet("NSMB/UI/track", "track_1");
            Image trackBar = UiRuntimeUtil.CreateImage(track.transform, "TrackBar", trackMid);
            trackBar.type = Image.Type.Sliced;
            trackBar.color = Color.white;
            RectTransform trackBarRt = trackBar.rectTransform;
            trackBarRt.anchorMin = new Vector2(0.5f, 0.5f);
            trackBarRt.anchorMax = new Vector2(0.5f, 0.5f);
            trackBarRt.pivot = new Vector2(0.5f, 0.5f);
            trackBarRt.anchoredPosition = new Vector2(0f, -2f);
            trackBarRt.sizeDelta = new Vector2(720f, 16f);

            Sprite endCapSprite = _sprites.LoadFromSheet("NSMB/UI/track", "track_0");
            Image leftCap = UiRuntimeUtil.CreateImage(track.transform, "LeftCap", endCapSprite);
            SetNativeSizeScaled(leftCap, 2f);
            RectTransform leftCapRt = leftCap.rectTransform;
            leftCapRt.anchorMin = new Vector2(0f, 0.5f);
            leftCapRt.anchorMax = new Vector2(0f, 0.5f);
            leftCapRt.pivot = new Vector2(0f, 0.5f);
            leftCapRt.anchoredPosition = new Vector2(0f, -2f);

            Image rightCap = UiRuntimeUtil.CreateImage(track.transform, "RightCap", endCapSprite);
            SetNativeSizeScaled(rightCap, 2f);
            RectTransform rightCapRt = rightCap.rectTransform;
            rightCapRt.anchorMin = new Vector2(1f, 0.5f);
            rightCapRt.anchorMax = new Vector2(1f, 0.5f);
            rightCapRt.pivot = new Vector2(1f, 0.5f);
            rightCapRt.anchoredPosition = new Vector2(0f, -2f);

            Sprite tickSprite = _sprites.LoadFromSheet("NSMB/UI/track", "track_3");
            if (tickSprite != null) {
                for (int i = 0; i <= 18; i++) {
                    Image tick = UiRuntimeUtil.CreateImage(trackBar.transform, "Tick_" + i, tickSprite);
                    SetNativeSizeScaled(tick, 2f);
                    RectTransform tickRt = tick.rectTransform;
                    tickRt.anchorMin = new Vector2(0f, 0.5f);
                    tickRt.anchorMax = new Vector2(0f, 0.5f);
                    tickRt.pivot = new Vector2(0.5f, 0.5f);
                    float t = (float)i / 18f;
                    float x = Mathf.Lerp(8f, trackBarRt.sizeDelta.x - 8f, t);
                    tickRt.anchoredPosition = new Vector2(x, 0f);
                }
            }

            Image trackStar = UiRuntimeUtil.CreateImage(trackBar.transform, "TrackStar", starIcon);
            SetNativeSizeScaled(trackStar, 2f);
            _trackStar = trackStar.rectTransform;
            _trackStar.anchorMin = new Vector2(0.5f, 0.5f);
            _trackStar.anchorMax = new Vector2(0.5f, 0.5f);
            _trackStar.pivot = new Vector2(0.5f, 0.5f);
            _trackStar.anchoredPosition = new Vector2(0f, 10f);

            // Initialize default strings.
            if (_coinsText != null) _coinsText.SetText("x 0/8");
            if (_starsText != null) _starsText.SetText("x 0/10");
        }

        private static void SetNativeSizeScaled(Image img, float s) {
            if (img == null || img.sprite == null) {
                return;
            }
            img.SetNativeSize();
            RectTransform rt = img.rectTransform;
            rt.sizeDelta = rt.sizeDelta * Mathf.Max(0.01f, s);
        }
    }
}
