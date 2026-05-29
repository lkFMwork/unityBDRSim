using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using NUnit.Framework;

namespace Fitzmark.BDRSim.Tests
{
    public class NegotiationEngineTests
    {
        private static Lane MakeLane() => new Lane
        {
            label = "Test Lane",
            miles = 290,
            loadsPerWeek = 8,
            currentRatePerMile = 2.60f,
            fitzmarkCostPerMile = 2.20f
        };

        [Test]
        public void AcceptsOfferWithinSavingsExpectation()
        {
            var lane = MakeLane();
            // Neutral prospect: required savings ~5.25%, ceiling ~$2.46/mi.
            var result = NegotiationEngine.Evaluate(lane, 2.40f, prospectTrust: 0.35f, priceSensitivity: 0.5f);

            Assert.AreEqual(NegotiationStatus.Accepted, result.Status);
            Assert.AreEqual(0.20f, result.MarginPerMile, 0.0001f);
        }

        [Test]
        public void RejectsOfferFarAboveCeiling()
        {
            var lane = MakeLane();
            var result = NegotiationEngine.Evaluate(lane, 2.95f, prospectTrust: 0.35f, priceSensitivity: 0.5f);

            Assert.AreEqual(NegotiationStatus.Rejected, result.Status);
        }

        [Test]
        public void CountersOfferJustAboveCeiling()
        {
            var lane = MakeLane();
            // Just over the ~$2.46 ceiling but within the 6% counter band.
            var result = NegotiationEngine.Evaluate(lane, 2.55f, prospectTrust: 0.35f, priceSensitivity: 0.5f);

            Assert.AreEqual(NegotiationStatus.Countered, result.Status);
            Assert.That(result.CounterRatePerMile, Is.GreaterThan(0f));
            Assert.That(result.CounterRatePerMile, Is.LessThan(lane.currentRatePerMile));
        }

        [Test]
        public void HigherTrustRaisesTheAcceptableRate()
        {
            var lane = MakeLane();

            var lowTrust = NegotiationEngine.Evaluate(lane, 2.50f, prospectTrust: 0.0f, priceSensitivity: 0.5f);
            var highTrust = NegotiationEngine.Evaluate(lane, 2.50f, prospectTrust: 0.9f, priceSensitivity: 0.5f);

            // The same offer a guarded prospect won't accept is acceptable once trust is high.
            Assert.AreNotEqual(NegotiationStatus.Accepted, lowTrust.Status);
            Assert.AreEqual(NegotiationStatus.Accepted, highTrust.Status);
        }

        [Test]
        public void PriceSensitivityLowersTheAcceptableRate()
        {
            var lane = MakeLane();

            var valueBuyer = NegotiationEngine.Evaluate(lane, 2.45f, prospectTrust: 0.4f, priceSensitivity: 0.1f);
            var priceBuyer = NegotiationEngine.Evaluate(lane, 2.45f, prospectTrust: 0.4f, priceSensitivity: 0.95f);

            Assert.AreEqual(NegotiationStatus.Accepted, valueBuyer.Status);
            Assert.AreNotEqual(NegotiationStatus.Accepted, priceBuyer.Status);
        }
    }
}
