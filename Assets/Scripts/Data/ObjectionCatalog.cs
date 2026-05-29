using System.Collections.Generic;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// Static reference content describing each objection: how the prospect
    /// phrases it and the coaching takeaway for handling it well. Used by the UI
    /// and the post-call report so the training value is visible to the rep.
    /// </summary>
    public static class ObjectionCatalog
    {
        public readonly struct Entry
        {
            public readonly string Title;
            public readonly string ProspectLine;
            public readonly string CoachingTip;

            public Entry(string title, string prospectLine, string coachingTip)
            {
                Title = title;
                ProspectLine = prospectLine;
                CoachingTip = coachingTip;
            }
        }

        private static readonly Dictionary<ObjectionType, Entry> Entries = new()
        {
            [ObjectionType.AlreadyHaveBroker] = new Entry(
                "Already have a broker",
                "We're already working with a broker, so we're all set.",
                "Don't attack the incumbent. Ask what's working and where they get exposed " +
                "on capacity, then position Fitzmark as a backstop on tough lanes."),

            [ObjectionType.RatesTooHigh] = new Entry(
                "Rates are too high",
                "Your number is higher than what we pay today.",
                "Re-anchor on total cost, not rate-per-mile: service failures, claims, and " +
                "reloads cost more than a few cents a mile. Defend your margin."),

            [ObjectionType.SendMeAnEmail] = new Entry(
                "Just send me an email",
                "Can you just email me something and I'll look at it?",
                "Email is where deals go to die. Trade the email for a specific next step: " +
                "\"I'll send it on a call Thursday so I can walk you through it.\""),

            [ObjectionType.WeGoDirectToCarriers] = new Entry(
                "We go direct to carriers",
                "We have our own carrier relationships, we don't use brokers.",
                "Acknowledge it, then sell surge/overflow coverage and the lanes their " +
                "direct carriers reject. You're additive, not a replacement."),

            [ObjectionType.NoTimeRightNow] = new Entry(
                "No time right now",
                "I'm slammed, this isn't a good time.",
                "Respect the time, shrink the ask: \"Give me 60 seconds, and if it's not " +
                "relevant I'll let you go.\" Then book a real time."),

            [ObjectionType.NotInterested] = new Entry(
                "Not interested",
                "We're good, not interested.",
                "\"Not interested\" usually means \"not yet convinced it's worth my time.\" " +
                "Ask one sharp, relevant question to earn 30 more seconds."),

            [ObjectionType.BadPastExperience] = new Entry(
                "Bad past experience",
                "We tried a broker before and got burned on a claim.",
                "Validate the frustration, get specifics, then show the process safeguard " +
                "(tracking, claims handling, escalation) that prevents a repeat."),

            [ObjectionType.NeedToCheckWithBoss] = new Entry(
                "Need to check with my boss",
                "I'd have to run this by my manager.",
                "Arm your champion: confirm the decision criteria and offer to join the " +
                "internal conversation so the pitch survives the hand-off."),
        };

        public static Entry Get(ObjectionType type) =>
            Entries.TryGetValue(type, out var entry)
                ? entry
                : new Entry(type.ToString(), "...", "No coaching content registered.");
    }
}
