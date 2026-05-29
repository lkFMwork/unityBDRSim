using System.Collections.Generic;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// A purchasable perk. Effects are expressed as plain numbers so they can be
    /// summed without any per-perk special-casing (see <c>PerkSystem.Aggregate</c>).
    /// </summary>
    public class PerkDefinition
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Description;
        public readonly int Cost; // skill points

        public readonly float TrustBonus;
        public readonly float PatienceDrainReduction; // subtracted from the drain multiplier
        public readonly float NegotiationSkill;
        public readonly float XpMultiplierBonus;       // added on top of 1.0
        public readonly float ScoreBonusAll;           // added to every positive scored line
        public readonly int ExtraCallsPerDay;

        public PerkDefinition(string id, string name, string description, int cost,
            float trustBonus = 0f, float patienceDrainReduction = 0f, float negotiationSkill = 0f,
            float xpMultiplierBonus = 0f, float scoreBonusAll = 0f, int extraCallsPerDay = 0)
        {
            Id = id;
            Name = name;
            Description = description;
            Cost = cost;
            TrustBonus = trustBonus;
            PatienceDrainReduction = patienceDrainReduction;
            NegotiationSkill = negotiationSkill;
            XpMultiplierBonus = xpMultiplierBonus;
            ScoreBonusAll = scoreBonusAll;
            ExtraCallsPerDay = extraCallsPerDay;
        }
    }

    /// <summary>Summed effect of all of a character's unlocked perks.</summary>
    public class PerkEffects
    {
        public float TrustBonus;
        public float PatienceDrainReduction;
        public float NegotiationSkill;
        public float ScoreBonusAll;
        public float XpMultiplier = 1f;
        public int ExtraCallsPerDay;
    }

    public static class PerkLibrary
    {
        public static readonly List<PerkDefinition> All = new()
        {
            new PerkDefinition("silver_tongue", "Silver Tongue",
                "+5% starting trust on every call.", 2, trustBonus: 0.05f),
            new PerkDefinition("thick_skin", "Thick Skin",
                "Patience drains 10% slower under pressure.", 2, patienceDrainReduction: 0.10f),
            new PerkDefinition("closers_instinct", "Closer's Instinct",
                "Hold roughly 3% higher rates before a prospect balks.", 2, negotiationSkill: 0.03f),
            new PerkDefinition("polished_pro", "Polished Pro",
                "+1 to every well-played line you score.", 2, scoreBonusAll: 1.0f),
            new PerkDefinition("fast_learner", "Fast Learner",
                "+25% XP from every call.", 3, xpMultiplierBonus: 0.25f),
            new PerkDefinition("workaholic", "Workaholic",
                "+2 calls available each day.", 3, extraCallsPerDay: 2),
            new PerkDefinition("rainmaker", "Rainmaker",
                "+3% starting trust and +3% rate headroom.", 4, trustBonus: 0.03f, negotiationSkill: 0.03f),
            new PerkDefinition("networker", "Networker",
                "+15% XP and +1 call per day.", 4, xpMultiplierBonus: 0.15f, extraCallsPerDay: 1),
        };

        public static PerkDefinition Get(string id)
        {
            foreach (var p in All)
                if (p.Id == id) return p;
            return null;
        }
    }
}
