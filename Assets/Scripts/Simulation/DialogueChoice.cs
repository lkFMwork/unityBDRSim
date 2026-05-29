using Fitzmark.BDRSim.Data;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>
    /// One selectable thing the rep can say at a given moment. Choices are plain
    /// data produced by <see cref="DialogueLibrary"/>; applying one mutates the
    /// <see cref="ProspectState"/> and the <see cref="Scorecard"/> via
    /// <see cref="CallSession.Choose"/>.
    /// </summary>
    public class DialogueChoice
    {
        /// <summary>What the rep says (button label / transcript line).</summary>
        public string Text;

        /// <summary>The prospect's / narrator's reply shown after the choice.</summary>
        public string Response;

        /// <summary>Coaching tier for this option.</summary>
        public ChoiceQuality Quality = ChoiceQuality.Adequate;

        /// <summary>Which score bucket this choice contributes to.</summary>
        public ScoreCategory Category = ScoreCategory.Rapport;

        /// <summary>Points awarded toward <see cref="Category"/> (can be negative).</summary>
        public float ScoreValue;

        /// <summary>Change to prospect trust in [-1, 1] space.</summary>
        public float TrustDelta;

        /// <summary>Change to prospect patience in [-1, 1] space.</summary>
        public float PatienceDelta;

        /// <summary>If set, the call advances to this stage after the choice resolves.</summary>
        public CallStage? AdvanceTo;

        /// <summary>If set, the prospect raises this objection after the choice.</summary>
        public ObjectionType? RaisesObjection;

        /// <summary>If set, this choice resolves (clears) the given active objection.</summary>
        public ObjectionType? ResolvesObjection;

        /// <summary>If true this is a rate offer and <see cref="OfferRatePerMile"/> is used.</summary>
        public bool IsRateOffer;

        /// <summary>Rate per mile offered to the prospect (only when <see cref="IsRateOffer"/>).</summary>
        public float OfferRatePerMile;

        /// <summary>If true, choosing this ends the call (used by the Wrap stage).</summary>
        public bool EndsCall;
    }
}
