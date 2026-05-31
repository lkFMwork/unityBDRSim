using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Procedural SNES-style pixel art for the Texas overworld: a rasterized Texas
    /// landmass, a freight truck token, and node markers. Everything is generated as
    /// small, point-filtered textures so it scales up into chunky pixels — no art
    /// assets required. World coordinates match <c>TerritoryRegistry</c> (x = west→east,
    /// z = south→north) so city nodes land on the painted terrain.
    /// </summary>
    public static class OverworldArt
    {
        // Map bounds in world (x,z) space — padded a little around the cities.
        public const float MinX = -40f, MaxX = 28f, MinZ = -36f, MaxZ = 36f;

        // Rough Texas outline in (x,z), traced so the registry cities fall inside.
        private static readonly Vector2[] Texas =
        {
            new(-37f, 2f),   // El Paso tip (west)
            new(-30f, 9f),
            new(-19f, 12f),  // base of the panhandle, west
            new(-19f, 33f),  // panhandle NW
            new(-7f, 33f),   // panhandle NE
            new(-7f, 17f),
            new(3f, 19f),    // Red River bend
            new(13f, 16f),
            new(18f, 7f),    // NE / Piney Woods
            new(25f, -7f),   // upper Gulf coast (Houston/Beaumont)
            new(18f, -23f),  // Coastal Bend (Corpus)
            new(9f, -33f),   // south tip (Brownsville)
            new(1f, -21f),   // up the Rio Grande
            new(-9f, -9f),
            new(-22f, 1f),
        };

        private static readonly int[,] Bayer =
        {
            { 0, 8, 2, 10 }, { 12, 4, 14, 6 }, { 3, 11, 1, 9 }, { 15, 7, 13, 5 }
        };

        // ---- terrain --------------------------------------------------------

        public static Texture2D TexasTerrain(int res = 220)
        {
            var tex = NewTex(res, res);
            var land = new bool[res, res];

            Color waterDeep = new(0.06f, 0.13f, 0.24f);
            Color water = new(0.09f, 0.19f, 0.31f);
            Color coast = new(0.20f, 0.37f, 0.50f);
            Color desert = new(0.64f, 0.53f, 0.35f);
            Color plain = new(0.50f, 0.52f, 0.31f);
            Color green = new(0.33f, 0.47f, 0.27f);
            Color shade = new(0.24f, 0.37f, 0.21f);

            for (int py = 0; py < res; py++)
                for (int px = 0; px < res; px++)
                {
                    float x = Mathf.Lerp(MinX, MaxX, px / (res - 1f));
                    float z = Mathf.Lerp(MinZ, MaxZ, py / (res - 1f));
                    bool inside = InTexas(x, z);
                    land[px, py] = inside;
                    float thr = (Bayer[py & 3, px & 3] + 0.5f) / 16f;

                    if (inside)
                    {
                        float t = Mathf.InverseLerp(MinX, MaxX, x); // 0 west → 1 east
                        Color baseCol = t < 0.4f
                            ? Color.Lerp(desert, plain, t / 0.4f)
                            : Color.Lerp(plain, green, (t - 0.4f) / 0.6f);
                        float n = Mathf.PerlinNoise(px * 0.09f + 3.1f, py * 0.09f + 1.7f);
                        tex.SetPixel(px, py, n > thr ? baseCol : Color.Lerp(baseCol, shade, 0.55f));
                    }
                    else
                    {
                        tex.SetPixel(px, py, 0.5f > thr ? water : waterDeep);
                    }
                }

            // One-pixel coastline where water meets land.
            for (int py = 0; py < res; py++)
                for (int px = 0; px < res; px++)
                    if (!land[px, py] && NearLand(land, px, py, res))
                        tex.SetPixel(px, py, coast);

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// A generic SNES-style world terrain for any state: a soft organic landmass on water,
        /// rolling desert→plain→green bands, dithered shade. Deterministic per <paramref name="seed"/>
        /// so each state looks distinct but stable. Used by the world map; cities are placed by
        /// their normalized in-state position, not a true outline.
        /// </summary>
        public static Texture2D StateTerrain(int seed, int res = 220)
        {
            var tex = NewTex(res, res);
            var land = new bool[res, res];
            var rng = new System.Random(seed);
            float ox = (float)rng.NextDouble() * 100f, oy = (float)rng.NextDouble() * 100f;
            // Tint the land palette a touch by seed so states differ.
            float hueShift = (float)rng.NextDouble();

            Color waterDeep = new(0.06f, 0.13f, 0.24f);
            Color water = new(0.09f, 0.19f, 0.31f);
            Color coast = new(0.20f, 0.37f, 0.50f);
            Color desert = Color.Lerp(new(0.64f, 0.53f, 0.35f), new(0.58f, 0.56f, 0.40f), hueShift);
            Color plain = Color.Lerp(new(0.50f, 0.52f, 0.31f), new(0.46f, 0.55f, 0.34f), hueShift);
            Color green = Color.Lerp(new(0.33f, 0.47f, 0.27f), new(0.30f, 0.50f, 0.32f), hueShift);
            Color shade = new(0.24f, 0.37f, 0.21f);

            for (int py = 0; py < res; py++)
                for (int px = 0; px < res; px++)
                {
                    float u = px / (res - 1f), v = py / (res - 1f);
                    // Distance from centre, warped by noise → an organic blob filling most of the frame.
                    float warp = Mathf.PerlinNoise(px * 0.045f + ox, py * 0.045f + oy);
                    float d = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f));
                    bool inside = d < 0.46f * (0.7f + 0.6f * warp);
                    land[px, py] = inside;
                    float thr = (Bayer[py & 3, px & 3] + 0.5f) / 16f;

                    if (inside)
                    {
                        float t = u; // west→east bands
                        Color baseCol = t < 0.4f
                            ? Color.Lerp(desert, plain, t / 0.4f)
                            : Color.Lerp(plain, green, (t - 0.4f) / 0.6f);
                        float n = Mathf.PerlinNoise(px * 0.09f + ox + 3.1f, py * 0.09f + oy + 1.7f);
                        tex.SetPixel(px, py, n > thr ? baseCol : Color.Lerp(baseCol, shade, 0.55f));
                    }
                    else tex.SetPixel(px, py, 0.5f > thr ? water : waterDeep);
                }

            for (int py = 0; py < res; py++)
                for (int px = 0; px < res; px++)
                    if (!land[px, py] && NearLand(land, px, py, res))
                        tex.SetPixel(px, py, coast);

            tex.Apply();
            return tex;
        }

        private static bool NearLand(bool[,] land, int px, int py, int res)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int x = px + dx, y = py + dy;
                    if (x >= 0 && x < res && y >= 0 && y < res && land[x, y]) return true;
                }
            return false;
        }

        private static bool InTexas(float x, float z)
        {
            bool inside = false;
            for (int i = 0, j = Texas.Length - 1; i < Texas.Length; j = i++)
            {
                Vector2 a = Texas[i], b = Texas[j];
                if (((a.y > z) != (b.y > z)) &&
                    (x < (b.x - a.x) * (z - a.y) / (b.y - a.y) + a.x))
                    inside = !inside;
            }
            return inside;
        }

        /// <summary>Normalized [0,1] UV for a world (x,z) on the terrain texture.</summary>
        public static Vector2 ToUv(float x, float z) => new(
            Mathf.InverseLerp(MinX, MaxX, x),
            Mathf.InverseLerp(MinZ, MaxZ, z));

        // ---- sprites --------------------------------------------------------

        private static Texture2D NewTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var clear = new Color32(0, 0, 0, 0);
            var fill = new Color32[w * h];
            for (int i = 0; i < fill.Length; i++) fill[i] = clear;
            t.SetPixels32(fill);
            return t;
        }

        private static Sprite ToSprite(Texture2D t)
        {
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height),
                new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
        }

        private static void Fill(Texture2D t, int x0, int y0, int w, int h, Color c)
        {
            for (int y = y0; y < y0 + h; y++)
                for (int x = x0; x < x0 + w; x++)
                    if (x >= 0 && x < t.width && y >= 0 && y < t.height) t.SetPixel(x, y, c);
        }

        private static Sprite _truck;

        /// <summary>A little side-view freight truck (cab + trailer), ~22x13.</summary>
        public static Sprite Truck()
        {
            if (_truck != null) return _truck;
            int w = 22, h = 13;
            var t = NewTex(w, h);
            Color outline = new(0.10f, 0.12f, 0.16f);
            Color trailer = new(0.86f, 0.88f, 0.92f);
            Color cab = new(0.86f, 0.30f, 0.26f);
            Color glass = new(0.55f, 0.78f, 0.92f);
            Color tyre = new(0.13f, 0.13f, 0.15f);
            Color hub = new(0.45f, 0.46f, 0.50f);

            Fill(t, 1, 4, 13, 7, trailer);     // trailer box
            Fill(t, 14, 3, 6, 5, cab);          // cab body
            Fill(t, 15, 6, 4, 2, glass);        // windshield
            Fill(t, 1, 4, 19, 1, outline);      // bottom chassis line
            Fill(t, 1, 10, 13, 1, outline);     // trailer roof line
            Fill(t, 1, 4, 1, 7, outline);       // trailer back
            Fill(t, 20, 3, 1, 5, outline);      // cab nose
            // wheels
            Fill(t, 3, 1, 3, 3, tyre); Fill(t, 4, 2, 1, 1, hub);
            Fill(t, 9, 1, 3, 3, tyre); Fill(t, 10, 2, 1, 1, hub);
            Fill(t, 15, 1, 3, 3, tyre); Fill(t, 16, 2, 1, 1, hub);

            return _truck = ToSprite(t);
        }

        /// <summary>
        /// A small building marker, ~16x16, drawn in greyscale so a tint colours it
        /// (the dark outline/shading survive the multiply). <paramref name="tall"/>
        /// makes an HQ-style tower; otherwise a pitched-roof shopfront.
        /// </summary>
        public static Sprite Marker(bool tall = false)
        {
            int w = 16, h = 16;
            var t = NewTex(w, h);
            Color body = Color.white;
            Color roof = new(0.66f, 0.66f, 0.66f);
            Color dark = new(0.22f, 0.22f, 0.24f);
            Color door = new(0.40f, 0.40f, 0.42f);

            if (tall)
            {
                Fill(t, 3, 1, 10, 14, body);
                Fill(t, 3, 1, 10, 1, dark);
                Fill(t, 3, 14, 10, 1, roof);
                for (int wy = 3; wy <= 11; wy += 3)         // window grid
                    for (int wx = 5; wx <= 9; wx += 3)
                        Fill(t, wx, wy, 2, 2, door);
                Fill(t, 3, 1, 1, 14, dark); Fill(t, 12, 1, 1, 14, dark);
            }
            else
            {
                Fill(t, 2, 1, 12, 9, body);                 // shop body
                Fill(t, 1, 9, 14, 2, roof);                 // eaves
                Fill(t, 3, 10, 10, 4, roof);                // pitched roof
                Fill(t, 6, 11, 4, 2, dark);
                Fill(t, 6, 1, 4, 5, door);                  // door
                Fill(t, 2, 1, 1, 9, dark); Fill(t, 13, 1, 1, 9, dark);
                Fill(t, 2, 1, 12, 1, dark);
            }
            return ToSprite(t);
        }
    }
}
