namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Real topography for each state's overworld map, distilled from its actual geography so the
    /// world reads as that place: where the mountains are, which edges meet open water, and any
    /// major river. The map renderer classifies each grid cell against this. Coordinates are
    /// normalized (0 = west/south, 1 = east/north), matching the city MapX/MapZ axes.
    /// Sources: state geography (Wikipedia / Britannica) — e.g. TN Smokies east + Mississippi west,
    /// GA/AL Appalachians north + Gulf/Atlantic south, AZ Sonoran desert + plateau north, etc.
    /// </summary>
    [System.Flags]
    public enum MapEdge { None = 0, N = 1, S = 2, E = 4, W = 8 }

    public struct MapRiver
    {
        public bool Exists;
        public bool Horizontal; // true = runs west→east; false = north→south
        public float Pos;       // 0..1 along the perpendicular axis (y for horizontal, x for vertical)
    }

    public class StateGeography
    {
        public MapEdge Water;        // edges that are open water (lake / gulf / ocean / big-river border)
        public MapEdge Mountains;    // side band(s) holding the mountains
        public float MountainDepth;  // fraction of the map the mountain band occupies (0..1)
        public MapRiver River;       // an internal river line, if any
        public bool DesertBase;      // sand everywhere (Arizona)
        public float DesertWest;     // sand for the western fraction nx < this (Texas trans-Pecos)

        private static StateGeography G(MapEdge water, MapEdge mtns, float depth = 0.26f,
            MapRiver river = default, bool desert = false, float desertWest = 0f) =>
            new StateGeography { Water = water, Mountains = mtns, MountainDepth = depth,
                River = river, DesertBase = desert, DesertWest = desertWest };

        private static MapRiver H(float pos) => new MapRiver { Exists = true, Horizontal = true, Pos = pos };
        private static MapRiver V(float pos) => new MapRiver { Exists = true, Horizontal = false, Pos = pos };

        public static StateGeography For(string stateId)
        {
            switch (stateId)
            {
                // Smoky Mtns east; Mississippi River forms the west border (Memphis).
                case "tennessee": return G(MapEdge.W, MapEdge.E, 0.28f);
                // Blue Ridge/Appalachians north; Atlantic at the SE (Savannah).
                case "georgia":   return G(MapEdge.E, MapEdge.N, 0.24f);
                // Appalachian foothills north; Gulf of Mexico south (Mobile).
                case "alabama":   return G(MapEdge.S, MapEdge.N, 0.26f);
                // Sonoran desert throughout; Colorado Plateau/Grand Canyon north; Colorado R west.
                case "arizona":   return G(MapEdge.W, MapEdge.N, 0.30f, desert: true);
                // Trans-Pecos mountains far west; Gulf coast SE; dry desert in the west.
                case "texas":     return G(MapEdge.S, MapEdge.W, 0.16f, desertWest: 0.30f);
                // Adirondacks/Catskills east; Great Lakes (Erie/Ontario) north; Hudson R east.
                case "new_york":  return G(MapEdge.N, MapEdge.E, 0.30f, V(0.82f));
                // Ozark Plateau south; Mississippi east border; Missouri R across the middle.
                case "missouri":  return G(MapEdge.E, MapEdge.S, 0.30f, H(0.60f));
                // Great Plains; Platte R west→east across the middle; Missouri R east border.
                case "nebraska":  return G(MapEdge.E, MapEdge.None, 0f, H(0.45f));
                // Flat farmland; Ohio River along the south border; Lake Michigan touches the NW.
                case "indiana":   return G(MapEdge.S, MapEdge.None, 0f);
                default:          return G(MapEdge.None, MapEdge.None, 0f);
            }
        }
    }
}
