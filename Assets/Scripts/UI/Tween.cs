using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// Tiny, dependency-free tween/juice helpers backed by a persistent coroutine
    /// runner. Gives the UI life — count-ups on money, a punch on confirm, fades and
    /// shakes — without pulling in an animation package. All driven by unscaled time
    /// so juice still plays if the game pauses. Targets are null-checked every frame,
    /// so it's safe to tween something that may be destroyed mid-animation.
    /// </summary>
    public static class Tween
    {
        /// <summary>When set (accessibility), tweens snap to their end state instantly.</summary>
        public static bool ReducedMotion = false;

        private class Runner : MonoBehaviour { }
        private static Runner _runner;

        private static Runner R
        {
            get
            {
                if (_runner == null)
                {
                    var go = new GameObject("[Tween]") { hideFlags = HideFlags.HideInHierarchy };
                    Object.DontDestroyOnLoad(go);
                    _runner = go.AddComponent<Runner>();
                }
                return _runner;
            }
        }

        /// <summary>Animate a label from one number to another (e.g. a cash balance).</summary>
        public static void CountUp(Text label, float from, float to, string prefix = "$",
            string format = "N0", float dur = 0.5f)
        {
            if (label == null) return;
            if (ReducedMotion) { label.text = prefix + to.ToString(format); return; }
            R.StartCoroutine(CountRoutine(label, from, to, prefix, format, dur));
        }

        private static IEnumerator CountRoutine(Text label, float from, float to, string prefix,
            string format, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                if (label == null) yield break;
                t += Time.unscaledDeltaTime;
                float v = Mathf.Lerp(from, to, Ease(Mathf.Clamp01(t / dur)));
                label.text = prefix + v.ToString(format);
                yield return null;
            }
            if (label != null) label.text = prefix + to.ToString(format);
        }

        /// <summary>A quick scale-up-and-back to acknowledge an action.</summary>
        public static void PunchScale(Transform target, float strength = 0.18f, float dur = 0.28f)
        {
            if (target == null || ReducedMotion) return;
            R.StartCoroutine(PunchRoutine(target, strength, dur));
        }

        private static IEnumerator PunchRoutine(Transform target, float strength, float dur)
        {
            Vector3 baseScale = target.localScale;
            float t = 0f;
            while (t < dur)
            {
                if (target == null) yield break;
                t += Time.unscaledDeltaTime;
                float p = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI); // 0 → 1 → 0
                target.localScale = baseScale * (1f + strength * p);
                yield return null;
            }
            if (target != null) target.localScale = baseScale;
        }

        /// <summary>Fade a CanvasGroup in from transparent.</summary>
        public static void FadeIn(CanvasGroup cg, float dur = 0.25f)
        {
            if (cg == null) return;
            if (ReducedMotion) { cg.alpha = 1f; return; }
            cg.alpha = 0f;
            R.StartCoroutine(FadeRoutine(cg, 0f, 1f, dur));
        }

        private static IEnumerator FadeRoutine(CanvasGroup cg, float from, float to, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                if (cg == null) yield break;
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
                yield return null;
            }
            if (cg != null) cg.alpha = to;
        }

        /// <summary>Decaying positional shake on a RectTransform (e.g. on a failure).</summary>
        public static void Shake(RectTransform target, float amount = 12f, float dur = 0.3f)
        {
            if (target == null || ReducedMotion) return;
            R.StartCoroutine(ShakeRoutine(target, amount, dur));
        }

        private static IEnumerator ShakeRoutine(RectTransform target, float amount, float dur)
        {
            Vector2 home = target.anchoredPosition;
            float t = 0f;
            while (t < dur)
            {
                if (target == null) yield break;
                t += Time.unscaledDeltaTime;
                float damp = 1f - Mathf.Clamp01(t / dur);
                target.anchoredPosition = home + new Vector2(
                    (Random.value * 2f - 1f) * amount * damp,
                    (Random.value * 2f - 1f) * amount * damp);
                yield return null;
            }
            if (target != null) target.anchoredPosition = home;
        }

        private static float Ease(float x) => 1f - Mathf.Pow(1f - x, 3f); // easeOutCubic
    }
}
