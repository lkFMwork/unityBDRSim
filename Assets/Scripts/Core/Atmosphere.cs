using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fitzmark.BDRSim.Core
{
    /// <summary>
    /// Scene presentation, applied once per scene load by <see cref="GameManager"/>:
    /// 1) a soft three-color ambient fill + subtle distance fog (core APIs, always on),
    ///    so the flat-shaded world reads with depth/warmth instead of raw greybox; and
    /// 2) a URP post-processing stack (tonemapping, bloom, color grading, vignette) set
    ///    up via reflection — no compile-time URP dependency, and fully guarded so a
    ///    version/type mismatch degrades to "no post" with a warning rather than
    ///    breaking the game. Screen-space-overlay UI composites after post, so it stays
    ///    crisp.
    /// </summary>
    public static class Atmosphere
    {
        private const string VolumeName = "Fitzmark Post Volume";
        private static bool _postDisabled;

        public static void Apply()
        {
            ApplyAmbientAndFog();
            SetupPostProcessing();
        }

        // ---- ambient + fog (core, always safe) ------------------------------

        private static void ApplyAmbientAndFog()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.66f, 0.74f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.44f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.18f, 0.22f);

            var cam = Camera.main;
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

        // ---- URP post-processing (reflection, best-effort) ------------------

        private static void SetupPostProcessing()
        {
            if (_postDisabled) return;
            var cam = Camera.main;
            if (cam == null) return;

            try
            {
                // Enable post on the URP camera data (auto-added by URP; add if missing).
                Type addType = FindType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
                if (addType == null) { _postDisabled = true; return; } // not a URP project — bail quietly
                var camData = cam.GetComponent(addType);
                if (camData == null) camData = cam.gameObject.AddComponent(addType);
                SetMember(camData, "renderPostProcessing", true);

                // One global volume per scene (scene-owned, so it unloads with the scene).
                if (GameObject.Find(VolumeName) != null) return;

                Type volumeType = FindType("UnityEngine.Rendering.Volume");
                Type profileType = FindType("UnityEngine.Rendering.VolumeProfile");
                if (volumeType == null || profileType == null) { _postDisabled = true; return; }

                var profile = ScriptableObject.CreateInstance(profileType);
                MethodInfo add = profileType.GetMethod("Add", new[] { typeof(Type), typeof(bool) });
                if (add == null) { _postDisabled = true; return; }

                // Tonemapping — filmic response (Neutral is the safe, non-aggressive choice).
                var tone = add.Invoke(profile, new object[] { FindType("UnityEngine.Rendering.Universal.Tonemapping"), true });
                SetEnumParam(tone, "mode", "Neutral");

                // Bloom — soft glow on the brightest pixels.
                var bloom = add.Invoke(profile, new object[] { FindType("UnityEngine.Rendering.Universal.Bloom"), true });
                SetFloatParam(bloom, "intensity", 0.7f);
                SetFloatParam(bloom, "threshold", 0.9f);
                SetFloatParam(bloom, "scatter", 0.6f);

                // Color adjustments — a gentle pop.
                var grade = add.Invoke(profile, new object[] { FindType("UnityEngine.Rendering.Universal.ColorAdjustments"), true });
                SetFloatParam(grade, "postExposure", 0.08f);
                SetFloatParam(grade, "contrast", 8f);
                SetFloatParam(grade, "saturation", 6f);

                // Vignette — focus the frame.
                var vignette = add.Invoke(profile, new object[] { FindType("UnityEngine.Rendering.Universal.Vignette"), true });
                SetFloatParam(vignette, "intensity", 0.26f);
                SetFloatParam(vignette, "smoothness", 0.4f);

                var go = new GameObject(VolumeName);
                var volume = go.AddComponent(volumeType);
                SetMember(volume, "isGlobal", true);
                SetMember(volume, "priority", 1f);
                SetMember(volume, "sharedProfile", profile);
            }
            catch (Exception e)
            {
                _postDisabled = true; // don't spam every scene load
                Debug.LogWarning("[Fitzmark BDR] Post-processing setup skipped (non-fatal): " + e.Message);
            }
        }

        // ---- reflection helpers --------------------------------------------

        private static void SetFloatParam(object effect, string param, float v)
        {
            var p = GetParam(effect, param);
            if (p == null) return;
            var valueProp = p.GetType().GetProperty("value");
            if (valueProp != null && valueProp.PropertyType == typeof(float)) valueProp.SetValue(p, v);
            SetOverride(p);
        }

        private static void SetEnumParam(object effect, string param, string enumName)
        {
            var p = GetParam(effect, param);
            if (p == null) return;
            var valueProp = p.GetType().GetProperty("value");
            if (valueProp != null && valueProp.PropertyType.IsEnum)
                valueProp.SetValue(p, Enum.Parse(valueProp.PropertyType, enumName));
            SetOverride(p);
        }

        private static object GetParam(object effect, string fieldName)
        {
            var f = effect?.GetType().GetField(fieldName);
            return f?.GetValue(effect);
        }

        private static void SetOverride(object param)
        {
            var os = param.GetType().GetField("overrideState");
            if (os != null && os.FieldType == typeof(bool)) os.SetValue(param, true);
        }

        private static void SetMember(object obj, string name, object value)
        {
            if (obj == null) return;
            var t = obj.GetType();
            var f = t.GetField(name);
            if (f != null) { f.SetValue(obj, value); return; }
            var p = t.GetProperty(name);
            if (p != null && p.CanWrite) p.SetValue(obj, value);
        }

        private static Type FindType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }
    }
}
