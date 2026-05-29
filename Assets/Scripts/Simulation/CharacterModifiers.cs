using Fitzmark.BDRSim.Data;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>
    /// Translates a BDR's <see cref="BDRAttributes"/> into concrete effects on a
    /// call. Attribute 5 is neutral (no effect); every point above or below shifts
    /// the corresponding lever. This is where "your build matters" is made real,
    /// and it's pure data so it can be unit-tested.
    /// </summary>
    public class CharacterModifiers
    {
        /// <summary>Added to the prospect's starting trust (0..1 space).</summary>
        public float TrustBonus;

        /// <summary>Multiplier on patience *losses* (&lt;1 means you wear down slower).</summary>
        public float PatienceDrainMultiplier = 1f;

        /// <summary>Extra savings headroom in negotiation (lets you hold a higher rate).</summary>
        public float NegotiationSkill;

        private float _rapportBonus;
        private float _discoveryBonus;
        private float _valueBonus;
        private float _objectionBonus;

        /// <summary>Point bonus applied when a good play scores in the given category.</summary>
        public float BonusFor(ScoreCategory category) => category switch
        {
            ScoreCategory.Rapport => _rapportBonus,
            ScoreCategory.Discovery => _discoveryBonus,
            ScoreCategory.ValueArticulation => _valueBonus,
            ScoreCategory.ObjectionHandling => _objectionBonus,
            _ => 0f
        };

        /// <summary>A no-op modifier set (used when there's no character).</summary>
        public static CharacterModifiers Neutral => new CharacterModifiers();

        public static CharacterModifiers FromAttributes(BDRAttributes a)
        {
            if (a == null) return Neutral;

            int cha = a.charisma - BDRAttributes.Baseline;
            int neg = a.negotiation - BDRAttributes.Baseline;
            int pk = a.productKnowledge - BDRAttributes.Baseline;
            int res = a.resilience - BDRAttributes.Baseline;
            int pro = a.prospecting - BDRAttributes.Baseline;

            return new CharacterModifiers
            {
                TrustBonus = Clamp(cha * 0.03f, -0.20f, 0.20f),
                PatienceDrainMultiplier = Clamp(1f - res * 0.06f, 0.55f, 1.45f),
                NegotiationSkill = Clamp(neg * 0.012f, -0.06f, 0.06f),
                _rapportBonus = cha * 0.4f,
                _discoveryBonus = pro * 0.4f,
                _valueBonus = pk * 0.4f,
                _objectionBonus = pk * 0.4f,
            };
        }

        private static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
    }
}
