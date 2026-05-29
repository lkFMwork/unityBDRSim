using System;
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

        public static Image Panel(Transform parent, Color color, string name = "Panel")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
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

        public static Text Label(Transform parent, string content, int size, Color color,
            TextAnchor align = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal,
            string name = "Label")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.color = color;
            t.text = content;
            t.alignment = align;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            return t;
        }

        public static Button Button(Transform parent, string label, Action onClick,
            Color bg, Color fg, int size = 18, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = bg;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var label2 = Label(go.transform, label, size, fg, align, FontStyle.Normal, "Text");
            var lrt = label2.rectTransform;
            Stretch(lrt);
            lrt.offsetMin = new Vector2(14f, 6f);
            lrt.offsetMax = new Vector2(-14f, -6f);

            if (onClick != null)
                btn.onClick.AddListener(() => onClick());

            return btn;
        }

        // ---- meters ---------------------------------------------------------

        /// <summary>A horizontal fill meter whose width tracks a 0..1 value.</summary>
        public class Meter
        {
            public RectTransform Fill;
            public Text Caption;
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
            public Text Text;

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
            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(Text),
                typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            var text = contentGo.GetComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.color = fg;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
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

            var textT = Label(go.transform, string.Empty, 16, UiTheme.TextPrimary,
                TextAnchor.MiddleLeft, FontStyle.Normal, "Text");
            textT.supportRichText = false;
            var trt = textT.rectTransform;
            Stretch(trt);
            trt.offsetMin = new Vector2(10f, 4f);
            trt.offsetMax = new Vector2(-10f, -4f);

            var phT = Label(go.transform, placeholder, 16, UiTheme.TextMuted,
                TextAnchor.MiddleLeft, FontStyle.Italic, "Placeholder");
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
