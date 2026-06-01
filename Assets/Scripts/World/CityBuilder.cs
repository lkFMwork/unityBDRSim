using System.Collections.Generic;
using UnityEngine;
using Fitzmark.BDRSim.UI;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Procedurally lays out a drivable city from the Kenney road + building kits, re-skinned
    /// per <see cref="CityTheme"/>. A street grid of road tiles, building-filled blocks, a
    /// ground plane, and "business" pads (where you press E to enter a meeting) are placed
    /// at real-world scale: the road tile is measured at runtime so streets tile seamlessly
    /// and everything is sized from that, the same auto-scaling approach that fixed the office.
    /// </summary>
    public class CityBuilder
    {
        public float TileSize { get; private set; } = 12f; // fixed real-world block size (metres)
        public Vector3 PlayerSpawn { get; private set; }
        public readonly List<(Vector3 pos, float yaw, int index)> Businesses = new();

        private readonly Transform _root;
        private readonly CityTheme _theme;
        private readonly System.Random _rng;
        private readonly string _stateId;
        private readonly float _cityNx, _cityNy;

        public CityTerrain Terrain { get; private set; }

        public CityBuilder(Transform root, CityTheme theme, int seed,
            string stateId = "", float cityNx = 0.5f, float cityNy = 0.5f)
        {
            _root = root;
            _theme = theme;
            _rng = new System.Random(seed);
            _stateId = stateId;
            _cityNx = cityNx;
            _cityNy = cityNy;
        }

        public void Build()
        {
            // Fixed real-world block size (metres) — the grid is deterministic and
            // human-scaled regardless of the road FBX's native size; each road tile is
            // fit to exactly TileSize, so streets always tile seamlessly. Doubled to 24m so
            // streets are wide enough to drive and buildings read at a believable city scale.
            TileSize = 24f;
            int r = _theme.Blocks;                 // grid radius in road cells
            float span = (2 * r + 1) * TileSize;

            Ground(span * 1.6f); // terrain extends past the grid so surrounding relief shows
            LayStreets(r);
            FillBlocks(r);

            // Spawn on the south end of the central avenue, draped onto the terrain.
            float sx = 0f, sz = -(r + 0.5f) * TileSize;
            PlayerSpawn = new Vector3(sx, GroundY(sx, sz) + 0.2f, sz);
        }

        // Real Unity terrain shaped by the city's place in its state (mountains/water around it),
        // replacing the old flat plane. Everything drapes onto it via GroundY().
        private void Ground(float size)
        {
            Terrain = new CityTerrain();
            Terrain.Build(_root, _stateId, _cityNx, _cityNy, size, _theme.Ground);
        }

        /// <summary>Terrain surface height at (x,z); 0 if terrain is somehow absent.</summary>
        public float GroundY(float x, float z) => Terrain != null ? Terrain.SampleHeight(x, z) : 0f;

        // A '+'-grid of streets: every cell on an even row/column is road; intersections crossroad.
        private void LayStreets(int r)
        {
            var roads = new GameObject("Roads").transform;
            roads.SetParent(_root, false);
            for (int gx = -r; gx <= r; gx++)
            {
                for (int gz = -r; gz <= r; gz++)
                {
                    bool roadX = gx % 2 == 0;     // avenues run N-S on even columns
                    bool roadZ = gz % 2 == 0;     // streets run E-W on even rows
                    if (!roadX && !roadZ) continue;
                    Vector3 p = Cell(gx, gz);
                    if (roadX && roadZ)
                        Tile(roads, CityThemes.RoadCrossroad, p, 0f);
                    else if (roadX)
                        Tile(roads, CityThemes.RoadStraight, p, 0f);       // N-S
                    else
                        Tile(roads, CityThemes.RoadStraight, p, 90f);      // E-W
                }
            }
        }

        // Odd/odd cells are building blocks; fill each with 1–4 themed buildings.
        private void FillBlocks(int r)
        {
            var blocks = new GameObject("Blocks").transform;
            blocks.SetParent(_root, false);
            int businessTarget = Mathf.Clamp(3 * r, 4, 10); // more doorways → more of the city's book is closeable in person
            var candidates = new List<(Vector3 c, float yaw)>();

            for (int gx = -r; gx <= r; gx++)
            for (int gz = -r; gz <= r; gz++)
            {
                if (gx % 2 == 0 || gz % 2 == 0) continue; // skip road cells
                Vector3 c = Cell(gx, gz);
                BuildBlock(blocks, c);
                // each block edge faces a street → a candidate business doorway
                candidates.Add((c + new Vector3(0f, 0f, -TileSize * 0.42f), 0f));
            }

            // Pick a spread of blocks to be enterable businesses.
            Shuffle(candidates);
            int n = Mathf.Min(businessTarget, candidates.Count);
            for (int i = 0; i < n; i++)
                Businesses.Add((candidates[i].c, candidates[i].yaw, i));
        }

        // A block is a grass lot ringed by roads. Buildings sit along the FOUR EDGES, each
        // facing OUTWARD toward its adjacent street; trees fill the leftover grass in the lot
        // interior and corners, never overlapping a building footprint.
        private void BuildBlock(Transform parent, Vector3 center)
        {
            float lotHalf = TileSize * 0.46f;          // grass nearly fills the cell
            var lot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lot.name = "Lot";
            lot.transform.SetParent(parent, false);
            lot.transform.localScale = new Vector3(lotHalf * 2f, 0.04f, lotHalf * 2f);
            lot.transform.position = center;
            var col = lot.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
            Paint(lot, _theme.Grass);

            float footprint = TileSize * (0.38f + 0.10f * (float)_rng.NextDouble());
            float setback = lotHalf - footprint * 0.5f - 0.5f; // building edge sits just inside the lot
            float maxH = _theme.Style == CityStyle.Downtown ? 44f : 18f;

            // Each side: outward direction + the yaw that turns the model's "front" to face it.
            // (dir = which edge; yaw rotates the building so its facade looks down that street.)
            var sides = new (Vector3 dir, float yaw)[]
            {
                (new Vector3(0f, 0f, -1f), 0f),     // south edge faces -Z (toward the player spawn)
                (new Vector3(0f, 0f,  1f), 180f),   // north
                (new Vector3(-1f, 0f, 0f), 90f),    // west
                (new Vector3( 1f, 0f, 0f), 270f),   // east
            };

            int count = 1 + _rng.Next(0, _theme.Style == CityStyle.Suburban ? 2 : 4);
            // Shuffle which edges get buildings so blocks vary.
            for (int i = sides.Length - 1; i > 0; i--) { int j = _rng.Next(0, i + 1); (sides[i], sides[j]) = (sides[j], sides[i]); }

            var footprints = new List<(Vector3 pos, float radius)>();
            for (int i = 0; i < count && i < sides.Length; i++)
            {
                var (dir, yaw) = sides[i];
                Vector3 pos = center + dir * setback;
                pos.y = GroundY(pos.x, pos.z); // re-drape: sit on the terrain under this building
                string model = _theme.Buildings[_rng.Next(_theme.Buildings.Length)];
                ModelLibrary.Spawn(model, parent, pos, yaw, 1f,
                    placeholderColor: new Color(0.5f, 0.5f, 0.55f), placeholderLabel: false,
                    fitFootprint: footprint, fitMaxHeight: maxH);
                footprints.Add((pos, footprint * 0.7f));
            }

            // Trees: scatter in the grass, but reject any spot that overlaps a building.
            if (_theme.Trees)
            {
                string tree = "Models/kenney_city-kit-suburban_20/Models/FBX format/tree-large";
                int trees = 1 + _rng.Next(0, 3);
                for (int t = 0; t < trees; t++)
                {
                    Vector3 spot = center + new Vector3(
                        ((float)_rng.NextDouble() * 2f - 1f) * lotHalf * 0.8f, 0f,
                        ((float)_rng.NextDouble() * 2f - 1f) * lotHalf * 0.8f);
                    spot.y = GroundY(spot.x, spot.z); // drape onto terrain
                    bool clear = true;
                    foreach (var (fpos, frad) in footprints)
                        if ((spot - fpos).sqrMagnitude < (frad + 1.5f) * (frad + 1.5f)) { clear = false; break; }
                    if (clear)
                        ModelLibrary.Spawn(tree, parent, spot, _rng.Next(0, 4) * 90f, 1f,
                            placeholderColor: new Color(0.26f, 0.5f, 0.28f), placeholderLabel: false,
                            fitHeight: 6.4f);
                }
            }
        }

        private void Tile(Transform parent, string model, Vector3 pos, float yaw)
        {
            // Fit by footprint so road tiles tile edge-to-edge at exactly TileSize.
            ModelLibrary.Spawn(model, parent, pos, yaw, 1f,
                placeholderColor: new Color(0.14f, 0.14f, 0.16f), placeholderLabel: false,
                fitFootprint: TileSize);
        }

        // Cell centre draped onto the terrain surface so road tiles follow the ground.
        private Vector3 Cell(int gx, int gz)
        {
            float x = gx * TileSize, z = gz * TileSize;
            return new Vector3(x, GroundY(x, z), z);
        }

        private void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static void Paint(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = MaterialLibrary.Get(color);
        }
    }
}
