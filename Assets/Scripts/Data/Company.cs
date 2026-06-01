using System;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// One prospect company that lives inside a city. Every city holds a fixed book of these
    /// (see <see cref="CompanyRegistry.PerCity"/>), generated deterministically from the city id so
    /// the same names come back every session. A company is the unit you build a relationship with —
    /// cold on the first in-person visit, warmer on repeat visits, until it's a managed account.
    /// </summary>
    [Serializable]
    public class Company
    {
        public readonly string Id;       // "indianapolis-04"
        public readonly string Name;     // "Crossroads Manufacturing"
        public readonly string CityId;   // "indianapolis"
        public readonly string StateId;  // "indiana"
        public readonly string City;     // "Indianapolis, IN" (display)
        public readonly string Industry; // one of GeneratorData.Industries
        public readonly int Size;        // 1..5 — scales the freight you win once they're managed

        public Company(string id, string name, string cityId, string stateId,
            string city, string industry, int size)
        {
            Id = id; Name = name; CityId = cityId; StateId = stateId;
            City = city; Industry = industry; Size = size;
        }
    }
}
