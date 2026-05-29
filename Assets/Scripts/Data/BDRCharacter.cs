using System;
using System.Collections.Generic;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// The player's persistent BDR: identity, look, attributes, and career
    /// progress. This is the save payload — plain serializable fields only, so
    /// <c>JsonUtility</c> can round-trip it to disk.
    /// </summary>
    [Serializable]
    public class BDRCharacter
    {
        // Identity
        public string firstName = "New";
        public string lastName = "Rep";
        public string styleId = "closer";

        // Look + build
        public AvatarConfig avatar = new AvatarConfig();
        public BDRAttributes attributes = new BDRAttributes();

        // Progression
        public int level = 1;
        public int xp = 0;
        public int unspentSkillPoints = 0;
        public List<string> unlockedPerks = new List<string>();

        // Career meta-loop
        public CareerState career = new CareerState();

        // Career record (lifetime)
        public int callsMade = 0;
        public int dealsWon = 0;
        public int callsHungUp = 0;
        public float totalWeeklyMarginWon = 0f;
        public int gatekeepersBeaten = 0;
        public int mentorTalks = 0;
        public int inPersonMeetings = 0;

        // Quests
        public List<string> completedQuests = new List<string>();

        public string DisplayName => $"{firstName} {lastName}".Trim();

        public float WinRate => callsMade > 0 ? (float)dealsWon / callsMade : 0f;

        public static BDRCharacter CreateDefault()
        {
            return new BDRCharacter
            {
                styleId = SalesStyleLibrary.All[0].Id,
                attributes = PointBuy.NewBaseline()
            };
        }
    }
}
