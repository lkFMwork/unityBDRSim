using System.Collections.Generic;
using UnityEngine;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>Surface treatment for a library material (stylized, flat by default).</summary>
    public enum MatKind { Matte, Metal, Glass, Emissive, Unlit }

    /// <summary>
    /// Central, batching-friendly material cache. Every runtime prop used to do its
    /// own <c>Shader.Find</c> + <c>new Material</c>, which hammers the CPU and breaks
    /// batching (each object got a unique material instance). Instead, geometry now
    /// shares a small set of materials keyed by (kind, color): the shader is resolved
    /// once and colors snap to a finite palette, so the SRP Batcher / GPU instancing
    /// can fold them together. Default look is a flat, low-smoothness stylized matte.
    /// </summary>
    public static class MaterialLibrary
    {
        private static Shader _lit;
        private static Shader _unlit;
        private static readonly Dictionary<long, Material> _cache = new Dictionary<long, Material>();

        /// <summary>Shared matte material for a color (the common case).</summary>
        public static Material Get(Color color) => Get(MatKind.Matte, color);

        /// <summary>Shared material for a (kind, color) — created once, reused forever.</summary>
        public static Material Get(MatKind kind, Color color)
        {
            long key = Key(kind, color);
            if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;

            Shader shader = kind == MatKind.Unlit ? UnlitShader() : LitShader();
            var m = new Material(shader) { name = $"Lib_{kind}_{ColorUtility.ToHtmlStringRGBA(color)}" };
            m.enableInstancing = true;
            ApplyColor(m, color);
            ApplyKind(m, kind);
            _cache[key] = m;
            return m;
        }

        /// <summary>Assign a shared library material to a GameObject's renderer.</summary>
        public static void Paint(GameObject go, Color color, MatKind kind = MatKind.Matte)
        {
            if (go == null) return;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = Get(kind, color);
        }

        // ---- internals -----------------------------------------------------

        private static Shader LitShader()
        {
            if (_lit != null) return _lit;
            _lit = Shader.Find("Universal Render Pipeline/Lit");
            if (_lit == null) _lit = Shader.Find("Standard");
            if (_lit == null) _lit = Shader.Find("Sprites/Default");
            return _lit;
        }

        private static Shader UnlitShader()
        {
            if (_unlit != null) return _unlit;
            _unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (_unlit == null) _unlit = Shader.Find("Unlit/Color");
            if (_unlit == null) _unlit = LitShader();
            return _unlit;
        }

        private static void ApplyColor(Material m, Color color)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        }

        private static void ApplyKind(Material m, MatKind kind)
        {
            switch (kind)
            {
                case MatKind.Matte:
                    SetFloat(m, "_Smoothness", 0.12f); SetFloat(m, "_Glossiness", 0.12f); SetFloat(m, "_Metallic", 0f);
                    break;
                case MatKind.Metal:
                    SetFloat(m, "_Smoothness", 0.62f); SetFloat(m, "_Glossiness", 0.62f); SetFloat(m, "_Metallic", 0.9f);
                    break;
                case MatKind.Glass:
                    SetFloat(m, "_Smoothness", 0.9f); SetFloat(m, "_Glossiness", 0.9f); SetFloat(m, "_Metallic", 0f);
                    break;
                case MatKind.Emissive:
                    if (m.HasProperty("_EmissionColor"))
                    {
                        Color e = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
                        m.SetColor("_EmissionColor", e);
                        m.EnableKeyword("_EMISSION");
                    }
                    break;
            }
        }

        private static void SetFloat(Material m, string prop, float v)
        {
            if (m.HasProperty(prop)) m.SetFloat(prop, v);
        }

        private static long Key(MatKind kind, Color color)
        {
            int r = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
            int g = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
            int b = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);
            int a = Mathf.Clamp(Mathf.RoundToInt(color.a * 255f), 0, 255);
            long rgba = ((long)r << 24) | ((long)g << 16) | ((long)b << 8) | (long)a;
            return ((long)kind << 40) | rgba;
        }
    }
}
