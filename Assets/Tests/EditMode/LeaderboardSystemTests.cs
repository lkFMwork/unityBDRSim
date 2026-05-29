using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using NUnit.Framework;

namespace Fitzmark.BDRSim.Tests
{
    public class LeaderboardSystemTests
    {
        [Test]
        public void EnsureRivalsSeedsOnePerRoster()
        {
            var c = new BDRCharacter();
            LeaderboardSystem.EnsureRivals(c);
            Assert.AreEqual(RivalRoster.Names.Length, c.rivalDeals.Count);
        }

        [Test]
        public void StandingsIncludeThePlayerAndAllRivals()
        {
            var c = new BDRCharacter();
            var standings = LeaderboardSystem.Standings(c);

            Assert.AreEqual(RivalRoster.Names.Length + 1, standings.Count);
            int players = 0;
            foreach (var e in standings) if (e.IsPlayer) players++;
            Assert.AreEqual(1, players);
        }

        [Test]
        public void StandingsAreSortedByDealsDescending()
        {
            var c = new BDRCharacter { dealsWon = 999 }; // should land on top
            var standings = LeaderboardSystem.Standings(c);

            for (int i = 1; i < standings.Count; i++)
                Assert.That(standings[i - 1].Deals, Is.GreaterThanOrEqualTo(standings[i].Deals));
            Assert.IsTrue(standings[0].IsPlayer);
        }

        [Test]
        public void AdvancingRivalsNeverLowersTheirScores()
        {
            var c = new BDRCharacter();
            LeaderboardSystem.EnsureRivals(c);
            var before = new System.Collections.Generic.List<int>(c.rivalDeals);

            c.career.day = 1; LeaderboardSystem.AdvanceRivals(c);
            c.career.day = 2; LeaderboardSystem.AdvanceRivals(c);
            c.career.day = 3; LeaderboardSystem.AdvanceRivals(c);

            int sumBefore = 0, sumAfter = 0;
            for (int i = 0; i < before.Count; i++)
            {
                Assert.That(c.rivalDeals[i], Is.GreaterThanOrEqualTo(before[i]));
                sumBefore += before[i];
                sumAfter += c.rivalDeals[i];
            }
            Assert.That(sumAfter, Is.GreaterThan(sumBefore)); // some progress over three days
        }
    }
}
