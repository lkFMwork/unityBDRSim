using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Builds a real Unity Terrain under a city, shaped by the city's place in its state — the
    /// "9 points": the city's overworld cell and its 8 neighbours (sampled continuously from
    /// <see cref="StateGeography"/>). The ground rises toward the state's mountains and dips below
    /// the waterline toward its lakes/coasts/rivers, so the drivable city is consistent with the
    /// overworld map. The central core is kept gentle so the road grid stays drivable; relief
    /// ramps in toward the edges. Exposes <see cref="SampleHeight"/> so roads/buildings/the car
    /// drape onto the surface.
    /// </summary>
    public class CityTerrain
    {
        public Terrain Terrain { get; private set; }
        public float WaterLevel { get; private set; }   // world Y of the water plane (0 = base)

        private TerrainData _data;
        private float _size;       // world metres (square)
        private float _maxHeight;  // world metres of full elevation
        private Vector3 _origin;   // terrain corner in world space (terrain is corner-pivoted)

        private const float CoreFlatRadius = 0.34f; // fraction of the map kept flat for the grid

        /// <summary>
        /// Generate the terrain centered on (0,0,0). <paramref name="size"/> is the square extent
        /// in metres; <paramref name="stateId"/> + the city's normalized position drive the shape.
        /// </summary>
        public void Build(Transform parent, string stateId, float cityNx, float cityNy,
            float size, Color groundTint)
        {
            _size = size;
            _maxHeight = Mathf.Clamp(size * 0.22f, 24f, 80f); // mountains read tall but not absurd
            var geo = StateGeography.For(stateId);
            bool desert = geo.DesertBase || cityNx < geo.DesertWest;

            int res = 129; // heightmap resolution (2^n + 1)
            _data = new TerrainData
            {
                heightmapResolution = res,
                size = new Vector3(size, _maxHeight, size)
            };

            // The city occupies a small footprint within its state; sample the state's elevation
            // field across a window around the city so the in-city terrain shows the nearby
            // mountains/water direction. windowFrac = how much of the state spans the city map.
            const float windowFrac = 0.18f;
            float baseElev = geo.Elevation(cityNx, cityNy, out _);
            var heights = new float[res, res];
            float lowest = float.MaxValue;

            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float u = x / (res - 1f);          // 0..1 across the city (west→east)
                    float v = y / (res - 1f);          // 0..1 (south→north)
                    // Map this terrain point to a state position near the city.
                    float snx = Mathf.Clamp01(cityNx + (u - 0.5f) * windowFrac);
                    float sny = Mathf.Clamp01(cityNy + (v - 0.5f) * windowFrac);
                    float e = geo.Elevation(snx, sny, out bool _);

                    // Keep the core gentle: blend the sampled elevation toward the city's base
                    // elevation near the centre, full relief out at the rim.
                    float r = Mathf.Max(Mathf.Abs(u - 0.5f), Mathf.Abs(v - 0.5f)) * 2f; // 0 centre →1 edge
                    float relief = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(CoreFlatRadius, 1f, r));
                    float elev = Mathf.Lerp(baseElev, e, relief);

                    // a little noise so slopes aren't glassy
                    elev += (Mathf.PerlinNoise(x * 0.12f, y * 0.12f) - 0.5f) * 0.04f * relief;

                    heights[y, x] = elev;
                    if (elev < lowest) lowest = elev;
                }

            // Normalize so the lowest point maps to terrain-y 0; remember where the waterline sits.
            // Elevation 0 in StateGeography space is "ground"; anything below is water.
            float span = Mathf.Max(0.001f, 1f - lowest);
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                    heights[y, x] = (heights[y, x] - lowest) / span;
            _data.SetHeights(0, 0, heights);
            PaintSplat(desert, groundTint); // grass / rock / sand / snow by height + slope

            float groundNorm = (0f - lowest) / span;       // where elevation 0 lands in [0..1]
            WaterLevel = groundNorm * _maxHeight - 0.5f;    // water plane just under "ground" level

            var go = Terrain.CreateTerrainGameObject(_data);
            go.name = "CityTerrain";
            go.transform.SetParent(parent, false);
            _origin = new Vector3(-size * 0.5f, 0f, -size * 0.5f);
            go.transform.localPosition = _origin;
            Terrain = go.GetComponent<Terrain>();
            var tmat = TerrainMat();
            if (tmat != null) Terrain.materialTemplate = tmat; // URP's own terrain material (a built-in terrain shader renders pink)

            BuildWater(parent, size, groundTint);
            ScatterTrees(parent, geo, desert, size, stateId); // forest the relief ring around the city
        }

        /// <summary>World-space ground height at (x,z). Roads/buildings/car drape onto this.</summary>
        public float SampleHeight(float worldX, float worldZ)
        {
            if (Terrain == null) return 0f;
            return Terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + _origin.y;
        }

        public bool IsUnderwater(float worldX, float worldZ) => SampleHeight(worldX, worldZ) < WaterLevel + 0.05f;

        private void BuildWater(Transform parent, float size, Color tint)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Plane);
            w.name = "CityWater";
            Object.Destroy(w.GetComponent<Collider>());
            w.transform.SetParent(parent, false);
            w.transform.localScale = new Vector3(size / 10f, 1f, size / 10f);
            w.transform.localPosition = new Vector3(0f, WaterLevel, 0f);
            var mat = MaterialLibrary.Get(new Color(0.18f, 0.40f, 0.62f, 1f));
            var r = w.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }

        private static Material TerrainMat()
        {
            // Use the ACTIVE render pipeline's terrain material. A built-in terrain shader under
            // URP renders magenta ("pink terrain"), and Shader.Find for the URP terrain shader can
            // return null when nothing else references it — the pipeline's defaultTerrainMaterial is
            // the reliable source. Falls back to the URP terrain shader, then null (Unity default).
            var rp = GraphicsSettings.currentRenderPipeline;
            if (rp != null && rp.defaultTerrainMaterial != null) return rp.defaultTerrainMaterial;
            var shader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            return shader != null ? new Material(shader) : null;
        }

        // ---- ground texturing: grass / rock / sand / snow by height + slope ----

        private void PaintSplat(bool desert, Color groundTint)
        {
            _data.terrainLayers = new[]
            {
                MakeLayer(GrassTint(groundTint),          0.10f, 13f), // 0 grass
                MakeLayer(new Color(0.43f, 0.41f, 0.38f), 0.10f,  9f), // 1 rock
                MakeLayer(new Color(0.80f, 0.73f, 0.52f), 0.06f, 11f), // 2 sand
                MakeLayer(new Color(0.93f, 0.95f, 0.99f), 0.04f, 13f), // 3 snow
            };

            const int ar = 128;
            _data.alphamapResolution = ar;
            var maps = new float[ar, ar, 4];
            for (int y = 0; y < ar; y++)
                for (int x = 0; x < ar; x++)
                {
                    float nx = x / (ar - 1f);
                    float ny = y / (ar - 1f);
                    float hn = Mathf.Clamp01(_data.GetInterpolatedHeight(nx, ny) / Mathf.Max(0.001f, _maxHeight));
                    float slope = Mathf.Clamp01(_data.GetSteepness(nx, ny) / 55f);

                    float snowStart = desert ? 0.85f : 0.62f;
                    float wSnow = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(snowStart, snowStart + 0.16f, hn));
                    float wRock = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.32f, 0.62f, slope));
                    float wSand = desert ? 0.9f : 1f - Mathf.InverseLerp(0.02f, 0.12f, hn); // low ground / shoreline
                    float wGrass = 1f;

                    // priority: snow over rock over sand over grass
                    wRock  *= 1f - wSnow;
                    wSand  *= (1f - wSnow) * (1f - wRock);
                    wGrass *= (1f - wSnow) * (1f - wRock) * (1f - wSand);

                    float sum = wSnow + wRock + wSand + wGrass + 1e-4f;
                    maps[y, x, 0] = wGrass / sum;
                    maps[y, x, 1] = wRock / sum;
                    maps[y, x, 2] = wSand / sum;
                    maps[y, x, 3] = wSnow / sum;
                }
            _data.SetAlphamaps(0, 0, maps);
        }

        private static Color GrassTint(Color themed)
        {
            // Pull the theme's ground colour toward a natural grass green so each city still
            // reads as itself, but the land looks planted rather than flatly tinted.
            return Color.Lerp(themed, new Color(0.34f, 0.52f, 0.27f), 0.55f);
        }

        private static TerrainLayer MakeLayer(Color baseCol, float noise, float tile)
        {
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color[n * n];
            for (int i = 0; i < px.Length; i++)
            {
                float g = (Mathf.PerlinNoise((i % n) * 0.6f, (i / n) * 0.6f) - 0.5f) * noise;
                px[i] = new Color(Mathf.Clamp01(baseCol.r + g),
                                  Mathf.Clamp01(baseCol.g + g),
                                  Mathf.Clamp01(baseCol.b + g), 1f);
            }
            tex.SetPixels(px);
            tex.Apply();
            return new TerrainLayer { diffuseTexture = tex, tileSize = new Vector2(tile, tile) };
        }

        // ---- forest the relief ring (deserts stay bare, mountains stay dense) ----

        private void ScatterTrees(Transform parent, StateGeography geo, bool desert, float size, string stateId)
        {
            if (Terrain == null) return;
            float density = desert ? 0.05f : geo.Mountains != MapEdge.None ? 1f : 0.5f;
            int target = Mathf.RoundToInt(Mathf.Min(size * size / 900f, 160f) * density);
            if (target <= 0) return;

            var forest = new GameObject("Forest").transform;
            forest.SetParent(parent, false);
            var rng = new System.Random(stateId.GetHashCode() ^ Mathf.RoundToInt(size));
            const string tree = "Models/kenney_city-kit-suburban_20/Models/FBX format/tree-large";

            int placed = 0, guard = target * 8;
            while (placed < target && guard-- > 0)
            {
                float u = (float)rng.NextDouble(), v = (float)rng.NextDouble();
                float r = Mathf.Max(Mathf.Abs(u - 0.5f), Mathf.Abs(v - 0.5f)) * 2f;
                if (r < 0.60f) continue; // keep the city core clear; plant the surrounding relief
                float wx = (u - 0.5f) * size, wz = (v - 0.5f) * size;
                float gy = SampleHeight(wx, wz);
                if (gy < WaterLevel + 0.4f) continue;                // not in the water
                float hn = gy / Mathf.Max(0.001f, _maxHeight);
                if (hn > 0.66f && rng.NextDouble() > 0.15) continue; // thin out toward the snow line
                ModelLibrary.Spawn(tree, forest, new Vector3(wx, gy, wz),
                    (float)rng.NextDouble() * 360f, 1f,
                    placeholderColor: new Color(0.26f, 0.5f, 0.28f), placeholderLabel: false,
                    fitHeight: 7f);
                placed++;
            }
        }
    }
}
