using UnityEngine;
using UnityEngine.UI;

namespace NSMB.UI {
    public sealed class MenuController : MonoBehaviour {
        private enum MenuMode {
            PressStart = 0,
            MainButtons = 1,
            Options = 2,
        }

        private GameObject _root;
        private GameObject _mainPanel;
        private GameObject _optionsPanel;
        private GameObject _pressStartPanel;
        private Image _selectionHighlight;
        private Button[] _mainButtons;
        private int _selectedIndex;
        private float _moveCooldown;
        private MenuMode _mode;

        private UiSpriteStore _sprites;
        private Font _font;

        public void Initialize(UiSpriteStore sprites) {
            _sprites = sprites;
            _font = UiRuntimeUtil.LoadFontOrDefault("Fonts/TTFs/BoldFont");
            BuildIfNeeded();
        }

        public void SetVisible(bool visible) {
            if (_root != null) {
                _root.SetActive(visible);
            }

            if (visible) {
                ResetToPressStart();
            }
        }

        private void Update() {
            if (_root == null || !_root.activeSelf) {
                return;
            }

            if (_mode == MenuMode.PressStart) {
                TickPressStart();
                return;
            }

            if (_mode == MenuMode.MainButtons) {
                TickMainButtons();
                return;
            }

            if (_mode == MenuMode.Options) {
                if (NSMB.Input.LegacyInput.GetBackDown()) {
                    PlayUi(NSMB.Audio.SoundEffectId.UI_Back);
                    ShowMain();
                }
            }
        }

        private void BuildIfNeeded() {
            if (_root != null) {
                return;
            }

            _root = UiRuntimeUtil.CreateUiObject("Menu", transform);
            RectTransform rootRt = _root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            BuildBackground();

            // Subtle gradient overlay (matches original UI styling)
            Sprite gradientSprite = _sprites.LoadSingle("NSMB/UI/bggradient");
            if (gradientSprite != null) {
                Image gradient = UiRuntimeUtil.CreateImage(_root.transform, "Gradient", gradientSprite);
                gradient.preserveAspect = false;
                gradient.color = new Color(1f, 1f, 1f, 0.85f);
                RectTransform grt = gradient.rectTransform;
                grt.anchorMin = Vector2.zero;
                grt.anchorMax = Vector2.one;
                grt.offsetMin = Vector2.zero;
                grt.offsetMax = Vector2.zero;
            }

            _mainPanel = UiRuntimeUtil.CreateUiObject("MainPanel", _root.transform);
            RectTransform mainRt = _mainPanel.GetComponent<RectTransform>();
            mainRt.anchorMin = Vector2.zero;
            mainRt.anchorMax = Vector2.one;
            mainRt.offsetMin = Vector2.zero;
            mainRt.offsetMax = Vector2.zero;

            _pressStartPanel = UiRuntimeUtil.CreateUiObject("PressStartPanel", _root.transform);
            RectTransform psRt = _pressStartPanel.GetComponent<RectTransform>();
            psRt.anchorMin = Vector2.zero;
            psRt.anchorMax = Vector2.one;
            psRt.offsetMin = Vector2.zero;
            psRt.offsetMax = Vector2.zero;

            Sprite logoSprite = _sprites.LoadSingle("NSMB/UI/Menu/logo");
            if (logoSprite == null) {
                logoSprite = _sprites.LoadSingle("NSMB/UI/Menu/Title");
            }
            Image logo = UiRuntimeUtil.CreateImage(_mainPanel.transform, "Logo", logoSprite);
            RectTransform logoRt = logo.rectTransform;
            logoRt.anchorMin = new Vector2(0.5f, 1f);
            logoRt.anchorMax = new Vector2(0.5f, 1f);
            logoRt.pivot = new Vector2(0.5f, 1f);
            logoRt.anchoredPosition = new Vector2(0f, -40f);
            logoRt.sizeDelta = new Vector2(700f, 220f);

            Sprite marioLuigiSprite = _sprites.LoadSingle("NSMB/UI/Menu/marioluigi");
            if (marioLuigiSprite != null) {
                Image ml = UiRuntimeUtil.CreateImage(_mainPanel.transform, "MarioLuigi", marioLuigiSprite);
                RectTransform mlRt = ml.rectTransform;
                mlRt.anchorMin = new Vector2(0.5f, 0f);
                mlRt.anchorMax = new Vector2(0.5f, 0f);
                mlRt.pivot = new Vector2(0.5f, 0f);
                mlRt.anchoredPosition = new Vector2(-360f, 10f);
                mlRt.sizeDelta = new Vector2(420f, 260f);
            }

            Sprite buttonSprite = _sprites.LoadSingle("NSMB/UI/Menu/Elements/rounded-rect-5px");
            if (buttonSprite == null) {
                buttonSprite = _sprites.LoadSingle("NSMB/UI/Menu/Elements/rounded-rect");
            }

            GameObject column = UiRuntimeUtil.CreateUiObject("Buttons", _mainPanel.transform);
            RectTransform colRt = column.GetComponent<RectTransform>();
            colRt.anchorMin = new Vector2(0.5f, 0.5f);
            colRt.anchorMax = new Vector2(0.5f, 0.5f);
            colRt.pivot = new Vector2(0.5f, 0.5f);
            colRt.anchoredPosition = new Vector2(0f, -40f);
            colRt.sizeDelta = new Vector2(420f, 260f);

            VerticalLayoutGroup vlg = column.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 16f;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            ContentSizeFitter fitter = column.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Button play = UiRuntimeUtil.CreateButton(column.transform, "PlayButton", buttonSprite, _font, "PLAY", 32);
            Button options = UiRuntimeUtil.CreateButton(column.transform, "OptionsButton", buttonSprite, _font, "OPTIONS", 28);
            Button quit = UiRuntimeUtil.CreateButton(column.transform, "QuitButton", buttonSprite, _font, "QUIT", 28);
            _mainButtons = new Button[] { play, options, quit };

            SetButtonSize(play, 420f, 72f);
            SetButtonSize(options, 420f, 72f);
            SetButtonSize(quit, 420f, 72f);

            play.onClick.AddListener(OnPlay);
            options.onClick.AddListener(ShowOptions);
            quit.onClick.AddListener(OnQuit);

            // Selection highlight (uses UI/selected.png)
            Sprite selectedSprite = _sprites.LoadSingle("NSMB/UI/selected");
            _selectionHighlight = UiRuntimeUtil.CreateImage(column.transform, "Selection", selectedSprite);
            _selectionHighlight.transform.SetAsFirstSibling();
            _selectionHighlight.preserveAspect = false;
            _selectionHighlight.color = Color.white;
            RectTransform selRt = _selectionHighlight.rectTransform;
            selRt.anchorMin = new Vector2(0.5f, 0.5f);
            selRt.anchorMax = new Vector2(0.5f, 0.5f);
            selRt.pivot = new Vector2(0.5f, 0.5f);
            selRt.sizeDelta = new Vector2(440f, 82f);

            // Footer hint
            Text hint = UiRuntimeUtil.CreateText(_mainPanel.transform, "Hint", UiRuntimeUtil.LoadFontOrDefault("Fonts/TTFs/PixelMplus12-Regular"), 18, TextAnchor.LowerCenter, Color.white);
            hint.text = "Press ESC to pause in-game";
            RectTransform hintRt = hint.rectTransform;
            hintRt.anchorMin = new Vector2(0.5f, 0f);
            hintRt.anchorMax = new Vector2(0.5f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.anchoredPosition = new Vector2(0f, 16f);
            hintRt.sizeDelta = new Vector2(900f, 28f);

            BuildPressStart();
            BuildOptionsPanel(buttonSprite);
            ResetToPressStart();
        }

        private void BuildBackground() {
            // Original menubg is a spritesheet (menubg_0..menubg_4) that spans 1280px width.
            // If slicing isn't imported yet, UiSpriteStore.LoadSingle will fall back to the texture.
            string sheet = "NSMB/UI/Menu/menubg";

            Sprite first = _sprites.LoadFromSheet(sheet, "menubg_0");
            if (first == null) {
                #if UNITY_EDITOR
                Debug.LogWarning("[NSMB] Menu background sprites not found (menubg_0). Run NSMB/Resync Sprite Import Settings (From Original) to import slicing.");
                #endif
                // Fallback: use a single image background so we never end up with a white screen.
                Sprite bgSprite = _sprites.LoadSingle("NSMB/UI/uibackground");
                if (bgSprite == null) {
                    bgSprite = _sprites.LoadSingle(sheet);
                }

                Image bg = UiRuntimeUtil.CreateImage(_root.transform, "Background", bgSprite);
                bg.preserveAspect = false;
                RectTransform bgRt = bg.rectTransform;
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;
                return;
            }

            GameObject tiles = UiRuntimeUtil.CreateUiObject("BackgroundTiles", _root.transform);
            RectTransform tilesRt = tiles.GetComponent<RectTransform>();
            tilesRt.anchorMin = new Vector2(0f, 0f);
            tilesRt.anchorMax = new Vector2(1f, 0f);
            tilesRt.pivot = new Vector2(0.5f, 0f);
            tilesRt.anchoredPosition = Vector2.zero;
            tilesRt.sizeDelta = new Vector2(0f, 192f);

            // Build 5 horizontal tiles (256px each) to cover 1280 reference width.
            for (int i = 0; i < 5; i++) {
                Sprite tile = _sprites.LoadFromSheet(sheet, "menubg_" + i);
                if (tile == null) {
                    // Handle cases where names aren't imported (older/partial reimport).
                    Sprite[] all = Resources.LoadAll<Sprite>(sheet);
                    if (all != null && all.Length > i && all[i] != null) {
                        tile = all[i];
                    } else if (all != null && all.Length > 0) {
                        tile = all[0];
                    }
                }

                Image img = UiRuntimeUtil.CreateImage(tiles.transform, "Tile_" + i, tile);
                img.preserveAspect = false;
                RectTransform rt = img.rectTransform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(256f * i, 0f);
                rt.sizeDelta = new Vector2(256f, 192f);
            }
        }

        private void BuildPressStart() {
            Sprite startPrompt = _sprites.LoadSingle("NSMB/UI/Menu/Elements/start-prompt");
            Sprite aPrompt = _sprites.LoadSingle("NSMB/UI/Menu/Elements/a-prompt");

            Image prompt = UiRuntimeUtil.CreateImage(_pressStartPanel.transform, "StartPrompt", startPrompt);
            RectTransform pRt = prompt.rectTransform;
            pRt.anchorMin = new Vector2(0.5f, 0f);
            pRt.anchorMax = new Vector2(0.5f, 0f);
            pRt.pivot = new Vector2(0.5f, 0f);
            pRt.anchoredPosition = new Vector2(0f, 70f);
            pRt.sizeDelta = new Vector2(520f, 64f);

            Image a = UiRuntimeUtil.CreateImage(_pressStartPanel.transform, "APrompt", aPrompt);
            RectTransform aRt = a.rectTransform;
            aRt.anchorMin = new Vector2(0.5f, 0f);
            aRt.anchorMax = new Vector2(0.5f, 0f);
            aRt.pivot = new Vector2(0.5f, 0f);
            aRt.anchoredPosition = new Vector2(-300f, 66f);
            aRt.sizeDelta = new Vector2(64f, 64f);

            Text t = UiRuntimeUtil.CreateText(_pressStartPanel.transform, "PressStartText", UiRuntimeUtil.LoadFontOrDefault("Fonts/TTFs/PixelMplus12-Bold"), 22, TextAnchor.LowerCenter, Color.white);
            t.text = "PRESS START";
            RectTransform tRt = t.rectTransform;
            tRt.anchorMin = new Vector2(0.5f, 0f);
            tRt.anchorMax = new Vector2(0.5f, 0f);
            tRt.pivot = new Vector2(0.5f, 0f);
            tRt.anchoredPosition = new Vector2(0f, 30f);
            tRt.sizeDelta = new Vector2(500f, 28f);

            // Small build string
            Text ver = UiRuntimeUtil.CreateText(_pressStartPanel.transform, "Version", UiRuntimeUtil.LoadFontOrDefault("Fonts/TTFs/PixelMplus12-Regular"), 16, TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0.9f));
            ver.text = Application.productName + " (Wii U/2017)";
            RectTransform verRt = ver.rectTransform;
            verRt.anchorMin = new Vector2(1f, 0f);
            verRt.anchorMax = new Vector2(1f, 0f);
            verRt.pivot = new Vector2(1f, 0f);
            verRt.anchoredPosition = new Vector2(-16f, 10f);
            verRt.sizeDelta = new Vector2(520f, 22f);
        }

        private static void SetButtonSize(Button b, float w, float h) {
            RectTransform rt = b.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);
        }

        private void ResetToPressStart() {
            _selectedIndex = 0;
            _moveCooldown = 0f;
            _mode = MenuMode.PressStart;
            if (_pressStartPanel != null) _pressStartPanel.SetActive(true);
            if (_mainPanel != null) _mainPanel.SetActive(false);
            if (_optionsPanel != null) _optionsPanel.SetActive(false);
        }

        private void TickPressStart() {
            // bob the prompt a bit
            if (_pressStartPanel != null) {
                RectTransform rt = _pressStartPanel.GetComponent<RectTransform>();
                if (rt != null) {
                    // no-op; keep panel stable
                }

                Transform prompt = _pressStartPanel.transform.Find("StartPrompt");
                if (prompt != null) {
                    RectTransform p = prompt.GetComponent<RectTransform>();
                    if (p != null) {
                        float y = 70f + Mathf.Sin(Time.unscaledTime * 3.2f) * 6f;
                        p.anchoredPosition = new Vector2(0f, y);
                    }
                }
            }

            if (NSMB.Input.LegacyInput.GetSubmitDown()) {
                PlayUi(NSMB.Audio.SoundEffectId.UI_WindowOpen);
                _mode = MenuMode.MainButtons;
                if (_pressStartPanel != null) _pressStartPanel.SetActive(false);
                if (_mainPanel != null) _mainPanel.SetActive(true);
                if (_optionsPanel != null) _optionsPanel.SetActive(false);
                Canvas.ForceUpdateCanvases();
                SelectIndex(0, false);
            }
        }

        private void TickMainButtons() {
            if (_moveCooldown > 0f) {
                _moveCooldown -= Time.unscaledDeltaTime;
                if (_moveCooldown < 0f) _moveCooldown = 0f;
            }

            Vector2 move = NSMB.Input.LegacyInput.GetMovement();
            if (_moveCooldown <= 0f) {
                if (move.y > 0.5f) {
                    SelectIndex(_selectedIndex - 1, true);
                    _moveCooldown = 0.18f;
                } else if (move.y < -0.5f) {
                    SelectIndex(_selectedIndex + 1, true);
                    _moveCooldown = 0.18f;
                }
            }

            if (NSMB.Input.LegacyInput.GetSubmitDown()) {
                if (_mainButtons != null && _selectedIndex >= 0 && _selectedIndex < _mainButtons.Length && _mainButtons[_selectedIndex] != null) {
                    _mainButtons[_selectedIndex].onClick.Invoke();
                }
            } else if (NSMB.Input.LegacyInput.GetBackDown()) {
                PlayUi(NSMB.Audio.SoundEffectId.UI_Back);
                ResetToPressStart();
            }

            UpdateSelectionHighlight();
        }

        private void SelectIndex(int index, bool playSound) {
            if (_mainButtons == null || _mainButtons.Length == 0) {
                return;
            }

            if (index < 0) index = _mainButtons.Length - 1;
            if (index >= _mainButtons.Length) index = 0;

            if (index != _selectedIndex && playSound) {
                PlayUi(NSMB.Audio.SoundEffectId.UI_Cursor);
            }

            _selectedIndex = index;
            UpdateSelectionHighlight();

            Button b = _mainButtons[_selectedIndex];
            if (b != null) {
                b.Select();
            }
        }

        private void UpdateSelectionHighlight() {
            if (_selectionHighlight == null || _mainButtons == null || _selectedIndex < 0 || _selectedIndex >= _mainButtons.Length) {
                return;
            }

            RectTransform btnRt = _mainButtons[_selectedIndex].GetComponent<RectTransform>();
            RectTransform selRt = _selectionHighlight.rectTransform;
            if (btnRt != null && selRt != null) {
                selRt.anchoredPosition = btnRt.anchoredPosition;
            }
        }

        private static void PlayUi(NSMB.Audio.SoundEffectId sound) {
            NSMB.Core.GameRoot root = NSMB.Core.GameRoot.Instance;
            if (root == null) {
                return;
            }
            NSMB.Audio.AudioManager audio = root.GetComponent<NSMB.Audio.AudioManager>();
            if (audio != null) {
                audio.PlayOneShot(sound, 0.9f);
            }
        }

        private static void OnPlay() {
            PlayUi(NSMB.Audio.SoundEffectId.UI_Decide);
            NSMB.Gameplay.GameFlow flow = NSMB.Gameplay.GameFlow.Instance;
            if (flow != null) {
                flow.StartGame();
            }
        }

        private static void OnQuit() {
            PlayUi(NSMB.Audio.SoundEffectId.UI_Decide);
            Application.Quit();
        }

        private void ShowMain() {
            _mode = MenuMode.MainButtons;
            if (_mainPanel != null) _mainPanel.SetActive(true);
            if (_optionsPanel != null) _optionsPanel.SetActive(false);
            SelectIndex(_selectedIndex, false);
        }

        private void ShowOptions() {
            _mode = MenuMode.Options;
            PlayUi(NSMB.Audio.SoundEffectId.UI_Decide);
            PlayUi(NSMB.Audio.SoundEffectId.UI_WindowOpen);
            if (_mainPanel != null) _mainPanel.SetActive(false);
            if (_optionsPanel != null) _optionsPanel.SetActive(true);
        }

        private void BuildOptionsPanel(Sprite buttonSprite) {
            _optionsPanel = UiRuntimeUtil.CreateUiObject("OptionsPanel", _root.transform);
            RectTransform optRt = _optionsPanel.GetComponent<RectTransform>();
            optRt.anchorMin = Vector2.zero;
            optRt.anchorMax = Vector2.one;
            optRt.offsetMin = Vector2.zero;
            optRt.offsetMax = Vector2.zero;

            Sprite panelSprite = _sprites.LoadSingle("NSMB/UI/options/optionbox");
            if (panelSprite == null) {
                panelSprite = _sprites.LoadSingle("NSMB/UI/Menu/Elements/rounded-rect-5px");
            }

            Image panel = UiRuntimeUtil.CreateImage(_optionsPanel.transform, "Panel", panelSprite);
            panel.type = Image.Type.Sliced;
            RectTransform panelRt = panel.rectTransform;
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(720f, 420f);

            Text title = UiRuntimeUtil.CreateText(panel.transform, "Title", _font, 36, TextAnchor.UpperCenter, Color.black);
            title.text = "OPTIONS";
            RectTransform titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -28f);
            titleRt.sizeDelta = new Vector2(400f, 50f);

            // SFX volume slider
            Text sfxLabel = UiRuntimeUtil.CreateText(panel.transform, "SfxLabel", UiRuntimeUtil.LoadFontOrDefault("Fonts/TTFs/PixelMplus12-Bold"), 20, TextAnchor.MiddleLeft, Color.black);
            sfxLabel.text = "SFX VOLUME";
            RectTransform sfxLabelRt = sfxLabel.rectTransform;
            sfxLabelRt.anchorMin = new Vector2(0f, 0.65f);
            sfxLabelRt.anchorMax = new Vector2(0f, 0.65f);
            sfxLabelRt.pivot = new Vector2(0f, 0.5f);
            sfxLabelRt.anchoredPosition = new Vector2(60f, 0f);
            sfxLabelRt.sizeDelta = new Vector2(240f, 28f);

            GameObject sliderGo = UiRuntimeUtil.CreateUiObject("SfxSlider", panel.transform);
            Slider slider = sliderGo.AddComponent<Slider>();
            RectTransform sliderRt = sliderGo.GetComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0f, 0.55f);
            sliderRt.anchorMax = new Vector2(0f, 0.55f);
            sliderRt.pivot = new Vector2(0f, 0.5f);
            sliderRt.anchoredPosition = new Vector2(60f, 0f);
            sliderRt.sizeDelta = new Vector2(520f, 24f);

            // Basic slider visuals
            Image bg = sliderGo.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.9f);

            GameObject fillArea = UiRuntimeUtil.CreateUiObject("FillArea", sliderGo.transform);
            RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0f);
            fillAreaRt.anchorMax = new Vector2(1f, 1f);
            fillAreaRt.offsetMin = new Vector2(6f, 6f);
            fillAreaRt.offsetMax = new Vector2(-6f, -6f);

            GameObject fill = UiRuntimeUtil.CreateUiObject("Fill", fillArea.transform);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.6f, 1f, 1f);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            GameObject handle = UiRuntimeUtil.CreateUiObject("Handle", sliderGo.transform);
            Image handleImg = handle.AddComponent<Image>();
            handleImg.sprite = buttonSprite;
            handleImg.type = Image.Type.Sliced;
            RectTransform handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(26f, 26f);

            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;

            NSMB.Audio.AudioManager audio = NSMB.Core.GameRoot.Instance != null ? NSMB.Core.GameRoot.Instance.GetComponent<NSMB.Audio.AudioManager>() : null;
            slider.value = audio != null ? audio.SfxVolume : 1f;
            slider.onValueChanged.AddListener(OnSfxVolumeChanged);

            Button back = UiRuntimeUtil.CreateButton(panel.transform, "BackButton", buttonSprite, _font, "BACK", 24);
            RectTransform backRt = back.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0.5f, 0f);
            backRt.anchorMax = new Vector2(0.5f, 0f);
            backRt.pivot = new Vector2(0.5f, 0f);
            backRt.anchoredPosition = new Vector2(0f, 26f);
            backRt.sizeDelta = new Vector2(260f, 60f);
            back.onClick.AddListener(delegate {
                PlayUi(NSMB.Audio.SoundEffectId.UI_WindowClose);
                ShowMain();
            });
        }

        private static void OnSfxVolumeChanged(float v) {
            NSMB.Core.GameRoot root = NSMB.Core.GameRoot.Instance;
            if (root == null) {
                return;
            }
            NSMB.Audio.AudioManager audio = root.GetComponent<NSMB.Audio.AudioManager>();
            if (audio != null) {
                audio.SfxVolume = v;
            }
        }
    }
}
