using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// A brief fade-in-from-black on every scene load, for smooth transitions. Lives on
    /// a persistent, non-interactive overlay (never blocks input) and honors
    /// reduced-motion. Self-bootstraps.
    /// </summary>
    public class SceneFade : MonoBehaviour
    {
        private static SceneFade _instance;
        private Image _img;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("[SceneFade]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SceneFade>();
            _instance.Build();
            SceneManager.sceneLoaded += _instance.OnSceneLoaded;
            _instance.FadeIn();
        }

        private void Build()
        {
            var canvasGo = new GameObject("FadeCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 195;

            var imgGo = new GameObject("Black", typeof(RectTransform), typeof(Image));
            imgGo.transform.SetParent(canvasGo.transform, false);
            _img = imgGo.GetComponent<Image>();
            _img.color = new Color(0f, 0f, 0f, 0f);
            _img.raycastTarget = false; // visual only — never blocks clicks
            UiFactory.Stretch(_img.rectTransform);
        }

        private void OnDestroy()
        {
            if (_instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => FadeIn();

        private void FadeIn()
        {
            if (_img == null) return;
            if (Tween.ReducedMotion) { _img.color = new Color(0f, 0f, 0f, 0f); return; }
            StopAllCoroutines();
            StartCoroutine(FadeRoutine());
        }

        private IEnumerator FadeRoutine()
        {
            float dur = 0.35f, t = 0f;
            while (t < dur)
            {
                if (_img == null) yield break;
                t += Time.unscaledDeltaTime;
                _img.color = new Color(0f, 0f, 0f, Mathf.Lerp(1f, 0f, t / dur));
                yield return null;
            }
            if (_img != null) _img.color = new Color(0f, 0f, 0f, 0f);
        }
    }
}
