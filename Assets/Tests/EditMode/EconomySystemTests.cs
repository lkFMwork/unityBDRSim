using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using NUnit.Framework;

namespace Fitzmark.BDRSim.Tests
{
    public class EconomySystemTests
    {
        private static BDRCharacter MakeChar(float cash = 1000f)
        {
            var c = BDRCharacter.CreateDefault();
            c.cash = cash;
            EconomySystem.EnsureStarted(c);
            return c;
        }

        [Test]
        public void Buy_DeductsCash_AndRaisesLevel()
        {
            var c = MakeChar(1000f);
            int cost = EconomySystem.CostToUpgrade(c, UpgradeCatalog.Crm);
            Assert.IsTrue(EconomySystem.Buy(c, UpgradeCatalog.Crm));
            Assert.AreEqual(1, EconomySystem.LevelOf(c, UpgradeCatalog.Crm));
            Assert.AreEqual(1000f - cost, c.cash, 0.01f);
            Assert.AreEqual(1, EconomySystem.CallsBonus(c)); // CRM feeds the daily call budget
        }

        [Test]
        public void Buy_FailsWhenCannotAfford()
        {
            var c = MakeChar(10f);
            Assert.IsFalse(EconomySystem.Buy(c, UpgradeCatalog.Crm));
            Assert.AreEqual(0, EconomySystem.LevelOf(c, UpgradeCatalog.Crm));
        }

        [Test]
        public void Buy_StopsAtMaxLevel()
        {
            var c = MakeChar(1_000_000f);
            var def = UpgradeCatalog.Get(UpgradeCatalog.Crm);
            for (int i = 0; i < def.maxLevel; i++)
                Assert.IsTrue(EconomySystem.Buy(c, UpgradeCatalog.Crm));
            Assert.AreEqual(def.maxLevel, EconomySystem.LevelOf(c, UpgradeCatalog.Crm));
            Assert.AreEqual(-1, EconomySystem.CostToUpgrade(c, UpgradeCatalog.Crm));
            Assert.IsFalse(EconomySystem.Buy(c, UpgradeCatalog.Crm));
        }

        [Test]
        public void WeeklyBills_GrowWithAccountsAndUpgrades()
        {
            var c = MakeChar(1_000_000f);
            float baseBills = EconomySystem.WeeklyBills(c);
            c.accounts.Add(new FreightAccount { active = true });
            float withAccount = EconomySystem.WeeklyBills(c);
            Assert.Greater(withAccount, baseBills);
            EconomySystem.Buy(c, UpgradeCatalog.Manager);
            Assert.Greater(EconomySystem.WeeklyBills(c), withAccount); // the AM's salary
        }

        [Test]
        public void OnDayAdvanced_WeekEnded_DeductsBills()
        {
            var c = MakeChar(1000f); // no accounts, no upgrades -> base overhead only
            float bills = EconomySystem.WeeklyBills(c);
            EconomySystem.OnDayAdvanced(c, 5, new System.Random(1), weekEnded: true);
            Assert.AreEqual(1000f - bills, c.cash, 0.01f);
        }

        [Test]
        public void AccountManager_AutoCoversWaitingLoads()
        {
            var c = MakeChar(1_000_000f);
            EconomySystem.Buy(c, UpgradeCatalog.Manager); // level 1 -> auto-cover 1/day
            c.accounts.Add(new FreightAccount { id = "a", company = "Co", active = true, health = 1f });
            var load = new FreightLoad
            {
                id = "l1", accountId = "a", status = LoadStatus.AwaitingCarrier,
                miles = 300, marketRatePerMile = 2.4f, quotedRatePerMile = 3.0f, carrierSeed = 42
            };
            c.loads.Add(load);

            EconomySystem.OnDayAdvanced(c, 2, new System.Random(1), weekEnded: false);

            Assert.AreEqual(LoadStatus.InTransit, load.status);
        }

        [Test]
        public void Manager_RaisesAutoCoverAndPoachResistance()
        {
            var c = MakeChar(1_000_000f);
            Assert.AreEqual(0, EconomySystem.AutoCoverPerDay(c));
            Assert.AreEqual(0f, EconomySystem.PoachResistance(c), 0.001f);
            EconomySystem.Buy(c, UpgradeCatalog.Manager);
            Assert.AreEqual(1, EconomySystem.AutoCoverPerDay(c));
            Assert.AreEqual(0.25f, EconomySystem.PoachResistance(c), 0.001f);
        }

        [Test]
        public void RivalPoach_EventuallyTakesANeglectedAccount()
        {
            var c = MakeChar();
            var acct = new FreightAccount { id = "weak", company = "Weak Co", active = true, health = 0f };
            c.accounts.Add(acct);
            var rng = new System.Random(7);
            bool poached = false;
            for (int d = 0; d < 500 && !poached; d++)
            {
                EconomySystem.OnDayAdvanced(c, d, rng, weekEnded: false);
                poached = !acct.active;
            }
            Assert.IsTrue(poached, "a zero-health account should eventually be poached by a rival");
        }
    }
}
