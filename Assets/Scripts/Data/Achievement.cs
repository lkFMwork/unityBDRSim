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
            new Achievement("whale", "Whale Hunter", "Land a key account.",
                c => c.keyAccountsWon >= 1),

            // Freight desk / economy / outreach
            new Achievement("freight_boss", "Freight Boss", "Deliver 50 freight loads.",
                c => Delivered(c) >= 50),
            new Achievement("self_made", "Self-Made", "Bank $5,000 in commission.",
                c => c.cash >= 5000f),
            new Achievement("empire", "Empire", "Grow your book to 10 accounts.",
                c => c.accounts != null && c.accounts.Count >= 10),
            new Achievement("tycoon", "Logistics Tycoon", "Move $50,000 in lifetime gross margin.",
                c => c.lifetimeMargin >= 50000f),
            new Achievement("fully_loaded", "Fully Loaded", "Max out every business upgrade.",
                c => UpgradeLevels(c) >= 9),
            new Achievement("networker", "Networker", "Convert 10 warm leads into meetings.",
                c => Converted(c) >= 10),
        };

        private static int Delivered(BDRCharacter c)
        {
            int n = 0;
            if (c.accounts != null) foreach (var a in c.accounts) if (a != null) n += a.loadsDelivered;
            return n;
        }

        private static int Converted(BDRCharacter c)
        {
            int n = 0;
            if (c.leads != null) foreach (var l in c.leads) if (l != null && l.status == LeadStatus.Converted) n++;
            return n;
        }

        private static int UpgradeLevels(BDRCharacter c)
        {
            int n = 0;
            if (c.upgrades != null) foreach (var u in c.upgrades) if (u != null) n += u.level;
            return n;
        }
    }
}
