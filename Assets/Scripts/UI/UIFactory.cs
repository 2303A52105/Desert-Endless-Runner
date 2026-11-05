using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DesertRunner
{
    /// <summary>Runtime uGUI builders (no prefabs or sprites needed).</summary>
    public static class UIFactory
    {
        public static readonly Color PanelColor = new Color(0.16f, 0.09f, 0.05f, 0.82f);
        public static readonly Color Orange = new Color(0.93f, 0.55f, 0.12f);
        public static readonly Color Green = new Color(0.35f, 0.62f, 0.25f);
        public static readonly Color Red = new Color(0.78f, 0.25f, 0.18f);
        public static readonly Color Teal = new Color(0.20f, 0.50f, 0.58f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.25f);
        public static readonly Color Cream = new Color(1f, 0.95f, 0.82f);

        static Sprite rounded, circle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { rounded = null; circle = null; }

        public static Sprite Rounded { get { if (rounded == null) rounded = MakeSprite(64, 22); return rounded; } }
        public static Sprite Circle { get { if (circle == null) circle = MakeSprite(64, 32); return circle; } }

        static Sprite MakeSprite(int size, int radius)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Max(radius - (x + 0.5f), (x + 0.5f) - (size - radius)), 0f);
                    float dy = Mathf.Max(Mathf.Max(radius - (y + 0.5f), (y + 0.5f) - (size - radius)), 0f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(radius - dist + 0.5f));
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave;
            Sprite sp = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            sp.hideFlags = HideFlags.DontSave;
            return sp;
        }

        public static Vector2 A(float x, float y) { return new Vector2(x, y); }

        public static Canvas CreateCanvas(string name, int sortOrder)
        {
            GameObject go = new GameObject(name);
            Canvas c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = sortOrder;
            CanvasScaler cs = go.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        static RectTransform SetRect(GameObject go, Vector2 aMin, Vector2 aMax)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static RectTransform MakePanel(Transform parent, string name, Color color, Vector2 aMin, Vector2 aMax, bool roundedCorners = true)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = SetRect(go, aMin, aMax);
            Image img = go.AddComponent<Image>();
            img.color = color;
            if (roundedCorners) { img.sprite = Rounded; img.type = Image.Type.Sliced; }
            return rt;
        }

        public static Image MakeIcon(Transform parent, string name, Color color, Vector2 aMin, Vector2 aMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            SetRect(go, aMin, aMax);
            Image img = go.AddComponent<Image>();
            img.sprite = Circle;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text MakeLabel(Transform parent, string name, string text, int size, Color color, TextAnchor align, Vector2 aMin, Vector2 aMax, bool outline = true)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            SetRect(go, aMin, aMax);
            Text t = go.AddComponent<Text>();
            t.font = MaterialFactory.BuiltinFont;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 12;
            t.resizeTextMaxSize = size;
            t.raycastTarget = false;
            if (outline)
            {
                Outline o = go.AddComponent<Outline>();
                o.effectColor = new Color(0.15f, 0.07f, 0.02f, 0.9f);
                o.effectDistance = new Vector2(2f, -2f);
            }
            return t;
        }

        public static Button MakeButton(Transform parent, string name, string label, Color color, Vector2 aMin, Vector2 aMax, UnityAction onClick, int fontSize = 44)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            SetRect(go, aMin, aMax);
            Image img = go.AddComponent<Image>();
            img.sprite = Rounded; img.type = Image.Type.Sliced; img.color = color;
            Button b = go.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 1f, 0.9f);
            cb.pressedColor = new Color(0.78f, 0.78f, 0.78f);
            cb.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.85f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            MakeLabel(go.transform, "Label", label, fontSize, Color.white, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));
            b.onClick.AddListener(() => { AudioManager.Play(Sfx.Click); if (onClick != null) onClick(); });
            return b;
        }

        public static void SetButtonLabel(Button b, string text)
        {
            if (b == null) return;
            Text t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }
    }
}
