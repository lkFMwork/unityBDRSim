using System.Collections.Generic;
using Fitzmark.BDRSim.Data;

namespace Fitzmark.BDRSim.Simulation
{
    public readonly struct LeaderboardEntry
    {
        public readonly string Name;
        public readonly int Deals;
        public readonly bool IsPlayer;

        public LeaderboardEntry(string name, int deals, bool isPlayer)
        {
            Name = name;
            Deals = deals;
            IsPlayer = isPlayer;
        }
    }

    /// <summary>
    /// A friendly office leaderboard: the player versus a roster of AI rivals whose
    /// deal counts tick up each workday. Deterministic per day so it doesn't jitter.
    /// </summary>
    public static class LeaderboardSystem
    {
        public static void EnsureRivals(BDRCharacter c)
        {
            if (c.rivalDeals == null) c.rivalDeals = new List<int>();
            if (c.rivalDeals.Count != RivalRoster.Names.Length)
            {
                c.rivalDeals.Clear();
                for (int i = 0; i < RivalRoster.Names.Length; i++)
                    c.rivalDeals.Add(RivalRoster.StartingDeals(i));
            }
        }

        public static void AdvanceRivals(BDRCharacter c)
        {
            EnsureRivals(c);
            var rng = new System.Random(c.career.day * 97 + 13);
            for (int i = 0; i < c.rivalDeals.Count; i++)
                c.rivalDeals[i] += rng.Next(0, 4);
        }

        public static List<LeaderboardEntry> Standings(BDRCharacter c)
        {
            EnsureRivals(c);
            var list = new List<LeaderboardEntry>
            {
                new LeaderboardEntry($"{c.DisplayName} (You)", c.dealsWon, true)
            };
            for (int i = 0; i < RivalRoster.Names.Length && i < c.rivalDeals.Count; i++)
                list.Add(new LeaderboardEntry(RivalRoster.Names[i], c.rivalDeals[i], false));

            list.Sort((a, b) => b.Deals.CompareTo(a.Deals));
            return list;
        }
    }
}
