using Fitzmark.BDRSim.Data;
using NUnit.Framework;

namespace Fitzmark.BDRSim.Tests
{
    public class PointBuyTests
    {
        [Test]
        public void BaselineSpendsNothing()
        {
            var a = PointBuy.NewBaseline();
            Assert.AreEqual(0, PointBuy.Spent(a));
            Assert.AreEqual(PointBuy.Budget, PointBuy.Remaining(a));
            foreach (var t in BDRAttributes.All)
                Assert.AreEqual(PointBuy.BaseValue, a.Get(t));
        }

        [Test]
        public void CostCurveChargesAPremiumForHighTiers()
        {
            Assert.AreEqual(0, PointBuy.CostFor(PointBuy.BaseValue));
            // 5,6,7 cost 1 each from a base of 4
            Assert.AreEqual(3, PointBuy.CostFor(7));
            // 8,9,10 cost 2 each on top -> 3 + 6 = 9
            Assert.AreEqual(9, PointBuy.CostFor(10));
            // the step into the premium band is more expensive than a normal step
            Assert.That(PointBuy.StepCost(8), Is.GreaterThan(PointBuy.StepCost(7)));
        }

        [Test]
        public void RaisingDeductsFromRemaining()
        {
            var a = PointBuy.NewBaseline();
            a.Adjust(AttributeType.Charisma, 2); // 4 -> 6, costs 2
            Assert.AreEqual(2, PointBuy.Spent(a));
            Assert.AreEqual(PointBuy.Budget - 2, PointBuy.Remaining(a));
        }

        [Test]
        public void CannotRaiseBeyondTheBudget()
        {
            var a = PointBuy.NewBaseline();
            while (a.Get(AttributeType.Charisma) < PointBuy.MaxValue && PointBuy.CanRaise(a, AttributeType.Charisma))
                a.Adjust(AttributeType.Charisma, 1);

            Assert.AreEqual(PointBuy.MaxValue, a.Get(AttributeType.Charisma)); // 9 points <= 14 budget
            Assert.That(PointBuy.Remaining(a), Is.GreaterThanOrEqualTo(0));

            // Spend the rest, then the next raise must be refused.
            while (PointBuy.CanRaise(a, AttributeType.Negotiation))
                a.Adjust(AttributeType.Negotiation, 1);
            Assert.IsFalse(PointBuy.CanRaise(a, AttributeType.Negotiation));
            Assert.That(PointBuy.Remaining(a), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void CannotLowerBelowBase()
        {
            var a = PointBuy.NewBaseline();
            Assert.IsFalse(PointBuy.CanLower(a, AttributeType.Charisma));
            a.Adjust(AttributeType.Charisma, 1);
            Assert.IsTrue(PointBuy.CanLower(a, AttributeType.Charisma));
        }
    }
}
