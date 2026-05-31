using System.Collections.Generic;
using System.Linq;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// A city-level you travel to in person (via a commute platformer) and advance through
    /// meeting stages. Cities are grouped into <see cref="StateWorld"/>s (states) on the
    /// SMW-style overworld; one city per branch state is the company's branch office.
    /// MapX/MapZ are rough positions WITHIN that state's world map.
    /// </summary>
    public class LocalClient
    {
        public readonly string Id;
        public readonly string Company;
        public readonly string City;
        public readonly string StateId;     // the StateWorld this city belongs to
        public readonly float MapX;
        public readonly float MapZ;
        public readonly int RequiredLevel;
        public readonly bool IsBranch;       // a FITZMARK branch office (selectable at creation)

        public LocalClient(string id, string company, string city, string stateId,
            float mapX, float mapZ, int requiredLevel, bool isBranch = false)
        {
            Id = id;
            Company = company;
            City = city;
            StateId = stateId;
            MapX = mapX;
            MapZ = mapZ;
            RequiredLevel = requiredLevel;
            IsBranch = isBranch;
        }
    }

    /// <summary>
    /// Flat lookup over every city across every world. Kept as the single by-id registry the
    /// rest of the game uses (city scene, territory progression); the worlds themselves live in
    /// <see cref="WorldRegistry"/>.
    /// </summary>
    public static class TerritoryRegistry
    {
        public static IReadOnlyList<LocalClient> All =>
            WorldRegistry.All.SelectMany(w => w.Cities).ToList();

        public static LocalClient Get(string id) =>
            WorldRegistry.All.SelectMany(w => w.Cities).FirstOrDefault(c => c.Id == id);
    }
}
