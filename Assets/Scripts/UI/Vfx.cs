using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// Procedural celebration VFX — confetti bursts and floating cash pops — drawn as
    /// short-lived UI quads on a persistent, non-interactive overlay. No particle
    /// assets; entirely code. Respects the reduced-motion setting.
    /// </summary>
    public class Vfx : MonoBehaviour
    {
        private static Vfx _instance;
        private Transform _root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("[Vfx]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<Vfx>();
            _instance.Build();
        }

        public static void Celebrate()
        {
            if (Tween.ReducedMotion) return;
            if (_instance == null) Bootstrap();
            _instance?.Burst(new Vector2(0f, 200f), 28);
        }

        public static void CashPop(float amount)
        {
            if (Tween.ReducedMotion || amount <= 0f) return;
            if (_instance == null) Bootstrap();
            _instance?.FloatLabel($"+${amount:N0}", new Vector2(0f, 90f), UiTheme.Positive);
        }

        // ---- build ----------------------------------------------------------

        private void Build()
        {
            var canvasGo = new GameObject("VfxCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 190; // above gameplay UI, below toasts (200)
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            _root = canvasGo.transform;
        }

        private void Burst(Vector2 origin, int count)
        {
            if (_root == null) Build();
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("confetti", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_root, false);
                var img = go.GetComponent<Image>();
                img.color = Confetti[Random.Range(0, Confetti.Length)];
                img.raycastTarget = false;
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(Random.Range(8f, 16f), Random.Range(8f, 16f));
                rt.anchoredPosition = origin + new Vector2(Random.Range(-60f, 60f), Random.Range(-20f, 20f));
                Vector2 vel = new Vector2(Random.Range(-220f, 220f), Random.Range(150f, 420f));
                StartCoroutine(Fly(go, rt, img, vel));
            }
        }

        private IEnumerator Fly(GameObject go, RectTransform rt, Image img, Vector2 vel)
        {
            float life = Random.Range(1.0f, 1.6f);
            float t = 0f;
            float spin = Random.Range(-360f, 360f);
            Color c0 = img.color;
            while (t < life)
            {
                if (go == null) yield break;
                float dt = Time.unscaledDeltaTime;
                t += dt;
                vel.y -= 680f * dt; // gravity
                rt.anchoredPosition += vel * dt;
                rt.Rotate(0f, 0f, spin * dt);
                img.color = new Color(c0.r, c0.g, c0.b, Mathf.Lerp(1f, 0f, t / life));
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        private void FloatLabel(string text, Vector2 origin, Color color)
        {
            if (_root == null) Build();
            var label = UiFactory.Label(_root, text, 30, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.raycastTarget = false;
            var rt = label.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(260f, 54f);
            rt.anchoredPosition = origin;
            StartCoroutine(Rise(label.gameObject, rt, label));
        }

        private IEnumerator Rise(GameObject go, RectTransform rt, Text label)
        {
            float life = 1.2f, t = 0f;
            Color c0 = label.color;
            while (t < life)
            {
                if (go == null) yield break;
                float dt = Time.unscaledDeltaTime;
                t += dt;
                rt.anchoredPosition += new Vector2(0f, 70f * dt);
                label.color = new Color(c0.r, c0.g, c0.b, Mathf.Lerp(1f, 0f, t / life));
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        private static readonly Color[] Confetti =
        {
            new Color(0.95f, 0.77f, 0.25f), new Color(0.30f, 0.70f, 0.45f), new Color(0.22f, 0.55f, 0.95f),
            new Color(0.85f, 0.30f, 0.30f), new Color(0.65f, 0.45f, 0.85f), Color.white
        };
    }
}
