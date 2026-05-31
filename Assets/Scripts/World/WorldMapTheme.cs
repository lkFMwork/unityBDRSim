using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// The visual theme for one state's Super Mario World–style tile map: the terrain palette and
    /// decoration style picked from the state's real character (Arizona = desert, New York = cool
    /// forest, Texas = dry plains, etc.). The map renderer (<see cref="OverworldArt"/>) paints a
    /// fixed tile grid with these colours so each world reads as its own place but the layout/logic
    /// stay identical.
    /// </summary>
    public enum MapBiome { Plains, Desert, Forest, Bluegrass, Coastal, Heartland }

    public struct WorldMapTheme
    {
        public Color GrassLight, GrassDark;   // base terrain checker
        public Color Soil;                    // the winding trail
        public Color SoilEdge;                // trail border
        public Color Water;                   // lakes/rivers/sea
        public Color Decor1, Decor2;          // trees/cacti/rocks
        public MapBiome Biome;

        public static WorldMapTheme For(string stateId)
        {
            switch (stateId)
            {
                case "arizona":
                    return Make(MapBiome.Desert,
                        new Color(0.80f, 0.66f, 0.42f), new Color(0.72f, 0.57f, 0.34f),
                        new Color(0.62f, 0.43f, 0.27f), new Color(0.30f, 0.55f, 0.62f),
                        new Color(0.36f, 0.55f, 0.32f), new Color(0.74f, 0.40f, 0.34f));
                case "new_york":
                    return Make(MapBiome.Forest,
                        new Color(0.34f, 0.52f, 0.31f), new Color(0.26f, 0.44f, 0.26f),
                        new Color(0.55f, 0.45f, 0.32f), new Color(0.24f, 0.42f, 0.58f),
                        new Color(0.20f, 0.40f, 0.24f), new Color(0.62f, 0.62f, 0.66f));
                case "texas":
                    return Make(MapBiome.Plains,
                        new Color(0.62f, 0.60f, 0.34f), new Color(0.52f, 0.52f, 0.30f),
                        new Color(0.60f, 0.44f, 0.28f), new Color(0.26f, 0.46f, 0.56f),
                        new Color(0.40f, 0.52f, 0.30f), new Color(0.74f, 0.62f, 0.40f));
                case "tennessee":
                case "alabama":
                case "georgia":
                    return Make(MapBiome.Bluegrass,
                        new Color(0.40f, 0.58f, 0.33f), new Color(0.32f, 0.50f, 0.29f),
                        new Color(0.58f, 0.46f, 0.31f), new Color(0.26f, 0.48f, 0.56f),
                        new Color(0.22f, 0.44f, 0.26f), new Color(0.78f, 0.74f, 0.45f));
                case "nebraska":
                case "missouri":
                case "indiana":
                default:
                    return Make(MapBiome.Heartland,
                        new Color(0.52f, 0.58f, 0.32f), new Color(0.44f, 0.52f, 0.30f),
                        new Color(0.58f, 0.45f, 0.30f), new Color(0.28f, 0.48f, 0.58f),
                        new Color(0.34f, 0.50f, 0.28f), new Color(0.80f, 0.72f, 0.40f));
            }
        }

        private static WorldMapTheme Make(MapBiome biome, Color gl, Color gd, Color soil,
            Color water, Color d1, Color d2) => new WorldMapTheme
        {
            Biome = biome,
            GrassLight = gl, GrassDark = gd,
            Soil = soil, SoilEdge = soil * 0.7f,
            Water = water, Decor1 = d1, Decor2 = d2
        };
    }
}
