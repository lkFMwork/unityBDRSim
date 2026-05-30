using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// Small helper library for building a uGUI interface entirely from code.
    /// Everything uses Unity's built-in runtime font and untextured Images
    /// (solid color quads), so no fonts, sprites, prefabs, or asset references are
    /// required for the UI to render — press Play and it's there. A production UI
    /// would migrate this to TextMeshPro / UI Toolkit with authored assets.
    /// </summary>
    public static class UiFactory
    {
        private static Font _font;

        /// <summary>The built-in legacy runtime font (no asset import required).</summary>
        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                return _font;
            }
        }

        // ---- canvas / event system -----------------------------------------

        public static Canvas CreateScreenCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // ---- rect helpers ---------------------------------------------------

        public static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        // ---- containers -----------------------------------------------------

        private static Sprite _rounded;

        /// <summary>A procedurally-generated, 9-sliced rounded-rect sprite for soft UI corners.</summary>
        public static Sprite RoundedSprite
        {
            get
            {
                if (_rounded != null) return _rounded;
                const int size = 48;
                const float r = 12f;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                float half = size / 2f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float px = x + 0.5f - half, py = y + 0.5f - half;
                        float qx = Mathf.Abs(px) - (half - r);
                        float qy = Mathf.Abs(py) - (half - r);
                        float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) +
                                                   Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
                        float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
                        float sdf = outside + inside - r;    // <0 inside, >0 outside
                        float a = Mathf.Clamp01(0.5f - sdf); // 1px antialiased edge
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                tex.Apply();
                _rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                    100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
                return _rounded;
            }
        }

        public static Image Panel(Transform parent, Color color, string name = "Panel")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.sprite = RoundedSprite;
            img.type = Image.Type.Sliced;
            return img;
        }

        public static VerticalLayoutGroup VLayout(GameObject go, int pad = 12, int spacing = 8,
            bool controlW = true, bool controlH = true, bool expandW = true, bool expandH = false,
            TextAnchor align = TextAnchor.UpperLeft)
        {
            var v = go.GetComponent<VerticalLayoutGroup>();
            if (v == null) v = go.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(pad, pad, pad, pad);
            v.spacing = spacing;
            v.childControlWidth = controlW;
            v.childControlHeight = controlH;
            v.childForceExpandWidth = expandW;
            v.childForceExpandHeight = expandH;
            v.childAlignment = align;
            return v;
        }

        public static HorizontalLayoutGroup HLayout(GameObject go, int pad = 0, int spacing = 8,
            bool controlW = true, bool controlH = true, bool expandW = false, bool expandH = true)
        {
            var h = go.GetComponent<HorizontalLayoutGroup>();
            if (h == null) h = go.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(pad, pad, pad, pad);
            h.spacing = spacing;
            h.childControlWidth = controlW;
            h.childControlHeight = controlH;
            h.childForceExpandWidth = expandW;
            h.childForceExpandHeight = expandH;
            return h;
        }

        public static LayoutElement Size(GameObject go, float minW = -1, float prefW = -1, float flexW = -1,
            float minH = -1, float prefH = -1, float flexH = -1)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            if (minW >= 0) le.minWidth = minW;
            if (prefW >= 0) le.preferredWidth = prefW;
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (minH >= 0) le.minHeight = minH;
            if (prefH >= 0) le.preferredHeight = prefH;
            if (flexH >= 0) le.flexibleHeight = flexH;
            return le;
        }

        // ---- text / buttons -------------------------------------------------

        public static TMP_Text Label(Transform parent, string content, int size, Color color,
            TextAnchor align = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal,
            string name = "Label")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.color = color;
            t.text = content;
            t.alignment = ToTmpAlign(align);
            t.fontStyle = ToTmpStyle(style);
            t.richText = true;
            return t;
        }

        /// <summary>Map a legacy <see cref="TextAnchor"/> to a TMP alignment.</summary>
        public static TextAlignmentOptions ToTmpAlign(TextAnchor a) => a switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
            TextAnchor.UpperCenter => TextAlignmentOptions.Top,
            TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
            TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
            TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
            TextAnchor.MiddleRight => TextAlignmentOptions.Right,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
            TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
            TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
            _ => TextAlignmentOptions.TopLeft
        };

        private static FontStyles ToTmpStyle(FontStyle s) => s switch
        {
            FontStyle.Bold => FontStyles.Bold,
            FontStyle.Italic => FontStyles.Italic,
            FontStyle.BoldAndItalic => FontStyles.Bold | FontStyles.Italic,
            _ => FontStyles.Normal
        };

        // Legacy uGUI Text, used only where a component requires it (the legacy InputField).
        private static Text LegacyText(Transform parent, string content, int size, Color color,
            FontStyle style, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.color = color;
            t.text = content;
            t.alignment = TextAnchor.MiddleLeft;
            t.fontStyle = style;
            t.supportRichText = false;
            return t;
        }

        public static Button Button(Transform parent, string label, Action onClick,
            Color bg, Color fg, int size = 18, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = bg;
            img.sprite = RoundedSprite;
            img.type = Image.Type.Sliced;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;                                         // tactile hover/press feedback
            cb.normalColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            cb.highlightedColor = Color.white;
            cb.pressedColor = new Color(0.66f, 0.66f, 0.66f, 1f);
            cb.selectedColor = cb.highlightedColor;
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            cb.fadeDuration = 0.08f;
            btn.colors = cb;

            var label2 = Label(go.transform, label, size, fg, align, FontStyle.Normal, "Text");
            var lrt = label2.rectTransform;
            Stretch(lrt);
            lrt.offsetMin = new Vector2(14f, 6f);
            lrt.offsetMax = new Vector2(-14f, -6f);

            btn.onClick.AddListener(Sfx.Click); // every button clicks
            if (onClick != null)
                btn.onClick.AddListener(() => onClick());

            return btn;
        }

        // ---- meters ---------------------------------------------------------

        /// <summary>A horizontal fill meter whose width tracks a 0..1 value.</summary>
        public class Meter
        {
            public RectTransform Fill;
            public TMP_Text Caption;
            public bool Mirror; // fill from the right (for a right-side fighter bar)

            public void Set(float value01)
            {
                value01 = Mathf.Clamp01(value01);
                if (Mirror)
                {
                    Fill.anchorMin = new Vector2(1f - value01, 0f);
                    Fill.anchorMax = new Vector2(1f, 1f);
                }
                else
                {
                    Fill.anchorMin = new Vector2(0f, 0f);
                    Fill.anchorMax = new Vector2(value01, 1f);
                }
                Fill.offsetMin = Vector2.zero;
                Fill.offsetMax = Vector2.zero;
            }
        }

        public static Meter MakeMeter(Transform parent, string caption, Color fillColor, float height = 22f)
        {
            var container = Panel(parent, new Color(0f, 0f, 0f, 0.35f), "Meter");
            Size(container.gameObject, prefH: height, flexW: 1f);

            var fill = Panel(container.transform, fillColor, "Fill");
            var fillRt = fill.rectTransform;

            var cap = Label(container.transform, caption, 13, Color.white, TextAnchor.MiddleCenter,
                FontStyle.Bold, "Caption");
            Stretch(cap.rectTransform);

            var meter = new Meter { Fill = fillRt, Caption = cap };
            meter.Set(1f);
            return meter;
        }

        // ---- scrolling transcript ------------------------------------------

        public class ScrollLog
        {
            public ScrollRect Scroll;
            public TMP_Text Text;

            public void Clear() => Text.text = string.Empty;

            public void Append(string line)
            {
                if (Text.text.Length > 0) Text.text += "\n";
                Text.text += line;
                Canvas.ForceUpdateCanvases();
                if (Scroll != null) Scroll.verticalNormalizedPosition = 0f;
            }
        }

        public static ScrollLog MakeScrollLog(Transform parent, Color bg, Color fg, int fontSize = 16)
        {
            var rootGo = new GameObject("Transcript", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            rootGo.transform.SetParent(parent, false);
            rootGo.GetComponent<Image>().color = bg;
            var scroll = rootGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            // Viewport (masks the content).
            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGo.transform.SetParent(rootGo.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            Stretch(viewportRt);
            viewportRt.offsetMin = new Vector2(10f, 10f);
            viewportRt.offsetMax = new Vector2(-10f, -10f);

            // Content holds the text and auto-sizes its height.
            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(TextMeshProUGUI),
                typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            var text = contentGo.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = fg;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.richText = true;
            text.text = string.Empty;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRt;
            scroll.content = contentRt;

            return new ScrollLog { Scroll = scroll, Text = text };
        }

        /// <summary>
        /// A vertical scroll view whose content is laid out by a VerticalLayoutGroup
        /// and auto-sizes. Returns the content transform — parent your rows to it.
        /// </summary>
        public static Transform MakeScrollView(Transform parent, Color bg, int pad = 10, int spacing = 8)
        {
            var rootGo = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            rootGo.transform.SetParent(parent, false);
            rootGo.GetComponent<Image>().color = bg;
            var scroll = rootGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGo.transform.SetParent(rootGo.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            Stretch(viewportRt);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            VLayout(contentGo, pad, spacing, controlW: true, controlH: true, expandW: true, expandH: false);

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRt;
            scroll.content = contentRt;
            return contentGo.transform;
        }

        /// <summary>A single-line text input with placeholder, built on the legacy InputField.</summary>
        public static InputField InputField(Transform parent, string placeholder, string value)
        {
            var go = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.10f);
            var input = go.GetComponent<InputField>();

            var textT = LegacyText(go.transform, string.Empty, 16, UiTheme.TextPrimary, FontStyle.Normal, "Text");
            var trt = textT.rectTransform;
            Stretch(trt);
            trt.offsetMin = new Vector2(10f, 4f);
            trt.offsetMax = new Vector2(-10f, -4f);

            var phT = LegacyText(go.transform, placeholder, 16, UiTheme.TextMuted, FontStyle.Italic, "Placeholder");
            var prt = phT.rectTransform;
            Stretch(prt);
            prt.offsetMin = new Vector2(10f, 4f);
            prt.offsetMax = new Vector2(-10f, -4f);

            input.textComponent = textT;
            input.placeholder = phT;
            input.lineType = UnityEngine.UI.InputField.LineType.SingleLine;
            input.characterLimit = 24;
            input.text = value ?? string.Empty;
            return input;
        }
    }
}
