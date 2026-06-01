using System;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// Per-company relationship save state for in-person field visits. The first visit is cold;
    /// each won revisit advances a <see cref="stage"/>; the final close flips <see cref="managed"/>,
    /// turning them into a managed transportation customer (a real freight account on the desk).
    /// <see cref="lastMeetingDay"/> drives the cooldown between visits to the same company.
    /// </summary>
    [Serializable]
    public class CompanyProgress
    {
        public string companyId;
        public int stage = 1;             // 1 = cold intro; climbs with each won visit
        public int lastMeetingDay = -999; // never met
        public bool managed = false;      // relationship closed → managed transportation customer
    }
}
