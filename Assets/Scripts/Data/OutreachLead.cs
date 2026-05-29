using System;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>The channels you can touch a prospect through while warming them up.</summary>
    public enum OutreachChannel { Email, LinkedIn, Call, Video }

    /// <summary>Where a lead sits in the outreach funnel.</summary>
    public enum LeadStatus { New, Working, Warm, Converted, Dead }

    /// <summary>
    /// A prospect you're working through a multi-touch cadence on the national book.
    /// Varied touches raise <see cref="interest"/>; once warm it converts to a
    /// higher-trust meeting. Spamming one channel burns the lead. Serializable so the
    /// pipeline persists.
    /// </summary>
    [Serializable]
    public class OutreachLead
    {
        public string id = "";
        public string company = "";
        public string contact = "";
        public string title = "";

        public OutreachChannel preferredChannel = OutreachChannel.Email;
        public float interest = 0f;     // 0..1 warmth
        public int touches = 0;
        public int createdDay = 1;

        public bool touchedBefore = false;
        public OutreachChannel lastChannel = OutreachChannel.Email;
        public int sameChannelStreak = 0;

        public LeadStatus status = LeadStatus.New;
    }
}
