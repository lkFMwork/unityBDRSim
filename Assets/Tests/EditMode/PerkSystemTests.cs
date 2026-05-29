using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using NUnit.Framework;

namespace Fitzmark.BDRSim.Tests
{
    public class PerkSystemTests
    {
        [Test]
        public void UnlockSpendsPointsAndRecordsNode()
        {
            var c = new BDRCharacter { unspentSkillPoints = 5 };
            var perk = PerkLibrary.Get("rapport_1"); // cost 2, no prerequisite

            Assert.IsTrue(PerkSystem.CanUnlock(c, perk));
            Assert.IsTrue(PerkSystem.Unlock(c, perk));
            Assert.AreEqual(3, c.unspentSkillPoints);
            Assert.IsTrue(PerkSystem.IsUnlocked(c, "rapport_1"));
        }

        [Test]
        public void CannotUnlockTheSameNodeTwice()
        {
            var c = new BDRCharacter { unspentSkillPoints = 10 };
            var perk = PerkLibrary.Get("rapport_1");

            Assert.IsTrue(PerkSystem.Unlock(c, perk));
            Assert.IsFalse(PerkSystem.CanUnlock(c, perk));
        }

        [Test]
        public void CannotUnlockWithoutEnoughPoints()
        {
            var c = new BDRCharacter { unspentSkillPoints = 1 };
            Assert.IsFalse(PerkSystem.CanUnlock(c, PerkLibrary.Get("rapport_1"))); // costs 2
        }

        [Test]
        public void PrerequisiteGatesHigherTiers()
        {
            var c = new BDRCharacter { unspentSkillPoints = 10 };
            var tier2 = PerkLibrary.Get("rapport_2"); // requires rapport_1

            Assert.IsFalse(PerkSystem.CanUnlock(c, tier2), "tier 2 should be locked without its prereq");

            PerkSystem.Unlock(c, PerkLibrary.Get("rapport_1"));
            Assert.IsTrue(PerkSystem.CanUnlock(c, tier2), "tier 2 should unlock once the prereq is owned");
        }

        [Test]
        public void StyleTraitIsIncludedInAggregate()
        {
            var c = new BDRCharacter { styleId = "farmer" }; // Trusted Advisor: +0.05 trust
            Assert.That(PerkSystem.Aggregate(c).TrustBonus, Is.GreaterThan(0.04f));
        }

        [Test]
        public void AggregateSumsUnlockedEffects()
        {
            var c = new BDRCharacter { unspentSkillPoints = 10 };
            PerkSystem.Unlock(c, PerkLibrary.Get("rapport_1")); // trust
            PerkSystem.Unlock(c, PerkLibrary.Get("deal_1"));    // negotiation

            var e = PerkSystem.Aggregate(c);
            Assert.That(e.TrustBonus, Is.GreaterThan(0f));
            Assert.That(e.NegotiationSkill, Is.GreaterThan(0f));
        }

        [Test]
        public void PerksFlowIntoCharacterModifiers()
        {
            var c = new BDRCharacter { unspentSkillPoints = 10 };
            float baseTrust = CharacterModifiers.FromCharacter(c).TrustBonus;

            PerkSystem.Unlock(c, PerkLibrary.Get("rapport_1"));
            Assert.That(CharacterModifiers.FromCharacter(c).TrustBonus, Is.GreaterThan(baseTrust));
        }

        [Test]
        public void XpNodeRaisesXpMultiplier()
        {
            var c = new BDRCharacter { unspentSkillPoints = 12 };
            PerkSystem.Unlock(c, PerkLibrary.Get("hustle_1"));
            PerkSystem.Unlock(c, PerkLibrary.Get("hustle_2"));
            PerkSystem.Unlock(c, PerkLibrary.Get("hustle_3")); // +20% XP
            Assert.That(PerkSystem.Aggregate(c).XpMultiplier, Is.GreaterThan(1f));
        }
    }
}
