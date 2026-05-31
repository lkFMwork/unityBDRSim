using System.Collections.Generic;
using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Loads 2D sprites from <c>Resources/Sprites</c>, with a generated solid-color square
    /// fallback when a sprite isn't imported yet — so the 2D platformer is fully playable on
    /// placeholders today and lights up with real Kenney Pixel Platformer art the moment the
    /// sheet is dropped in (same workflow as ModelLibrary for 3D). Sprites are point-filtered
    /// for crisp pixels. Drop the pack's individual sprites (or sliced sheet) under
    /// <c>Assets/Resources/Sprites/platformer/&lt;name&gt;.png</c> and they resolve by key.
    /// </summary>
    public static class SpriteLibrary
    {
        public const string Root = "Sprites/";

        private static readonly Dictionary<string, Sprite> _cache = new();
        private static readonly Dictionary<Color, Sprite> _solids = new();

        // A key may be a bare name (gets Sprites/ prefixed) or an already-full Resources path
        // (used as-is) — so pack sprites under Models/... resolve without moving files.
        private static string Resolve(string key) =>
            key.StartsWith("Models/") || key.StartsWith(Root) ? key : Root + key;

        /// <summary>True if a real sprite exists for this key (vs. a placeholder).</summary>
        public static bool Has(string key) => Resources.Load<Sprite>(Resolve(key)) != null;

        /// <summary>
        /// Load a sprite by key (e.g. "platformer/character_purple_walk_a"), or a solid-color
        /// placeholder square of <paramref name="fallback"/> if it's missing.
        /// </summary>
        public static Sprite Get(string key, Color fallback)
        {
            if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;
            var sprite = Resources.Load<Sprite>(Resolve(key));
            if (sprite == null) sprite = Solid(fallback);
            _cache[key] = sprite;
            return sprite;
        }

        /// <summary>First sprite found among the given keys (for animation-frame fallbacks).</summary>
        public static Sprite GetAny(Color fallback, params string[] keys)
        {
            foreach (var k in keys)
            {
                var s = Resources.Load<Sprite>(Resolve(k));
                if (s != null) return s;
            }
            return Solid(fallback);
        }

        /// <summary>A 1x1 white sprite tinted via SpriteRenderer.color — cached per color.</summary>
        public static Sprite Solid(Color color)
        {
            if (_solids.TryGetValue(color, out var s) && s != null) return s;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = color;
            tex.SetPixels(px);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            _solids[color] = sprite;
            return sprite;
        }
    }
}
