using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using NUnit.Framework;

namespace Fitzmark.BDRSim.Tests
{
    public class OutreachSystemTests
    {
        private static BDRCharacter MakeChar()
        {
            var c = BDRCharacter.CreateDefault();
            OutreachSystem.EnsureStarted(c);
            c.career.outreachRemainingToday = OutreachSystem.OutreachPerDay;
            return c;
        }

        private static OutreachLead AddLead(BDRCharacter c, OutreachChannel pref)
        {
            var lead = new OutreachLead { id = "L" + c.leads.Count, company = "Co", contact = "Pat", preferredChannel = pref };
            c.leads.Add(lead);
            return lead;
        }

        [Test]
        public void Source_ConsumesAnAction_AndAddsLead()
        {
            var c = MakeChar();
            int before = OutreachSystem.ActionsLeft(c);
            var lead = OutreachSystem.SourceLead(c, 123);
            Assert.IsNotNull(lead);
            Assert.AreEqual(1, c.leads.Count);
            Assert.AreEqual(before - 1, OutreachSystem.ActionsLeft(c));
        }

        [Test]
        public void Source_FailsWhenNoActionsLeft()
        {
            var c = MakeChar();
            c.career.outreachRemainingToday = 0;
            Assert.IsNull(OutreachSystem.SourceLead(c, 1));
            Assert.AreEqual(0, c.leads.Count);
        }

        [Test]
        public void VariedTouches_WarmTheLead()
        {
            var c = MakeChar();
            var lead = AddLead(c, OutreachChannel.Email);
            // Alternate channels (a real cadence) — should warm within the daily budget.
            var seq = new[] { OutreachChannel.Email, OutreachChannel.LinkedIn, OutreachChannel.Call,
                              OutreachChannel.Video, OutreachChannel.Email, OutreachChannel.LinkedIn };
            foreach (var ch in seq)
            {
                if (lead.status == LeadStatus.Warm) break;
                OutreachSystem.Touch(c, lead, ch);
            }
            Assert.AreEqual(LeadStatus.Warm, lead.status);
            Assert.GreaterOrEqual(lead.interest, OutreachSystem.WarmThreshold);
        }

        [Test]
        public void SpammingOneChannel_BurnsTheLead()
        {
            var c = MakeChar();
            var lead = AddLead(c, OutreachChannel.Video); // spam a non-preferred channel
            for (int i = 0; i < 4 && lead.status != LeadStatus.Dead; i++)
                OutreachSystem.Touch(c, lead, OutreachChannel.Email);
            Assert.AreEqual(LeadStatus.Dead, lead.status);
        }

        [Test]
        public void Touch_FailsWithNoActions()
        {
            var c = MakeChar();
            var lead = AddLead(c, OutreachChannel.Email);
            c.career.outreachRemainingToday = 0;
            var res = OutreachSystem.Touch(c, lead, OutreachChannel.Email);
            Assert.IsFalse(res.consumed);
        }

        [Test]
        public void OnDayAdvanced_RefillsTouches()
        {
            var c = MakeChar();
            c.career.outreachRemainingToday = 0;
            OutreachSystem.OnDayAdvanced(c);
            Assert.AreEqual(OutreachSystem.OutreachPerDay, c.career.outreachRemainingToday);
        }

        [Test]
        public void BuildWarmScenario_IsGatekeeperFree_AndConvertsLead()
        {
            var c = MakeChar();
            var lead = AddLead(c, OutreachChannel.Email);
            lead.interest = 0.8f;
            lead.status = LeadStatus.Warm;

            var scenario = OutreachSystem.BuildWarmScenario(c, lead);

            Assert.IsNotNull(scenario);
            Assert.IsFalse(scenario.gatekeeperPresent, "a warm intro should skip the gatekeeper");
            Assert.IsNotNull(scenario.prospect);
            Assert.AreEqual(lead.company, scenario.prospect.companyName);
            Assert.AreEqual(LeadStatus.Converted, lead.status);
        }
    }
}
