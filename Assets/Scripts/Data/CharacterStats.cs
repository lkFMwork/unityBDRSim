namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// Single source of truth for derived character stats that several systems read
    /// (quests, achievements, the CRM dashboard). Keeping them here avoids the same
    /// little loops drifting out of sync across the codebase.
    /// </summary>
    public static class CharacterStats
    {
        public static int LoadsDelivered(BDRCharacter c)
        {
            if (c?.accounts == null) return 0;
            int n = 0;
            foreach (var a in c.accounts) if (a != null) n += a.loadsDelivered;
            return n;
        }

        public static int ConvertedLeads(BDRCharacter c)
        {
            if (c?.leads == null) return 0;
            int n = 0;
            foreach (var l in c.leads) if (l != null && l.status == LeadStatus.Converted) n++;
            return n;
        }

        public static int UpgradeLevelsTotal(BDRCharacter c)
        {
            if (c?.upgrades == null) return 0;
            int n = 0;
            foreach (var u in c.upgrades) if (u != null) n += u.level;
            return n;
        }

        public static int AccountsWon(BDRCharacter c) => c?.accounts != null ? c.accounts.Count : 0;

        public static int ActiveAccounts(BDRCharacter c)
        {
            if (c?.accounts == null) return 0;
            int n = 0;
            foreach (var a in c.accounts) if (a != null && a.active) n++;
            return n;
        }
    }
}
