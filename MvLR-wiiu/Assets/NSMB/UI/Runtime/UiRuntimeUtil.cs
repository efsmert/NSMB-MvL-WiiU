using UnityEngine;
using UnityEngine.UI;

namespace NSMB.UI {
    internal static class UiRuntimeUtil {
        public static Font LoadFontOrDefault(string resourcesPath) {
            Font f = Resources.Load(resourcesPath) as Font;
            if (f != null) {
                return f;
            }
            return Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        }

        public static GameObject CreateUiObject(string name, Transform parent) {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        public static Image CreateImage(Transform parent, string name, Sprite sprite) {
            GameObject go = CreateUiObject(name, parent);
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        public static Text CreateText(Transform parent, string name, Font font, int size, TextAnchor anchor, Color color) {
            GameObject go = CreateUiObject(name, parent);
            Text t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Button CreateButton(Transform parent, string name, Sprite background, Font font, string label, int fontSize) {
            GameObject go = CreateUiObject(name, parent);
            Image img = go.AddComponent<Image>();
            img.sprite = background;
            img.type = Image.Type.Sliced;

            Button b = go.AddComponent<Button>();
            b.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = b.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            b.colors = colors;

            Text t = CreateText(go.transform, "Label", font, fontSize, TextAnchor.MiddleCenter, Color.white);
            RectTransform tr = t.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            t.text = label;

            // Ensure readability on both light/dark button sprites (Unity 2017-friendly).
            Shadow shadow = t.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);

            return b;
        }
    }
}
