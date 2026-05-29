using System.Collections.Generic;
using Fitzmark.BDRSim.Data;
using UnityEngine;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>One carrier you could book a load with — a transient option, not saved.</summary>
    public struct CarrierOption
    {
        public string name;
        public float ratePerMile; // what they'll haul for
        public float reliability; // 0..1 chance they deliver clean
    }

    /// <summary>
    /// The freight market: what a lane pays today (with a seasonal swing), how high a
    /// shipper will let you quote before walking, and the carrier capacity you can
    /// buy from. Deterministic given a lane + day (and a load's carrier seed), so the
    /// numbers are stable across rebuilds and unit-testable.
    /// </summary>
    public static class FreightMarket
    {
        /// <summary>Share of gross margin that lands in the rep's pocket as commission.</summary>
        public const float CommissionRate = 0.30f;

        /// <summary>Market all-in linehaul rate for a lane on a given day ($/mile).</summary>
        public static float MarketRatePerMile(Lane lane, int day)
        {
            if (lane == null) return 2.50f;
            float baseRate = Mathf.Max(0.5f, lane.currentRatePerMile);
            float seasonal = 1f + 0.10f * Mathf.Sin(day * 0.20f + EqPhase(lane.equipment));
            float laneBias = 0.97f + 0.06f * Frac01(StableHash(lane.label) * 0.12873f);
            float volatility = lane.equipment == EquipmentType.Reefer ? 1.04f : 1f; // reefer runs hot
            return baseRate * seasonal * laneBias * volatility;
        }

        /// <summary>
        /// The most this shipper will pay before going elsewhere. Loyal, low-price-
        /// sensitivity accounts give you more headroom to quote high (= more margin).
        /// </summary>
        public static float ShipperMaxPerMile(float market, float priceSensitivity, float health)
        {
            float headroom = 0.06f + (1f - Mathf.Clamp01(priceSensitivity)) * 0.12f
                                   + Mathf.Clamp01(health) * 0.04f;
            return market * (1f + headroom);
        }

        /// <summary>Rough transit time so a load doesn't deliver the instant it's covered.</summary>
        public static int TransitDays(int miles) => Mathf.Max(1, Mathf.RoundToInt(miles / 550f));

        /// <summary>
        /// A shortlist of carriers for a load. Cheaper carriers are less reliable, so
        /// the cover step is a real risk/reward call: book cheap for fat margin and
        /// gamble on service, or pay up for a carrier that won't fall through.
        /// </summary>
        public static List<CarrierOption> CarrierShortlist(FreightLoad load, int count = 3)
        {
            var list = new List<CarrierOption>();
            if (load == null) return list;
            var rng = new System.Random(load.carrierSeed);
            float market = load.marketRatePerMile;
            for (int i = 0; i < count; i++)
            {
                float reliability = 0.62f + (float)rng.NextDouble() * 0.35f; // 0.62..0.97
                float t = Mathf.InverseLerp(0.62f, 0.97f, reliability);
                float ask = market * (0.80f + t * 0.16f) * (0.99f + (float)rng.NextDouble() * 0.04f);
                list.Add(new CarrierOption
                {
                    name = CarrierName(rng),
                    ratePerMile = ask,
                    reliability = reliability
                });
            }
            list.Sort((a, b) => a.ratePerMile.CompareTo(b.ratePerMile));
            return list;
        }

        public static string CarrierName(System.Random rng) =>
            $"{Prefixes[rng.Next(Prefixes.Length)]} {Suffixes[rng.Next(Suffixes.Length)]}";

        public static string Commodity(EquipmentType eq, System.Random rng)
        {
            string[] pool = eq switch
            {
                EquipmentType.Reefer => new[] { "produce", "frozen foods", "dairy", "beverages" },
                EquipmentType.Flatbed => new[] { "steel coils", "lumber", "machinery", "pipe" },
                EquipmentType.StepDeck => new[] { "equipment", "transformers", "tractors" },
                EquipmentType.Container => new[] { "import goods", "retail freight" },
                EquipmentType.BoxTruck => new[] { "parcel freight", "store deliveries" },
                _ => new[] { "palletized goods", "auto parts", "packaging", "consumer goods" }
            };
            return pool[rng.Next(pool.Length)];
        }

        // ---- original (trademark-free) naming pools -------------------------
        private static readonly string[] Prefixes =
        {
            "Lone Star", "Redline", "Ironwood", "Blue Prairie", "Summit", "Cardinal",
            "Vanguard", "Brushfire", "Granite", "Northbound", "Sandhill", "Copperline",
            "Big Sky", "Steel River", "Hightower", "Crosswind"
        };

        private static readonly string[] Suffixes =
        {
            "Freight", "Trucking", "Carriers", "Logistics", "Transport", "Haulage",
            "Lines", "Expedite"
        };

        private static float EqPhase(EquipmentType eq) => (int)eq * 0.9f;

        private static int StableHash(string s)
        {
            int h = 17;
            if (s != null) foreach (char c in s) h = unchecked(h * 31 + c);
            return h;
        }

        private static float Frac01(float v)
        {
            v = Mathf.Abs(v);
            return v - Mathf.Floor(v);
        }
    }
}
