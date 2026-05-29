using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using NUnit.Framework;

namespace Fitzmark.BDRSim.Tests
{
    public class QuestSystemTests
    {
        [Test]
        public void CompletesWhenObjectivesAreMet()
        {
            var c = new BDRCharacter { callsMade = 1 };
            var done = QuestSystem.Sync(c);

            Assert.IsTrue(c.completedQuests.Contains("first_call"));
            Assert.IsTrue(done.Exists(q => q.Id == "first_call"));
        }

        [Test]
        public void DoesNotCompleteOrRewardTwice()
        {
            var c = new BDRCharacter { callsMade = 1 };
            QuestSystem.Sync(c);
            int xpAfterFirst = c.xp;

            var again = QuestSystem.Sync(c);
            Assert.IsFalse(again.Exists(q => q.Id == "first_call"));
            Assert.AreEqual(xpAfterFirst, c.xp); // no double reward
        }

        [Test]
        public void GrantsXpAndSkillPointRewards()
        {
            var c = new BDRCharacter { dealsWon = 1 };
            int spBefore = c.unspentSkillPoints;
            int xpBefore = c.xp;

            QuestSystem.Sync(c);

            Assert.IsTrue(c.completedQuests.Contains("first_deal"));
            Assert.That(c.unspentSkillPoints, Is.GreaterThan(spBefore)); // first_deal grants a skill point
            Assert.That(c.xp, Is.GreaterThan(xpBefore));
        }

        [Test]
        public void MultiObjectiveRequiresAllParts()
        {
            var c = new BDRCharacter { callsMade = 8, dealsWon = 0 };
            QuestSystem.Sync(c);
            Assert.IsFalse(c.completedQuests.Contains("the_grind")); // still needs the deals

            c.dealsWon = 3;
            QuestSystem.Sync(c);
            Assert.IsTrue(c.completedQuests.Contains("the_grind"));
        }

        [Test]
        public void ReachLevelObjectiveCompletes()
        {
            var c = new BDRCharacter { level = 3 };
            QuestSystem.Sync(c);
            Assert.IsTrue(c.completedQuests.Contains("climbing"));
        }

        [Test]
        public void ObjectiveProgressIsClampedToOne()
        {
            var c = new BDRCharacter { callsMade = 100 };
            var firstCall = QuestLibrary.Get("first_call");
            Assert.AreEqual(1f, QuestSystem.ObjectiveProgress(c, firstCall.Objectives[0]), 0.0001f);
        }
    }
}
