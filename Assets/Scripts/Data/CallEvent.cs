using System.Collections.Generic;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// A random "situation" that can color the start of a call — a warm referral,
    /// a bad connection, etc. Applied as a one-shot trust/patience swing with a
    /// line of narration.
    /// </summary>
    public class CallEvent
    {
        public readonly string Title;
        public readonly string Narration;
        public readonly float TrustDelta;
        public readonly float PatienceDelta;
        public readonly bool Good;

        public CallEvent(string title, string narration, float trustDelta, float patienceDelta, bool good)
        {
            Title = title;
            Narration = narration;
            TrustDelta = trustDelta;
            PatienceDelta = patienceDelta;
            Good = good;
        }
    }

    public static class CallEventLibrary
    {
        public static readonly List<CallEvent> All = new()
        {
            new CallEvent("Warm Lead", "A mutual contact put in a good word for you.", 0.12f, 0f, true),
            new CallEvent("Good Timing", "You caught them relaxed, just after a win.", 0f, 0.15f, true),
            new CallEvent("Budget Approved", "Their quarterly freight budget just cleared.", 0.10f, 0.05f, true),
            new CallEvent("Bad Connection", "The line keeps cutting in and out.", 0f, -0.12f, false),
            new CallEvent("Rough Morning", "They just got off a brutal call.", -0.05f, -0.12f, false),
            new CallEvent("Burned Before", "They got stung by a broker last year — and they remember.", -0.12f, 0f, false),
        };

        /// <summary>Returns a random event with the given probability, otherwise null.</summary>
        public static CallEvent Roll(System.Random rng, float chance)
        {
            if (rng == null || All.Count == 0) return null;
            if (rng.NextDouble() > chance) return null;
            return All[rng.Next(All.Count)];
        }
    }
}
