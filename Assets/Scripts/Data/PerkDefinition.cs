using System.Collections.Generic;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// A node in an ability tree. Effects are plain numbers so they sum without
    /// special-casing. A node may sit behind a prerequisite node (its tier in the
    /// tree) and may grant an active <see cref="AbilityDefinition"/>.
    /// </summary>
    public class PerkDefinition
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Description;
        public readonly int Cost; // skill points

        public readonly string Tree;
        public readonly int Tier;
        public readonly string RequiresPerkId;   // null = no prerequisite
        public readonly string GrantsAbilityId;  // null = passive only

        public readonly float TrustBonus;
        public readonly float PatienceDrainReduction;
        public readonly float NegotiationSkill;
        public readonly float XpMultiplierBonus;
        public readonly float ScoreBonusAll;
        public readonly int ExtraCallsPerDay;

        public PerkDefinition(string id, string name, string description, int cost,
            string tree, int tier, string requiresPerkId = null, string grantsAbilityId = null,
            float trustBonus = 0f, float patienceDrainReduction = 0f, float negotiationSkill = 0f,
            float xpMultiplierBonus = 0f, float scoreBonusAll = 0f, int extraCallsPerDay = 0)
        {
            Id = id; Name = name; Description = description; Cost = cost;
            Tree = tree; Tier = tier; RequiresPerkId = requiresPerkId; GrantsAbilityId = grantsAbilityId;
            TrustBonus = trustBonus; PatienceDrainReduction = patienceDrainReduction;
            NegotiationSkill = negotiationSkill; XpMultiplierBonus = xpMultiplierBonus;
            ScoreBonusAll = scoreBonusAll; ExtraCallsPerDay = extraCallsPerDay;
        }
    }

    /// <summary>Summed effect of a character's unlocked perks plus their style trait.</summary>
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
        public const string TreeRapport = "Rapport";
        public const string TreeDeals = "Deal-Making";
        public const string TreeHustle = "Hustle";

        public static readonly string[] Trees = { TreeRapport, TreeDeals, TreeHustle };

        public static readonly List<PerkDefinition> All = new()
        {
            // Rapport
            new PerkDefinition("rapport_1", "Silver Tongue", "+5% starting trust on every call.",
                2, TreeRapport, 1, trustBonus: 0.05f),
            new PerkDefinition("rapport_2", "Warm Opener", "+0.5 to every well-played line.",
                3, TreeRapport, 2, requiresPerkId: "rapport_1", scoreBonusAll: 0.5f),
            new PerkDefinition("rapport_3", "Trusted Voice", "+3% trust and unlock the Build Rapport ability.",
                4, TreeRapport, 3, requiresPerkId: "rapport_2", grantsAbilityId: "pep_talk",
                trustBonus: 0.03f),

            // Deal-Making
            new PerkDefinition("deal_1", "Closer's Instinct", "Hold ~3% higher rates before a balk.",
                2, TreeDeals, 1, negotiationSkill: 0.03f),
            new PerkDefinition("deal_2", "Anchor Master", "+3% headroom and unlock the Anchor High ability.",
                3, TreeDeals, 2, requiresPerkId: "deal_1", grantsAbilityId: "anchor",
                negotiationSkill: 0.03f),
            new PerkDefinition("deal_3", "Rate Defender", "+2% headroom and +0.5 to scored lines.",
                4, TreeDeals, 3, requiresPerkId: "deal_2", negotiationSkill: 0.02f, scoreBonusAll: 0.5f),

            // Hustle
            new PerkDefinition("hustle_1", "Thick Skin", "Patience drains 10% slower.",
                2, TreeHustle, 1, patienceDrainReduction: 0.10f),
            new PerkDefinition("hustle_2", "Workaholic", "+2 calls available each day.",
                3, TreeHustle, 2, requiresPerkId: "hustle_1", extraCallsPerDay: 2),
            new PerkDefinition("hustle_3", "Rainmaker", "+20% XP and unlock the Second Wind ability.",
                4, TreeHustle, 3, requiresPerkId: "hustle_2", grantsAbilityId: "second_wind",
                xpMultiplierBonus: 0.20f),
        };

        public static PerkDefinition Get(string id)
        {
            foreach (var p in All)
                if (p.Id == id) return p;
            return null;
        }

        /// <summary>Perks in a tree, ordered by tier.</summary>
        public static List<PerkDefinition> InTree(string tree)
        {
            var list = new List<PerkDefinition>();
            foreach (var p in All)
                if (p.Tree == tree) list.Add(p);
            list.Sort((a, b) => a.Tier.CompareTo(b.Tier));
            return list;
        }
    }
}
