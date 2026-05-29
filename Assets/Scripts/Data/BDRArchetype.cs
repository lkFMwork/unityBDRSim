using System.Collections.Generic;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// A starting template for a new BDR: a flavored background with a base
    /// attribute spread the player then customizes with bonus points. Defined in
    /// code (not assets) so the set is easy to read, tune, and unit-test.
    /// </summary>
    public class BDRArchetype
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Description;
        public readonly string Signature; // flavor / future perk hook
        public readonly BDRAttributes BaseAttributes;

        public BDRArchetype(string id, string name, string description, string signature,
            BDRAttributes baseAttributes)
        {
            Id = id;
            Name = name;
            Description = description;
            Signature = signature;
            BaseAttributes = baseAttributes;
        }
    }

    public static class ArchetypeLibrary
    {
        /// <summary>Bonus attribute points the player distributes on top of an archetype.</summary>
        public const int StartingBonusPoints = 6;

        public static readonly List<BDRArchetype> All = new()
        {
            new BDRArchetype(
                "closer", "The Closer",
                "A natural on the phone who lives for the handshake. Strong rapport and a spine in negotiation.",
                "Negotiation comes easy — you flinch less when defending a rate.",
                new BDRAttributes { charisma = 7, negotiation = 7, productKnowledge = 4, resilience = 5, prospecting = 4 }),

            new BDRArchetype(
                "hunter", "The Hunter",
                "Relentless prospector who eats rejection for breakfast. Fills the pipeline by sheer will.",
                "Thick skin — cold receptions wear you down more slowly.",
                new BDRAttributes { charisma = 5, negotiation = 4, productKnowledge = 4, resilience = 7, prospecting = 7 }),

            new BDRArchetype(
                "consultant", "The Consultant",
                "Knows freight cold and asks the questions that uncover real pain. Sells with insight.",
                "Deep product knowledge makes your pitch and objection handling land.",
                new BDRAttributes { charisma = 4, negotiation = 5, productKnowledge = 7, resilience = 4, prospecting = 7 }),

            new BDRArchetype(
                "natural", "The Natural",
                "No glaring weakness, no killer edge — a balanced rep who can grow in any direction.",
                "Well-rounded: a clean slate to build the BDR you want.",
                new BDRAttributes { charisma = 5, negotiation = 5, productKnowledge = 5, resilience = 5, prospecting = 5 }),
        };

        public static BDRArchetype Get(string id)
        {
            foreach (var a in All)
                if (a.Id == id) return a;
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
