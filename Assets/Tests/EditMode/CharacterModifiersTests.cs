using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using NUnit.Framework;

namespace Fitzmark.BDRSim.Tests
{
    public class CharacterModifiersTests
    {
        [Test]
        public void BaselineAttributesAreNeutral()
        {
            var m = CharacterModifiers.FromAttributes(new BDRAttributes()); // all 5
            Assert.AreEqual(0f, m.TrustBonus, 0.0001f);
            Assert.AreEqual(1f, m.PatienceDrainMultiplier, 0.0001f);
            Assert.AreEqual(0f, m.NegotiationSkill, 0.0001f);
            Assert.AreEqual(0f, m.BonusFor(ScoreCategory.Rapport), 0.0001f);
            Assert.AreEqual(0f, m.BonusFor(ScoreCategory.Discovery), 0.0001f);
        }

        [Test]
        public void NullAttributesYieldNeutral()
        {
            var m = CharacterModifiers.FromAttributes(null);
            Assert.AreEqual(0f, m.TrustBonus, 0.0001f);
            Assert.AreEqual(1f, m.PatienceDrainMultiplier, 0.0001f);
        }

        [Test]
        public void CharismaRaisesTrustAndRapport()
        {
            var m = CharacterModifiers.FromAttributes(new BDRAttributes { charisma = 10 });
            Assert.That(m.TrustBonus, Is.GreaterThan(0f));
            Assert.That(m.BonusFor(ScoreCategory.Rapport), Is.GreaterThan(0f));
        }

        [Test]
        public void ResilienceReducesPatienceDrain()
        {
            var m = CharacterModifiers.FromAttributes(new BDRAttributes { resilience = 10 });
            Assert.That(m.PatienceDrainMultiplier, Is.LessThan(1f));
        }

        [Test]
        public void NegotiationGivesRateHeadroom()
        {
            var m = CharacterModifiers.FromAttributes(new BDRAttributes { negotiation = 10 });
            Assert.That(m.NegotiationSkill, Is.GreaterThan(0f));
        }

        [Test]
        public void ProductKnowledgeBoostsPitchAndObjections()
        {
            var m = CharacterModifiers.FromAttributes(new BDRAttributes { productKnowledge = 10 });
            Assert.That(m.BonusFor(ScoreCategory.ValueArticulation), Is.GreaterThan(0f));
            Assert.That(m.BonusFor(ScoreCategory.ObjectionHandling), Is.GreaterThan(0f));
        }

        [Test]
        public void ProspectingBoostsDiscovery()
        {
            var m = CharacterModifiers.FromAttributes(new BDRAttributes { prospecting = 10 });
            Assert.That(m.BonusFor(ScoreCategory.Discovery), Is.GreaterThan(0f));
        }

        [Test]
        public void LowResilienceIncreasesPatienceDrain()
        {
            var m = CharacterModifiers.FromAttributes(new BDRAttributes { resilience = 1 });
            Assert.That(m.PatienceDrainMultiplier, Is.GreaterThan(1f));
        }
    }
}
