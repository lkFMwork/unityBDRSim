using System;
using System.Collections.Generic;
using Fitzmark.BDRSim.Data;
using UnityEngine;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>Outcome of quoting a load to the shipper.</summary>
    public struct QuoteResult
    {
        public bool won;
        public float projectedMargin;
        public string message;
    }

    /// <summary>What advancing a day did to the freight book — for a status flash.</summary>
    public readonly struct FreightDayDigest
    {
        public readonly int Delivered, Failed, Expired, Churned, Tendered;
        public readonly float MarginEarned, Commission;
        public readonly int Xp;

        public FreightDayDigest(int delivered, int failed, int expired, int churned, int tendered,
            float marginEarned, float commission, int xp)
        {
            Delivered = delivered; Failed = failed; Expired = expired; Churned = churned;
            Tendered = tendered; MarginEarned = marginEarned; Commission = commission; Xp = xp;
        }

        public bool Any => Delivered + Failed + Expired + Churned + Tendered > 0;

        public string Summary()
        {
            var parts = new List<string>();
            if (Delivered > 0) parts.Add($"{Delivered} delivered (+${Commission:N0} commission)");
            if (Failed > 0) parts.Add($"{Failed} fell through");
            if (Tendered > 0) parts.Add($"{Tendered} new tender(s)");
            if (Expired > 0) parts.Add($"{Expired} tender(s) expired");
            if (Churned > 0) parts.Add($"{Churned} account(s) churned");
            return parts.Count > 0 ? "Freight: " + string.Join(", ", parts) + "." : "";
        }
    }

    /// <summary>
    /// The freight desk loop: won accounts tender loads, you price them, cover them
    /// with a carrier, and earn the margin spread (minus the risk a cheap carrier
    /// falls through). Pure logic over <see cref="BDRCharacter"/> — RNG and the day
    /// are passed in, so it's deterministic and unit-testable.
    /// </summary>
    public static class FreightSystem
    {
        public const int MaxOpenLoadsPerAccount = 5;
        public const int MaxResolvedKept = 12;
        public const float ChurnHealth = 0.18f;
        public const int TenderWindowDays = 2;

        public static void EnsureStarted(BDRCharacter c)
        {
            if (c == null) return;
            c.accounts ??= new List<FreightAccount>();
            c.loads ??= new List<FreightLoad>();
        }

        // ---- queries (for the desk UI) -------------------------------------

        public static List<FreightAccount> ActiveAccounts(BDRCharacter c)
        {
            EnsureStarted(c);
            return c.accounts.FindAll(a => a != null && a.active);
        }

        public static List<FreightLoad> LoadsByStatus(BDRCharacter c, LoadStatus status)
        {
            EnsureStarted(c);
            return c.loads.FindAll(l => l != null && l.status == status);
        }

        public static FreightAccount Account(BDRCharacter c, string id)
        {
            EnsureStarted(c);
            return c.accounts.Find(a => a != null && a.id == id);
        }

        /// <summary>Most recently posted resolved loads (delivered / lost / fell-through).</summary>
        public static List<FreightLoad> Recent(BDRCharacter c, int max = 6)
        {
            EnsureStarted(c);
            var done = c.loads.FindAll(l => l != null && l.resolved);
            done.Sort((a, b) => b.postedDay.CompareTo(a.postedDay));
            if (done.Count > max) done.RemoveRange(max, done.Count - max);
            return done;
        }

        // ---- closing a deal opens an account -------------------------------

        /// <summary>
        /// Turn a won meeting into a real account: copy its lanes, seed its health,
        /// and tender a first committed load so there's something to work right away.
        /// Re-closing an existing account just warms it back up.
        /// </summary>
        public static FreightAccount OpenAccountFromWin(BDRCharacter c, ScenarioDefinition scenario, int day)
        {
            EnsureStarted(c);
            var p = scenario != null ? scenario.prospect : null;
            string company = p != null && !string.IsNullOrEmpty(p.companyName)
                ? p.companyName
                : (scenario != null ? scenario.title : "New Account");

            var existing = c.accounts.Find(a =>
                a != null && a.active && string.Equals(a.company, company, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.health = Mathf.Min(1f, existing.health + 0.10f);
                return existing;
            }

            var acct = new FreightAccount
            {
                id = $"acct-{StableHash(company):X8}-{day}",
                company = company,
                contact = p != null ? p.contactName : "",
                title = p != null ? p.title : "",
                location = p != null ? p.location : "",
                industry = p != null ? p.industry : "",
                priceSensitivity = p != null ? p.priceSensitivity : 0.5f,
                health = 0.65f,
                wonOnDay = day,
                lastTenderDay = day,
                active = true
            };

            if (p != null && p.lanes != null)
                foreach (var l in p.lanes)
                    if (l != null) acct.lanes.Add(CopyLane(l));
            if (acct.lanes.Count == 0)
            {
                var pl = scenario != null ? scenario.PrimaryLane : null;
                acct.lanes.Add(pl != null ? CopyLane(pl) : DefaultLane());
            }

            c.accounts.Add(acct);

            // First committed load — the freight they agreed to on the close.
            var rng = new System.Random(unchecked(StableHash(company) ^ (day * 7919)));
            c.loads.Add(TenderLoad(acct, acct.lanes[0], day, rng));
            return acct;
        }

        // ---- player verbs --------------------------------------------------

        /// <summary>Quote a sell rate. At/under the shipper's ceiling wins the freight.</summary>
        public static QuoteResult Quote(BDRCharacter c, FreightLoad load, float sellPerMile)
        {
            if (load == null || load.status != LoadStatus.Offered)
                return new QuoteResult { won = false, message = "That load can't be quoted right now." };

            load.quotedRatePerMile = sellPerMile;
            var acct = Account(c, load.accountId);

            if (sellPerMile <= load.shipperMaxPerMile + 0.001f)
            {
                load.status = LoadStatus.AwaitingCarrier;
                float projected = (sellPerMile - load.marketRatePerMile * 0.88f) * load.miles;
                return new QuoteResult
                {
                    won = true,
                    projectedMargin = projected,
                    message = $"Won the freight at ${sellPerMile:0.00}/mi — now cover it with a carrier."
                };
            }

            load.status = LoadStatus.Lost;
            load.resolved = true;
            if (acct != null) acct.health = Mathf.Max(0f, acct.health - 0.02f);
            load.note = "Quoted over the ceiling — the shipper went with someone else.";
            return new QuoteResult
            {
                won = false,
                message = $"${sellPerMile:0.00}/mi was above their ceiling (~${load.shipperMaxPerMile:0.00}). Lost the load."
            };
        }

        /// <summary>Cover a won load with a carrier, locking the margin and a delivery day.</summary>
        public static bool Cover(BDRCharacter c, FreightLoad load, CarrierOption carrier, int day)
        {
            if (load == null || load.status != LoadStatus.AwaitingCarrier) return false;
            load.carrierRatePerMile = carrier.ratePerMile;
            load.carrierName = carrier.name;
            load.carrierReliability = carrier.reliability;
            load.status = LoadStatus.InTransit;
            load.deliveryDay = day + FreightMarket.TransitDays(load.miles) + 1;
            load.note = $"Covered by {carrier.name}. ETA day {load.deliveryDay}.";
            return true;
        }

        // ---- daily tick ----------------------------------------------------

        /// <summary>
        /// Advance the freight book to a new day: resolve in-transit deliveries,
        /// expire un-quoted tenders, tender fresh loads, and churn accounts whose
        /// service reputation has bottomed out.
        /// </summary>
        public static FreightDayDigest OnDayAdvanced(BDRCharacter c, int newDay, System.Random rng)
        {
            EnsureStarted(c);
            rng ??= new System.Random();

            int delivered = 0, failed = 0, expired = 0, churned = 0, tendered = 0;
            float marginEarned = 0f, commission = 0f;
            int xp = 0;

            foreach (var load in c.loads)
            {
                if (load == null || load.resolved) continue;

                if (load.status == LoadStatus.Offered && newDay > load.expiresDay)
                {
                    load.status = LoadStatus.Lost;
                    load.resolved = true;
                    load.note = "Tender expired — you didn't quote it in time.";
                    var a = Account(c, load.accountId);
                    if (a != null) a.health = Mathf.Max(0f, a.health - 0.03f);
                    expired++;
                    continue;
                }

                if (load.status == LoadStatus.InTransit && newDay >= load.deliveryDay)
                {
                    var acct = Account(c, load.accountId);
                    float h = acct != null ? acct.health : 0.6f;
                    float success = Mathf.Clamp01(load.carrierReliability * (0.85f + h * 0.15f));
                    if (rng.NextDouble() < success)
                    {
                        load.status = LoadStatus.Delivered;
                        load.resolved = true;
                        float m = load.TotalMargin;
                        float comm = m * FreightMarket.CommissionRate;
                        c.cash += comm;
                        c.lifetimeMargin += m;
                        c.totalWeeklyMarginWon += m;
                        if (acct != null)
                        {
                            acct.lifetimeMargin += m;
                            acct.loadsDelivered++;
                            acct.health = Mathf.Min(1f, acct.health + 0.03f);
                        }
                        delivered++;
                        marginEarned += m;
                        commission += comm;
                        int lx = Mathf.Clamp(Mathf.RoundToInt(m * 0.08f), 2, 40);
                        xp += lx;
                        load.note = $"Delivered clean. +${comm:N0} commission (margin ${m:N0}).";
                    }
                    else
                    {
                        load.status = LoadStatus.FellThrough;
                        load.resolved = true;
                        float claim = Mathf.Min(200f, Mathf.Max(0f, load.TotalMargin) * 0.5f + 60f);
                        c.cash -= claim;
                        if (acct != null)
                        {
                            acct.loadsFailed++;
                            acct.health = Mathf.Max(0f, acct.health - 0.15f);
                        }
                        failed++;
                        load.note = $"Service failure — {Friendly(load.carrierName)} fell through. -${claim:N0} claim.";
                    }
                }
            }

            // Churn accounts whose reputation has cratered.
            foreach (var acct in c.accounts)
            {
                if (acct == null || !acct.active) continue;
                if (acct.health <= ChurnHealth)
                {
                    acct.active = false;
                    churned++;
                }
            }

            // Tender fresh freight on active accounts (bounded so the board stays workable).
            foreach (var acct in c.accounts)
            {
                if (acct == null || !acct.active || acct.lanes == null) continue;
                int open = c.loads.FindAll(l => l != null && l.accountId == acct.id && !l.resolved
                    && l.status != LoadStatus.Delivered).Count;
                foreach (var lane in acct.lanes)
                {
                    if (open >= MaxOpenLoadsPerAccount) break;
                    if (lane == null) continue;
                    float chance = Mathf.Clamp(lane.loadsPerWeek / 12f, 0.08f, 0.6f);
                    if (rng.NextDouble() < chance)
                    {
                        c.loads.Add(TenderLoad(acct, lane, newDay, rng));
                        open++;
                        tendered++;
                    }
                }
                acct.lastTenderDay = newDay;
            }

            PruneResolved(c);
            if (xp > 0) ProgressionSystem.AddXp(c, xp);

            return new FreightDayDigest(delivered, failed, expired, churned, tendered,
                marginEarned, commission, xp);
        }

        // ---- helpers -------------------------------------------------------

        private static FreightLoad TenderLoad(FreightAccount a, Lane lane, int day, System.Random rng)
        {
            float market = FreightMarket.MarketRatePerMile(lane, day);
            float shipperMax = FreightMarket.ShipperMaxPerMile(market, a.priceSensitivity, a.health);
            return new FreightLoad
            {
                id = Guid.NewGuid().ToString("N").Substring(0, 8),
                accountId = a.id,
                accountCompany = a.company,
                origin = lane.origin,
                destination = lane.destination,
                miles = Mathf.Max(1, lane.miles),
                equipment = lane.equipment,
                mode = lane.mode,
                commodity = FreightMarket.Commodity(lane.equipment, rng),
                marketRatePerMile = market,
                shipperMaxPerMile = shipperMax,
                postedDay = day,
                expiresDay = day + TenderWindowDays,
                carrierSeed = rng.Next(1, int.MaxValue),
                status = LoadStatus.Offered
            };
        }

        private static void PruneResolved(BDRCharacter c)
        {
            var resolved = c.loads.FindAll(l => l != null && l.resolved);
            if (resolved.Count <= MaxResolvedKept) return;
            resolved.Sort((a, b) => a.postedDay.CompareTo(b.postedDay)); // oldest first
            int toRemove = resolved.Count - MaxResolvedKept;
            for (int i = 0; i < toRemove; i++) c.loads.Remove(resolved[i]);
        }

        private static Lane CopyLane(Lane l) => new Lane
        {
            label = l.label,
            origin = l.origin,
            destination = l.destination,
            miles = l.miles,
            mode = l.mode,
            equipment = l.equipment,
            loadsPerWeek = l.loadsPerWeek,
            currentRatePerMile = l.currentRatePerMile,
            fitzmarkCostPerMile = l.fitzmarkCostPerMile
        };

        private static Lane DefaultLane() => new Lane
        {
            label = "Indianapolis, IN → Chicago, IL",
            origin = "Indianapolis, IN",
            destination = "Chicago, IL",
            miles = 280,
            mode = FreightMode.FullTruckload,
            equipment = EquipmentType.DryVan,
            loadsPerWeek = 6,
            currentRatePerMile = 2.55f,
            fitzmarkCostPerMile = 2.15f
        };

        private static string Friendly(string carrier) =>
            string.IsNullOrEmpty(carrier) ? "the carrier" : carrier;

        private static int StableHash(string s)
        {
            int h = 17;
            if (s != null) foreach (char ch in s) h = unchecked(h * 31 + ch);
            return h;
        }
    }
}
