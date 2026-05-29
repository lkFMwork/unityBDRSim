using Fitzmark.BDRSim.Data;

namespace Fitzmark.BDRSim.Simulation
{
    public enum NegotiationStatus
    {
        Accepted,
        Countered,
        Rejected
    }

    /// <summary>Outcome of evaluating a single rate offer against a prospect.</summary>
    public readonly struct NegotiationResult
    {
        public readonly NegotiationStatus Status;
        public readonly float CounterRatePerMile;  // valid when Status == Countered
        public readonly float MarginPerMile;       // at the offered rate
        public readonly float WeeklyMargin;        // at the offered rate
        public readonly float TrustDelta;
        public readonly float PatienceDelta;
        public readonly string Reason;

        public NegotiationResult(NegotiationStatus status, float counter, float marginPerMile,
            float weeklyMargin, float trustDelta, float patienceDelta, string reason)
        {
            Status = status;
            CounterRatePerMile = counter;
            MarginPerMile = marginPerMile;
            WeeklyMargin = weeklyMargin;
            TrustDelta = trustDelta;
            PatienceDelta = patienceDelta;
            Reason = reason;
        }
    }

    /// <summary>
    /// Deterministic rate-negotiation model. A prospect expects a certain amount
    /// of savings versus what they pay today; how much depends on how price-driven
    /// they are (raises the bar) and how much they trust the rep (lowers it — a
    /// trusted rep can sell value over a few cents per mile). No randomness, so the
    /// behavior is fully unit-testable.
    /// </summary>
    public static class NegotiationEngine
    {
        // Tunables for the savings expectation curve.
        private const float BaseRequiredSavings = 0.03f; // a neutral prospect wants ~3% off
        private const float PriceSensitivityWeight = 0.08f;
        private const float TrustWeight = 0.05f;
        private const float MinRequiredSavings = -0.03f; // a warm prospect may pay a small premium
        private const float MaxRequiredSavings = 0.15f;
        private const float CounterBandAbove = 1.06f;    // within 6% of the ceiling -> counter, else reject

        public static NegotiationResult Evaluate(Lane lane, float offerRatePerMile,
            float prospectTrust, float priceSensitivity, float negotiationSkill = 0f)
        {
            float current = lane.currentRatePerMile;

            float requiredSavings = Clamp(
                BaseRequiredSavings + priceSensitivity * PriceSensitivityWeight
                    - prospectTrust * TrustWeight - negotiationSkill,
                MinRequiredSavings, MaxRequiredSavings);

            float ceiling = current * (1f - requiredSavings);

            float marginPerMile = lane.MarginPerMile(offerRatePerMile);
            float weeklyMargin = lane.WeeklyMargin(offerRatePerMile);

            if (offerRatePerMile <= ceiling)
            {
                return new NegotiationResult(
                    NegotiationStatus.Accepted, offerRatePerMile, marginPerMile, weeklyMargin,
                    trustDelta: 0.08f, patienceDelta: 0.05f,
                    reason: "That works for us. Let's give it a shot.");
            }

            if (offerRatePerMile <= ceiling * CounterBandAbove)
            {
                float counter = Round2(ceiling);
                return new NegotiationResult(
                    NegotiationStatus.Countered, counter, marginPerMile, weeklyMargin,
                    trustDelta: -0.05f, patienceDelta: -0.10f,
                    reason: $"That's a little rich. I could do ${counter:0.00}/mile.");
            }

            return new NegotiationResult(
                NegotiationStatus.Rejected, 0f, marginPerMile, weeklyMargin,
                trustDelta: -0.12f, patienceDelta: -0.18f,
                reason: "That's way off from what we pay. I don't think this is a fit.");
        }

        private static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);

        private static float Round2(float v) => (float)System.Math.Round(v, 2);
    }
}
