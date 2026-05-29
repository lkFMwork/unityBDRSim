namespace Fitzmark.BDRSim.Data
{
    /// <summary>The four conversational "attacks" in a gatekeeper duel.</summary>
    public enum GatekeeperMove
    {
        Charm,      // win the gatekeeper over personally   (Charisma)
        Logic,      // establish credibility / relevance     (Product Knowledge)
        Pressure,   // urgency, brevity, assumptive          (Negotiation)
        Curveball   // pattern interrupt to break the script (Prospecting)
    }

    /// <summary>The gatekeeper's current posture. Each is weak to one move and hardened against another.</summary>
    public enum GatekeeperStance
    {
        Suspicious, // sizing you up
        Busy,       // slammed, no time
        Bored,      // on autopilot
        Defensive   // actively guarding the boss
    }

    /// <summary>
    /// Static rules and flavor for gatekeeper duels: which move beats which stance,
    /// the "tells" the player reads, and the lines the rep delivers. Pure data so
    /// the duel logic stays testable. These mappings double as real coaching — they
    /// mirror how good reps actually read and handle gatekeepers.
    /// </summary>
    public static class GatekeeperCombatData
    {
        public static readonly GatekeeperMove[] Moves =
        {
            GatekeeperMove.Charm, GatekeeperMove.Logic, GatekeeperMove.Pressure, GatekeeperMove.Curveball
        };

        public static readonly GatekeeperStance[] Stances =
        {
            GatekeeperStance.Suspicious, GatekeeperStance.Busy,
            GatekeeperStance.Bored, GatekeeperStance.Defensive
        };

        public static GatekeeperMove Weakness(GatekeeperStance s) => s switch
        {
            GatekeeperStance.Suspicious => GatekeeperMove.Logic,     // prove you're legit
            GatekeeperStance.Busy => GatekeeperMove.Pressure,        // be brief and urgent
            GatekeeperStance.Bored => GatekeeperMove.Curveball,      // wake them up
            GatekeeperStance.Defensive => GatekeeperMove.Charm,      // make a friend
            _ => GatekeeperMove.Charm
        };

        public static GatekeeperMove Resist(GatekeeperStance s) => s switch
        {
            GatekeeperStance.Suspicious => GatekeeperMove.Curveball, // weird = more suspicious
            GatekeeperStance.Busy => GatekeeperMove.Logic,           // rambling = annoying
            GatekeeperStance.Bored => GatekeeperMove.Charm,          // heard every pitch
            GatekeeperStance.Defensive => GatekeeperMove.Pressure,   // pushy = wall goes up
            _ => GatekeeperMove.Pressure
        };

        public static AttributeType MoveAttribute(GatekeeperMove m) => m switch
        {
            GatekeeperMove.Charm => AttributeType.Charisma,
            GatekeeperMove.Logic => AttributeType.ProductKnowledge,
            GatekeeperMove.Pressure => AttributeType.Negotiation,
            GatekeeperMove.Curveball => AttributeType.Prospecting,
            _ => AttributeType.Charisma
        };

        public static string MoveName(GatekeeperMove m) => m switch
        {
            GatekeeperMove.Charm => "Charm",
            GatekeeperMove.Logic => "Credibility",
            GatekeeperMove.Pressure => "Urgency",
            GatekeeperMove.Curveball => "Curveball",
            _ => m.ToString()
        };

        public static string MoveLine(GatekeeperMove m) => m switch
        {
            GatekeeperMove.Charm =>
                "\"I bet you're the one who actually keeps that place running — help me out here?\"",
            GatekeeperMove.Logic =>
                "\"I run specific lanes in your region and think there's a real fit — worth two minutes of their time.\"",
            GatekeeperMove.Pressure =>
                "\"I'll be quick — one question, and if it's not relevant I'm gone.\"",
            GatekeeperMove.Curveball =>
                "\"Random question — is it still pouring over there? ...anyway, while I've got you—\"",
            _ => "..."
        };

        public static string StanceName(GatekeeperStance s) => s switch
        {
            GatekeeperStance.Suspicious => "Suspicious",
            GatekeeperStance.Busy => "Busy",
            GatekeeperStance.Bored => "Bored",
            GatekeeperStance.Defensive => "Defensive",
            _ => s.ToString()
        };

        /// <summary>A readable hint at the gatekeeper's posture — read it to pick the right move.</summary>
        public static string StanceTell(GatekeeperStance s) => s switch
        {
            GatekeeperStance.Suspicious =>
                "\"...who did you say you were with again?\"  — they're sizing you up. Earn credibility.",
            GatekeeperStance.Busy =>
                "\"I've got about ten seconds, what is this?\"  — they're slammed. Be brief and urgent.",
            GatekeeperStance.Bored =>
                "\"mm-hm... sure... uh-huh...\"  — they're on autopilot. Shake them awake.",
            GatekeeperStance.Defensive =>
                "\"They're not taking calls right now.\"  — they're guarding the boss. Win them over.",
            _ => "They wait."
        };
    }
}
