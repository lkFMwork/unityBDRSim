using System.Collections.Generic;
using Fitzmark.BDRSim.Data;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>
    /// Tracks quests against the character's lifetime stats. Progress is derived
    /// (no separate counters to drift), so all the game has to do is call
    /// <see cref="Sync"/> after anything changes; it completes any newly-finished
    /// quests, grants their rewards once, and returns them for a notification.
    /// </summary>
    public static class QuestSystem
    {
        /// <summary>Current value of an objective, read from the character.</summary>
        public static int ValueOf(BDRCharacter c, QuestObjectiveType type) => type switch
        {
            QuestObjectiveType.MakeCalls => c.callsMade,
            QuestObjectiveType.WinDeals => c.dealsWon,
            QuestObjectiveType.BeatGatekeepers => c.gatekeepersBeaten,
            QuestObjectiveType.MeetInPerson => c.inPersonMeetings,
            QuestObjectiveType.TalkToMentors => c.mentorTalks,
            QuestObjectiveType.EarnWeeklyMargin => (int)c.totalWeeklyMarginWon,
            QuestObjectiveType.ReachLevel => c.level,
            QuestObjectiveType.DeliverLoads => CharacterStats.LoadsDelivered(c),
            QuestObjectiveType.MoveTotalMargin => (int)c.lifetimeMargin,
            QuestObjectiveType.WinAccounts => CharacterStats.AccountsWon(c),
            QuestObjectiveType.ConvertLeads => CharacterStats.ConvertedLeads(c),
            QuestObjectiveType.BuyUpgrades => CharacterStats.UpgradeLevelsTotal(c),
            _ => 0
        };

        public static bool IsComplete(BDRCharacter c, QuestDefinition quest) =>
            c.completedQuests != null && c.completedQuests.Contains(quest.Id);

        public static bool AreObjectivesMet(BDRCharacter c, QuestDefinition quest)
        {
            foreach (var obj in quest.Objectives)
                if (ValueOf(c, obj.Type) < obj.Target) return false;
            return true;
        }

        /// <summary>0..1 progress for a single objective (for progress bars).</summary>
        public static float ObjectiveProgress(BDRCharacter c, QuestObjective obj)
        {
            if (obj.Target <= 0) return 1f;
            float p = (float)ValueOf(c, obj.Type) / obj.Target;
            return p < 0f ? 0f : (p > 1f ? 1f : p);
        }

        /// <summary>
        /// Completes any quests whose objectives are now met, grants rewards once,
        /// and returns the quests that were just finished.
        /// </summary>
        public static List<QuestDefinition> Sync(BDRCharacter c)
        {
            var newlyCompleted = new List<QuestDefinition>();
            if (c == null) return newlyCompleted;
            c.completedQuests ??= new List<string>();

            foreach (var quest in QuestLibrary.All)
            {
                if (c.completedQuests.Contains(quest.Id)) continue;
                if (!AreObjectivesMet(c, quest)) continue;

                c.completedQuests.Add(quest.Id);
                if (quest.RewardXp > 0) ProgressionSystem.AddXp(c, quest.RewardXp);
                if (quest.RewardSkillPoints > 0) c.unspentSkillPoints += quest.RewardSkillPoints;
                newlyCompleted.Add(quest);
            }
            return newlyCompleted;
        }

        /// <summary>A one-line notification for a set of freshly completed quests.</summary>
        public static string FlashFor(List<QuestDefinition> quests)
        {
            if (quests == null || quests.Count == 0) return null;
            var titles = new List<string>();
            foreach (var q in quests) titles.Add(q.Title);
            return "Quest complete: " + string.Join(", ", titles) + "!";
        }
    }
}
