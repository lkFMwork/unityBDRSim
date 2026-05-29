using Fitzmark.BDRSim.Data;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>Result of applying a call's outcome to a character's progression.</summary>
    public readonly struct ProgressionResult
    {
        public readonly int XpGained;
        public readonly bool LeveledUp;
        public readonly int NewLevel;
        public readonly int SkillPointsGained;

        public ProgressionResult(int xpGained, bool leveledUp, int newLevel, int skillPointsGained)
        {
            XpGained = xpGained;
            LeveledUp = leveledUp;
            NewLevel = newLevel;
            SkillPointsGained = skillPointsGained;
        }
    }

    /// <summary>
    /// XP, leveling, skill points, and career rank. Pure C# and deterministic so
    /// the curve is easy to tune and unit-test.
    /// </summary>
    public static class ProgressionSystem
    {
        public const int SkillPointsPerLevel = 2;

        /// <summary>Total cumulative XP required to have reached a given level.</summary>
        public static int TotalXpForLevel(int level)
        {
            if (level <= 1) return 0;
            // Each level n costs 100 + (n-2)*50; sum the costs up to `level`.
            int total = 0;
            for (int n = 2; n <= level; n++)
                total += 100 + (n - 2) * 50;
            return total;
        }

        /// <summary>XP still needed to advance from the current level to the next.</summary>
        public static int XpForNextLevel(int level) => TotalXpForLevel(level + 1) - TotalXpForLevel(level);

        /// <summary>The level a given total XP corresponds to (>= 1).</summary>
        public static int LevelForXp(int xp)
        {
            int level = 1;
            while (xp >= TotalXpForLevel(level + 1))
                level++;
            return level;
        }

        /// <summary>0..1 progress through the current level for an XP total.</summary>
        public static float LevelProgress(int xp)
        {
            int level = LevelForXp(xp);
            int floor = TotalXpForLevel(level);
            int span = XpForNextLevel(level);
            return span > 0 ? (float)(xp - floor) / span : 0f;
        }

        /// <summary>XP awarded for a completed call, from its grade and outcome.</summary>
        public static int XpForCall(CallReport report)
        {
            if (report == null) return 0;
            int gradeXp = UnityEngine.Mathf.RoundToInt(report.OverallPercent * 100f);
            int outcomeXp = report.Outcome switch
            {
                CallOutcome.WonCommitment => 60,
                CallOutcome.WonTrial => 35,
                CallOutcome.NoSaleFollowUp => 12,
                CallOutcome.Rejected => 5,
                CallOutcome.HungUp => 0,
                _ => 0
            };
            return gradeXp + outcomeXp;
        }

        /// <summary>
        /// Records a finished call on the character (career stats + XP) and returns
        /// what changed. Mutates the passed character.
        /// </summary>
        public static ProgressionResult ApplyCall(BDRCharacter character, CallReport report,
            CallSession session)
        {
            if (character == null || report == null)
                return new ProgressionResult(0, false, character?.level ?? 1, 0);

            bool won = report.Outcome == CallOutcome.WonCommitment || report.Outcome == CallOutcome.WonTrial;
            character.callsMade++;
            if (won)
            {
                character.dealsWon++;
                character.currentWinStreak++;
                if (character.currentWinStreak > character.bestWinStreak)
                    character.bestWinStreak = character.currentWinStreak;
                if (session != null && session.Prospect.DealAgreed && session.Lane != null)
                    character.totalWeeklyMarginWon +=
                        session.Lane.WeeklyMargin(session.Prospect.AgreedRatePerMile);
            }
            else
            {
                character.currentWinStreak = 0;
            }
            if (report.Outcome == CallOutcome.HungUp)
                character.callsHungUp++;

            int gradePct = UnityEngine.Mathf.RoundToInt(report.OverallPercent * 100f);
            if (gradePct > character.bestCallGradePercent)
                character.bestCallGradePercent = gradePct;

            float xpMultiplier = PerkSystem.Aggregate(character).XpMultiplier;
            int xpGained = UnityEngine.Mathf.RoundToInt(XpForCall(report) * xpMultiplier);
            return AddXp(character, xpGained);
        }

        /// <summary>Add XP and apply any resulting level-ups and skill points.</summary>
        public static ProgressionResult AddXp(BDRCharacter character, int amount)
        {
            if (character == null) return new ProgressionResult(0, false, 1, 0);

            int oldLevel = character.level;
            character.xp += amount;
            int newLevel = LevelForXp(character.xp);

            bool leveled = newLevel > oldLevel;
            int skillPoints = 0;
            if (leveled)
            {
                skillPoints = (newLevel - oldLevel) * SkillPointsPerLevel;
                character.level = newLevel;
                character.unspentSkillPoints += skillPoints;
            }
            return new ProgressionResult(amount, leveled, character.level, skillPoints);
        }

        /// <summary>Career title for a level.</summary>
        public static string RankTitle(int level)
        {
            if (level >= 15) return "Sales Manager";
            if (level >= 10) return "Account Executive";
            if (level >= 5) return "Senior BDR";
            return "BDR";
        }
    }
}
