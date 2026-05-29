using System.Collections.Generic;
using Fitzmark.BDRSim.Data;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>
    /// Mutable runtime state of the prospect during a single call: how warm they
    /// are (trust), how close they are to ending the call (patience), which
    /// objections have surfaced, and any rate they've agreed to. Seeded from a
    /// <see cref="ProspectProfile"/>.
    /// </summary>
    public class ProspectState
    {
        public readonly ProspectProfile Profile;

        /// <summary>0 = guarded, 1 = fully bought-in.</summary>
        public float Trust { get; private set; }

        /// <summary>0 = about to hang up, 1 = relaxed.</summary>
        public float Patience { get; private set; }

        public CallStage Stage { get; set; } = CallStage.Opening;

        public ObjectionType? ActiveObjection { get; private set; }

        private readonly HashSet<ObjectionType> _raised = new();
        private readonly HashSet<ObjectionType> _handled = new();

        public bool DealAgreed { get; private set; }
        public float AgreedRatePerMile { get; private set; }

        public ProspectState(ProspectProfile profile)
        {
            Profile = profile;
            Trust = profile != null ? profile.startingTrust : 0.35f;
            Patience = profile != null ? profile.startingPatience : 0.7f;
        }

        public void AdjustTrust(float delta) => Trust = Clamp01(Trust + delta);

        public void AdjustPatience(float delta) => Patience = Clamp01(Patience + delta);

        /// <summary>True once patience is exhausted — the prospect is hanging up.</summary>
        public bool IsBailing => Patience <= 0.001f;

        public bool HasRaised(ObjectionType type) => _raised.Contains(type);
        public bool HasHandled(ObjectionType type) => _handled.Contains(type);

        public void RaiseObjection(ObjectionType type)
        {
            _raised.Add(type);
            ActiveObjection = type;
        }

        public void ResolveActiveObjection()
        {
            if (ActiveObjection.HasValue)
                _handled.Add(ActiveObjection.Value);
            ActiveObjection = null;
        }

        public void AgreeToDeal(float ratePerMile)
        {
            DealAgreed = true;
            AgreedRatePerMile = ratePerMile;
        }

        /// <summary>A short human-readable mood label for the HUD.</summary>
        public string MoodLabel
        {
            get
            {
                if (IsBailing) return "Hanging up";
                float blend = (Trust + Patience) * 0.5f;
                if (blend >= 0.75f) return "Engaged";
                if (blend >= 0.5f) return "Receptive";
                if (blend >= 0.3f) return "Guarded";
                return "Cold";
            }
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
