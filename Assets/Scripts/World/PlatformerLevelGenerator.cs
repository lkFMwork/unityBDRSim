using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Builds a real, *guaranteed-beatable* side-scroller from the Kenney platformer kit.
    /// The cardinal rule of the genre: derive geometry from the jump arc, not magic numbers.
    /// Every gap is clamped under the player's safe jump distance and every step under the
    /// safe jump height (see <see cref="JumpArc"/>), so no pit or climb is impossible.
    /// Difficulty (1–10) scales length and pushes gaps/steps toward those safe limits — never
    /// past them. Keeps the same <see cref="PlatformerProp"/> kinds so the controller is unchanged.
    /// </summary>
    public static class PlatformerLevelGenerator
    {
        private const string Kit = "Models/kenney_platformer-kit/Models/FBX format/";
        private const float Tile = 2f;        // world units per ground block (footprint)

        // The arc the player actually has (matches PlatformerController defaults). The
        // generator measures gaps/heights against this so everything is reachable.
        private static readonly JumpArc Arc = new JumpArc(runSpeed: 7f, jumpSpeed: 13f, gravity: 32f);

        public static float Build(int difficulty, int seed, Transform parent, out Vector3 start)
        {
            var rng = new System.Random(seed);
            difficulty = Mathf.Clamp(difficulty, 1, 10);
            float diff01 = (difficulty - 1) / 9f;

            // Reachability budget (with margins), then a hard cap so a bad roll can't exceed it.
            float maxGap = Arc.SafeGap();          // ~4.3m  (the player can clear ~5.7m)
            float maxStep = Arc.SafeStepUp();      // ~1.8m  (the player can rise ~2.6m)

            float x = 0f;
            float groundY = 0f;

            // Safe starting flat.
            for (int i = 0; i < 4; i++) { GroundBlock(parent, x, groundY); x += Tile; }
            start = new Vector3(2f, groundY + 2f, 0f);

            int sections = 14 + difficulty * 2;
            for (int s = 0; s < sections; s++)
            {
                switch (rng.Next(0, 5))
                {
                    case 0: Stairs(parent, ref x, ref groundY, rng, diff01, maxStep); break;
                    case 1: Pit(parent, ref x, ref groundY, rng, diff01, maxGap); break;
                    case 2: PlatformChain(parent, ref x, ref groundY, rng, diff01, maxGap, maxStep); break;
                    case 3: Spring(parent, ref x, ref groundY, rng); break;
                    default: Gauntlet(parent, ref x, ref groundY, rng, difficulty); break;
                }
                // A short breather flat between sections keeps things fair and readable.
                GroundBlock(parent, x, groundY); x += Tile;
            }

            for (int i = 0; i < 3; i++) { GroundBlock(parent, x, groundY); x += Tile; }
            float goalX = x - Tile;
            Pickup(parent, Kit + "flag", new Vector3(goalX, groundY + 1.2f, 0f), 2.8f, PlatformerProp.Kind.Goal,
                new Color(0.30f, 0.70f, 1f));
            return goalX;
        }

        // ---- sections (all bounded by the jump arc) -------------------------

        private static void Stairs(Transform parent, ref float x, ref float groundY,
            System.Random rng, float diff01, float maxStep)
        {
            int steps = 2 + rng.Next(0, 3);
            int dir = rng.NextDouble() < 0.72 ? 1 : -1;
            float rise = Mathf.Min(Tile, maxStep);          // one tile, but never above the cap
            for (int s = 0; s < steps; s++)
            {
                GroundBlock(parent, x, groundY);
                if (rng.NextDouble() < 0.4) Coin(parent, x, groundY + 2.0f);
                groundY = Mathf.Max(0f, groundY + dir * rise);
                x += Tile;
            }
            GroundBlock(parent, x, groundY);
            x += Tile;
        }

        private static void Pit(Transform parent, ref float x, ref float groundY,
            System.Random rng, float diff01, float maxGap)
        {
            GroundBlock(parent, x, groundY); x += Tile;

            // Gap width grows with difficulty but is hard-capped at the safe jump distance.
            float gap = Mathf.Lerp(Tile * 0.8f, maxGap, diff01) * (0.85f + (float)rng.NextDouble() * 0.15f);
            gap = Mathf.Min(gap, maxGap);

            // Coin arc over the leap (follows the parabola so it rewards the jump).
            int coins = Mathf.Max(2, Mathf.RoundToInt(gap / 1.2f));
            for (int c = 0; c < coins; c++)
            {
                float t = (c + 0.5f) / coins;
                float cx = x + t * gap;
                float cy = groundY + 1.6f + Mathf.Sin(t * Mathf.PI) * 1.6f;
                Coin(parent, cx, cy);
            }
            x += gap;
            GroundBlock(parent, x, groundY); x += Tile;
        }

        private static void PlatformChain(Transform parent, ref float x, ref float groundY,
            System.Random rng, float diff01, float maxGap, float maxStep)
        {
            GroundBlock(parent, x, groundY); x += Tile;

            int plats = 2 + rng.Next(0, 1 + Mathf.RoundToInt(diff01 * 2f));
            float py = groundY + Mathf.Min(Tile, maxStep);
            float hop = Mathf.Min(Mathf.Lerp(Tile * 1.2f, maxGap * 0.8f, diff01), maxGap * 0.85f);
            for (int p = 0; p < plats; p++)
            {
                Platform(parent, x, py);
                if (rng.NextDouble() < 0.6) Coin(parent, x, py + 1.4f);
                // Next platform: vary height within a safe step, advance within a safe hop.
                float dy = (rng.NextDouble() < 0.5 ? 1f : -1f) * Mathf.Min(Tile, maxStep) * 0.6f;
                py = Mathf.Clamp(py + dy, groundY + 1f, groundY + maxStep * 1.4f);
                x += hop;
            }
            GroundBlock(parent, x, groundY); x += Tile;
        }

        private static void Spring(Transform parent, ref float x, ref float groundY, System.Random rng)
        {
            GroundBlock(parent, x, groundY);
            Pickup(parent, Kit + "spring", new Vector3(x, groundY + 0.5f, 0f), 1.2f, PlatformerProp.Kind.Spring,
                new Color(0.95f, 0.85f, 0.25f));
            x += Tile;
            // The spring launches ~1.6× jump height, so a reward platform up high is reachable.
            float hy = groundY + Arc.MaxJumpHeight * 1.4f;
            Platform(parent, x, hy);
            Coin(parent, x, hy + 1.3f);
            Coin(parent, x + Tile, hy + 1.3f);
            GroundBlock(parent, x, groundY); x += Tile;
            GroundBlock(parent, x, groundY); x += Tile;
        }

        private static void Gauntlet(Transform parent, ref float x, ref float groundY,
            System.Random rng, int difficulty)
        {
            int cells = 4 + rng.Next(0, 3);
            float startX = x;
            for (int i = 0; i < cells; i++) { GroundBlock(parent, x, groundY); x += Tile; }
            int enemies = 1 + Mathf.Clamp(difficulty / 3, 0, 2);
            for (int e = 0; e < enemies; e++)
            {
                float ex = startX + Tile * (1 + e * 2);
                Enemy(parent, ex, groundY + 0.9f, startX + Tile, x - Tile);
            }
            if (rng.NextDouble() < 0.4)
                Pickup(parent, Kit + "heart", new Vector3(startX + cells * Tile * 0.5f, groundY + 2.0f, 0f),
                    0.9f, PlatformerProp.Kind.Heart, new Color(0.9f, 0.3f, 0.4f));
        }

        // ---- pieces ---------------------------------------------------------

        private static void GroundBlock(Transform parent, float x, float y)
        {
            var go = Solid(parent, Kit + "block-grass", new Vector3(x, y, 0f), Tile,
                new Color(0.34f, 0.52f, 0.30f));
            EnsureSolidBox(go, new Vector3(Tile, Tile, Tile), y);
        }

        private static void Platform(Transform parent, float x, float y)
        {
            var go = Solid(parent, Kit + "platform", new Vector3(x, y, 0f), Tile,
                new Color(0.55f, 0.45f, 0.30f));
            EnsureSolidBox(go, new Vector3(Tile, 0.6f, Tile), y);
        }

        private static void Enemy(Transform parent, float x, float y, float minX, float maxX)
        {
            var go = ModelLibrary.Spawn(Kit + "character-oozi", parent, new Vector3(x, y, 0f), 0f, 1f,
                placeholderColor: new Color(0.80f, 0.25f, 0.25f), placeholderLabel: false, fitHeight: 1.1f);
            var prop = go.GetComponent<PlatformerProp>() ?? go.AddComponent<PlatformerProp>();
            prop.kind = PlatformerProp.Kind.Enemy;
            prop.minX = minX; prop.maxX = maxX; prop.speed = 1.6f + Random.value * 1.6f;
            MakeTrigger(go);
        }

        private static void Coin(Transform parent, float x, float y) =>
            Pickup(parent, Kit + "coin-gold", new Vector3(x, y, 0f), 0.8f, PlatformerProp.Kind.Coin,
                new Color(0.95f, 0.80f, 0.20f));

        // A solid kit model (no trigger) with no prop.
        private static GameObject Solid(Transform parent, string path, Vector3 pos, float fit, Color fallback)
            => ModelLibrary.Spawn(path, parent, pos, 0f, 1f,
                placeholderColor: fallback, placeholderLabel: false, fitHeight: fit);

        // A trigger pickup/goal with a PlatformerProp of the given kind.
        private static GameObject Pickup(Transform parent, string path, Vector3 pos, float fit,
            PlatformerProp.Kind kind, Color fallback)
        {
            var go = ModelLibrary.Spawn(path, parent, pos, 0f, 1f,
                placeholderColor: fallback, placeholderLabel: false, fitHeight: fit);
            var prop = go.GetComponent<PlatformerProp>() ?? go.AddComponent<PlatformerProp>();
            prop.kind = kind;
            MakeTrigger(go);
            return go;
        }

        private static void EnsureSolidBox(GameObject go, Vector3 size, float baseY)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            var box = go.AddComponent<BoxCollider>();
            box.center = go.transform.InverseTransformPoint(new Vector3(go.transform.position.x, baseY + size.y * 0.5f, 0f));
            box.size = size;
        }

        private static void MakeTrigger(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = Vector3.one * 1.2f;
            box.center = new Vector3(0f, 0.6f, 0f);
        }
    }
}
