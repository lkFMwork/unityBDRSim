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
    /// states; the state of the branch you pick at character creation becomes World 1. Each state
    /// carries its seven largest cities by population (the branch first, then the rest), and each of
    /// those cities holds a full book of prospect companies (see <see cref="CompanyRegistry"/>).
    /// </summary>
    public static class WorldRegistry
    {
        // Helper to keep the city table terse. RequiredLevel is kept at 1 everywhere — the SMW
        // path is the only gate, so the player is never level-locked out of progression. x/z are
        // real (longitude, latitude); the overworld normalizes them per-state, so only the relative
        // positions matter and the map reads geographically.
        private static LocalClient C(string id, string company, string city, string stateId,
            float x, float z, bool branch = false) =>
            new LocalClient(id, company, city, stateId, x, z, 1, branch);

        public static readonly List<StateWorld> All = new()
        {
            // --- World order is by Difficulty (1 = easiest). Home state is pulled to the front.
            // Each state lists its 7 largest cities, branch first, then by population. ---

            new StateWorld("indiana", "Indiana", "IN", 1, 0.66f, 0.60f, new()
            {
                C("indianapolis", "Crossroads Freight Co.", "Indianapolis, IN", "indiana", -86.15f, 39.77f, branch: true),
                C("fort_wayne", "Summit City Logistics", "Fort Wayne, IN", "indiana", -85.13f, 41.08f),
                C("evansville", "River Bend Industrial", "Evansville, IN", "indiana", -87.57f, 37.97f),
                C("south_bend", "Bend Manufacturing", "South Bend, IN", "indiana", -86.25f, 41.68f),
                C("carmel", "Monon Distribution", "Carmel, IN", "indiana", -86.12f, 39.98f),
                C("fishers", "Geist Components", "Fishers, IN", "indiana", -86.01f, 39.96f),
                C("bloomington_in", "Limestone Supply Co.", "Bloomington, IN", "indiana", -86.53f, 39.17f),
            }),

            new StateWorld("nebraska", "Nebraska", "NE", 2, 0.46f, 0.56f, new()
            {
                C("omaha", "Cornhusker Freight", "Omaha, NE", "nebraska", -95.94f, 41.26f, branch: true),
                C("lincoln", "Capital Plains Supply", "Lincoln, NE", "nebraska", -96.67f, 40.81f),
                C("bellevue", "Offutt Industrial", "Bellevue, NE", "nebraska", -95.91f, 41.14f),
                C("grand_island", "Platte Valley Distribution", "Grand Island, NE", "nebraska", -98.34f, 40.92f),
                C("kearney", "Archway Materials", "Kearney, NE", "nebraska", -99.08f, 40.70f),
                C("fremont_ne", "Dodge County Mills", "Fremont, NE", "nebraska", -96.50f, 41.43f),
                C("hastings_ne", "Prairie Lake Products", "Hastings, NE", "nebraska", -98.39f, 40.59f),
            }),

            new StateWorld("missouri", "Missouri", "MO", 3, 0.55f, 0.49f, new()
            {
                C("kansas_city", "Heartland Freight Co.", "Kansas City, MO", "missouri", -94.58f, 39.10f, branch: true),
                C("st_louis", "Gateway Distribution", "St. Louis, MO", "missouri", -90.20f, 38.63f),
                C("springfield_mo", "Ozark Industrial Supply", "Springfield, MO", "missouri", -93.29f, 37.21f),
                C("columbia_mo", "Mid-Missouri Logistics", "Columbia, MO", "missouri", -92.33f, 38.95f),
                C("independence_mo", "Truman Components", "Independence, MO", "missouri", -94.42f, 39.09f),
                C("lees_summit", "Summit Trace Materials", "Lee's Summit, MO", "missouri", -94.38f, 38.91f),
                C("st_joseph", "Pony Express Freight", "St. Joseph, MO", "missouri", -94.85f, 39.77f),
            }),

            new StateWorld("tennessee", "Tennessee", "TN", 4, 0.64f, 0.42f, new()
            {
                C("nashville", "Music City Logistics", "Nashville, TN", "tennessee", -86.78f, 36.16f, branch: true),
                C("memphis", "Bluff City Distribution", "Memphis, TN", "tennessee", -90.05f, 35.15f),
                C("knoxville", "Smoky Mountain Supply", "Knoxville, TN", "tennessee", -83.92f, 35.96f),
                C("chattanooga", "Scenic City Freight", "Chattanooga, TN", "tennessee", -85.31f, 35.05f, branch: true),
                C("clarksville", "Cumberland Components", "Clarksville, TN", "tennessee", -87.36f, 36.53f),
                C("murfreesboro", "Stones River Mills", "Murfreesboro, TN", "tennessee", -86.39f, 35.85f),
                C("jackson_tn", "West Tennessee Haulers", "Jackson, TN", "tennessee", -88.81f, 35.61f, branch: true),
            }),

            new StateWorld("alabama", "Alabama", "AL", 5, 0.68f, 0.30f, new()
            {
                C("birmingham", "Magic City Steel & Freight", "Birmingham, AL", "alabama", -86.80f, 33.52f, branch: true),
                C("huntsville", "Rocket City Components", "Huntsville, AL", "alabama", -86.59f, 34.73f),
                C("montgomery", "Capital Line Distribution", "Montgomery, AL", "alabama", -86.30f, 32.37f),
                C("mobile", "Port City Logistics", "Mobile, AL", "alabama", -88.04f, 30.69f),
                C("tuscaloosa", "Druid City Materials", "Tuscaloosa, AL", "alabama", -87.57f, 33.21f),
                C("hoover_al", "Riverchase Distribution", "Hoover, AL", "alabama", -86.81f, 33.41f),
                C("auburn_al", "Plainsman Industrial", "Auburn, AL", "alabama", -85.48f, 32.61f),
            }),

            new StateWorld("georgia", "Georgia", "GA", 6, 0.72f, 0.31f, new()
            {
                C("atlanta", "Peachtree Distribution", "Atlanta, GA", "georgia", -84.39f, 33.75f, branch: true),
                C("augusta_ga", "Savannah River Supply", "Augusta, GA", "georgia", -82.00f, 33.47f),
                C("columbus_ga", "Chattahoochee Components", "Columbus, GA", "georgia", -84.99f, 32.46f),
                C("macon", "Central Georgia Mills", "Macon, GA", "georgia", -83.63f, 32.84f),
                C("savannah", "Coastal Empire Cargo", "Savannah, GA", "georgia", -81.10f, 32.08f),
                C("athens_ga", "Classic City Materials", "Athens, GA", "georgia", -83.38f, 33.96f),
                C("gainesville_ga", "Lanier Freight Works", "Gainesville, GA", "georgia", -83.82f, 34.30f, branch: true),
            }),

            new StateWorld("texas", "Texas", "TX", 7, 0.42f, 0.20f, new()
            {
                // Fort Worth is the branch; the rest are Texas's largest metros by population.
                C("fort_worth", "Stockyard Supply Co.", "Fort Worth, TX", "texas", -97.33f, 32.76f, branch: true),
                C("houston", "Bayou City Components", "Houston, TX", "texas", -95.37f, 29.76f),
                C("san_antonio", "Alamo Distribution", "San Antonio, TX", "texas", -98.49f, 29.42f),
                C("dallas", "Trinity Freight Foods", "Dallas, TX", "texas", -96.80f, 32.78f),
                C("austin", "Hill Country Materials", "Austin, TX", "texas", -97.74f, 30.27f),
                C("el_paso", "Sun City Logistics Group", "El Paso, TX", "texas", -106.49f, 31.76f),
                C("arlington", "Mid-Cities Industrial", "Arlington, TX", "texas", -97.11f, 32.74f),
            }),

            new StateWorld("arizona", "Arizona", "AZ", 8, 0.18f, 0.34f, new()
            {
                C("scottsdale", "Sonoran Logistics Group", "Scottsdale, AZ", "arizona", -111.93f, 33.49f, branch: true),
                C("phoenix", "Valley Sun Freight", "Phoenix, AZ", "arizona", -112.07f, 33.45f),
                C("tucson", "Old Pueblo Industrial", "Tucson, AZ", "arizona", -110.97f, 32.22f),
                C("mesa", "Superstition Supply Co.", "Mesa, AZ", "arizona", -111.83f, 33.42f),
                C("chandler", "Ocotillo Components", "Chandler, AZ", "arizona", -111.84f, 33.31f),
                C("glendale_az", "Cardinal Distribution", "Glendale, AZ", "arizona", -112.19f, 33.54f),
                C("gilbert_az", "Heritage Materials", "Gilbert, AZ", "arizona", -111.79f, 33.35f),
            }),

            new StateWorld("new_york", "New York", "NY", 9, 0.84f, 0.70f, new()
            {
                C("buffalo", "Queen City Freight", "Buffalo, NY", "new_york", -78.88f, 42.89f, branch: true),
                C("new_york_city", "Empire Harbor Distribution", "New York, NY", "new_york", -74.01f, 40.71f),
                C("yonkers", "Hudson Line Logistics", "Yonkers, NY", "new_york", -73.90f, 40.93f),
                C("rochester", "Genesee Valley Supply", "Rochester, NY", "new_york", -77.61f, 43.16f),
                C("syracuse", "Salt City Industrial", "Syracuse, NY", "new_york", -76.15f, 43.05f),
                C("albany", "Empire Capital Cargo", "Albany, NY", "new_york", -73.76f, 42.65f),
                C("new_rochelle", "Sound Shore Components", "New Rochelle, NY", "new_york", -73.78f, 40.91f),
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
