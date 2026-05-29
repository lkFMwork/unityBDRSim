using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using NUnit.Framework;

namespace Fitzmark.BDRSim.Tests
{
    public class ProspectGeneratorTests
    {
        [Test]
        public void SameSeedProducesIdenticalProspect()
        {
            var a = ProspectGenerator.Generate(5, 4242);
            var b = ProspectGenerator.Generate(5, 4242);

            Assert.AreEqual(a.prospect.contactName, b.prospect.contactName);
            Assert.AreEqual(a.PrimaryLane.label, b.PrimaryLane.label);
            Assert.AreEqual(a.PrimaryLane.miles, b.PrimaryLane.miles);
        }

        [Test]
        public void ProducesAValidScenario()
        {
            var s = ProspectGenerator.Generate(3, 99);

            Assert.IsTrue(s.IsValid);
            Assert.That(s.prospect.contactName, Is.Not.Empty);
            Assert.That(s.prospect.lanes.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(s.prospect.likelyObjections.Count, Is.GreaterThanOrEqualTo(1));
            // Fitzmark must be able to cover below the incumbent rate for a deal to exist.
            Assert.That(s.PrimaryLane.fitzmarkCostPerMile, Is.LessThan(s.PrimaryLane.currentRatePerMile));
        }

        [Test]
        public void DifficultyTierScalesWithLevel()
        {
            Assert.AreEqual(DifficultyTier.Easy, ProspectGenerator.Generate(1, 1).difficulty);
            Assert.AreEqual(DifficultyTier.Hard, ProspectGenerator.Generate(10, 1).difficulty);
        }

        [Test]
        public void HarderLeadsHaveMoreObjections()
        {
            var easy = ProspectGenerator.Generate(1, 7);
            var hard = ProspectGenerator.Generate(10, 7);
            Assert.That(hard.prospect.likelyObjections.Count,
                Is.GreaterThanOrEqualTo(easy.prospect.likelyObjections.Count));
        }
    }
}
