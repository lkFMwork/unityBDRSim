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
        /// Renders a Super Mario World–style tile map for one world: a fixed grid of themed tiles
        /// (a grass checker), a connected dirt TRAIL snaking through the given level cells, water on
        /// some edges, and scattered decorations. <paramref name="cells"/> are the level positions in
        /// GRID coordinates (col,row); the trail connects them in order. Deterministic per
        /// <paramref name="seed"/>. Returns a point-filtered texture sized cols*tile × rows*tile.
        /// </summary>
        public static Texture2D WorldMap(WorldMapTheme theme, int cols, int rows,
            System.Collections.Generic.IList<Vector2Int> cells, int seed, int tile = 16)
        {
            int W = cols * tile, H = rows * tile;
            var tex = NewTex(W, H);
            var rng = new System.Random(seed);
            float ox = (float)rng.NextDouble() * 50f, oy = (float)rng.NextDouble() * 50f;

            // 0 = grass, 1 = trail, 2 = water, 3 = decoration anchor (drawn on grass)
            var kind = new int[cols, rows];

            // Water: a soft border on two seeded edges + an occasional inland lake, so the play
            // area (where the trail runs) stays clear.
            bool waterTop = rng.Next(2) == 0, waterRight = rng.Next(2) == 0;
            for (int c = 0; c < cols; c++)
                for (int r = 0; r < rows; r++)
                {
                    float edge = 0f;
                    if (waterTop) edge = Mathf.Max(edge, (r - (rows - 2)) / 2f);
                    if (waterRight) edge = Mathf.Max(edge, (c - (cols - 2)) / 2f);
                    if (edge > 0f && rng.NextDouble() < edge) kind[c, r] = 2;
                }

            // Trail: connect consecutive level cells with an L-shaped dirt path (carves through water).
            for (int i = 0; i < cells.Count - 1; i++) CarveTrail(kind, cells[i], cells[i + 1], rng);
            foreach (var cell in cells) if (InBounds(kind, cell.x, cell.y)) kind[cell.x, cell.y] = 1;

            // Decorations on grass away from the trail.
            int decorCount = Mathf.Max(3, cols * rows / 12);
            for (int n = 0; n < decorCount; n++)
            {
                int c = rng.Next(cols), r = rng.Next(rows);
                if (kind[c, r] == 0 && !NearTrail(kind, c, r)) kind[c, r] = 3;
            }

            // Paint each tile.
            for (int c = 0; c < cols; c++)
                for (int r = 0; r < rows; r++)
                {
                    int k = kind[c, r];
                    bool darkSquare = ((c + r) & 1) == 0;
                    Color grass = darkSquare ? theme.GrassDark : theme.GrassLight;
                    // subtle perlin mottling
                    float m = Mathf.PerlinNoise(c * 0.4f + ox, r * 0.4f + oy);
                    grass = Color.Lerp(grass, theme.GrassDark, m * 0.25f);

                    if (k == 2) PaintTile(tex, c, r, tile, theme.Water, theme.Water * 0.85f, rng);
                    else PaintTile(tex, c, r, tile, grass, grass, rng);

                    if (k == 1) PaintTrailTile(tex, kind, c, r, tile, theme);
                    if (k == 3) PaintDecor(tex, c, r, tile, theme, rng);
                }

            tex.Apply();
            return tex;
        }

        private static void CarveTrail(int[,] kind, Vector2Int a, Vector2Int b, System.Random rng)
        {
            int x = a.x, y = a.y;
            bool horizFirst = rng.Next(2) == 0;
            void StepX() { while (x != b.x) { x += x < b.x ? 1 : -1; if (InBounds(kind, x, y)) kind[x, y] = 1; } }
            void StepY() { while (y != b.y) { y += y < b.y ? 1 : -1; if (InBounds(kind, x, y)) kind[x, y] = 1; } }
            if (horizFirst) { StepX(); StepY(); } else { StepY(); StepX(); }
        }

        private static bool NearTrail(int[,] kind, int c, int r)
        {
            for (int dc = -1; dc <= 1; dc++)
                for (int dr = -1; dr <= 1; dr++)
                    if (InBounds(kind, c + dc, r + dr) && kind[c + dc, r + dr] == 1) return true;
            return false;
        }

        private static bool InBounds(int[,] a, int c, int r) =>
            c >= 0 && c < a.GetLength(0) && r >= 0 && r < a.GetLength(1);

        // Fill a tile with a base colour + a touch of dither between two shades.
        private static void PaintTile(Texture2D t, int c, int r, int tile, Color a, Color b, System.Random rng)
        {
            int x0 = c * tile, y0 = r * tile;
            for (int y = 0; y < tile; y++)
                for (int x = 0; x < tile; x++)
                {
                    float thr = (Bayer[(y0 + y) & 3, (x0 + x) & 3] + 0.5f) / 16f;
                    t.SetPixel(x0 + x, y0 + y, 0.5f > thr ? a : b);
                }
        }

        // A rounded dirt trail tile with a darker edge where it meets grass (not another trail).
        private static void PaintTrailTile(Texture2D t, int[,] kind, int c, int r, int tile, WorldMapTheme th)
        {
            int x0 = c * tile, y0 = r * tile;
            bool up = InBounds(kind, c, r + 1) && kind[c, r + 1] == 1;
            bool dn = InBounds(kind, c, r - 1) && kind[c, r - 1] == 1;
            bool lf = InBounds(kind, c - 1, r) && kind[c - 1, r] == 1;
            bool rt = InBounds(kind, c + 1, r) && kind[c + 1, r] == 1;
            int inset = 3;
            for (int y = 0; y < tile; y++)
                for (int x = 0; x < tile; x++)
                {
                    // keep trail within an inset band unless it continues toward a neighbour
                    bool band = x >= inset && x < tile - inset && y >= inset && y < tile - inset;
                    if (!band)
                    {
                        if (lf && x < inset && y >= inset && y < tile - inset) band = true;
                        if (rt && x >= tile - inset && y >= inset && y < tile - inset) band = true;
                        if (dn && y < inset && x >= inset && x < tile - inset) band = true;
                        if (up && y >= tile - inset && x >= inset && x < tile - inset) band = true;
                    }
                    if (!band) continue;
                    bool edge = x == inset || x == tile - inset - 1 || y == inset || y == tile - inset - 1;
                    t.SetPixel(x0 + x, y0 + y, edge ? th.SoilEdge : th.Soil);
                }
        }

        private static void PaintDecor(Texture2D t, int c, int r, int tile, WorldMapTheme th, System.Random rng)
        {
            int x0 = c * tile, y0 = r * tile;
            Color col = rng.Next(2) == 0 ? th.Decor1 : th.Decor2;
            switch (th.Biome)
            {
                case MapBiome.Desert: // a cactus
                    Fill(t, x0 + tile / 2 - 1, y0 + 3, 2, tile - 6, col);
                    Fill(t, x0 + tile / 2 - 3, y0 + tile / 2, 2, 3, col);
                    Fill(t, x0 + tile / 2 + 1, y0 + tile / 2 + 1, 2, 3, col);
                    break;
                case MapBiome.Forest: // a pine
                    for (int s = 0; s < 3; s++)
                        Fill(t, x0 + 3 + s, y0 + 3 + s * 3, tile - 6 - s * 2, 3, col);
                    Fill(t, x0 + tile / 2 - 1, y0 + 2, 2, 3, th.SoilEdge);
                    break;
                default: // a round bush/tree
                    Fill(t, x0 + 4, y0 + 5, tile - 8, tile - 8, col);
                    Fill(t, x0 + 5, y0 + 4, tile - 10, 1, col);
                    Fill(t, x0 + 5, y0 + tile - 5, tile - 10, 1, col);
                    break;
            }
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
