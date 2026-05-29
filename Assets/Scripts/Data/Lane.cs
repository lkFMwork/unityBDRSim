using System;
using UnityEngine;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// A single freight lane in a prospect's network: an origin/destination pair
    /// moved at some cadence. Holds both what the shipper pays today (the
    /// incumbent rate) and what it costs Fitzmark to cover the load, which is the
    /// basis for margin during rate negotiation.
    /// </summary>
    [Serializable]
    public class Lane
    {
        [Tooltip("Display name, e.g. \"Indianapolis, IN -> Detroit, MI\".")]
        public string label = "New Lane";

        public string origin = "Indianapolis, IN";
        public string destination = "Chicago, IL";

        [Min(1)] public int miles = 250;

        public FreightMode mode = FreightMode.FullTruckload;
        public EquipmentType equipment = EquipmentType.DryVan;

        [Tooltip("Loads moved on this lane per week.")]
        [Min(1)] public int loadsPerWeek = 5;

        [Tooltip("$/mile the shipper pays their current provider on this lane.")]
        [Min(0f)] public float currentRatePerMile = 2.60f;

        [Tooltip("$/mile it costs Fitzmark to cover the load (carrier + buffer). " +
                 "The rep's offer minus this value is the gross margin per mile.")]
        [Min(0f)] public float fitzmarkCostPerMile = 2.20f;

        /// <summary>Gross margin per mile for a given offered rate.</summary>
        public float MarginPerMile(float offerRatePerMile) => offerRatePerMile - fitzmarkCostPerMile;

        /// <summary>Total gross margin per load for a given offered rate.</summary>
        public float MarginPerLoad(float offerRatePerMile) => MarginPerMile(offerRatePerMile) * miles;

        /// <summary>Projected weekly gross margin if Fitzmark wins this lane at the offered rate.</summary>
        public float WeeklyMargin(float offerRatePerMile) => MarginPerLoad(offerRatePerMile) * loadsPerWeek;

        /// <summary>What the shipper currently spends on this lane each week.</summary>
        public float WeeklyIncumbentSpend => currentRatePerMile * miles * loadsPerWeek;

        public override string ToString() =>
            $"{label} ({mode}/{equipment}, {miles} mi, {loadsPerWeek}/wk)";
    }
}
