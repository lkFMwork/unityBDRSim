using System;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// A single load tendered by a won account. It carries a snapshot of the lane
    /// (so it's self-contained on disk), the market rate and the shipper's ceiling
    /// at the time it posted, and — once you act on it — your sell rate, the carrier
    /// you bought at, and a schedule. Margin is the spread between your sell and your
    /// buy: <c>(quoted - carrier) × miles</c>.
    /// </summary>
    [Serializable]
    public class FreightLoad
    {
        public string id = "";
        public string accountId = "";
        public string accountCompany = "";

        // Lane snapshot
        public string origin = "";
        public string destination = "";
        public int miles = 250;
        public EquipmentType equipment = EquipmentType.DryVan;
        public FreightMode mode = FreightMode.FullTruckload;
        public string commodity = "general freight";

        // Economics ($/mile)
        public float marketRatePerMile;    // fair market for this lane the day it posted
        public float shipperMaxPerMile;    // the most this shipper will pay before walking
        public float quotedRatePerMile;    // your sell to the shipper
        public float carrierRatePerMile;   // your buy from the carrier

        // Carrier (set on cover)
        public string carrierName = "";
        public float carrierReliability;
        public int carrierSeed;            // deterministic carrier shortlist for this load

        // Schedule (in career days)
        public int postedDay;
        public int expiresDay;             // quote before this or the tender is lost
        public int deliveryDay;            // set when covered

        public LoadStatus status = LoadStatus.Offered;
        public bool resolved;              // delivered / lost / fell-through accounted for
        public string note = "";

        /// <summary>Gross margin per mile at the booked rates.</summary>
        public float MarginPerMile => quotedRatePerMile - carrierRatePerMile;

        /// <summary>Total gross margin for the load at the booked rates.</summary>
        public float TotalMargin => MarginPerMile * miles;

        /// <summary>What the shipper pays you for the load at your quoted rate.</summary>
        public float ShipperTotal => quotedRatePerMile * miles;

        public string LaneLabel => $"{origin} → {destination}";
    }
}
