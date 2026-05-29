using System;

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
        public string archetypeId = "natural";

        // Look + build
        public AvatarConfig avatar = new AvatarConfig();
        public BDRAttributes attributes = new BDRAttributes();

        // Progression
        public int level = 1;
        public int xp = 0;
        public int unspentSkillPoints = 0;

        // Career record (lifetime)
        public int callsMade = 0;
        public int dealsWon = 0;
        public int callsHungUp = 0;
        public float totalWeeklyMarginWon = 0f;

        public string DisplayName => $"{firstName} {lastName}".Trim();

        public float WinRate => callsMade > 0 ? (float)dealsWon / callsMade : 0f;

        public static BDRCharacter CreateDefault()
        {
            var archetype = ArchetypeLibrary.All[0];
            return new BDRCharacter
            {
                archetypeId = archetype.Id,
                attributes = archetype.BaseAttributes.Clone()
            };
        }
    }
}
