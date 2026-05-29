using UnityEngine;
using UnityEngine.Rendering;

namespace Fitzmark.BDRSim.Core
{
    /// <summary>
    /// Lightweight, dependency-free scene atmosphere: a soft three-color ambient fill
    /// plus subtle distance fog keyed to the camera background, so the flat-shaded
    /// world reads with depth and warmth instead of looking like raw greybox. Uses only
    /// core UnityEngine APIs (no URP compile-time dependency); the post-processing pass
    /// (bloom / tonemapping / color grading) layers on top in a later step. Applied once
    /// per scene load by <see cref="GameManager"/>.
    /// </summary>
    public static class Atmosphere
    {
        public static void Apply()
        {
            // Soft trilight ambient lifts flat shapes without washing them out.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.66f, 0.74f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.44f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.18f, 0.22f);

            var cam = Camera.main;
            // Distance fog tuned to the camera background gives depth in 3D scenes and is
            // invisible on flat UI-only scenes (geometry is either absent or too close).
            if (cam != null && cam.clearFlags == CameraClearFlags.SolidColor)
            {
                float far = Mathf.Max(40f, cam.farClipPlane);
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = cam.backgroundColor;
                RenderSettings.fogStartDistance = far * 0.30f;
                RenderSettings.fogEndDistance = far * 0.95f;
            }
            else
            {
                RenderSettings.fog = false;
            }
        }
    }
}
