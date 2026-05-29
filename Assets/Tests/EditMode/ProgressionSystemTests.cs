using System.Collections.Generic;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Fitzmark.BDRSim.Tests
{
    public class ProgressionSystemTests
    {
        [Test]
        public void Level1RequiresNoXp()
        {
            Assert.AreEqual(0, ProgressionSystem.TotalXpForLevel(1));
            Assert.AreEqual(1, ProgressionSystem.LevelForXp(0));
        }

        [Test]
        public void TotalXpIncreasesWithLevel()
        {
            int prev = -1;
            for (int level = 1; level <= 20; level++)
            {
                int total = ProgressionSystem.TotalXpForLevel(level);
                Assert.That(total, Is.GreaterThan(prev), $"level {level} should cost more total XP");
                prev = total;
            }
        }

        [Test]
        public void LevelForXpMatchesThresholds()
        {
            int xpForL3 = ProgressionSystem.TotalXpForLevel(3);
            Assert.AreEqual(3, ProgressionSystem.LevelForXp(xpForL3));
            Assert.AreEqual(2, ProgressionSystem.LevelForXp(xpForL3 - 1));
        }

        [Test]
        public void LevelProgressStaysInRange()
        {
            for (int xp = 0; xp < 2000; xp += 37)
            {
                float p = ProgressionSystem.LevelProgress(xp);
                Assert.That(p, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void XpForCallRewardsBetterOutcomes()
        {
            var won = new CallReport { OverallPercent = 0.9f, Outcome = CallOutcome.WonCommitment };
            var followUp = new CallReport { OverallPercent = 0.9f, Outcome = CallOutcome.NoSaleFollowUp };
            var hungUp = new CallReport { OverallPercent = 0.9f, Outcome = CallOutcome.HungUp };

            Assert.That(ProgressionSystem.XpForCall(won), Is.GreaterThan(ProgressionSystem.XpForCall(followUp)));
            Assert.That(ProgressionSystem.XpForCall(followUp), Is.GreaterThan(ProgressionSystem.XpForCall(hungUp)));
        }

        [Test]
        public void ApplyCallRecordsStatsAndLevelsUp()
        {
            var character = new BDRCharacter(); // level 1, 0 xp
            var report = new CallReport { OverallPercent = 1f, Outcome = CallOutcome.WonCommitment };

            var result = ProgressionSystem.ApplyCall(character, report, null);

            Assert.AreEqual(1, character.callsMade);
            Assert.AreEqual(1, character.dealsWon);
            Assert.That(result.XpGained, Is.GreaterThan(0));
            Assert.IsTrue(result.LeveledUp);
            Assert.That(character.level, Is.GreaterThan(1));
            Assert.AreEqual(result.SkillPointsGained, character.unspentSkillPoints);
        }

        [Test]
        public void ApplyCallCountsHangUps()
        {
            var character = new BDRCharacter();
            var report = new CallReport { OverallPercent = 0.1f, Outcome = CallOutcome.HungUp };

            ProgressionSystem.ApplyCall(character, report, null);

            Assert.AreEqual(1, character.callsMade);
            Assert.AreEqual(0, character.dealsWon);
            Assert.AreEqual(1, character.callsHungUp);
        }

        [Test]
        public void RankTitleClimbsWithLevel()
        {
            Assert.AreEqual("BDR", ProgressionSystem.RankTitle(1));
            Assert.AreEqual("Senior BDR", ProgressionSystem.RankTitle(5));
            Assert.AreEqual("Account Executive", ProgressionSystem.RankTitle(10));
            Assert.AreEqual("Sales Manager", ProgressionSystem.RankTitle(15));
        }

        [Test]
        public void KeyAccountAwardsBonusXp()
        {
            var prospect = ScriptableObject.CreateInstance<ProspectProfile>();
            prospect.lanes = new List<Lane> { new Lane() };

            var keyScenario = ScriptableObject.CreateInstance<ScenarioDefinition>();
            keyScenario.prospect = prospect;
            keyScenario.isKeyAccount = true;

            var normalScenario = ScriptableObject.CreateInstance<ScenarioDefinition>();
            normalScenario.prospect = prospect;
            normalScenario.isKeyAccount = false;

            var report = new CallReport { OverallPercent = 0.5f, Outcome = CallOutcome.NoSaleFollowUp };

            var keyResult = ProgressionSystem.ApplyCall(new BDRCharacter(), report, new CallSession(keyScenario));
            var normalResult = ProgressionSystem.ApplyCall(new BDRCharacter(), report, new CallSession(normalScenario));

            Assert.That(keyResult.XpGained, Is.GreaterThan(normalResult.XpGained));
        }
    }
}
