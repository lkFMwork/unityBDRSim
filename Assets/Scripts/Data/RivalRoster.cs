namespace Fitzmark.BDRSim.Data
{
    /// <summary>The AI reps the player competes against on the office leaderboard.</summary>
    public static class RivalRoster
    {
        public static readonly string[] Names =
        {
            "Jordan B.", "Sam K.", "Riley P.", "Casey M.", "Drew L."
        };

        /// <summary>A deterministic starting deal count for each rival.</summary>
        public static int StartingDeals(int index) => 2 + index * 2;
    }
}
