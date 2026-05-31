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

        public CityBuilder(Transform root, CityTheme theme, int seed)
        {
            _root = root;
            _theme = theme;
            _rng = new System.Random(seed);
        }

        public void Build()
        {
            // Fixed real-world block size (metres) — the grid is deterministic and
            // human-scaled regardless of the road FBX's native size; each road tile is
            // fit to exactly TileSize, so streets always tile seamlessly.
            TileSize = 12f;
            int r = _theme.Blocks;                 // grid radius in road cells
            float span = (2 * r + 1) * TileSize;

            Ground(span * 1.25f);
            LayStreets(r);
            FillBlocks(r);

            // Spawn on the south end of the central avenue, facing north up the street.
            PlayerSpawn = new Vector3(0f, 0.2f, -(r + 0.5f) * TileSize);
        }

        private void Ground(float size)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Plane);
            g.name = "CityGround";
            g.transform.SetParent(_root, false);
            g.transform.localScale = new Vector3(size / 10f, 1f, size / 10f);
            g.transform.position = new Vector3(0f, -0.02f, 0f);
            Paint(g, _theme.Ground);
        }

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
            int businessTarget = Mathf.Clamp((2 * r) , 3, 6);
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

        private void BuildBlock(Transform parent, Vector3 center)
        {
            // Grass lot under the block.
            var lot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lot.name = "Lot";
            lot.transform.SetParent(parent, false);
            lot.transform.localScale = new Vector3(TileSize * 0.92f, 0.04f, TileSize * 0.92f);
            lot.transform.position = center + new Vector3(0f, 0.0f, 0f);
            var col = lot.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
            Paint(lot, _theme.Grass);

            // 1–4 buildings on a small inner grid; fit by FOOTPRINT so tall buildings stay
            // tall (a box-fit squashed them into tiny cubes). Height follows naturally.
            float half = TileSize * 0.24f;
            var offsets = new[]
            {
                new Vector3(-half, 0f, -half), new Vector3(half, 0f, -half),
                new Vector3(-half, 0f, half),  new Vector3(half, 0f, half),
            };
            int count = 1 + _rng.Next(0, 4);
            for (int i = 0; i < count; i++)
            {
                string model = _theme.Buildings[_rng.Next(_theme.Buildings.Length)];
                float yaw = 90f * _rng.Next(0, 4);
                float footprint = TileSize * (0.34f + 0.10f * (float)_rng.NextDouble());
                // Downtown towers may be tall; suburban/commercial capped lower so nothing looms.
                float maxH = _theme.Style == CityStyle.Downtown ? 22f : 10f;
                ModelLibrary.Spawn(model, parent, center + offsets[i], yaw, 1f,
                    placeholderColor: new Color(0.5f, 0.5f, 0.55f), placeholderLabel: false,
                    fitFootprint: footprint, fitMaxHeight: maxH);
            }

            if (_theme.Trees && _rng.Next(0, 2) == 0)
            {
                string tree = "Models/kenney_city-kit-suburban_20/Models/FBX format/tree-large";
                ModelLibrary.Spawn(tree, parent, center + new Vector3(half * 1.4f, 0f, -half * 1.4f), 0f, 1f,
                    placeholderColor: new Color(0.26f, 0.5f, 0.28f), placeholderLabel: false,
                    fitHeight: 3.2f);
            }
        }

        private void Tile(Transform parent, string model, Vector3 pos, float yaw)
        {
            // Fit by footprint so road tiles tile edge-to-edge at exactly TileSize.
            ModelLibrary.Spawn(model, parent, pos, yaw, 1f,
                placeholderColor: new Color(0.14f, 0.14f, 0.16f), placeholderLabel: false,
                fitFootprint: TileSize);
        }

        private Vector3 Cell(int gx, int gz) => new Vector3(gx * TileSize, 0f, gz * TileSize);

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
