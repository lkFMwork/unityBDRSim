using System.Collections.Generic;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Fitzmark.BDRSim.Tests
{
    public class FreightSystemTests
    {
        private static BDRCharacter MakeChar()
        {
            var c = BDRCharacter.CreateDefault();
            FreightSystem.EnsureStarted(c);
            return c;
        }

        private static FreightAccount AddAccount(BDRCharacter c, float health = 0.65f, float priceSensitivity = 0.4f)
        {
            var a = new FreightAccount
            {
                id = "acct-test",
                company = "Testco",
                health = health,
                priceSensitivity = priceSensitivity,
                active = true,
                lanes = new List<Lane> { new Lane { label = "A -> B", miles = 100, loadsPerWeek = 4 } }
            };
            c.accounts.Add(a);
            return a;
        }

        private static FreightLoad AddOffered(BDRCharacter c, FreightAccount a, float market = 2.50f, float shipperMax = 2.80f)
        {
            var l = new FreightLoad
            {
                id = "load-" + c.loads.Count,
                accountId = a.id,
                accountCompany = a.company,
                miles = 100,
                marketRatePerMile = market,
                shipperMaxPerMile = shipperMax,
                postedDay = 1,
                expiresDay = 3,
                carrierSeed = 12345,
                status = LoadStatus.Offered
            };
            c.loads.Add(l);
            return l;
        }

        [Test]
        public void Quote_AtOrUnderCeiling_WinsAndAwaitsCarrier()
        {
            var c = MakeChar();
            var a = AddAccount(c);
            var l = AddOffered(c, a, market: 2.50f, shipperMax: 2.80f);

            var res = FreightSystem.Quote(c, l, 2.80f);

            Assert.IsTrue(res.won);
            Assert.AreEqual(LoadStatus.AwaitingCarrier, l.status);
            Assert.AreEqual(2.80f, l.quotedRatePerMile, 0.001f);
        }

        [Test]
        public void Quote_OverCeiling_LosesLoad()
        {
            var c = MakeChar();
            var a = AddAccount(c);
            var l = AddOffered(c, a, market: 2.50f, shipperMax: 2.80f);

            var res = FreightSystem.Quote(c, l, 3.20f);

            Assert.IsFalse(res.won);
            Assert.AreEqual(LoadStatus.Lost, l.status);
            Assert.IsTrue(l.resolved);
        }

        [Test]
        public void CoverThenAdvance_DeliversAndPaysCommission()
        {
            var c = MakeChar();
            var a = AddAccount(c, health: 1f); // health 1 + reliability 1 => guaranteed delivery
            var l = AddOffered(c, a, market: 2.00f, shipperMax: 3.00f);
            FreightSystem.Quote(c, l, 3.00f); // sell at 3.00

            bool covered = FreightSystem.Cover(c, l,
                new CarrierOption { name = "Test Carrier", ratePerMile = 2.00f, reliability = 1f }, day: 1);
            Assert.IsTrue(covered);
            Assert.AreEqual(LoadStatus.InTransit, l.status);

            float cashBefore = c.cash;
            FreightSystem.OnDayAdvanced(c, l.deliveryDay, new System.Random(1));

            Assert.AreEqual(LoadStatus.Delivered, l.status);
            // margin = (3.00 - 2.00) * 100 = 100; commission = 30% = 30
            Assert.AreEqual(cashBefore + 30f, c.cash, 0.01f);
            Assert.AreEqual(1, a.loadsDelivered);
        }

        [Test]
        public void CheapUnreliableCarrier_FallsThrough_AndChargesClaim()
        {
            var c = MakeChar();
            var a = AddAccount(c, health: 0.7f);
            var l = AddOffered(c, a, market: 2.00f, shipperMax: 3.00f);
            FreightSystem.Quote(c, l, 3.00f);
            FreightSystem.Cover(c, l,
                new CarrierOption { name = "Sketchy Lines", ratePerMile = 1.60f, reliability = 0f }, day: 1);

            FreightSystem.OnDayAdvanced(c, l.deliveryDay, new System.Random(1));

            Assert.AreEqual(LoadStatus.FellThrough, l.status);
            Assert.Less(c.cash, 0f); // a claim was charged
            Assert.AreEqual(1, a.loadsFailed);
        }

        [Test]
        public void UnquotedTender_Expires_AndDingsHealth()
        {
            var c = MakeChar();
            var a = AddAccount(c, health: 0.65f);
            var l = AddOffered(c, a); // expiresDay = 3

            FreightSystem.OnDayAdvanced(c, 4, new System.Random(1)); // past expiry

            Assert.AreEqual(LoadStatus.Lost, l.status);
            Assert.IsTrue(l.resolved);
            Assert.Less(a.health, 0.65f);
        }

        [Test]
        public void OpenAccountFromWin_CopiesLanes_AndTendersFirstLoad()
        {
            var c = MakeChar();

            var p = ScriptableObject.CreateInstance<ProspectProfile>();
            p.companyName = "Acme Freight";
            p.contactName = "Sam Buyer";
            p.priceSensitivity = 0.5f;
            p.lanes = new List<Lane>
            {
                new Lane { label = "INDY -> CHI", miles = 280, loadsPerWeek = 6,
                    currentRatePerMile = 2.55f, fitzmarkCostPerMile = 2.15f }
            };
            var s = ScriptableObject.CreateInstance<ScenarioDefinition>();
            s.prospect = p;

            var acct = FreightSystem.OpenAccountFromWin(c, s, day: 1);

            Assert.IsNotNull(acct);
            Assert.AreEqual(1, c.accounts.Count);
            Assert.AreEqual("Acme Freight", acct.company);
            Assert.AreEqual(1, acct.lanes.Count);
            Assert.AreEqual(1, c.loads.Count, "a first committed load should be tendered on close");
            Assert.AreEqual(LoadStatus.Offered, c.loads[0].status);
            Assert.AreEqual(acct.id, c.loads[0].accountId);
        }

        [Test]
        public void OpenAccountFromWin_SameCompanyTwice_DoesNotDuplicate()
        {
            var c = MakeChar();
            var p = ScriptableObject.CreateInstance<ProspectProfile>();
            p.companyName = "Repeat Co";
            p.lanes = new List<Lane> { new Lane { label = "X -> Y", miles = 200 } };
            var s = ScriptableObject.CreateInstance<ScenarioDefinition>();
            s.prospect = p;

            FreightSystem.OpenAccountFromWin(c, s, 1);
            FreightSystem.OpenAccountFromWin(c, s, 2);

            Assert.AreEqual(1, c.accounts.Count, "re-closing the same company warms the account, not a duplicate");
        }

        [Test]
        public void Market_ShipperMax_ExceedsMarket_AndRateIsDeterministic()
        {
            var lane = new Lane { label = "Indianapolis, IN -> Atlanta, GA", miles = 530,
                equipment = EquipmentType.Reefer, currentRatePerMile = 3.10f };

            float r1 = FreightMarket.MarketRatePerMile(lane, 7);
            float r2 = FreightMarket.MarketRatePerMile(lane, 7);
            Assert.AreEqual(r1, r2, 0.0001f, "market rate must be deterministic for a lane+day");

            float max = FreightMarket.ShipperMaxPerMile(r1, 0.4f, 0.7f);
            Assert.Greater(max, r1, "a shipper ceiling must sit above market so margin is possible");
        }

        [Test]
        public void CarrierShortlist_IsDeterministic_AndSortedByRate()
        {
            var l = new FreightLoad { marketRatePerMile = 2.40f, carrierSeed = 999, miles = 400 };

            var a = FreightMarket.CarrierShortlist(l, 3);
            var b = FreightMarket.CarrierShortlist(l, 3);

            Assert.AreEqual(3, a.Count);
            Assert.AreEqual(a[0].name, b[0].name, "same seed => same shortlist");
            for (int i = 1; i < a.Count; i++)
                Assert.LessOrEqual(a[i - 1].ratePerMile, a[i].ratePerMile, "carriers sorted cheapest-first");
        }
    }
}
