using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using NUnit.Framework;

namespace Fitzmark.BDRSim.Tests
{
    public class AchievementSystemTests
    {
        [Test]
        public void UnlocksWhenEarned()
        {
            var c = new BDRCharacter { dealsWon = 1 };
            var newly = AchievementSystem.Sync(c);

            Assert.IsTrue(AchievementSystem.IsUnlocked(c, "first_deal"));
            Assert.IsTrue(newly.Exists(a => a.Id == "first_deal"));
        }

        [Test]
        public void DoesNotUnlockTwice()
        {
            var c = new BDRCharacter { dealsWon = 1 };
            AchievementSystem.Sync(c);
            var again = AchievementSystem.Sync(c);
            Assert.IsFalse(again.Exists(a => a.Id == "first_deal"));
        }

        [Test]
        public void StaysLockedUntilConditionMet()
        {
            var c = new BDRCharacter { level = 5 };
            AchievementSystem.Sync(c);
            Assert.IsFalse(AchievementSystem.IsUnlocked(c, "executive")); // needs level 10
        }

        [Test]
        public void StreakAchievementUnlocks()
        {
            var c = new BDRCharacter { bestWinStreak = 5 };
            AchievementSystem.Sync(c);
            Assert.IsTrue(AchievementSystem.IsUnlocked(c, "hot_hand"));
        }
    }
}
