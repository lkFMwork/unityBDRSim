using UnityEngine;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// Procedural sound effects — every clip is synthesized in code at startup
    /// (short tones / noise with envelopes), so there are no imported audio files and
    /// nothing to license. Self-bootstraps a persistent 2D AudioSource. Wired into
    /// buttons, toasts, and cash gains through single integration points. Volume and
    /// mute are static so a settings screen can drive them.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        public static float Volume = 0.55f;
        public static bool Muted = false;

        private static Sfx _instance;
        private AudioSource _source;
        private AudioClip _click, _confirm, _error, _cash, _whoosh;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("[Sfx]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<Sfx>();
        }

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f; // 2D

            _click = MakeClip("click", 0.05f, t => Mathf.Sin(2f * Mathf.PI * 900f * t) * Decay(t, 45f) * 0.5f);
            _confirm = MakeClip("confirm", 0.20f,
                t => Mathf.Sin(2f * Mathf.PI * (660f + 380f * (t / 0.20f)) * t) * Decay(t, 7f) * 0.45f);
            _error = MakeClip("error", 0.22f,
                t => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 165f * t)) * Decay(t, 7f) * 0.30f);
            _cash = MakeClip("cash", 0.24f, CashSample);
            _whoosh = MakeClip("whoosh", 0.20f,
                t => (Random.value * 2f - 1f) * Mathf.Sin(Mathf.PI * (t / 0.20f)) * 0.22f);
        }

        // ---- public API -----------------------------------------------------

        public static void Click() => Play(_instance != null ? _instance._click : null);
        public static void Confirm() => Play(_instance != null ? _instance._confirm : null);
        public static void Error() => Play(_instance != null ? _instance._error : null);
        public static void Cash() => Play(_instance != null ? _instance._cash : null);
        public static void Whoosh() => Play(_instance != null ? _instance._whoosh : null);

        public static void ForToast(ToastKind kind)
        {
            switch (kind)
            {
                case ToastKind.Positive: Confirm(); break;
                case ToastKind.Danger: Error(); break;
                case ToastKind.Warning: Error(); break;
                default: Click(); break;
            }
        }

        private static void Play(AudioClip clip)
        {
            if (Muted || clip == null || _instance == null || _instance._source == null) return;
            _instance._source.PlayOneShot(clip, Mathf.Clamp01(Volume));
        }

        // ---- synthesis ------------------------------------------------------

        private static float Decay(float t, float rate) => Mathf.Exp(-t * rate);

        private static float CashSample(float t)
        {
            // three quick ascending blips — a little "ka-ching" arpeggio
            const float dur = 0.24f;
            int seg = Mathf.Clamp((int)(t / dur * 3f), 0, 2);
            float[] freqs = { 880f, 1320f, 1760f };
            float segStart = seg * (dur / 3f);
            return Mathf.Sin(2f * Mathf.PI * freqs[seg] * t) * Decay(t - segStart, 28f) * 0.4f;
        }

        private static AudioClip MakeClip(string name, float duration, System.Func<float, float> sample)
        {
            const int sampleRate = 44100;
            int count = Mathf.Max(1, (int)(sampleRate * duration));
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                data[i] = Mathf.Clamp(sample(t), -1f, 1f);
            }
            var clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
