using System.Collections.Generic;
using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Builds a real side-scrolling platformer level from the Kenney platformer kit:
    /// solid grass blocks at varying heights, ascending staircases, floating-platform
    /// chains spanning pits, spring pads, enemy gauntlets, coin arcs over gaps, and a
    /// flag goal. Difficulty (1–10) scales length, pit width, climb height and enemy
    /// density. Real models when present (auto-fit to the tile grid), primitive fallback
    /// otherwise. Keeps the same <see cref="PlatformerProp"/> kinds, so the existing
    /// platformer controller (stomp/coins/goal) works unchanged.
    /// </summary>
    public static class PlatformerLevelGenerator
    {
        private const string Kit = "Models/kenney_platformer-kit/Models/FBX format/";
        private const float Tile = 2f;     // world units per grid cell (and block footprint)

        public static float Build(int difficulty, int seed, Transform parent, out Vector3 start)
        {
            var rng = new System.Random(seed);
            difficulty = Mathf.Clamp(difficulty, 1, 10);

            float x = 0f;
            float groundY = 0f;
            int length = 16 + difficulty * 3;     // number of "sections"

            // Safe starting platform.
            Run(parent, ref x, groundY, 5, rng);
            start = new Vector3(2f, 2f, 0f);

            for (int i = 0; i < length; i++)
            {
                // Pick a section type, weighted by difficulty.
                int roll = rng.Next(0, 100);
                if (roll < 24) SectionStairs(parent, ref x, ref groundY, rng, difficulty);
                else if (roll < 48) SectionPit(parent, ref x, ref groundY, rng, difficulty);
                else if (roll < 68) SectionPlatformChain(parent, ref x, ref groundY, rng, difficulty);
                else if (roll < 84) SectionSpring(parent, ref x, ref groundY, rng, difficulty);
                else SectionGauntlet(parent, ref x, ref groundY, rng, difficulty);
            }

            // Run-up to the goal flag.
            Run(parent, ref x, groundY, 4, rng);
            float goalX = x - Tile;
            Spawn(parent, Kit + "flag", new Vector3(goalX, groundY + 1f, 0f), 2.6f, PlatformerProp.Kind.Goal,
                new Color(0.30f, 0.70f, 1f));
            return goalX;
        }

        // ---- sections -------------------------------------------------------

        // Flat run of ground blocks at the current height; scatters a coin or enemy.
        private static void Run(Transform parent, ref float x, float y, int cells, System.Random rng)
        {
            for (int i = 0; i < cells; i++)
            {
                GroundBlock(parent, x, y);
                x += Tile;
            }
        }

        // Ascending or descending staircase of blocks.
        private static void SectionStairs(Transform parent, ref float x, ref float groundY,
            System.Random rng, int diff)
        {
            int steps = 2 + rng.Next(0, 3);
            int dir = rng.NextDouble() < 0.7 ? 1 : -1;            // mostly climb
            for (int s = 0; s < steps; s++)
            {
                GroundBlock(parent, x, groundY);
                if (rng.NextDouble() < 0.4) Coin(parent, x, groundY + 2.2f);
                groundY = Mathf.Max(0f, groundY + dir * Tile);
                x += Tile;
            }
            GroundBlock(parent, x, groundY);
            x += Tile;
        }

        // A pit you clear by jumping; a coin arc rewards the leap.
        private static void SectionPit(Transform parent, ref float x, ref float groundY,
            System.Random rng, int diff)
        {
            GroundBlock(parent, x, groundY); x += Tile;
            int gap = 1 + Mathf.Clamp(diff / 3, 0, 2);            // 1–3 cells wide
            float midX = x + gap * Tile * 0.5f - Tile * 0.5f;
            for (int c = 0; c < gap; c++)
            {
                Coin(parent, x + c * Tile, groundY + 2.2f + Mathf.Sin((c + 0.5f) / gap * Mathf.PI) * 1.4f); // arc
            }
            x += gap * Tile;
            GroundBlock(parent, x, groundY); x += Tile;
        }

        // Floating platforms across a wider pit (the path is the platforms).
        private static void SectionPlatformChain(Transform parent, ref float x, ref float groundY,
            System.Random rng, int diff)
        {
            GroundBlock(parent, x, groundY); x += Tile;
            int plats = 2 + rng.Next(0, 1 + Mathf.Clamp(diff / 2, 1, 3));
            float py = groundY + Tile;
            for (int p = 0; p < plats; p++)
            {
                py = Mathf.Clamp(py + (rng.NextDouble() < 0.5 ? Tile : -Tile) * 0.5f, groundY + 1f, groundY + 4f);
                Platform(parent, x, py);
                if (rng.NextDouble() < 0.6) Coin(parent, x, py + 1.4f);
                if (diff >= 5 && rng.NextDouble() < 0.3) Enemy(parent, x, py + 0.9f, x - 0.8f, x + 0.8f);
                x += Tile + Tile * 0.4f;       // a real gap between platforms
            }
            GroundBlock(parent, x, groundY); x += Tile;
        }

        // A spring pad that launches you up to a high reward platform.
        private static void SectionSpring(Transform parent, ref float x, ref float groundY,
            System.Random rng, int diff)
        {
            GroundBlock(parent, x, groundY);
            Spawn(parent, Kit + "spring", new Vector3(x, groundY + 0.5f, 0f), 1.2f, PlatformerProp.Kind.Spring,
                new Color(0.95f, 0.85f, 0.25f));
            x += Tile;
            // High platform + coins as the payoff.
            float hy = groundY + 4f;
            Platform(parent, x, hy);
            Coin(parent, x, hy + 1.3f);
            Coin(parent, x + Tile, hy + 1.3f);
            GroundBlock(parent, x, groundY); x += Tile;
            GroundBlock(parent, x, groundY); x += Tile;
        }

        // A run of ground with a couple of patrolling enemies and a heart.
        private static void SectionGauntlet(Transform parent, ref float x, ref float groundY,
            System.Random rng, int diff)
        {
            int cells = 4 + rng.Next(0, 3);
            float startX = x;
            for (int i = 0; i < cells; i++) { GroundBlock(parent, x, groundY); x += Tile; }
            int enemies = 1 + Mathf.Clamp(diff / 3, 0, 2);
            for (int e = 0; e < enemies; e++)
            {
                float ex = startX + Tile * (1 + e * 2);
                Enemy(parent, ex, groundY + 0.9f, startX + Tile, x - Tile);
            }
            if (rng.NextDouble() < 0.4) Spawn(parent, Kit + "heart",
                new Vector3(startX + cells * Tile * 0.5f, groundY + 2.2f, 0f), 0.9f, PlatformerProp.Kind.Heart,
                new Color(0.9f, 0.3f, 0.4f));
        }

        // ---- pieces ---------------------------------------------------------

        private static void GroundBlock(Transform parent, float x, float y)
        {
            var go = Spawn(parent, Kit + "block-grass", new Vector3(x, y, 0f), Tile, PlatformerProp.Kind.None,
                new Color(0.34f, 0.52f, 0.30f));
            EnsureSolidBox(go, new Vector3(Tile, Tile, Tile), y);
        }

        private static void Platform(Transform parent, float x, float y)
        {
            var go = Spawn(parent, Kit + "platform", new Vector3(x, y, 0f), Tile, PlatformerProp.Kind.None,
                new Color(0.55f, 0.45f, 0.30f));
            EnsureSolidBox(go, new Vector3(Tile, 0.6f, Tile), y);
        }

        private static void Enemy(Transform parent, float x, float y, float minX, float maxX)
        {
            var go = Spawn(parent, Kit + "character-oozi", new Vector3(x, y, 0f), 1.1f, PlatformerProp.Kind.Enemy,
                new Color(0.80f, 0.25f, 0.25f));
            var prop = go.GetComponent<PlatformerProp>();
            prop.minX = minX; prop.maxX = maxX; prop.speed = 1.6f + Random.value * 1.6f;
            MakeTrigger(go);
        }

        private static void Coin(Transform parent, float x, float y) =>
            Spawn(parent, Kit + "coin-gold", new Vector3(x, y, 0f), 0.8f, PlatformerProp.Kind.Coin,
                new Color(0.95f, 0.80f, 0.20f));

        // Spawn a kit model (auto-fit to `fit` metres) with a PlatformerProp; triggers for
        // pickups/enemies/goal, solid for blocks (collider handled by caller).
        private static GameObject Spawn(Transform parent, string path, Vector3 pos, float fit,
            PlatformerProp.Kind kind, Color fallback)
        {
            var go = ModelLibrary.Spawn(path, parent, pos, 0f, 1f,
                placeholderColor: fallback, placeholderLabel: false, fitHeight: fit);
            var prop = go.GetComponent<PlatformerProp>();
            if (prop == null) prop = go.AddComponent<PlatformerProp>();
            prop.kind = kind;
            if (kind == PlatformerProp.Kind.Coin || kind == PlatformerProp.Kind.Goal ||
                kind == PlatformerProp.Kind.Heart || kind == PlatformerProp.Kind.Spring)
                MakeTrigger(go);
            return go;
        }

        // Models may import with no/odd collider; guarantee a solid box collider sized to the cell.
        private static void EnsureSolidBox(GameObject go, Vector3 size, float baseY)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            var box = go.AddComponent<BoxCollider>();
            // Position the collider to fill the cell from the floor up, regardless of model pivot.
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
