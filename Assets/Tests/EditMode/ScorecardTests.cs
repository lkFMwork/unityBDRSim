using System.Collections.Generic;
using Fitzmark.BDRSim.Simulation;
using NUnit.Framework;

namespace Fitzmark.BDRSim.Tests
{
    public class ScorecardTests
    {
        [Test]
        public void PointsClampToCategoryMaximum()
        {
            var card = new Scorecard();
            card.Add(ScoreCategory.Discovery, 999f);
            Assert.AreEqual(Scorecard.MaxPerCategory, card.GetPoints(ScoreCategory.Discovery));
            Assert.AreEqual(1f, card.GetPercent(ScoreCategory.Discovery), 0.0001f);
        }

        [Test]
        public void PointsNeverGoNegative()
        {
            var card = new Scorecard();
            card.Add(ScoreCategory.Rapport, -50f);
            Assert.AreEqual(0f, card.GetPoints(ScoreCategory.Rapport));
        }

        [Test]
        public void PerfectCardIsAnA()
        {
            var card = new Scorecard();
            foreach (ScoreCategory cat in System.Enum.GetValues(typeof(ScoreCategory)))
                card.Add(cat, Scorecard.MaxPerCategory);

            Assert.AreEqual(1f, card.OverallPercent, 0.0001f);
            Assert.AreEqual("A", card.LetterGrade);
        }

        [Test]
        public void EmptyCardIsAnF()
        {
            var card = new Scorecard();
            Assert.AreEqual(0f, card.OverallPercent, 0.0001f);
            Assert.AreEqual("F", card.LetterGrade);
        }

        [Test]
        public void GradeThresholdsMapCorrectly()
        {
            Assert.AreEqual("A", Scorecard.GradeFor(0.90f));
            Assert.AreEqual("B", Scorecard.GradeFor(0.85f));
            Assert.AreEqual("C", Scorecard.GradeFor(0.75f));
            Assert.AreEqual("D", Scorecard.GradeFor(0.65f));
            Assert.AreEqual("F", Scorecard.GradeFor(0.40f));
        }

        [Test]
        public void WeakestFirstOrdersByPercentAscending()
        {
            var card = new Scorecard();
            // Distinct values for every category so the ordering is fully determined.
            card.Add(ScoreCategory.Rapport, 18f);           // .90
            card.Add(ScoreCategory.Discovery, 16f);         // .80
            card.Add(ScoreCategory.ValueArticulation, 14f); // .70
            card.Add(ScoreCategory.ObjectionHandling, 12f); // .60
            card.Add(ScoreCategory.Negotiation, 4f);        // .20
            card.Add(ScoreCategory.Close, 2f);              // .10

            var ordered = new List<ScoreCategory>(card.WeakestFirst());
            Assert.AreEqual(ScoreCategory.Close, ordered[0]);
            Assert.AreEqual(ScoreCategory.Rapport, ordered[ordered.Count - 1]);
        }
    }
}
