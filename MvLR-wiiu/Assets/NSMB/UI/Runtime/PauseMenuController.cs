using UnityEngine;
using UnityEngine.UI;

namespace NSMB.UI {
    public sealed class PauseMenuController : MonoBehaviour {
        private GameObject _root;
        private UiSpriteStore _sprites;
        private Font _font;

        public void Initialize(UiSpriteStore sprites) {
            _sprites = sprites;
            _font = UiRuntimeUtil.LoadFontOrDefault("Fonts/TTFs/PauseFontTTF");
            BuildIfNeeded();
        }

        public void SetVisible(bool visible) {
            if (_root != null) {
                _root.SetActive(visible);
            }
        }

        private void BuildIfNeeded() {
            if (_root != null) {
                return;
            }

            _root = UiRuntimeUtil.CreateUiObject("PauseMenu", transform);
            RectTransform rootRt = _root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            // Dark overlay
            Image overlay = UiRuntimeUtil.CreateImage(_root.transform, "Overlay", null);
            overlay.color = new Color(0f, 0f, 0f, 0.55f);
            RectTransform overlayRt = overlay.rectTransform;
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            Sprite panelSprite = _sprites.LoadSingle("NSMB/UI/options/pausemenu");
            if (panelSprite == null) {
                panelSprite = _sprites.LoadSingle("NSMB/UI/Menu/Elements/rounded-rect-5px");
            }

            Image panel = UiRuntimeUtil.CreateImage(_root.transform, "Panel", panelSprite);
            panel.type = Image.Type.Sliced;
            RectTransform panelRt = panel.rectTransform;
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(520f, 340f);

            Text title = UiRuntimeUtil.CreateText(panel.transform, "Title", _font, 42, TextAnchor.UpperCenter, Color.black);
            title.text = "PAUSE";
            RectTransform titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -24f);
            titleRt.sizeDelta = new Vector2(400f, 60f);

            Sprite buttonSprite = _sprites.LoadSingle("NSMB/UI/Menu/Elements/rounded-rect-5px");
            if (buttonSprite == null) {
                buttonSprite = _sprites.LoadSingle("NSMB/UI/Menu/Elements/rounded-rect");
            }

            GameObject column = UiRuntimeUtil.CreateUiObject("Buttons", panel.transform);
            RectTransform colRt = column.GetComponent<RectTransform>();
            colRt.anchorMin = new Vector2(0.5f, 0.5f);
            colRt.anchorMax = new Vector2(0.5f, 0.5f);
            colRt.pivot = new Vector2(0.5f, 0.5f);
            colRt.anchoredPosition = new Vector2(0f, -10f);
            colRt.sizeDelta = new Vector2(420f, 220f);

            VerticalLayoutGroup vlg = column.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 14f;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            ContentSizeFitter fitter = column.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Button resume = UiRuntimeUtil.CreateButton(column.transform, "ResumeButton", buttonSprite, _font, "RESUME", 28);
            Button quit = UiRuntimeUtil.CreateButton(column.transform, "QuitButton", buttonSprite, _font, "QUIT TO MENU", 24);
            SetButtonSize(resume, 420f, 70f);
            SetButtonSize(quit, 420f, 70f);

            resume.onClick.AddListener(OnResume);
            quit.onClick.AddListener(OnQuitToMenu);

            SetVisible(false);
        }

        private static void SetButtonSize(Button b, float w, float h) {
            RectTransform rt = b.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void OnResume() {
            NSMB.Gameplay.GameFlow flow = NSMB.Gameplay.GameFlow.Instance;
            if (flow != null) {
                flow.EnterInGame();
            }

            NSMB.Core.GameRoot root = NSMB.Core.GameRoot.Instance;
            if (root != null) {
                NSMB.Audio.AudioManager audio = root.GetComponent<NSMB.Audio.AudioManager>();
                if (audio != null) {
                    audio.PlayOneShot(NSMB.Audio.SoundEffectId.UI_Back, 0.9f);
                }
            }
        }

        private static void OnQuitToMenu() {
            NSMB.Gameplay.GameFlow flow = NSMB.Gameplay.GameFlow.Instance;
            if (flow != null) {
                flow.QuitToMenu();
            }

            NSMB.Core.GameRoot root = NSMB.Core.GameRoot.Instance;
            if (root != null) {
                NSMB.Audio.AudioManager audio = root.GetComponent<NSMB.Audio.AudioManager>();
                if (audio != null) {
                    audio.PlayOneShot(NSMB.Audio.SoundEffectId.UI_WindowClose, 0.9f);
                }
            }
        }
    }
}
