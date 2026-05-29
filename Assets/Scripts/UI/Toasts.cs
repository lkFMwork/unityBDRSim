using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    public enum ToastKind { Info, Positive, Warning, Danger }

    /// <summary>
    /// Lightweight, non-interactive toast notifications stacked at the top of the
    /// screen. Self-bootstraps into its own persistent overlay canvas with no
    /// GraphicRaycaster and every graphic set non-raycast, so it renders above
    /// everything yet never steals input. Any system can call
    /// <see cref="Show"/> for feedback — commissions, churn, new tenders, level-ups.
    /// </summary>
    public class Toasts : MonoBehaviour
    {
        private static Toasts _instance;
        private Transform _stack;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("[Toasts]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<Toasts>();
            _instance.Build();
        }

        public static void Show(string message, ToastKind kind = ToastKind.Info)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (_instance == null) Bootstrap();
            if (_instance != null) _instance.Spawn(message, kind);
        }

        private void Build()
        {
            var canvasGo = new GameObject("ToastCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // above all gameplay UI

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            var stackGo = new GameObject("Stack", typeof(RectTransform));
            stackGo.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)stackGo.transform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -16f);
            rt.sizeDelta = new Vector2(560f, 0f);

            var v = stackGo.AddComponent<VerticalLayoutGroup>();
            v.spacing = 8;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var fitter = stackGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _stack = stackGo.transform;
        }

        private void Spawn(string message, ToastKind kind)
        {
            if (_stack == null) Build();

            var panel = UiFactory.Panel(_stack, Bg(kind), "Toast");
            panel.raycastTarget = false;
            var le = panel.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 40f;

            var label = UiFactory.Label(panel.transform, message, 15, Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            label.raycastTarget = false;
            UiFactory.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(16f, 8f);
            label.rectTransform.offsetMax = new Vector2(-16f, -8f);

            var cg = panel.gameObject.AddComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;

            Tween.FadeIn(cg, 0.2f);
            Tween.PunchScale(panel.transform, 0.10f, 0.24f);
            Sfx.ForToast(kind);
            StartCoroutine(Dismiss(panel.gameObject, cg, 2.6f));
        }

        private IEnumerator Dismiss(GameObject go, CanvasGroup cg, float life)
        {
            float t = 0f;
            while (t < life)
            {
                if (go == null) yield break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            float f = 0f;
            while (f < 0.35f)
            {
                if (go == null || cg == null) yield break;
                f += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, f / 0.35f);
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        private static Color Bg(ToastKind kind) => kind switch
        {
            ToastKind.Positive => new Color(0.16f, 0.45f, 0.30f, 0.96f),
            ToastKind.Warning => new Color(0.55f, 0.42f, 0.12f, 0.96f),
            ToastKind.Danger => new Color(0.55f, 0.20f, 0.20f, 0.96f),
            _ => new Color(0.12f, 0.16f, 0.26f, 0.96f)
        };
    }
}
