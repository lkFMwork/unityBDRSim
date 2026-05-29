using System.Collections.Generic;
using Fitzmark.BDRSim.Data;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>
    /// Evaluates prestige badges against the character's lifetime stats. Like
    /// quests, progress is derived — call <see cref="Sync"/> after anything
    /// changes and it unlocks any newly-earned achievements.
    /// </summary>
    public static class AchievementSystem
    {
        public static bool IsUnlocked(BDRCharacter c, string id) =>
            c?.unlockedAchievements != null && c.unlockedAchievements.Contains(id);

        public static List<Achievement> Sync(BDRCharacter c)
        {
            var newly = new List<Achievement>();
            if (c == null) return newly;
            c.unlockedAchievements ??= new List<string>();

            foreach (var a in AchievementLibrary.All)
            {
                if (c.unlockedAchievements.Contains(a.Id)) continue;
                if (a.IsEarned != null && a.IsEarned(c))
                {
                    c.unlockedAchievements.Add(a.Id);
                    newly.Add(a);
                }
            }
            return newly;
        }

        public static string FlashFor(List<Achievement> achievements)
        {
            if (achievements == null || achievements.Count == 0) return null;
            var names = new List<string>();
            foreach (var a in achievements) names.Add(a.Name);
            return "Achievement unlocked: " + string.Join(", ", names) + "!";
        }
    }
}
