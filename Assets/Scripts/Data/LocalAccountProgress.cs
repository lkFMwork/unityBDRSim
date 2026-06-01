using System;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>Per-city overworld save state: whether you've unlocked the city (beaten its commute)
    /// and whether you've cleared it on the SMW path. Meeting progress is tracked per company now —
    /// see <see cref="CompanyProgress"/>.</summary>
    [Serializable]
    public class LocalAccountProgress
    {
        public string clientId;
        public bool cityUnlocked = false; // beaten the commute platformer → fast-travel enabled
        public bool cleared = false;      // SMW: this city-level is cleared → opens the next on the path
    }
}
