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

        // Build cursor: current column and ground-surface row, threaded through the authored beats.
        private static int _gx, _row, _layer;
        private static Transform _p;

        /// <summary>
        /// A hand-authored level following Kishōtenketsu (introduce → develop → twist → conclude),
        /// the structure Nintendo uses (sources: MCV/Develop "Nintendo's level design secrets in
        /// four steps"; Mario 1-1 analyses). Each mechanic is taught in a SAFE space (failure =
        /// restart, not death) before a dangerous version; coins breadcrumb the path and reward the
        /// optimal jump arc; optional high routes give risk/reward. Difficulty nudges gap widths and
        /// enemy counts but the beats and their teaching order are fixed, so it's always a good level
        /// rather than random noise.
        /// </summary>
        public static float Build(int difficulty, int seed, Transform parent, int solidLayer, out Vector3 start)
        {
            difficulty = Mathf.Clamp(difficulty, 1, 10);
            _p = parent; _layer = solidLayer; _gx = 0; _row = 0;

            // ---- KI: Introduce. Flat run, one coin trail (teaches: move right toward reward). ----
            Flat(6);
            start = new Vector3(2f * Cell, (_row + 1.5f) * Cell, 0f);
            CoinRow(3, _row + 2, 3);

            // Teach JUMP with a single low step you must hop (safe: ground continues after).
            Flat(2); StepTo(_row + 1); Flat(3); CoinArcTo(_gx - 3, _gx, _row + 2);

            // Teach STOMP: one slow enemy on flat ground, coins luring you onto its head.
            Flat(1); var e1 = _gx; Flat(5); Patroller(e1 + 1, e1 + 4); Coin(e1 + 2, _row + 3); Coin(e1 + 3, _row + 3);

            // The "? block" power-up (grow big). A coin breadcrumb leads up to it; safe ground below.
            Flat(2); QuestionMushroom(_gx, _row + 3); Coin(_gx, _row + 2); Flat(3);

            // ---- SHŌ: Develop. First real PIT (teaches commitment), coin arc over it as the guide. ----
            Flat(2);
            int gap1 = 2 + difficulty / 4;                 // 2–4 cells
            Gap(gap1, arcCoins: true);
            Flat(3);

            // Staircase UP, then a higher optional coin ledge (risk/reward: jump up for extra coins).
            Climb(3); Coin(_gx - 1, _row + 4); Coin(_gx, _row + 4); Coin(_gx + 1, _row + 4); Flat(2);

            // Two enemies on a stretch — stomp-chain or run past. Pit right after demands control.
            int e2 = _gx; Flat(6); Patroller(e2 + 1, e2 + 5); Patroller(e2 + 3, e2 + 5);
            Gap(2, arcCoins: false); Flat(3);

            // ---- TEN: Twist. Combine everything: pit + enemy on a floating platform mid-jump. ----
            Flat(1);
            Gap(2, arcCoins: false);
            FloatPlatform(_gx, _row + 1, 2); Patroller(_gx, _gx + 1); Coin(_gx, _row + 3);
            _gx += 3;
            Gap(2, arcCoins: true);
            // A spring (sits ON the ground) launches to a high coin reward — the level's showpiece.
            Flat(2); Spring(_gx); CoinColumn(_gx, _row + 3, 3); Flat(1);
            // Descending steps down to the finish (let the player breathe after the twist).
            Drop(2); Flat(2); Heart(_gx - 1, _row + 2); Drop(1);

            // ---- KETSU: Conclude. A short victory run to the flag (show off, collect the last coins). ----
            Flat(2); CoinRow(_gx, _row + 2, 3); Flat(2);
            float goalX = _gx * Cell;
            Prop(_p, PixelPlatformerArt.Flag, new Vector3(goalX, (_row + 2f) * Cell, 0f), 2f,
                PlatformerProp.Kind.Goal, FlagC);
            Flat(3); // landing strip past the flag
            return goalX;
        }

        // ---- authored building blocks (advance _gx; mutate _row) -------------

        private static void Flat(int cells)
        {
            for (int i = 0; i < cells; i++) Column(_p, _gx++, _row, _row + 2, _layer);
        }

        private static void StepTo(int newRow) { _row = newRow; Column(_p, _gx++, _row, _row + 2, _layer); }
        private static void Climb(int steps) { for (int i = 0; i < steps; i++) { _row += 1; Column(_p, _gx++, _row, _row + 2, _layer); } }
        private static void Drop(int steps) { for (int i = 0; i < steps; i++) { _row = Mathf.Max(0, _row - 1); Column(_p, _gx++, _row, _row + 2, _layer); } }

        // A pit of `cells` empty columns; optional coin arc over it tracing the jump parabola.
        private static void Gap(int cells, bool arcCoins)
        {
            if (arcCoins)
                for (int c = 0; c < cells; c++)
                {
                    float t = (c + 0.5f) / cells;
                    Coin(_gx + c, _row + 1 + Mathf.RoundToInt(Mathf.Sin(t * Mathf.PI) * 2f));
                }
            _gx += cells;
        }

        private static void FloatPlatform(int gx, int row, int width)
        {
            for (int w = 0; w < width; w++) SolidTile(_p, (gx + w) * Cell, row * Cell, PixelPlatformerArt.GrassTop, GrassTop, _layer);
        }

        private static void Spring(int gx)
        {
            Column(_p, gx, _row, _row + 2, _layer);
            // Sits ON the surface (row+0.6, half a tile up) — no longer floating in the air.
            Prop(_p, PixelPlatformerArt.Spring, new Vector3(gx * Cell, (_row + 0.6f) * Cell, 0f), 0.9f,
                PlatformerProp.Kind.Spring, SpringC);
            _gx = gx + 1;
        }

        // A "?" block at (gx,row) holding a power-up mushroom that pops just above it.
        private static void QuestionMushroom(int gx, int row)
        {
            SolidTile(_p, gx * Cell, row * Cell, PixelPlatformerArt.QBlock, new Color(0.9f, 0.7f, 0.2f), _layer);
            Prop(_p, PixelPlatformerArt.Mushroom, new Vector3(gx * Cell, (row + 1) * Cell, 0f), 0.8f,
                PlatformerProp.Kind.Mushroom, new Color(0.9f, 0.4f, 0.3f));
        }

        private static void CoinRow(int gx, int row, int n) { for (int i = 0; i < n; i++) Coin(gx + i, row); }
        private static void CoinColumn(int gx, int row, int n) { for (int i = 0; i < n; i++) Coin(gx, row + i); }
        private static void CoinArcTo(int gxA, int gxB, int peakRow)
        {
            int span = Mathf.Max(1, gxB - gxA);
            for (int c = 0; c <= span; c++)
            {
                float t = (float)c / span;
                Coin(gxA + c, _row + 1 + Mathf.RoundToInt(Mathf.Sin(t * Mathf.PI) * (peakRow - _row)));
            }
        }
        private static void Patroller(int minGx, int maxGx) => Enemy(_p, (minGx + maxGx) / 2, _row + 1, minGx, maxGx);
        private static void Coin(int gx, int row) => Coin(_p, gx, row);
        private static void Heart(int gx, int row) => Prop(_p, PixelPlatformerArt.Heart,
            new Vector3(gx * Cell, row * Cell, 0f), 0.8f, PlatformerProp.Kind.Heart, HeartC);

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
