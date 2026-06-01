using System.Collections.Generic;
using System.Linq;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// The national book of prospect companies. Every city carries exactly <see cref="PerCity"/>
    /// companies, generated deterministically from the city id (stable names across sessions) out of
    /// the shared <see cref="GeneratorData"/> name pools. Built once, lazily, and cached.
    /// </summary>
    public static class CompanyRegistry
    {
        public const int PerCity = 15;

        private static Dictionary<string, List<Company>> _byCity;

        private static void EnsureBuilt()
        {
            if (_byCity != null) return;
            _byCity = new Dictionary<string, List<Company>>();
            foreach (var state in WorldRegistry.All)
                foreach (var city in state.Cities)
                    _byCity[city.Id] = Build(city);
        }

        private static List<Company> Build(LocalClient city)
        {
            var list = new List<Company>(PerCity);
            var rng = new System.Random(StableHash(city.Id));   // same seed -> same 15 names every run
            var used = new HashSet<string>();
            int index = 0, guard = 0;
            while (list.Count < PerCity && guard++ < PerCity * 40)
            {
                // ~30% of a city's book carries a local flavor (the city's own name as the root);
                // the rest draw from the shared company roots, all paired with a business suffix.
                string root = rng.Next(100) < 30
                    ? CityRoot(city)
                    : GeneratorData.CompanyRoots[rng.Next(GeneratorData.CompanyRoots.Length)];
                string suffix = GeneratorData.CompanySuffixes[rng.Next(GeneratorData.CompanySuffixes.Length)];
                string name = $"{root} {suffix}";
                if (!used.Add(name)) continue;

                string industry = GeneratorData.Industries[rng.Next(GeneratorData.Industries.Length)];
                int size = 1 + rng.Next(5); // 1..5
                list.Add(new Company($"{city.Id}-{index:00}", name, city.Id, city.StateId,
                    city.City, industry, size));
                index++;
            }
            return list;
        }

        // A root drawn from the city's own name, e.g. "Indianapolis, IN" -> "Indianapolis".
        private static string CityRoot(LocalClient city)
        {
            string n = city.City ?? city.Id;
            int comma = n.IndexOf(',');
            return comma > 0 ? n.Substring(0, comma) : n;
        }

        // FNV-ish stable hash — independent of runtime string.GetHashCode randomization.
        private static int StableHash(string s)
        {
            unchecked
            {
                int h = 23;
                foreach (char c in s) h = h * 31 + c;
                return h;
            }
        }

        /// <summary>The fixed book of companies in a city (empty for an unknown city id).</summary>
        public static IReadOnlyList<Company> ForCity(string cityId)
        {
            EnsureBuilt();
            return _byCity.TryGetValue(cityId, out var l)
                ? l
                : (IReadOnlyList<Company>)System.Array.Empty<Company>();
        }

        /// <summary>Every company in the country — the nationwide cold-call book.</summary>
        public static IEnumerable<Company> All
        {
            get { EnsureBuilt(); return _byCity.Values.SelectMany(v => v); }
        }

        public static Company Get(string companyId)
        {
            EnsureBuilt();
            foreach (var list in _byCity.Values)
                foreach (var c in list)
                    if (c.Id == companyId) return c;
            return null;
        }
    }
}
