using UnityEngine;

namespace Fitzmark.BDRSim.Core
{
    /// <summary>
    /// Applies low-end-friendly render settings at startup so the game holds 60fps on the
    /// company's GPU-light machines (and over Remote Desktop). Runs automatically before the
    /// first scene in both the editor and builds — no scene wiring needed. The heavy hitters on
    /// weak/virtual GPUs are real-time shadows, MSAA, and fill rate; this disables the first two
    /// and caps the frame rate. IMPORTANT: judge performance from a real BUILD, not the editor —
    /// the editor + RDP add overhead a shipped build does not.
    /// </summary>
    public static class PerformanceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;            // don't hard-lock to 30 on a 60Hz panel
            QualitySettings.antiAliasing = 0;          // MSAA is costly on weak GPUs
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.softParticles = false;
            QualitySettings.billboardsFaceCameraPosition = false;
            QualitySettings.skinWeights = SkinWeights.TwoBones; // cheaper character skinning
        }
    }
}
