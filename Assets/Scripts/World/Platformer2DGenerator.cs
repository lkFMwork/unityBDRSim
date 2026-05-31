using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Builds a true 2D sprite-tile platformer on a fixed grid. Tiles are 1-unit cells with a
    /// SpriteRenderer + BoxCollider2D on the "Solid" layer — pivot-free, so there is no float
    /// or burial: a tile IS its cell. Geometry is clamped to the player's jump arc (gaps under
    /// the safe jump distance, steps under the safe jump height) so every level is beatable.
    /// Real Kenney Pixel Platformer sprites when imported, solid-color placeholders until then.
    /// </summary>
    public static class Platformer2DGenerator
    {
        public const float Cell = 1f;                 // world units per tile

        private static readonly Color GrassTop = new Color(0.45f, 0.78f, 0.42f);
        private static readonly Color Dirt = new Color(0.55f, 0.40f, 0.28f);
        private static readonly Color CoinC = new Color(0.98f, 0.82f, 0.25f);
        private static readonly Color HeartC = new Color(0.92f, 0.32f, 0.42f);
        private static readonly Color SpringC = new Color(0.95f, 0.85f, 0.30f);
        private static readonly Color EnemyC = new Color(0.85f, 0.28f, 0.28f);
        private static readonly Color FlagC = new Color(0.30f, 0.70f, 1f);

        public static float Build(int difficulty, int seed, Transform parent, int solidLayer, out Vector3 start)
        {
            var rng = new System.Random(seed);
            difficulty = Mathf.Clamp(difficulty, 1, 10);
            float diff01 = (difficulty - 1) / 9f;

            // Reachability budget from the controller's arc (matches Platformer2DController).
            var arc = new JumpArc(7f, (2f * 3.2f) / 0.38f, (2f * 3.2f) / (0.38f * 0.38f));
            int maxGapCells = Mathf.Max(1, Mathf.FloorToInt(arc.SafeGap() / Cell));     // ~ up to 4 cells
            int maxStepCells = Mathf.Max(1, Mathf.FloorToInt(arc.SafeStepUp() / Cell)); // ~ 1–2 cells

            int gx = 0;          // current grid column
            int groundRow = 0;   // current ground top row (cells above 0)

            for (int i = 0; i < 5; i++) Column(parent, gx++, groundRow, 4, solidLayer); // safe start
            start = new Vector3(2f * Cell, (groundRow + 1.5f) * Cell, 0f);

            int sections = 12 + difficulty * 2;
            for (int s = 0; s < sections; s++)
            {
                switch (rng.Next(0, 5))
                {
                    case 0: Stairs(parent, ref gx, ref groundRow, rng, maxStepCells, solidLayer); break;
                    case 1: Pit(parent, ref gx, ref groundRow, rng, diff01, maxGapCells, solidLayer); break;
                    case 2: Floats(parent, ref gx, ref groundRow, rng, diff01, maxGapCells, maxStepCells, solidLayer); break;
                    case 3: SpringSec(parent, ref gx, ref groundRow, rng, solidLayer); break;
                    default: Gauntlet(parent, ref gx, ref groundRow, rng, difficulty, solidLayer); break;
                }
                Column(parent, gx++, groundRow, 3, solidLayer); // breather
            }

            for (int i = 0; i < 3; i++) Column(parent, gx++, groundRow, 3, solidLayer);
            float goalX = (gx - 1) * Cell;
            Prop(parent, PixelPlatformerArt.Flag, new Vector3(goalX, (groundRow + 2f) * Cell, 0f), 2f,
                PlatformerProp.Kind.Goal, FlagC);
            return goalX;
        }

        // ---- sections (cell-based, reachability-clamped) --------------------

        private static void Stairs(Transform parent, ref int gx, ref int row, System.Random rng,
            int maxStep, int layer)
        {
            int steps = 2 + rng.Next(0, 3);
            int dir = rng.NextDouble() < 0.72 ? 1 : -1;
            int rise = Mathf.Min(1, maxStep); // one cell per step — always clearable
            for (int s = 0; s < steps; s++)
            {
                Column(parent, gx, row, Mathf.Max(2, row + 1), layer);
                if (rng.NextDouble() < 0.4) Coin(parent, gx, row + 2);
                row = Mathf.Max(0, row + dir * rise);
                gx++;
            }
            Column(parent, gx++, row, Mathf.Max(2, row + 1), layer);
        }

        private static void Pit(Transform parent, ref int gx, ref int row, System.Random rng,
            float diff01, int maxGap, int layer)
        {
            Column(parent, gx++, row, Mathf.Max(2, row + 1), layer);
            int gap = Mathf.Clamp(1 + Mathf.RoundToInt(diff01 * (maxGap - 1)), 1, maxGap);
            for (int c = 0; c < gap; c++)
            {
                float t = (c + 0.5f) / gap;
                Coin(parent, gx + c, row + 1 + Mathf.RoundToInt(Mathf.Sin(t * Mathf.PI) * 2f)); // arc
            }
            gx += gap;
            Column(parent, gx++, row, Mathf.Max(2, row + 1), layer);
        }

        private static void Floats(Transform parent, ref int gx, ref int row, System.Random rng,
            float diff01, int maxGap, int maxStep, int layer)
        {
            Column(parent, gx++, row, Mathf.Max(2, row + 1), layer);
            int plats = 2 + rng.Next(0, 1 + Mathf.RoundToInt(diff01 * 2f));
            int py = row + Mathf.Min(1, maxStep);
            int hop = Mathf.Clamp(1 + Mathf.RoundToInt(diff01 * (maxGap - 1)), 1, maxGap);
            for (int p = 0; p < plats; p++)
            {
                Platform(parent, gx, py, 2, layer);
                if (rng.NextDouble() < 0.6) Coin(parent, gx, py + 1);
                if (diff01 > 0.4f && rng.NextDouble() < 0.3) Enemy(parent, gx, py + 1, gx - 1, gx + 1);
                py = Mathf.Clamp(py + (rng.NextDouble() < 0.5 ? 1 : -1), row + 1, row + maxStep + 1);
                gx += 1 + hop;
            }
            Column(parent, gx++, row, Mathf.Max(2, row + 1), layer);
        }

        private static void SpringSec(Transform parent, ref int gx, ref int row, System.Random rng, int layer)
        {
            Column(parent, gx, row, Mathf.Max(2, row + 1), layer);
            Prop(parent, PixelPlatformerArt.Spring, new Vector3(gx * Cell, (row + 1.4f) * Cell, 0f), 1f,
                PlatformerProp.Kind.Spring, SpringC);
            gx++;
            int hy = row + 5;
            Platform(parent, gx, hy, 2, layer);
            Coin(parent, gx, hy + 1);
            Coin(parent, gx + 1, hy + 1);
            Column(parent, gx++, row, Mathf.Max(2, row + 1), layer);
            Column(parent, gx++, row, Mathf.Max(2, row + 1), layer);
        }

        private static void Gauntlet(Transform parent, ref int gx, ref int row, System.Random rng,
            int difficulty, int layer)
        {
            int cells = 4 + rng.Next(0, 3);
            int startX = gx;
            for (int i = 0; i < cells; i++) Column(parent, gx++, row, Mathf.Max(2, row + 1), layer);
            int enemies = 1 + Mathf.Clamp(difficulty / 3, 0, 2);
            for (int e = 0; e < enemies; e++)
                Enemy(parent, startX + 1 + e * 2, row + 1, startX + 1, gx - 1);
            if (rng.NextDouble() < 0.4)
                Prop(parent, PixelPlatformerArt.Heart, new Vector3((startX + cells / 2f) * Cell, (row + 2) * Cell, 0f),
                    1f, PlatformerProp.Kind.Heart, HeartC);
        }

        // ---- tiles & props --------------------------------------------------

        // A solid vertical column: grass top at `topRow`, dirt down to row 0 (depth rows).
        private static void Column(Transform parent, int gx, int topRow, int depthRows, int layer)
        {
            float x = gx * Cell;
            SolidTile(parent, x, topRow * Cell, PixelPlatformerArt.GrassTop, GrassTop, layer);
            for (int r = 1; r < Mathf.Max(1, depthRows); r++)
                SolidTile(parent, x, (topRow - r) * Cell, PixelPlatformerArt.Dirt, Dirt, layer, collider: false);
        }

        private static void Platform(Transform parent, int gx, int row, int width, int layer)
        {
            for (int w = 0; w < width; w++)
                SolidTile(parent, (gx + w) * Cell, row * Cell, PixelPlatformerArt.GrassTop, GrassTop, layer);
        }

        private static GameObject SolidTile(Transform parent, float x, float y, string spriteKey,
            Color fallback, int layer, bool collider = true)
        {
            var go = new GameObject("Tile", typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            go.layer = layer;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = SpriteLibrary.Get(spriteKey, fallback);
            sr.color = SpriteLibrary.Has(spriteKey) ? Color.white : fallback;
            sr.sortingOrder = 0;
            FitSprite(sr, Cell);
            if (collider) go.AddComponent<BoxCollider2D>().size = Vector2.one * Cell;
            return go;
        }

        private static void Enemy(Transform parent, int gx, int row, int minGx, int maxGx)
        {
            var go = PropObject(parent, PixelPlatformerArt.Enemy, new Vector3(gx * Cell, row * Cell, 0f),
                0.9f, EnemyC);
            var prop = go.GetComponent<Platformer2DProp>();
            prop.kind = PlatformerProp.Kind.Enemy;
            prop.minX = minGx * Cell; prop.maxX = maxGx * Cell; prop.speed = 1.6f + (float)Random.value * 1.4f;
        }

        private static void Coin(Transform parent, int gx, int row) =>
            Prop(parent, PixelPlatformerArt.Coin, new Vector3(gx * Cell, row * Cell, 0f), 0.7f,
                PlatformerProp.Kind.Coin, CoinC);

        private static void Prop(Transform parent, string key, Vector3 pos, float size,
            PlatformerProp.Kind kind, Color fallback)
        {
            var go = PropObject(parent, key, pos, size, fallback);
            go.GetComponent<Platformer2DProp>().kind = kind;
        }

        private static GameObject PropObject(Transform parent, string key, Vector3 pos, float size, Color fallback)
        {
            var go = new GameObject("Prop", typeof(SpriteRenderer), typeof(Platformer2DProp));
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = SpriteLibrary.Get(key, fallback);
            sr.color = SpriteLibrary.Has(key) ? Color.white : fallback;
            sr.sortingOrder = 5;
            FitSprite(sr, size);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one * size;
            return go;
        }

        // Scale a sprite so its world size is `target` units (sprites import at their own PPU).
        private static void FitSprite(SpriteRenderer sr, float target)
        {
            if (sr.sprite == null) return;
            var b = sr.sprite.bounds.size;
            float max = Mathf.Max(b.x, b.y);
            if (max > 0.0001f) sr.transform.localScale = Vector3.one * (target / max);
        }
    }
}
