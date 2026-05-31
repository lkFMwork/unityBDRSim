namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Verified tile keys into the imported Kenney Map Pack (Resources/Models/kenney_map-pack/PNG,
    /// 64×64 tiles), identified by eye from the sheet. The SMW overworld composites these: an
    /// opaque grass/terrain BASE per cell, transparent yellow PATH overlays for the winding trail
    /// (the clean node-less 121–142 set: straights, corners, tee, cross), and decoration sprites.
    /// Connections are named by the directions each piece joins.
    /// </summary>
    public static class MapPackArt
    {
        private const string T = "Models/kenney_map-pack/PNG/mapTile_";

        // Base terrain (full opaque tiles) — chosen per state theme.
        public const string Grass = T + "022";   // solid green grass
        public const string Sand = T + "004";    // sand/desert
        public const string Stone = T + "026";   // grey stone/gravel
        public const string Snow = T + "061";    // snow/ice
        public const string Dirt = T + "067";    // packed dirt/tan

        // Path overlays (transparent background; lay over the base). Named by what they connect.
        public const string PathH = T + "127";       // ── left+right
        public const string PathV = T + "126";       // │  up+down
        public const string PathCross = T + "128";   // ┼  all four
        public const string PathCornerDR = T + "123"; // ┌ down+right
        public const string PathCornerDL = T + "124"; // ┐ down+left
        public const string PathCornerUR = T + "140"; // └ up+right
        public const string PathCornerUL = T + "141"; // ┘ up+left
        public const string PathTeeDown = T + "125";  // ┬ left+right+down
        public const string PathTeeUp = T + "142";    // ┴ left+right+up

        // Decorations (drawn over grass, away from the trail).
        public const string Tree = T + "056";
        public const string Pine = T + "040";
        public const string Bush = T + "055";
        public const string Rock = T + "039";
        public const string Cactus = T + "034";
        public const string Flower = T + "044";
    }
}
