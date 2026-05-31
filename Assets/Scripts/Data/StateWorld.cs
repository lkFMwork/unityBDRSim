using System.Collections.Generic;
using System.Linq;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// A WORLD in the Super Mario World sense — one US state. Its <see cref="Cities"/> are the
    /// level-nodes laid along a path (the branch office is one of them). You clear a city-level by
    /// beating its commute, which opens the next on the path; clearing the whole state opens the
    /// next state (in fixed company-wide <see cref="Difficulty"/> order, with your home state first).
    /// </summary>
    public class StateWorld
    {
        public readonly string Id;          // "texas"
        public readonly string Name;        // "Texas"
        public readonly string Abbrev;      // "TX"
        public readonly int Difficulty;     // fixed sequence rank (1 = company's easiest)
        public readonly float UsaX, UsaY;   // position on the national map (0..1, W→E / S→N)
        public readonly List<LocalClient> Cities; // level-nodes, in path order

        public StateWorld(string id, string name, string abbrev, int difficulty,
            float usaX, float usaY, List<LocalClient> cities)
        {
            Id = id; Name = name; Abbrev = abbrev; Difficulty = difficulty;
            UsaX = usaX; UsaY = usaY; Cities = cities;
        }
    }

    /// <summary>
    /// All worlds (states) and the company branch offices. The 12 FITZMARK branches map onto 9
    /// states; the state of the branch you pick at character creation becomes World 1.
    /// </summary>
    public static class WorldRegistry
    {
        // Helper to keep the city table terse. RequiredLevel is kept at 1 everywhere — the SMW
        // path is the only gate, so the player is never level-locked out of progression.
        private static LocalClient C(string id, string company, string city, string stateId,
            float x, float z, bool branch = false) =>
            new LocalClient(id, company, city, stateId, x, z, 1, branch);

        public static readonly List<StateWorld> All = new()
        {
            // --- World order is by Difficulty (1 = easiest). Home state is pulled to the front. ---

            new StateWorld("indiana", "Indiana", "IN", 1, 0.66f, 0.60f, new()
            {
                C("indianapolis", "Crossroads Freight Co.", "Indianapolis, IN", "indiana", 0f, 0f, branch: true),
                C("fort_wayne", "Summit City Logistics", "Fort Wayne, IN", "indiana", 8f, 12f),
                C("evansville", "River Bend Industrial", "Evansville, IN", "indiana", -4f, -18f),
            }),

            new StateWorld("nebraska", "Nebraska", "NE", 2, 0.46f, 0.56f, new()
            {
                C("omaha", "Cornhusker Freight", "Omaha, NE", "nebraska", 12f, 2f, branch: true),
                C("lincoln", "Capital Plains Supply", "Lincoln, NE", "nebraska", 6f, -3f),
                C("grand_island", "Platte Valley Distribution", "Grand Island, NE", "nebraska", -12f, 0f),
            }),

            new StateWorld("missouri", "Missouri", "MO", 3, 0.55f, 0.49f, new()
            {
                C("kansas_city", "Heartland Freight Co.", "Kansas City, MO", "missouri", -16f, 4f, branch: true),
                C("columbia_mo", "Mid-Missouri Logistics", "Columbia, MO", "missouri", -2f, 3f),
                C("springfield_mo", "Ozark Industrial Supply", "Springfield, MO", "missouri", -8f, -12f),
                C("st_louis", "Gateway Distribution", "St. Louis, MO", "missouri", 16f, 3f),
            }),

            new StateWorld("tennessee", "Tennessee", "TN", 4, 0.64f, 0.42f, new()
            {
                C("nashville", "Music City Logistics", "Nashville, TN", "tennessee", -2f, 4f, branch: true),
                C("jackson_tn", "West Tennessee Haulers", "Jackson, TN", "tennessee", -18f, 0f, branch: true),
                C("chattanooga", "Scenic City Freight", "Chattanooga, TN", "tennessee", 12f, -6f, branch: true),
                C("memphis", "Bluff City Distribution", "Memphis, TN", "tennessee", -30f, -2f),
                C("knoxville", "Smoky Mountain Supply", "Knoxville, TN", "tennessee", 22f, 2f),
            }),

            new StateWorld("alabama", "Alabama", "AL", 5, 0.68f, 0.30f, new()
            {
                C("birmingham", "Magic City Steel & Freight", "Birmingham, AL", "alabama", 0f, 4f, branch: true),
                C("huntsville", "Rocket City Components", "Huntsville, AL", "alabama", 2f, 16f),
                C("montgomery", "Capital Line Distribution", "Montgomery, AL", "alabama", 1f, -12f),
                C("mobile", "Port City Logistics", "Mobile, AL", "alabama", -2f, -28f),
            }),

            new StateWorld("georgia", "Georgia", "GA", 6, 0.72f, 0.31f, new()
            {
                C("atlanta", "Peachtree Distribution", "Atlanta, GA", "georgia", 0f, 6f, branch: true),
                C("gainesville_ga", "Lanier Freight Works", "Gainesville, GA", "georgia", 5f, 13f, branch: true),
                C("macon", "Central Georgia Mills", "Macon, GA", "georgia", 2f, -8f),
                C("savannah", "Coastal Empire Cargo", "Savannah, GA", "georgia", 18f, -16f),
            }),

            new StateWorld("texas", "Texas", "TX", 7, 0.42f, 0.20f, new()
            {
                // Fort Worth is the branch; the rest are the original Texas territory.
                C("fort_worth", "Stockyard Supply Co.", "Fort Worth, TX", "texas", 2f, 14f, branch: true),
                C("dallas", "Trinity Freight Foods", "Dallas, TX", "texas", 9f, 14f),
                C("waco", "Brazos Manufacturing", "Waco, TX", "texas", 6f, 4f),
                C("austin", "Hill Country Materials", "Austin, TX", "texas", 4f, -6f),
                C("san_antonio", "Alamo Distribution", "San Antonio, TX", "texas", 0f, -16f),
                C("houston", "Bayou City Components", "Houston, TX", "texas", 22f, -10f),
                C("corpus", "Gulf Coast Mills", "Corpus Christi, TX", "texas", 14f, -26f),
                C("lubbock", "Plains Industrial", "Lubbock, TX", "texas", -16f, 18f),
                C("amarillo", "Panhandle Products", "Amarillo, TX", "texas", -12f, 30f),
                C("el_paso", "Sun City Logistics Group", "El Paso, TX", "texas", -34f, 6f),
            }),

            new StateWorld("arizona", "Arizona", "AZ", 8, 0.18f, 0.34f, new()
            {
                C("scottsdale", "Sonoran Logistics Group", "Scottsdale, AZ", "arizona", 2f, 2f, branch: true),
                C("phoenix", "Valley Sun Freight", "Phoenix, AZ", "arizona", 0f, 0f),
                C("mesa", "Superstition Supply Co.", "Mesa, AZ", "arizona", 6f, -1f),
                C("tucson", "Old Pueblo Industrial", "Tucson, AZ", "arizona", 9f, -16f),
            }),

            new StateWorld("new_york", "New York", "NY", 9, 0.84f, 0.70f, new()
            {
                C("buffalo", "Queen City Freight", "Buffalo, NY", "new_york", -16f, 4f, branch: true),
                C("rochester", "Genesee Valley Supply", "Rochester, NY", "new_york", -6f, 7f),
                C("syracuse", "Salt City Industrial", "Syracuse, NY", "new_york", 4f, 6f),
                C("albany", "Empire Capital Cargo", "Albany, NY", "new_york", 16f, 4f),
            }),
        };

        public static StateWorld Get(string stateId) => All.FirstOrDefault(w => w.Id == stateId);

        /// <summary>The world a city-level belongs to.</summary>
        public static StateWorld WorldOf(string cityId) =>
            All.FirstOrDefault(w => w.Cities.Any(c => c.Id == cityId));

        /// <summary>Every branch-office city across all states (selectable at character creation).</summary>
        public static IReadOnlyList<LocalClient> Branches =>
            All.SelectMany(w => w.Cities).Where(c => c.IsBranch).ToList();

        /// <summary>
        /// The world play-order for a given home state: home first, then the rest by fixed
        /// company difficulty. This is the sequence the SMW path follows.
        /// </summary>
        public static IReadOnlyList<StateWorld> SequenceFor(string homeStateId)
        {
            var home = Get(homeStateId);
            var rest = All.Where(w => w.Id != homeStateId).OrderBy(w => w.Difficulty);
            var seq = new List<StateWorld>();
            if (home != null) seq.Add(home);
            seq.AddRange(rest);
            return seq;
        }
    }
}
