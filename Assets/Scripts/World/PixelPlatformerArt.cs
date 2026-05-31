namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Verified sprite keys into the imported Kenney Pixel Platformer pack. The pack ships as
    /// individually-numbered PNGs (Tiles/tile_NNNN, Characters/tile_NNNN), referenced by index,
    /// not name — these constants map our semantic needs to the exact tiles, confirmed by eye
    /// from the tilesheet (terrain 18px, characters 24px; row-major index = row*cols + col).
    /// Paths are relative to Resources/, so SpriteLibrary loads them directly.
    /// </summary>
    public static class PixelPlatformerArt
    {
        private const string Pack = "Models/kenney_pixel-platformer/Tiles/";
        private const string Chars = Pack + "Characters/";

        // Terrain
        public const string GrassTop = Pack + "tile_0000";   // grass-topped block (surface)
        public const string Dirt = Pack + "tile_0020";       // solid dirt fill (below surface)

        // Pickups / goal
        public const string Coin = Pack + "tile_0151";       // gold coin
        public const string Heart = Pack + "tile_0044";      // heart (+1 life)
        public const string Flag = Pack + "tile_0111";       // flag on a pole (goal)
        public const string Spring = Pack + "tile_0028";     // sign/spring-ish marker

        // Player character (green) — idle / walk / jump frames
        public const string CharIdle = Chars + "tile_0000";
        public const string CharWalkA = Chars + "tile_0001";
        public const string CharWalkB = Chars + "tile_0000"; // 2-frame cycle (idle↔step)
        public const string CharJump = Chars + "tile_0002";

        // Enemy
        public const string Enemy = Chars + "tile_0024";
    }
}
