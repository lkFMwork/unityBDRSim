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
        public const string Mushroom = Pack + "tile_0067";   // gem → power-up (grow big)
        public const string QBlock = Pack + "tile_0009";     // crate / "?" block (holds power-up)

        // Player character (GREEN). Kenney lays characters out as 2-frame pairs, so the green
        // character is ONLY tile_0000 (idle) + tile_0001 (step) — tile_0002+ are OTHER colors
        // (blue, etc.). Jump reuses the step frame so the character stays green throughout.
        public const string CharIdle = Chars + "tile_0000";
        public const string CharWalkA = Chars + "tile_0001";
        public const string CharWalkB = Chars + "tile_0000"; // 2-frame cycle (idle↔step)
        public const string CharJump = Chars + "tile_0001";  // step pose (NOT tile_0002 = blue char)

        // Enemy
        public const string Enemy = Chars + "tile_0024";
    }
}
