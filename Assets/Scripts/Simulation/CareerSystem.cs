using System;
using Fitzmark.BDRSim.Data;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>What ending a day did — used to flash a week-summary to the player.</summary>
    public readonly struct CareerDayResult
    {
        public readonly bool WeekEnded;
        public readonly bool QuotaMet;
        public readonly int DealsWon;
        public readonly int Goal;
        public readonly int RewardSkillPoints;
        public readonly int RewardXp;

        public CareerDayResult(bool weekEnded, bool quotaMet, int dealsWon, int goal,
            int rewardSkillPoints, int rewardXp)
        {
            WeekEnded = weekEnded;
            QuotaMet = quotaMet;
            DealsWon = dealsWon;
            Goal = goal;
            RewardSkillPoints = rewardSkillPoints;
            RewardXp = rewardXp;
        }
    }

    /// <summary>
    /// The career meta-loop: workdays with a limited number of calls, grouped into
    /// weeks with a deal quota. Meeting quota grants a skill point and bonus XP.
    /// Pure logic, fully unit-testable.
    /// </summary>
    public static class CareerSystem
    {
        public const int BaseCallsPerDay = 5;
        public const int DaysPerWeek = 5;

        public static int Week(int day) => (day - 1) / DaysPerWeek + 1;

        public static int CallsPerDay(BDRCharacter character) =>
            BaseCallsPerDay + PerkSystem.Aggregate(character).ExtraCallsPerDay;

        public static int GoalForWeek(int week) => Math.Min(3 + (week - 1), 10);

        public static void EnsureStarted(BDRCharacter character)
        {
            if (character.career == null) character.career = new CareerState();
            if (character.career.initialized) return;

            character.career.initialized = true;
            character.career.day = 1;
            character.career.weekDealsWon = 0;
            character.career.weekDealsGoal = GoalForWeek(1);
            character.career.callsRemainingToday = CallsPerDay(character);
        }

        public static bool HasCallsLeft(BDRCharacter character) =>
            character.career.callsRemainingToday > 0;

        public static void ConsumeCall(BDRCharacter character)
        {
            if (character.career.callsRemainingToday > 0)
                character.career.callsRemainingToday--;
        }

        public static void RecordResult(BDRCharacter character, CallReport report)
        {
            if (report == null) return;
            if (report.Outcome == CallOutcome.WonCommitment || report.Outcome == CallOutcome.WonTrial)
                character.career.weekDealsWon++;
        }

        public static CareerDayResult EndDay(BDRCharacter character)
        {
            EnsureStarted(character);

            int oldWeek = Week(character.career.day);
            character.career.day++;
            int newWeek = Week(character.career.day);
            character.career.callsRemainingToday = CallsPerDay(character);

            LeaderboardSystem.AdvanceRivals(character); // rivals grind too



            if (newWeek == oldWeek)
                return new CareerDayResult(false, false,
                    character.career.weekDealsWon, character.career.weekDealsGoal, 0, 0);

            bool met = character.career.weekDealsWon >= character.career.weekDealsGoal;
            int dealsWon = character.career.weekDealsWon;
            int goal = character.career.weekDealsGoal;

            int rewardSp = 0, rewardXp = 0;
            if (met)
            {
                rewardSp = 1;
                rewardXp = 50;
                character.unspentSkillPoints += rewardSp;
                ProgressionSystem.AddXp(character, rewardXp);
            }

            character.career.weekDealsWon = 0;
            character.career.weekDealsGoal = GoalForWeek(newWeek);

            return new CareerDayResult(true, met, dealsWon, goal, rewardSp, rewardXp);
        }
    }
}
