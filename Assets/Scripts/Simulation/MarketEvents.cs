using System.Collections.Generic;
using Fitzmark.BDRSim.Data;
using UnityEngine;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>
    /// Weekly market events — the dynamic-world layer. Once a week a random event may
    /// fire: a fuel-surcharge windfall, a freight claim, a referral wave that lifts
    /// account health, an inbound RFP that drops a warm lead into the pipeline, or
    /// just market color. Kept separate from <see cref="EconomySystem"/> so the core
    /// weekly tick stays deterministic and unit-tested; the message is surfaced to the
    /// player as a flash/toast.
    /// </summary>
    public static class MarketEvents
    {
        public static string RollWeekly(BDRCharacter c, System.Random rng)
        {
            if (c == null) return null;
            rng ??= new System.Random();

            switch (rng.Next(6))
            {
                case 1:
                    float bonus = 120f + rng.Next(0, 5) * 30f;
                    c.cash += bonus;
                    return $"Fuel surcharges recovered — +${bonus:N0} this week.";
                case 2:
                    float claim = 100f + rng.Next(0, 4) * 25f;
                    c.cash -= claim;
                    return $"A freight claim hit the books — -${claim:N0}.";
                case 3:
                    BoostHealth(c, 0.08f);
                    return "Referral wave — your accounts' service reputation ticked up.";
                case 4:
                    GrantWarmLead(c);
                    return "Inbound RFP — a warm lead landed in your outreach pipeline.";
                case 5:
                    return "Produce season — reefer capacity is tight nationwide.";
                default:
                    return null; // quiet week
            }
        }

        private static void BoostHealth(BDRCharacter c, float amount)
        {
            if (c.accounts == null) return;
            foreach (var a in c.accounts)
                if (a != null && a.active) a.health = Mathf.Min(1f, a.health + amount);
        }

        private static void GrantWarmLead(BDRCharacter c)
        {
            c.leads ??= new List<OutreachLead>();
            c.leads.Add(new OutreachLead
            {
                id = System.Guid.NewGuid().ToString("N").Substring(0, 8),
                company = "Inbound RFP Co.",
                contact = "Inbound Lead",
                title = "Procurement",
                interest = 0.8f,
                touchedBefore = true,
                status = LeadStatus.Warm
            });
        }
    }
}
