using UnityEngine;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// Player settings — master volume, mute, graphics quality tier, and a
    /// reduced-motion accessibility toggle — persisted via PlayerPrefs and applied to
    /// the live systems (Sfx, Tween, QualitySettings). Loads and applies before the
    /// first scene so preferences take effect immediately.
    /// </summary>
    public static class SettingsService
    {
        private const string KVol = "fm_volume";
        private const string KMute = "fm_muted";
        private const string KQual = "fm_quality";
        private const string KMotion = "fm_reducedmotion";

        public static float MasterVolume { get; private set; } = 0.55f;
        public static bool Muted { get; private set; }
        public static int QualityLevel { get; private set; }
        public static bool ReducedMotion { get; private set; }

        public static int QualityCount => Mathf.Max(1, QualitySettings.names.Length);
        public static string QualityName =>
            (QualityLevel >= 0 && QualityLevel < QualitySettings.names.Length)
                ? QualitySettings.names[QualityLevel] : "Default";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            MasterVolume = PlayerPrefs.GetFloat(KVol, 0.55f);
            Muted = PlayerPrefs.GetInt(KMute, 0) == 1;
            QualityLevel = PlayerPrefs.GetInt(KQual, QualitySettings.GetQualityLevel());
            ReducedMotion = PlayerPrefs.GetInt(KMotion, 0) == 1;
            Apply();
        }

        public static void SetVolume(float v) { MasterVolume = Mathf.Clamp01(v); Save(); Apply(); }
        public static void ToggleMute() { Muted = !Muted; Save(); Apply(); }
        public static void CycleQuality() { QualityLevel = (QualityLevel + 1) % QualityCount; Save(); Apply(); }
        public static void ToggleReducedMotion() { ReducedMotion = !ReducedMotion; Save(); Apply(); }

        private static void Save()
        {
            PlayerPrefs.SetFloat(KVol, MasterVolume);
            PlayerPrefs.SetInt(KMute, Muted ? 1 : 0);
            PlayerPrefs.SetInt(KQual, QualityLevel);
            PlayerPrefs.SetInt(KMotion, ReducedMotion ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static void Apply()
        {
            Sfx.Volume = MasterVolume;
            Sfx.Muted = Muted;
            Tween.ReducedMotion = ReducedMotion;
            if (QualityLevel >= 0 && QualityLevel < QualitySettings.names.Length)
                QualitySettings.SetQualityLevel(QualityLevel, true);
        }
    }
}
