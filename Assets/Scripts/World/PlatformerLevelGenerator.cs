using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Builds a side-scrolling 2.5D level out of primitives: ground segments with
    /// jumpable gaps, floating platforms, patrolling enemies, coins, and a goal at
    /// the end. Difficulty (1–10) scales length, gaps, and enemy density. Ground
    /// and platforms are solid; enemies/coins/goal are triggers the player reacts to.
    /// </summary>
    public static class PlatformerLevelGenerator
    {
        public static float Build(int difficulty, int seed, Transform parent, out Vector3 start)
        {
            var rng = new System.Random(seed);
            difficulty = Mathf.Clamp(difficulty, 1, 10);

            Material groundMat = Mat(new Color(0.34f, 0.52f, 0.30f));
            Material platMat = Mat(new Color(0.55f, 0.45f, 0.30f));
            Material enemyMat = Mat(new Color(0.80f, 0.25f, 0.25f));
            Material coinMat = Mat(new Color(0.95f, 0.80f, 0.20f));
            Material goalMat = Mat(new Color(0.30f, 0.70f, 1.00f));

            float length = 34f + difficulty * 7f;

            AddGround(parent, 0f, 9f, groundMat); // safe start
            float x = 9f;

            while (x < length)
            {
                bool gap = rng.NextDouble() < (0.22 + difficulty * 0.03);
                if (gap)
                {
                    float gapW = Mathf.Min(4f, 2f + (float)rng.NextDouble() * (1f + difficulty * 0.2f));
                    if (rng.NextDouble() < 0.6) // a platform to help cross
                        AddPlatform(parent, x + gapW * 0.5f, 2.2f, 1.8f + (float)rng.NextDouble() * 1.6f, platMat,
                            rng, coinMat);
                    x += gapW;
                }

                float segLen = 5f + (float)rng.NextDouble() * 5f;
                AddGround(parent, x, segLen, groundMat);

                if (rng.NextDouble() < (0.18 + difficulty * 0.05))
                    AddEnemy(parent, x + 1f, x + segLen - 1f, enemyMat);
                if (rng.NextDouble() < 0.4)
                    AddCoin(parent, x + segLen * 0.5f, 2.2f, coinMat);

                x += segLen;
            }

            AddGround(parent, x, 7f, groundMat);
            float goalX = x + 3.5f;
            AddGoal(parent, goalX, goalMat);

            start = new Vector3(2f, 2f, 0f);
            return goalX;
        }

        private static void AddGround(Transform parent, float startX, float len, Material mat)
        {
            var go = Cube(parent, "Ground", new Vector3(startX + len * 0.5f, -0.5f, 0f), new Vector3(len, 1f, 3f), mat);
            // solid (default)
            _ = go;
        }

        private static void AddPlatform(Transform parent, float centerX, float width, float y, Material mat,
            System.Random rng, Material coinMat)
        {
            Cube(parent, "Platform", new Vector3(centerX, y, 0f), new Vector3(width, 0.5f, 3f), mat);
            if (rng.NextDouble() < 0.5) AddCoin(parent, centerX, y + 1.2f, coinMat);
        }

        private static void AddEnemy(Transform parent, float minX, float maxX, Material mat)
        {
            var go = Cube(parent, "Enemy", new Vector3(minX, 0.5f, 0f), new Vector3(0.7f, 1f, 0.7f), mat);
            MakeTrigger(go);
            var prop = go.AddComponent<PlatformerProp>();
            prop.kind = PlatformerProp.Kind.Enemy;
            prop.minX = minX;
            prop.maxX = maxX;
            prop.speed = 2f + Random.value * 1.5f;
        }

        private static void AddCoin(Transform parent, float x, float y, Material mat)
        {
            var go = Cube(parent, "Coin", new Vector3(x, y, 0f), new Vector3(0.4f, 0.4f, 0.4f), mat);
            MakeTrigger(go);
            go.AddComponent<PlatformerProp>().kind = PlatformerProp.Kind.Coin;
        }

        private static void AddGoal(Transform parent, float x, Material mat)
        {
            var go = Cube(parent, "Goal", new Vector3(x, 2f, 0f), new Vector3(0.8f, 4f, 3f), mat);
            MakeTrigger(go);
            go.AddComponent<PlatformerProp>().kind = PlatformerProp.Kind.Goal;
        }

        private static GameObject Cube(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
            return go;
        }

        private static void MakeTrigger(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private static Material Mat(Color color) => Fitzmark.BDRSim.UI.MaterialLibrary.Get(color);
    }
}
