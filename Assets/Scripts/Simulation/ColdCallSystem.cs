using Fitzmark.BDRSim.Data;
using UnityEngine;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>
    /// The nationwide cold-call book. A desk call reaches any company in the country
    /// (<see cref="CompanyRegistry"/>), branding the meeting with a real company and advancing its
    /// relationship — but the phone is the weaker channel: it can only warm a company up to
    /// <see cref="TerritorySystem.PhoneStageCap"/> (Proposal). Turning them into a managed
    /// transportation customer — the close — takes an in-person field visit. To build a pipeline
    /// without a picker, a call prefers a relationship you've already started (off cooldown, below
    /// the phone cap); otherwise it opens a fresh prospect from anywhere in the country.
    /// </summary>
    public static class ColdCallSystem
    {
        public static ScenarioDefinition ServeColdCall(BDRCharacter c, int extraDifficulty = 0)
        {
            var company = PickTarget(c);
            int stage = company != null ? (TerritorySystem.PeekCompany(c, company.Id)?.stage ?? 1) : 1;
            int week = CareerSystem.Week(c.career.day);
            int difficulty = Mathf.Clamp(1 + (c.level - 1) / 2 + (week - 1) + (stage - 1) + extraDifficulty, 1, 10);
            int seed = unchecked(c.career.day * 17 + c.callsMade * 3 + 91);
            var scenario = ProspectGenerator.Generate(difficulty, seed);
            scenario.fieldVisit = false;        // the phone — not an in-person visit
            scenario.gatekeeperPresent = false; // no front desk to get past on a call

            if (company != null)
            {
                scenario.localCompanyId = company.Id;
                scenario.title = company.Name;
                if (scenario.prospect != null)
                {
                    scenario.prospect.companyName = company.Name;
                    scenario.prospect.industry = company.Industry;
                    scenario.prospect.location = company.City;
                }
            }
            return scenario;
        }

        // Prefer advancing a relationship you've already started (stage >= 2, off cooldown, below the
        // phone cap); otherwise open a fresh prospect from anywhere in the national book. Reservoir
        // sampling keeps it varied, and PeekCompany is read-only so scanning never bloats the save.
        private static Company PickTarget(BDRCharacter c)
        {
            int day = c.career.day;
            var rng = new System.Random(unchecked(day * 2657 + c.callsMade * 131 + 17));
            Company started = null, fresh = null;
            int startedSeen = 0, freshSeen = 0;
            foreach (var co in CompanyRegistry.All)
            {
                var p = TerritorySystem.PeekCompany(c, co.Id);
                if (p != null && p.managed) continue;
                int stage = p != null ? p.stage : 1;
                if (stage >= TerritorySystem.PhoneStageCap) continue;                    // phone can't push further
                int last = p != null ? p.lastMeetingDay : -999;
                if (last >= 0 && day - last < TerritorySystem.CompanyCooldownDays) continue; // cooling off
                if (stage >= 2) { if (rng.Next(++startedSeen) == 0) started = co; }
                else            { if (rng.Next(++freshSeen) == 0) fresh = co; }
            }
            return started ?? fresh;
        }
    }
}
