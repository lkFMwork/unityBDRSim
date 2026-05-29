using System.Collections.Generic;
using Fitzmark.BDRSim.Data;
using UnityEngine;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>Result of a single outreach touch — for UI feedback.</summary>
    public struct TouchResult
    {
        public bool consumed;     // did it spend an action
        public float delta;       // interest change
        public LeadStatus status; // lead status after the touch
        public string message;
    }

    /// <summary>
    /// Multi-channel outreach on the national book: source leads, then warm them with
    /// a <i>cadence</i> of email / LinkedIn / call / video touches. Variety beats
    /// spamming one channel (which fatigues and eventually burns the lead); hitting a
    /// prospect's preferred channel helps; your prospecting/charisma help. A warmed
    /// lead converts to a higher-trust, gatekeeper-free meeting. Pure, deterministic
    /// logic over the character — fully unit-testable.
    /// </summary>
    public static class OutreachSystem
    {
        public const int OutreachPerDay = 8;
        public const float WarmThreshold = 0.75f;
        public const int BurnStreak = 3; // same channel this many times in a row → dead

        public static void EnsureStarted(BDRCharacter c)
        {
            if (c == null) return;
            c.leads ??= new List<OutreachLead>();
            if (c.career != null && c.career.outreachRemainingToday <= 0 && c.career.day <= 1)
                c.career.outreachRemainingToday = OutreachPerDay;
        }

        public static int ActionsLeft(BDRCharacter c) =>
            c?.career != null ? Mathf.Max(0, c.career.outreachRemainingToday) : 0;

        // ---- queries (UI) --------------------------------------------------

        public static List<OutreachLead> Active(BDRCharacter c)
        {
            EnsureStarted(c);
            return c.leads.FindAll(l => l != null && l.status != LeadStatus.Dead && l.status != LeadStatus.Converted);
        }

        public static bool IsWarm(OutreachLead l) => l != null && l.status == LeadStatus.Warm;

        // ---- daily tick ----------------------------------------------------

        /// <summary>Refill the day's touches and apply mild interest decay (keep the cadence going).</summary>
        public static void OnDayAdvanced(BDRCharacter c)
        {
            EnsureStarted(c);
            if (c.career != null) c.career.outreachRemainingToday = OutreachPerDay;
            foreach (var l in c.leads)
            {
                if (l == null || l.status != LeadStatus.Working) continue;
                l.interest = Mathf.Max(0f, l.interest - 0.03f); // cools off if neglected
            }
        }

        // ---- source --------------------------------------------------------

        public static OutreachLead SourceLead(BDRCharacter c, int seed)
        {
            EnsureStarted(c);
            if (ActionsLeft(c) <= 0) return null;
            c.career.outreachRemainingToday--;

            var rng = new System.Random(seed);
            var lead = new OutreachLead
            {
                id = System.Guid.NewGuid().ToString("N").Substring(0, 8),
                company = Companies[rng.Next(Companies.Length)],
                contact = $"{First[rng.Next(First.Length)]} {Last[rng.Next(Last.Length)]}",
                title = Titles[rng.Next(Titles.Length)],
                preferredChannel = (OutreachChannel)rng.Next(4),
                createdDay = c.career != null ? c.career.day : 1,
                status = LeadStatus.New
            };
            c.leads.Add(lead);
            return lead;
        }

        // ---- touch ---------------------------------------------------------

        public static TouchResult Touch(BDRCharacter c, OutreachLead lead, OutreachChannel channel)
        {
            EnsureStarted(c);
            if (lead == null || lead.status == LeadStatus.Dead || lead.status == LeadStatus.Warm
                || lead.status == LeadStatus.Converted)
                return new TouchResult { consumed = false, message = "That lead can't be worked right now." };
            if (ActionsLeft(c) <= 0)
                return new TouchResult { consumed = false, message = "No outreach actions left today." };

            c.career.outreachRemainingToday--;

            float delta = 0.12f;
            if (channel == lead.preferredChannel) delta += 0.08f; // hits their preferred channel

            bool repeat = lead.touchedBefore && channel == lead.lastChannel;
            if (repeat) { delta -= 0.04f; lead.sameChannelStreak++; }
            else { delta += 0.05f; lead.sameChannelStreak = 0; } // cadence variety bonus

            var a = c.attributes;
            if (a != null) delta += a.prospecting * 0.004f + a.charisma * 0.002f;

            lead.interest = Mathf.Clamp01(lead.interest + delta);
            lead.touches++;
            lead.touchedBefore = true;
            lead.lastChannel = channel;
            lead.status = LeadStatus.Working;

            string note;
            if (lead.sameChannelStreak >= BurnStreak)
            {
                lead.status = LeadStatus.Dead;
                note = $"{Name(channel)} again — you've worn them out. Lead went cold.";
            }
            else if (lead.interest >= WarmThreshold)
            {
                lead.status = LeadStatus.Warm;
                note = $"{Name(channel)} landed — {lead.contact} is warm. Book the meeting!";
            }
            else
            {
                note = $"{Name(channel)}: interest {Mathf.RoundToInt(lead.interest * 100f)}%.";
            }

            return new TouchResult { consumed = true, delta = delta, status = lead.status, message = note };
        }

        // ---- convert -------------------------------------------------------

        /// <summary>Build a warm, gatekeeper-free meeting scenario for a warmed lead.</summary>
        public static ScenarioDefinition BuildWarmScenario(BDRCharacter c, OutreachLead lead)
        {
            int week = c.career != null ? CareerSystem.Week(c.career.day) : 1;
            int difficulty = Mathf.Clamp(1 + (c.level - 1) / 2 + (week - 1) + EconomySystem.LeadQuality(c), 1, 10);
            int seed = StableHash(lead.id) + (c.career != null ? c.career.day : 0);

            var scenario = ProspectGenerator.Generate(difficulty, seed);
            if (scenario != null && scenario.prospect != null)
            {
                scenario.prospect.companyName = lead.company;
                scenario.prospect.contactName = lead.contact;
                if (!string.IsNullOrEmpty(lead.title)) scenario.prospect.title = lead.title;
                scenario.prospect.startingTrust = Mathf.Clamp01(scenario.prospect.startingTrust + 0.22f);
                scenario.prospect.startingPatience = Mathf.Clamp01(scenario.prospect.startingPatience + 0.15f);
                scenario.gatekeeperPresent = false; // warm intro — no front desk to fight
                scenario.title = $"Warm meeting: {lead.company}";
            }
            lead.status = LeadStatus.Converted;
            return scenario;
        }

        // ---- helpers -------------------------------------------------------

        public static string Name(OutreachChannel ch) => ch switch
        {
            OutreachChannel.Email => "Email",
            OutreachChannel.LinkedIn => "LinkedIn",
            OutreachChannel.Call => "Cold call",
            OutreachChannel.Video => "Video",
            _ => ch.ToString()
        };

        private static int StableHash(string s)
        {
            int h = 17;
            if (s != null) foreach (char ch in s) h = unchecked(h * 31 + ch);
            return h;
        }

        private static readonly string[] Companies =
        {
            "Summit Steel", "Cardinal Foods", "Ironwood Mills", "Blue Prairie Produce",
            "Granite Components", "Northbound Retail", "Copperline Chemicals", "Sandhill Beverage",
            "Hightower Plastics", "Crosswind Paper", "Redstone Building Supply", "Lakeside Distribution"
        };
        private static readonly string[] First =
        {
            "Pat", "Dana", "Alex", "Sam", "Jordan", "Casey", "Morgan", "Riley", "Taylor", "Jamie", "Quinn", "Avery"
        };
        private static readonly string[] Last =
        {
            "Morgan", "Cole", "Rivera", "Nguyen", "Patel", "Brooks", "Hayes", "Reed", "Flores", "Bennett", "Ortiz", "Shaw"
        };
        private static readonly string[] Titles =
        {
            "Logistics Manager", "Director of Logistics", "VP of Supply Chain",
            "Transportation Manager", "Procurement Lead", "Operations Manager"
        };
    }
}
