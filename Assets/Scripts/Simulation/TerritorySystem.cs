using Fitzmark.BDRSim.Data;
using UnityEngine;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>
    /// Two things: the city overworld state (whether a city is unlocked by its commute and cleared
    /// on the SMW path), and the per-company relationship engine — the stage a company is at
    /// (cold intro → … → managed), its visit cooldown, and recording a meeting/call result. In
    /// person can close a managed customer; the phone caps below it (see <see cref="PhoneStageCap"/>).
    /// </summary>
    public static class TerritorySystem
    {
        public static LocalAccountProgress GetProgress(BDRCharacter c, string clientId)
        {
            c.localAccounts ??= new System.Collections.Generic.List<LocalAccountProgress>();
            foreach (var p in c.localAccounts)
                if (p.clientId == clientId) return p;
            var created = new LocalAccountProgress { clientId = clientId };
            c.localAccounts.Add(created);
            return created;
        }

        /// <summary>True once the player has beaten the commute platformer to reach this city.</summary>
        public static bool IsCityUnlocked(BDRCharacter c, string clientId) => GetProgress(c, clientId).cityUnlocked;

        /// <summary>Mark a city reachable by fast-travel (after winning the commute level).</summary>
        public static void UnlockCity(BDRCharacter c, string clientId) =>
            GetProgress(c, clientId).cityUnlocked = true;

        // --- Per-company relationships (15 companies per city) -------------------------------
        // In-person field visits build a relationship with a SPECIFIC company: the first meeting is
        // cold (gatekept), each won revisit advances a stage, and the final close turns them into a
        // managed transportation customer (a freight account on the desk).

        public const int CompanyStages = 4;       // cold intro → ... → managed
        public const int CompanyCooldownDays = 2; // in-game gap between visits to the same company
        public const int PhoneStageCap = 3;       // the phone only warms a company to Proposal; the
                                                  // close (managed customer) takes an in-person visit

        public static string CompanyStageName(int stage) => stage switch
        {
            1 => "Cold Intro",
            2 => "Follow-up",
            3 => "Proposal",
            4 => "Closing",
            _ => "Managed Customer",
        };

        public static CompanyProgress GetCompany(BDRCharacter c, string companyId)
        {
            c.companyAccounts ??= new System.Collections.Generic.List<CompanyProgress>();
            foreach (var p in c.companyAccounts)
                if (p.companyId == companyId) return p;
            var created = new CompanyProgress { companyId = companyId };
            c.companyAccounts.Add(created);
            return created;
        }

        /// <summary>Read-only progress lookup — returns null if untouched and never creates an entry,
        /// so scanning the whole national book (945 companies) doesn't bloat the save.</summary>
        public static CompanyProgress PeekCompany(BDRCharacter c, string companyId)
        {
            if (c.companyAccounts == null) return null;
            foreach (var p in c.companyAccounts)
                if (p.companyId == companyId) return p;
            return null;
        }

        public static int CompanyStage(BDRCharacter c, string companyId) => GetCompany(c, companyId).stage;

        /// <summary>True once the relationship has closed — they're a managed transportation customer.</summary>
        public static bool IsCompanyManaged(BDRCharacter c, string companyId) => GetCompany(c, companyId).managed;

        public static bool IsCompanyAvailable(BDRCharacter c, string companyId, int day)
        {
            var p = GetCompany(c, companyId);
            if (p.managed) return false;
            if (p.lastMeetingDay < 0) return true;
            return day - p.lastMeetingDay >= CompanyCooldownDays;
        }

        public static int CompanyDaysUntilAvailable(BDRCharacter c, string companyId, int day)
        {
            var p = GetCompany(c, companyId);
            if (p.lastMeetingDay < 0) return 0;
            return Mathf.Max(0, CompanyCooldownDays - (day - p.lastMeetingDay));
        }

        /// <summary>Meeting difficulty climbs as the relationship deepens toward the close.</summary>
        public static int CompanyStageDifficulty(BDRCharacter c, Company company)
        {
            int stage = GetCompany(c, company.Id).stage;
            int week = CareerSystem.Week(c.career.day);
            return Mathf.Clamp(1 + (c.level - 1) / 2 + (week - 1) + (stage - 1), 1, 10);
        }

        /// <summary>Record a finished in-person visit: start the cooldown; on a win, advance the
        /// relationship a stage (or, on the final stage, make them a managed customer).</summary>
        public static void RecordCompanyMeeting(BDRCharacter c, string companyId, int day, bool won)
        {
            var p = GetCompany(c, companyId);
            p.lastMeetingDay = day;
            if (!won) return;
            if (p.stage >= CompanyStages) p.managed = true;
            else p.stage++;
        }

        /// <summary>Record a finished nationwide cold call: start the cooldown; on a win, warm the
        /// relationship a stage — but only up to <see cref="PhoneStageCap"/>. The phone never closes
        /// a managed customer; that takes an in-person field visit.</summary>
        public static void RecordCompanyColdCall(BDRCharacter c, string companyId, int day, bool won)
        {
            var p = GetCompany(c, companyId);
            p.lastMeetingDay = day;
            if (won && p.stage < PhoneStageCap) p.stage++;
        }
    }
}
