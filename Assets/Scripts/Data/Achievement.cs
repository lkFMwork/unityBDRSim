using System;
using System.Collections.Generic;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// A one-time prestige badge. Earned condition is a predicate over the
    /// character's lifetime stats, so (like quests) there's nothing extra to track.
    /// </summary>
    public class Achievement
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Description;
        public readonly Func<BDRCharacter, bool> IsEarned;

        public Achievement(string id, string name, string description, Func<BDRCharacter, bool> isEarned)
        {
            Id = id;
            Name = name;
            Description = description;
            IsEarned = isEarned;
        }
    }

    public static class AchievementLibrary
    {
        public static readonly List<Achievement> All = new()
        {
            new Achievement("first_deal", "Sold!", "Win your first deal.",
                c => c.dealsWon >= 1),
            new Achievement("hot_hand", "Hot Hand", "Hit a 5-deal win streak.",
                c => c.bestWinStreak >= 5),
            new Achievement("ace", "Ace Caller", "Score an A on a call (90%+).",
                c => c.bestCallGradePercent >= 90),
            new Achievement("untouchable", "Untouchable", "Win a Gatekeeper Gauntlet without losing composure.",
                c => c.flawlessGatekeepers >= 1),
            new Achievement("road_warrior", "Road Warrior", "Meet 5 clients in person.",
                c => c.inPersonMeetings >= 5),
            new Achievement("centurion", "Centurion", "Make 25 calls.",
                c => c.callsMade >= 25),
            new Achievement("rainmaker", "Rainmaker", "Bank $3,000 in weekly gross margin.",
                c => c.totalWeeklyMarginWon >= 3000f),
            new Achievement("executive", "Executive Material", "Reach level 10.",
                c => c.level >= 10),
            new Achievement("specced_out", "Specced Out", "Unlock 5 ability-tree nodes.",
                c => c.unlockedPerks != null && c.unlockedPerks.Count >= 5),
            new Achievement("student", "Student of the Game", "Get advice from the team 5 times.",
                c => c.mentorTalks >= 5),
        };
    }
}
