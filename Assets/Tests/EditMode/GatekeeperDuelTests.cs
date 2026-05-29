using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using NUnit.Framework;

namespace Fitzmark.BDRSim.Tests
{
    public class GatekeeperDuelTests
    {
        private static GatekeeperDuel Started(int resolve = 60, BDRAttributes attr = null, int seed = 1)
        {
            var d = new GatekeeperDuel(resolve, attr ?? new BDRAttributes(), 1f, seed);
            d.Begin();
            return d;
        }

        [Test]
        public void PlayingTheWeaknessReducesResolve()
        {
            var d = Started(seed: 5);
            int before = d.GatekeeperResolve;
            d.Play(GatekeeperCombatData.Weakness(d.CurrentStance));
            Assert.That(d.GatekeeperResolve, Is.LessThan(before));
        }

        [Test]
        public void PlayingTheResistBackfiresOntoComposure()
        {
            var d = Started(seed: 5);
            int resolveBefore = d.GatekeeperResolve;
            int composureBefore = d.PlayerComposure;

            d.Play(GatekeeperCombatData.Resist(d.CurrentStance));

            Assert.AreEqual(resolveBefore, d.GatekeeperResolve);          // resist deals no resolve damage
            Assert.That(d.PlayerComposure, Is.LessThan(composureBefore)); // it hurts you instead
        }

        [Test]
        public void ReadingCorrectlyEveryTurnWins()
        {
            var d = Started(seed: 7);
            int guard = 50;
            while (!d.IsOver && guard-- > 0)
                d.Play(GatekeeperCombatData.Weakness(d.CurrentStance));

            Assert.AreEqual(DuelOutcome.Won, d.Outcome);
        }

        [Test]
        public void ReadingWrongEveryTurnLoses()
        {
            var d = Started(seed: 9);
            int guard = 100;
            while (!d.IsOver && guard-- > 0)
                d.Play(GatekeeperCombatData.Resist(d.CurrentStance));

            Assert.AreEqual(DuelOutcome.Lost, d.Outcome);
        }

        [Test]
        public void ResilienceRaisesStartingComposure()
        {
            var soft = new GatekeeperDuel(60, new BDRAttributes { resilience = 5 }, 1f, 1);
            var tough = new GatekeeperDuel(60, new BDRAttributes { resilience = 10 }, 1f, 1);
            Assert.That(tough.PlayerMaxComposure, Is.GreaterThan(soft.PlayerMaxComposure));
        }
    }
}
