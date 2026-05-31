using System.Collections.Generic;
using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Loads and slices the LPC Overworld pack (Resources/Models/LPC Overworld, 16px tiles) into
    /// the sprites the world map needs: solid terrain fill (grass/sand/snow/dirt), animated water
    /// frames, and whole-object sprites (mountains, hills, forest, trees). Sheets are sliced with
    /// Sprite.Create from the readable source textures (see the importer). Everything is cached.
    /// Falls back to a flat colour sprite if the pack isn't imported yet.
    /// </summary>
    public static class LpcOverworldArt
    {
        public const int Tile = 16;
        private const string Root = "Models/LPC Overworld/";

        private static readonly Dictionary<string, Sprite> _cache = new();
        private static readonly Dictionary<string, Sprite[]> _multiCache = new();

        public static bool Available => LoadTex("Grass/Grass") != null;

        // ---- terrain fill (the solid tileable tile, bottom-left of each terrain sheet) ----
        public static Sprite Grass => Fill("Grass/Grass", new Color(0.30f, 0.55f, 0.27f));
        public static Sprite Sand => Fill("Desert/Sand", new Color(0.78f, 0.70f, 0.45f));
        public static Sprite Snow => Fill("Snow/Snow", new Color(0.85f, 0.88f, 0.93f));
        public static Sprite Dirt => Fill("Path Dirt", new Color(0.55f, 0.42f, 0.28f));

        // A solid fill tile = bottom-left 16x16 of the sheet (the repeating ground in LPC sheets).
        private static Sprite Fill(string key, Color fallback)
        {
            string ck = "fill:" + key;
            if (_cache.TryGetValue(ck, out var s) && s != null) return s;
            var tex = LoadTex(key);
            Sprite sprite = tex == null ? Solid(fallback)
                : Sprite.Create(tex, new Rect(0, 0, Tile, Tile), new Vector2(0.5f, 0.5f), Tile,
                    0, SpriteMeshType.FullRect);
            _cache[ck] = sprite;
            return sprite;
        }

        // ---- animated water (cycle the bottom row of Water.png) ----
        public static Sprite[] WaterFrames()
        {
            if (_multiCache.TryGetValue("water", out var f) && f != null) return f;
            var tex = LoadTex("Water/Water");
            Sprite[] frames;
            if (tex == null) frames = new[] { Solid(new Color(0.16f, 0.42f, 0.62f)) };
            else
            {
                // Bottom row holds the plain water surface variants → use as animation frames.
                int cols = tex.width / Tile;
                var list = new List<Sprite>();
                for (int c = 0; c < cols; c++)
                    list.Add(Sprite.Create(tex, new Rect(c * Tile, 0, Tile, Tile),
                        new Vector2(0.5f, 0.5f), Tile, 0, SpriteMeshType.FullRect));
                frames = list.ToArray();
            }
            _multiCache["water"] = frames;
            return frames;
        }

        // ---- whole-object sprites (mountains/hills are a strip of N objects across) ----
        /// <summary>One of the snow-capped mountain objects (240x48 sheet = 5 across).</summary>
        public static Sprite Mountain(int variant) => Object("Mountains", 5, variant,
            new Color(0.32f, 0.30f, 0.34f));
        public static Sprite Hill(int variant) => Object("Grass/Hills", 5, variant,
            new Color(0.34f, 0.55f, 0.30f));
        public static Sprite HillDesert(int variant) => Object("Desert/Hills Desert", 5, variant,
            new Color(0.74f, 0.64f, 0.42f));
        public static Sprite HillSnow(int variant) => Object("Snow/Hills Snow", 5, variant,
            new Color(0.82f, 0.86f, 0.92f));

        // Slice the variant-th object from a horizontal strip of `count` equal cells.
        private static Sprite Object(string key, int count, int variant, Color fallback)
        {
            string ck = $"obj:{key}:{variant}";
            if (_cache.TryGetValue(ck, out var s) && s != null) return s;
            var tex = LoadTex(key);
            Sprite sprite;
            if (tex == null) sprite = Solid(fallback);
            else
            {
                int cw = tex.width / count;
                int v = ((variant % count) + count) % count;
                sprite = Sprite.Create(tex, new Rect(v * cw, 0, cw, tex.height),
                    new Vector2(0.5f, 0.5f), Tile, 0, SpriteMeshType.FullRect);
            }
            _cache[ck] = sprite;
            return sprite;
        }

        // ---- forest / trees (Forest.png 48x96: small trees top-left, big blob, bottom fill) ----
        /// <summary>A small tree object (top-left ~16x24 of Forest.png).</summary>
        public static Sprite Tree(bool snowy)
        {
            string key = snowy ? "Snow/Forest Snowy" : "Grass/Forest";
            string ck = "tree:" + key;
            if (_cache.TryGetValue(ck, out var s) && s != null) return s;
            var tex = LoadTex(key);
            Sprite sprite = tex == null ? Solid(new Color(0.18f, 0.36f, 0.22f))
                : Sprite.Create(tex, new Rect(0, tex.height - 24, 16, 24), new Vector2(0.5f, 0.5f),
                    Tile, 0, SpriteMeshType.FullRect);
            _cache[ck] = sprite;
            return sprite;
        }

        // ---- helpers ----
        private static Texture2D LoadTex(string key) => Resources.Load<Texture2D>(Root + key);

        private static readonly Dictionary<Color, Sprite> _solids = new();
        private static Sprite Solid(Color c)
        {
            if (_solids.TryGetValue(c, out var s) && s != null) return s;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = c;
            tex.SetPixels(px); tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            _solids[c] = sprite;
            return sprite;
        }
    }
}
