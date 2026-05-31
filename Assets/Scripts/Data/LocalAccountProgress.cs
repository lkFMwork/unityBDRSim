using System;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>Per-local-account save state: which meeting stage you're on, when you
    /// last met them (for the time-gap cooldown), and whether the account is closed.</summary>
    [Serializable]
    public class LocalAccountProgress
    {
        public string clientId;
        public int stage = 1;
        public int lastMeetingDay = -999; // never met
        public bool closed = false;
        public bool cityUnlocked = false; // beaten the commute platformer → fast-travel enabled
        public bool cleared = false;      // SMW: this city-level is cleared → opens the next on the path
    }
}
