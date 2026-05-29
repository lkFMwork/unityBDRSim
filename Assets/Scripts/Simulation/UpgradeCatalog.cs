using System.Collections.Generic;
using UnityEngine;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>Definition of a buyable business upgrade — metadata + cost curve.</summary>
    public class UpgradeDef
    {
        public string id;
        public string name;
        public string description;
        public int maxLevel;
        public int baseCost;
        public float costGrowth;

        /// <summary>Cost to go from <paramref name="currentLevel"/> to the next level.</summary>
        public int CostFor(int currentLevel) =>
            Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowth, currentLevel));
    }

    /// <summary>
    /// The catalog of upgrades you can sink commission into. Effects are applied by
    /// <see cref="EconomySystem"/>; this is just the buyable metadata + cost curve.
    /// </summary>
    public static class UpgradeCatalog
    {
        public const string Crm = "crm";
        public const string Leads = "leads";
        public const string Manager = "manager";

        public static readonly List<UpgradeDef> All = new List<UpgradeDef>
        {
            new UpgradeDef
            {
                id = Crm, name = "CRM Suite", maxLevel = 3, baseCost = 400, costGrowth = 2.0f,
                description = "+1 dial in your daily call budget per level — work more of the national book each day."
            },
            new UpgradeDef
            {
                id = Leads, name = "Lead Intelligence", maxLevel = 3, baseCost = 500, costGrowth = 2.0f,
                description = "Better targeting surfaces bigger shippers — higher-value (tougher) prospects on career calls."
            },
            new UpgradeDef
            {
                id = Manager, name = "Account Manager", maxLevel = 3, baseCost = 800, costGrowth = 2.0f,
                description = "Hired help auto-covers waiting loads, nudges account health up, and fends off rival poaching. (Salary shows up in weekly bills.)"
            }
        };

        public static UpgradeDef Get(string id) => All.Find(u => u.id == id);
    }
}
