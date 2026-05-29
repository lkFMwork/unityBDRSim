using System.Collections.Generic;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>What an active, in-call ability does when used.</summary>
    public enum AbilityEffectType
    {
        RestorePatience,   // the prospect relaxes (patience += magnitude)
        BoostTrust,        // build rapport on demand (trust += magnitude)
        ClearObjection,    // instantly handle the active objection
        NegotiationPrimer  // your next rate offer gets +magnitude headroom
    }

    /// <summary>
    /// An active ability the rep can fire during a call (like a D&D class ability):
    /// limited uses per call, an effect, and a magnitude. Granted by a Sales Style
    /// and/or unlocked from an ability tree.
    /// </summary>
    public class AbilityDefinition
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Description;
        public readonly AbilityEffectType EffectType;
        public readonly float Magnitude;
        public readonly int UsesPerCall;

        public AbilityDefinition(string id, string name, string description,
            AbilityEffectType effectType, float magnitude, int usesPerCall = 1)
        {
            Id = id;
            Name = name;
            Description = description;
            EffectType = effectType;
            Magnitude = magnitude;
            UsesPerCall = usesPerCall;
        }
    }

    public static class AbilityLibrary
    {
        public static readonly List<AbilityDefinition> All = new()
        {
            new AbilityDefinition("anchor", "Anchor High",
                "Your next rate offer gets ~5% more headroom before the prospect balks.",
                AbilityEffectType.NegotiationPrimer, 0.05f),
            new AbilityDefinition("second_wind", "Second Wind",
                "Read the room and ease off — restore a chunk of the prospect's patience.",
                AbilityEffectType.RestorePatience, 0.30f),
            new AbilityDefinition("reframe", "Reframe",
                "Flip the current objection on its head and handle it cleanly.",
                AbilityEffectType.ClearObjection, 0f),
            new AbilityDefinition("pep_talk", "Build Rapport",
                "Land a genuine, well-timed connection — a burst of trust.",
                AbilityEffectType.BoostTrust, 0.12f),
        };

        public static AbilityDefinition Get(string id)
        {
            foreach (var a in All)
                if (a.Id == id) return a;
            return null;
        }
    }
}
