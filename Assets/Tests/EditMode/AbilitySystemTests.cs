using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using NUnit.Framework;

namespace Fitzmark.BDRSim.Tests
{
    public class AbilitySystemTests
    {
        [Test]
        public void StyleGrantsItsStartingAbility()
        {
            var c = new BDRCharacter { styleId = "closer" }; // grants "anchor"
            var abilities = AbilitySystem.AvailableAbilities(c);
            Assert.IsTrue(abilities.Exists(a => a.Id == "anchor"));
        }

        [Test]
        public void TreeNodesGrantAbilities()
        {
            var c = new BDRCharacter { styleId = "generalist", unspentSkillPoints = 20 };
            PerkSystem.Unlock(c, PerkLibrary.Get("deal_1"));
            PerkSystem.Unlock(c, PerkLibrary.Get("deal_2")); // grants "anchor"

            var abilities = AbilitySystem.AvailableAbilities(c);
            Assert.IsTrue(abilities.Exists(a => a.Id == "anchor"));   // from the tree
            Assert.IsTrue(abilities.Exists(a => a.Id == "pep_talk")); // from the Generalist style
        }

        [Test]
        public void DuplicateGrantsAreDeduplicated()
        {
            // Farmer grants pep_talk; rapport_3 also grants pep_talk.
            var c = new BDRCharacter { styleId = "farmer", unspentSkillPoints = 20 };
            PerkSystem.Unlock(c, PerkLibrary.Get("rapport_1"));
            PerkSystem.Unlock(c, PerkLibrary.Get("rapport_2"));
            PerkSystem.Unlock(c, PerkLibrary.Get("rapport_3"));

            var abilities = AbilitySystem.AvailableAbilities(c);
            Assert.AreEqual(1, abilities.FindAll(a => a.Id == "pep_talk").Count);
        }
    }
}
