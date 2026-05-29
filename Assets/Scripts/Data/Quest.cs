namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// What a quest objective tracks. Every type maps to an absolute value that
    /// can be read straight off the <see cref="BDRCharacter"/>, so quest progress
    /// needs no separate bookkeeping — just the set of completed quest ids.
    /// </summary>
    public enum QuestObjectiveType
    {
        MakeCalls,
        WinDeals,
        BeatGatekeepers,
        MeetInPerson,
        TalkToMentors,
        EarnWeeklyMargin,
        ReachLevel,
        DeliverLoads,
        MoveTotalMargin,
        WinAccounts,
        ConvertLeads,
        BuyUpgrades
    }

    public class QuestObjective
    {
        public readonly QuestObjectiveType Type;
        public readonly int Target;

        public QuestObjective(QuestObjectiveType type, int target)
        {
            Type = type;
            Target = target;
        }

        public string Describe() => Type switch
        {
            QuestObjectiveType.MakeCalls => $"Make {Target} call(s)",
            QuestObjectiveType.WinDeals => $"Win {Target} deal(s)",
            QuestObjectiveType.BeatGatekeepers => $"Beat {Target} gatekeeper(s)",
            QuestObjectiveType.MeetInPerson => $"Meet {Target} client(s) in person",
            QuestObjectiveType.TalkToMentors => $"Get advice {Target} time(s)",
            QuestObjectiveType.EarnWeeklyMargin => $"Bank ${Target} in weekly gross margin",
            QuestObjectiveType.ReachLevel => $"Reach level {Target}",
            QuestObjectiveType.DeliverLoads => $"Deliver {Target} freight load(s)",
            QuestObjectiveType.MoveTotalMargin => $"Move ${Target} in lifetime gross margin",
            QuestObjectiveType.WinAccounts => $"Win {Target} account(s)",
            QuestObjectiveType.ConvertLeads => $"Convert {Target} warm lead(s) to meetings",
            QuestObjectiveType.BuyUpgrades => $"Buy {Target} business upgrade(s)",
            _ => Type.ToString()
        };
    }

    public class QuestDefinition
    {
        public readonly string Id;
        public readonly string Title;
        public readonly string Description;
        public readonly QuestObjective[] Objectives;
        public readonly int RewardXp;
        public readonly int RewardSkillPoints;

        public QuestDefinition(string id, string title, string description,
            QuestObjective[] objectives, int rewardXp = 0, int rewardSkillPoints = 0)
        {
            Id = id;
            Title = title;
            Description = description;
            Objectives = objectives;
            RewardXp = rewardXp;
            RewardSkillPoints = rewardSkillPoints;
        }
    }

    public static class QuestLibrary
    {
        public static readonly System.Collections.Generic.List<QuestDefinition> All = new()
        {
            new QuestDefinition("first_call", "First Contact", "Get on the phone and make your first call.",
                new[] { new QuestObjective(QuestObjectiveType.MakeCalls, 1) }, rewardXp: 30),

            new QuestDefinition("first_deal", "Closer", "Earn your very first deal.",
                new[] { new QuestObjective(QuestObjectiveType.WinDeals, 1) }, rewardXp: 40, rewardSkillPoints: 1),

            new QuestDefinition("door_kicker", "Door Kicker", "Win the Gatekeeper Gauntlet twice.",
                new[] { new QuestObjective(QuestObjectiveType.BeatGatekeepers, 2) }, rewardXp: 50),

            new QuestDefinition("road_warrior", "Road Warrior", "Drive out and meet clients in person.",
                new[] { new QuestObjective(QuestObjectiveType.MeetInPerson, 3) }, rewardXp: 50),

            new QuestDefinition("mentored", "Mentored", "Soak up wisdom from the senior reps.",
                new[] { new QuestObjective(QuestObjectiveType.TalkToMentors, 3) }, rewardXp: 30, rewardSkillPoints: 1),

            new QuestDefinition("the_grind", "The Grind", "Put in the reps.",
                new[]
                {
                    new QuestObjective(QuestObjectiveType.MakeCalls, 8),
                    new QuestObjective(QuestObjectiveType.WinDeals, 3)
                }, rewardXp: 70, rewardSkillPoints: 1),

            new QuestDefinition("rainmaker", "Rainmaker", "Bank serious weekly gross margin.",
                new[] { new QuestObjective(QuestObjectiveType.EarnWeeklyMargin, 1500) }, rewardXp: 80, rewardSkillPoints: 1),

            new QuestDefinition("climbing", "Climbing the Ladder", "Earn a promotion.",
                new[] { new QuestObjective(QuestObjectiveType.ReachLevel, 3) }, rewardSkillPoints: 2),

            // Freight desk
            new QuestDefinition("first_load", "First Haul", "Deliver your first freight load for margin.",
                new[] { new QuestObjective(QuestObjectiveType.DeliverLoads, 1) }, rewardXp: 40),

            new QuestDefinition("logistics_engine", "Logistics Engine", "Keep the freight moving.",
                new[] { new QuestObjective(QuestObjectiveType.DeliverLoads, 15) }, rewardXp: 90, rewardSkillPoints: 1),

            new QuestDefinition("book_of_business", "Book of Business", "Build a real book of accounts.",
                new[] { new QuestObjective(QuestObjectiveType.WinAccounts, 5) }, rewardXp: 80, rewardSkillPoints: 1),

            new QuestDefinition("margin_mogul", "Margin Mogul", "Move serious lifetime gross margin.",
                new[] { new QuestObjective(QuestObjectiveType.MoveTotalMargin, 25000) }, rewardXp: 120, rewardSkillPoints: 2),

            // Outreach
            new QuestDefinition("cadence_master", "Cadence Master", "Turn cold outreach into warm meetings.",
                new[] { new QuestObjective(QuestObjectiveType.ConvertLeads, 3) }, rewardXp: 70),

            // Economy
            new QuestDefinition("invest_in_growth", "Invest in Growth", "Reinvest commission into the business.",
                new[] { new QuestObjective(QuestObjectiveType.BuyUpgrades, 3) }, rewardXp: 60, rewardSkillPoints: 1),
        };

        public static QuestDefinition Get(string id)
        {
            foreach (var q in All)
                if (q.Id == id) return q;
            return null;
        }
    }
}
