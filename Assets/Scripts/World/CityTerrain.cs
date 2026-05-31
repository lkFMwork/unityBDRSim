using Fitzmark.BDRSim.Data;
using UnityEngine;

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

            float groundNorm = (0f - lowest) / span;       // where elevation 0 lands in [0..1]
            WaterLevel = groundNorm * _maxHeight - 0.5f;    // water plane just under "ground" level

            var go = Terrain.CreateTerrainGameObject(_data);
            go.name = "CityTerrain";
            go.transform.SetParent(parent, false);
            _origin = new Vector3(-size * 0.5f, 0f, -size * 0.5f);
            go.transform.localPosition = _origin;
            Terrain = go.GetComponent<Terrain>();
            Terrain.materialTemplate = TerrainMat(groundTint);

            BuildWater(parent, size, groundTint);
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

        private static Material TerrainMat(Color tint)
        {
            // Terrain needs a terrain-compatible shader; fall back to a tinted lit material.
            var shader = Shader.Find("Universal Render Pipeline/Terrain/Lit")
                         ?? Shader.Find("Nature/Terrain/Standard");
            var mat = shader != null ? new Material(shader) : MaterialLibrary.Get(tint);
            if (shader != null) mat.color = tint;
            return mat;
        }
    }
}
