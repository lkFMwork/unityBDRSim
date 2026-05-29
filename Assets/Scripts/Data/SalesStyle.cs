using System.Collections.Generic;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// A "race" for a BDR: a sales style that grants stat modifiers, a passive
    /// trait (numeric effects that aggregate alongside perks), and a starting
    /// active ability. Chosen at character creation.
    /// </summary>
    public class SalesStyle
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Description;

        // Stat modifiers applied on top of point-bought scores.
        public readonly int Charisma;
        public readonly int Negotiation;
        public readonly int ProductKnowledge;
        public readonly int Resilience;
        public readonly int Prospecting;

        // Passive trait (same effect shape as perks, summed in PerkSystem.Aggregate).
        public readonly string TraitName;
        public readonly string TraitDescription;
        public readonly float TrustBonus;
        public readonly float PatienceDrainReduction;
        public readonly float NegotiationSkill;
        public readonly float XpMultiplierBonus;
        public readonly float ScoreBonusAll;
        public readonly int ExtraCallsPerDay;

        // Starting active ability id (see AbilityLibrary).
        public readonly string GrantedAbilityId;

        public SalesStyle(string id, string name, string description,
            int cha, int neg, int pk, int res, int pro,
            string traitName, string traitDescription, string grantedAbilityId,
            float trustBonus = 0f, float patienceDrainReduction = 0f, float negotiationSkill = 0f,
            float xpMultiplierBonus = 0f, float scoreBonusAll = 0f, int extraCallsPerDay = 0)
        {
            Id = id; Name = name; Description = description;
            Charisma = cha; Negotiation = neg; ProductKnowledge = pk; Resilience = res; Prospecting = pro;
            TraitName = traitName; TraitDescription = traitDescription; GrantedAbilityId = grantedAbilityId;
            TrustBonus = trustBonus; PatienceDrainReduction = patienceDrainReduction;
            NegotiationSkill = negotiationSkill; XpMultiplierBonus = xpMultiplierBonus;
            ScoreBonusAll = scoreBonusAll; ExtraCallsPerDay = extraCallsPerDay;
        }

        public int Mod(AttributeType type) => type switch
        {
            AttributeType.Charisma => Charisma,
            AttributeType.Negotiation => Negotiation,
            AttributeType.ProductKnowledge => ProductKnowledge,
            AttributeType.Resilience => Resilience,
            AttributeType.Prospecting => Prospecting,
            _ => 0
        };
    }

    public static class SalesStyleLibrary
    {
        public static readonly List<SalesStyle> All = new()
        {
            new SalesStyle("closer", "The Closer",
                "Lives for the handshake. Reads buying signals and isn't afraid to ask for the order.",
                cha: 1, neg: 2, pk: 0, res: 0, pro: -1,
                "Always Be Closing", "Holds a little more margin on every deal.",
                grantedAbilityId: "anchor", negotiationSkill: 0.02f),

            new SalesStyle("hunter", "The Hunter",
                "Relentless prospector who eats rejection for breakfast and keeps dialing.",
                cha: 0, neg: -1, pk: 0, res: 2, pro: 1,
                "Relentless", "Squeezes an extra call out of every day.",
                grantedAbilityId: "second_wind", extraCallsPerDay: 1),

            new SalesStyle("farmer", "The Farmer",
                "Patient relationship builder who grows accounts over time and earns deep trust.",
                cha: 2, neg: -1, pk: 1, res: 0, pro: -1,
                "Trusted Advisor", "Prospects warm up to you faster.",
                grantedAbilityId: "pep_talk", trustBonus: 0.05f),

            new SalesStyle("challenger", "The Challenger",
                "Teaches the prospect something new and reframes the problem on their terms.",
                cha: -1, neg: 1, pk: 2, res: 0, pro: 0,
                "Reframe", "Sharper handling of pushback.",
                grantedAbilityId: "reframe", scoreBonusAll: 0.5f),

            new SalesStyle("networker", "The Networker",
                "Works the room and the referral. Every conversation seeds the next.",
                cha: 1, neg: 0, pk: -1, res: 0, pro: 2,
                "Warm Intros", "Learns faster from every call.",
                grantedAbilityId: "pep_talk", xpMultiplierBonus: 0.10f),

            new SalesStyle("generalist", "The Generalist",
                "No glaring weakness, no killer edge — adaptable and steady across the board.",
                cha: 1, neg: 1, pk: 1, res: 0, pro: 0,
                "Adaptable", "A small bonus to everything you do well.",
                grantedAbilityId: "pep_talk", scoreBonusAll: 0.3f),
        };

        public static SalesStyle Get(string id)
        {
            foreach (var s in All)
                if (s.Id == id) return s;
            return All[0];
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i].Id == id) return i;
            return 0;
        }
    }
}
