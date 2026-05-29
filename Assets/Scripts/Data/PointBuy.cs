namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// D&D-style point-buy for ability scores. Every stat starts at
    /// <see cref="BaseValue"/>; the player spends a fixed <see cref="Budget"/> to
    /// raise them, with the top tiers costing more than the early ones. Pure math,
    /// fully unit-testable.
    /// </summary>
    public static class PointBuy
    {
        public const int BaseValue = 4;
        public const int MaxValue = 10;   // can't point-buy above this (style mods can push higher)
        public const int Budget = 14;

        /// <summary>Incremental cost to raise a stat from (value-1) up to value.</summary>
        public static int StepCost(int value) => value <= 7 ? 1 : 2;

        /// <summary>Total points spent to bring one stat from the base up to <paramref name="value"/>.</summary>
        public static int CostFor(int value)
        {
            int cost = 0;
            for (int v = BaseValue + 1; v <= value; v++)
                cost += StepCost(v);
            return cost;
        }

        public static int Spent(BDRAttributes a)
        {
            int total = 0;
            foreach (var t in BDRAttributes.All)
                total += CostFor(a.Get(t));
            return total;
        }

        public static int Remaining(BDRAttributes a) => Budget - Spent(a);

        public static bool CanRaise(BDRAttributes a, AttributeType type)
        {
            int current = a.Get(type);
            return current < MaxValue && Remaining(a) >= StepCost(current + 1);
        }

        public static bool CanLower(BDRAttributes a, AttributeType type) => a.Get(type) > BaseValue;

        /// <summary>A fresh attribute block with every stat at the point-buy base.</summary>
        public static BDRAttributes NewBaseline()
        {
            var a = new BDRAttributes();
            foreach (var t in BDRAttributes.All)
                a.Set(t, BaseValue);
            return a;
        }
    }
}
