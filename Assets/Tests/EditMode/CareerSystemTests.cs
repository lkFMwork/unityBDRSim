using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using NUnit.Framework;

namespace Fitzmark.BDRSim.Tests
{
    public class CareerSystemTests
    {
        [Test]
        public void EnsureStartedInitializesTheWeek()
        {
            var c = new BDRCharacter();
            CareerSystem.EnsureStarted(c);

            Assert.IsTrue(c.career.initialized);
            Assert.AreEqual(1, c.career.day);
            Assert.AreEqual(CareerSystem.BaseCallsPerDay, c.career.callsRemainingToday);
            Assert.That(c.career.weekDealsGoal, Is.GreaterThan(0));
        }

        [Test]
        public void WeekIsFiveDaysLong()
        {
            Assert.AreEqual(1, CareerSystem.Week(1));
            Assert.AreEqual(1, CareerSystem.Week(5));
            Assert.AreEqual(2, CareerSystem.Week(6));
        }

        [Test]
        public void ConsumingCallsDecrementsRemaining()
        {
            var c = new BDRCharacter();
            CareerSystem.EnsureStarted(c);
            int before = c.career.callsRemainingToday;

            CareerSystem.ConsumeCall(c);

            Assert.AreEqual(before - 1, c.career.callsRemainingToday);
        }

        [Test]
        public void EndDayAdvancesDayAndRefillsCalls()
        {
            var c = new BDRCharacter();
            CareerSystem.EnsureStarted(c);
            CareerSystem.ConsumeCall(c);

            CareerSystem.EndDay(c);

            Assert.AreEqual(2, c.career.day);
            Assert.AreEqual(CareerSystem.CallsPerDay(c), c.career.callsRemainingToday);
        }

        [Test]
        public void MeetingWeeklyQuotaGrantsReward()
        {
            var c = new BDRCharacter();
            CareerSystem.EnsureStarted(c);
            c.career.day = 5;                                   // last day of week 1
            c.career.weekDealsWon = c.career.weekDealsGoal;     // quota met
            int skillPointsBefore = c.unspentSkillPoints;

            var result = CareerSystem.EndDay(c);                // crosses into week 2

            Assert.IsTrue(result.WeekEnded);
            Assert.IsTrue(result.QuotaMet);
            Assert.AreEqual(skillPointsBefore + result.RewardSkillPoints, c.unspentSkillPoints);
            Assert.That(result.RewardSkillPoints, Is.GreaterThan(0));
            Assert.AreEqual(0, c.career.weekDealsWon);          // counters reset
        }

        [Test]
        public void MissingWeeklyQuotaGivesNoReward()
        {
            var c = new BDRCharacter();
            CareerSystem.EnsureStarted(c);
            c.career.day = 5;
            c.career.weekDealsWon = 0;
            int skillPointsBefore = c.unspentSkillPoints;

            var result = CareerSystem.EndDay(c);

            Assert.IsTrue(result.WeekEnded);
            Assert.IsFalse(result.QuotaMet);
            Assert.AreEqual(skillPointsBefore, c.unspentSkillPoints);
        }
    }
}
