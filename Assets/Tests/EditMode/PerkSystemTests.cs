using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using NUnit.Framework;

namespace Fitzmark.BDRSim.Tests
{
    public class PerkSystemTests
    {
        [Test]
        public void UnlockSpendsPointsAndRecordsPerk()
        {
            var c = new BDRCharacter { unspentSkillPoints = 5 };
            var perk = PerkLibrary.Get("silver_tongue"); // cost 2

            Assert.IsTrue(PerkSystem.CanUnlock(c, perk));
            Assert.IsTrue(PerkSystem.Unlock(c, perk));
            Assert.AreEqual(3, c.unspentSkillPoints);
            Assert.IsTrue(PerkSystem.IsUnlocked(c, "silver_tongue"));
        }

        [Test]
        public void CannotUnlockTheSamePerkTwice()
        {
            var c = new BDRCharacter { unspentSkillPoints = 10 };
            var perk = PerkLibrary.Get("silver_tongue");

            Assert.IsTrue(PerkSystem.Unlock(c, perk));
            Assert.IsFalse(PerkSystem.CanUnlock(c, perk));
            Assert.IsFalse(PerkSystem.Unlock(c, perk));
        }

        [Test]
        public void CannotUnlockWithoutEnoughPoints()
        {
            var c = new BDRCharacter { unspentSkillPoints = 1 };
            var perk = PerkLibrary.Get("silver_tongue"); // cost 2

            Assert.IsFalse(PerkSystem.CanUnlock(c, perk));
            Assert.IsFalse(PerkSystem.Unlock(c, perk));
            Assert.AreEqual(1, c.unspentSkillPoints);
        }

        [Test]
        public void AggregateSumsUnlockedEffects()
        {
            var c = new BDRCharacter { unspentSkillPoints = 10 };
            PerkSystem.Unlock(c, PerkLibrary.Get("silver_tongue"));   // trust
            PerkSystem.Unlock(c, PerkLibrary.Get("closers_instinct")); // negotiation

            var e = PerkSystem.Aggregate(c);
            Assert.That(e.TrustBonus, Is.GreaterThan(0f));
            Assert.That(e.NegotiationSkill, Is.GreaterThan(0f));
            Assert.AreEqual(1f, e.XpMultiplier, 0.0001f); // unchanged without XP perks
        }

        [Test]
        public void PerksFlowIntoCharacterModifiers()
        {
            var c = new BDRCharacter { unspentSkillPoints = 10 };
            float baseTrust = CharacterModifiers.FromCharacter(c).TrustBonus;

            PerkSystem.Unlock(c, PerkLibrary.Get("silver_tongue"));
            float withPerk = CharacterModifiers.FromCharacter(c).TrustBonus;

            Assert.That(withPerk, Is.GreaterThan(baseTrust));
        }

        [Test]
        public void XpPerkRaisesXpMultiplier()
        {
            var c = new BDRCharacter { unspentSkillPoints = 10 };
            PerkSystem.Unlock(c, PerkLibrary.Get("fast_learner")); // +25% XP
            Assert.That(PerkSystem.Aggregate(c).XpMultiplier, Is.GreaterThan(1f));
        }
    }
}
