using UnityEngine;
using Fitzmark.BDRSim.UI;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Builds enclosed interior rooms from simple slabs: floor, ceiling, and four walls
    /// with optional centered doorways (gap + lintel above) so you can walk between rooms
    /// in first person. Walls are inset half a thickness so adjacent rooms' partition walls
    /// sit flush instead of z-fighting. Each room gets a ceiling light. Real furniture and
    /// characters are added separately by the caller.
    /// </summary>
    public static class RoomBuilder
    {
        public enum Side { None, N, S, E, W }

        public const float WallThickness = 0.16f;
        public const float DoorWidth = 2.6f;
        public const float DoorHeight = 2.4f;

        /// <summary>Build a full enclosed room. <paramref name="door"/> opens one wall to the corridor.</summary>
        public static void Room(Transform parent, Vector3 min, Vector3 max, float height,
            Color wall, Color floor, Color ceiling, Side door, Color lightColor)
        {
            Floor(parent, min, max, floor);
            Ceiling(parent, min, max, height, ceiling);

            float t = WallThickness;
            float midX = (min.x + max.x) / 2f;
            float midZ = (min.z + max.z) / 2f;

            // Walls inset so neighbours tile flush.
            WallX(parent, min.x, max.x, min.z + t / 2f, min.y, height, wall, door == Side.S ? midX : (float?)null);
            WallX(parent, min.x, max.x, max.z - t / 2f, min.y, height, wall, door == Side.N ? midX : (float?)null);
            WallZ(parent, min.z, max.z, min.x + t / 2f, min.y, height, wall, door == Side.W ? midZ : (float?)null);
            WallZ(parent, min.z, max.z, max.x - t / 2f, min.y, height, wall, door == Side.E ? midZ : (float?)null);

            CeilingLight(parent, new Vector3(midX, min.y + height - 0.15f, midZ), lightColor);
        }

        public static void Floor(Transform parent, Vector3 min, Vector3 max, Color color)
        {
            Slab(parent, "Floor", new Vector3((min.x + max.x) / 2f, min.y - 0.05f, (min.z + max.z) / 2f),
                new Vector3(max.x - min.x, 0.1f, max.z - min.z), color);
        }

        public static void Ceiling(Transform parent, Vector3 min, Vector3 max, float height, Color color)
        {
            Slab(parent, "Ceiling", new Vector3((min.x + max.x) / 2f, min.y + height + 0.05f, (min.z + max.z) / 2f),
                new Vector3(max.x - min.x, 0.1f, max.z - min.z), color);
        }

        /// <summary>Wall running along X at fixed z, with an optional centered doorway at doorX.</summary>
        public static void WallX(Transform parent, float x0, float x1, float z, float baseY, float h,
            Color color, float? doorX = null)
        {
            if (doorX == null)
            {
                Slab(parent, "Wall", new Vector3((x0 + x1) / 2f, baseY + h / 2f, z),
                    new Vector3(Mathf.Abs(x1 - x0), h, WallThickness), color);
                return;
            }
            float dl = doorX.Value - DoorWidth / 2f, dr = doorX.Value + DoorWidth / 2f;
            if (dl > x0) Slab(parent, "Wall", new Vector3((x0 + dl) / 2f, baseY + h / 2f, z),
                new Vector3(dl - x0, h, WallThickness), color);
            if (x1 > dr) Slab(parent, "Wall", new Vector3((dr + x1) / 2f, baseY + h / 2f, z),
                new Vector3(x1 - dr, h, WallThickness), color);
            if (h > DoorHeight) Slab(parent, "Lintel",
                new Vector3(doorX.Value, baseY + DoorHeight + (h - DoorHeight) / 2f, z),
                new Vector3(DoorWidth, h - DoorHeight, WallThickness), color);
        }

        /// <summary>Wall running along Z at fixed x, with an optional centered doorway at doorZ.</summary>
        public static void WallZ(Transform parent, float z0, float z1, float x, float baseY, float h,
            Color color, float? doorZ = null)
        {
            if (doorZ == null)
            {
                Slab(parent, "Wall", new Vector3(x, baseY + h / 2f, (z0 + z1) / 2f),
                    new Vector3(WallThickness, h, Mathf.Abs(z1 - z0)), color);
                return;
            }
            float dl = doorZ.Value - DoorWidth / 2f, dr = doorZ.Value + DoorWidth / 2f;
            if (dl > z0) Slab(parent, "Wall", new Vector3(x, baseY + h / 2f, (z0 + dl) / 2f),
                new Vector3(WallThickness, h, dl - z0), color);
            if (z1 > dr) Slab(parent, "Wall", new Vector3(x, baseY + h / 2f, (dr + z1) / 2f),
                new Vector3(WallThickness, h, z1 - dr), color);
            if (h > DoorHeight) Slab(parent, "Lintel",
                new Vector3(x, baseY + DoorHeight + (h - DoorHeight) / 2f, (z0 + z1) / 2f),
                new Vector3(WallThickness, h - DoorHeight, DoorWidth), color);
        }

        public static void CeilingLight(Transform parent, Vector3 pos, Color color)
        {
            // Bright fixture panel + a point light below it.
            var fixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fixture.name = "LightFixture";
            fixture.transform.SetParent(parent, false);
            fixture.transform.position = pos;
            fixture.transform.localScale = new Vector3(1.2f, 0.08f, 1.2f);
            var col = fixture.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
            var mat = MaterialLibrary.Get(new Color(1f, 0.98f, 0.92f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(1f, 0.97f, 0.88f) * 1.5f);
            var r = fixture.GetComponent<Renderer>(); if (r != null) r.sharedMaterial = mat;

            var lgo = new GameObject("RoomLight");
            lgo.transform.SetParent(parent, false);
            lgo.transform.position = pos - new Vector3(0f, 0.3f, 0f);
            var light = lgo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 4.0f;
            light.range = 16f;
            light.shadows = LightShadows.None;
        }

        private static void Slab(Transform parent, string name, Vector3 center, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = MaterialLibrary.Get(color);
        }
    }
}
