using System.Collections.Generic;
using Fitzmark.BDRSim.Data;
using UnityEngine;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>What the living economy did on a day advance — for a status flash.</summary>
    public readonly struct EconomyDigest
    {
        public readonly int Poached, AutoCovered;
        public readonly float BillsPaid;
        public readonly string PoachedCompany;

        public EconomyDigest(int poached, int autoCovered, float billsPaid, string poachedCompany)
        {
            Poached = poached; AutoCovered = autoCovered; BillsPaid = billsPaid; PoachedCompany = poachedCompany;
        }

        public bool Any => Poached > 0 || AutoCovered > 0 || BillsPaid > 0f;

        public string Summary()
        {
            var parts = new List<string>();
            if (AutoCovered > 0) parts.Add($"account manager covered {AutoCovered} load(s)");
            if (BillsPaid > 0f) parts.Add($"paid ${BillsPaid:N0} in weekly bills");
            if (Poached > 0)
                parts.Add(string.IsNullOrEmpty(PoachedCompany)
                    ? $"{Poached} account(s) poached by a rival"
                    : $"a rival poached {PoachedCompany}");
            return parts.Count > 0 ? "Business: " + string.Join(", ", parts) + "." : "";
        }
    }

    /// <summary>
    /// The living economy on top of the freight desk: spend commission on upgrades
    /// (CRM dials, lead quality, an account manager), pay weekly bills, get auto-cover
    /// help, and lose neglected accounts to rival brokers. Pure logic over the
    /// character (RNG + day passed in), so it's deterministic and unit-testable.
    /// </summary>
    public static class EconomySystem
    {
        public const float PoachBaseChance = 0.05f;
        public const float BaseWeeklyOverhead = 250f; // gentler early game; scales up with the book

        public static void EnsureStarted(BDRCharacter c)
        {
            if (c != null) c.upgrades ??= new List<UpgradeLevel>();
        }

        // ---- upgrade levels & effects --------------------------------------

        public static int LevelOf(BDRCharacter c, string id)
        {
            if (c?.upgrades == null) return 0;
            var u = c.upgrades.Find(x => x != null && x.id == id);
            return u != null ? u.level : 0;
        }

        public static int CallsBonus(BDRCharacter c) => LevelOf(c, UpgradeCatalog.Crm);
        public static int LeadQuality(BDRCharacter c) => LevelOf(c, UpgradeCatalog.Leads);
        public static int AutoCoverPerDay(BDRCharacter c) => LevelOf(c, UpgradeCatalog.Manager);
        public static float PoachResistance(BDRCharacter c) => Mathf.Clamp01(LevelOf(c, UpgradeCatalog.Manager) * 0.25f);

        // ---- buying --------------------------------------------------------

        /// <summary>Cost to buy the next level of an upgrade, or -1 if maxed/unknown.</summary>
        public static int CostToUpgrade(BDRCharacter c, string id)
        {
            var def = UpgradeCatalog.Get(id);
            if (def == null) return -1;
            int level = LevelOf(c, id);
            return level >= def.maxLevel ? -1 : def.CostFor(level);
        }

        public static bool CanAfford(BDRCharacter c, string id)
        {
            int cost = CostToUpgrade(c, id);
            return cost >= 0 && c != null && c.cash >= cost;
        }

        public static bool Buy(BDRCharacter c, string id)
        {
            if (!CanAfford(c, id)) return false;
            int cost = CostToUpgrade(c, id);
            c.cash -= cost;
            var u = c.upgrades.Find(x => x != null && x.id == id);
            if (u == null) { u = new UpgradeLevel { id = id, level = 0 }; c.upgrades.Add(u); }
            u.level++;
            return true;
        }

        // ---- bills ---------------------------------------------------------

        public static float WeeklyBills(BDRCharacter c)
        {
            if (c == null) return BaseWeeklyOverhead;
            int activeAccounts = c.accounts != null ? c.accounts.FindAll(a => a != null && a.active).Count : 0;
            float bills = BaseWeeklyOverhead
                          + activeAccounts * 25f
                          + LevelOf(c, UpgradeCatalog.Crm) * 100f
                          + LevelOf(c, UpgradeCatalog.Leads) * 60f
                          + LevelOf(c, UpgradeCatalog.Manager) * 250f; // the AM's salary
            return bills;
        }

        // ---- daily tick ----------------------------------------------------

        public static EconomyDigest OnDayAdvanced(BDRCharacter c, int day, System.Random rng, bool weekEnded)
        {
            EnsureStarted(c);
            if (c == null) return default;
            rng ??= new System.Random();

            int autoCovered = AutoCover(c, day);
            ManagerPassiveHealth(c);
            (int poached, string poachedCompany) = RivalPoach(c, rng);

            float bills = 0f;
            if (weekEnded)
            {
                bills = WeeklyBills(c);
                c.cash -= bills;
            }

            return new EconomyDigest(poached, autoCovered, bills, poachedCompany);
        }

        private static int AutoCover(BDRCharacter c, int day)
        {
            int budget = AutoCoverPerDay(c);
            if (budget <= 0 || c.loads == null) return 0;
            int covered = 0;
            foreach (var load in c.loads)
            {
                if (covered >= budget) break;
                if (load == null || load.status != LoadStatus.AwaitingCarrier) continue;
                var shortlist = FreightMarket.CarrierShortlist(load);
                if (shortlist.Count == 0) continue;
                var carrier = shortlist[shortlist.Count / 2]; // a sensible mid carrier
                if (FreightSystem.Cover(c, load, carrier, day)) covered++;
            }
            return covered;
        }

        private static void ManagerPassiveHealth(BDRCharacter c)
        {
            float boost = LevelOf(c, UpgradeCatalog.Manager) * 0.01f;
            if (boost <= 0f || c.accounts == null) return;
            foreach (var a in c.accounts)
                if (a != null && a.active) a.health = Mathf.Min(1f, a.health + boost);
        }

        private static (int, string) RivalPoach(BDRCharacter c, System.Random rng)
        {
            if (c.accounts == null) return (0, null);
            float resist = 1f - PoachResistance(c);
            // Rivals target your weakest account; lower health = more exposed.
            FreightAccount target = null;
            float worst = float.MaxValue;
            foreach (var a in c.accounts)
            {
                if (a == null || !a.active) continue;
                if (a.health < worst) { worst = a.health; target = a; }
            }
            if (target == null) return (0, null);

            // Good service protects you: well-kept accounts (health >= ~0.8) are safe;
            // only genuinely neglected accounts are exposed to rivals.
            float chance = PoachBaseChance * Mathf.Max(0f, 0.8f - Mathf.Clamp01(target.health)) * resist;
            if (rng.NextDouble() < chance)
            {
                target.active = false;
                return (1, target.company);
            }
            return (0, null);
        }
    }
}
