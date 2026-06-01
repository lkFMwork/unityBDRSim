using UnityEngine;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// A self-contained training exercise: the prospect to call, the pre-call
    /// briefing the rep sees, and the success bar (a weekly gross-margin target on
    /// the primary lane). Authored as an asset and discovered at runtime from a
    /// Resources/Scenarios folder.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Scenario_",
        menuName = "Fitzmark BDR/Scenario",
        order = 0)]
    public class ScenarioDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string scenarioId = "scenario-id";
        public string title = "Untitled Cold Call";
        public DifficultyTier difficulty = DifficultyTier.Easy;

        [Header("Briefing")]
        [TextArea(3, 8)]
        [Tooltip("What the rep knows before dialing. Shown on the pre-call screen.")]
        public string briefing =
            "You're cold-calling a shipper to open a new account. " +
            "Build rapport, run discovery, and earn a first load.";

        [Header("Prospect")]
        public ProspectProfile prospect;

        [Header("Call Setup")]
        [Tooltip("If true, the rep must get past a gatekeeper before reaching the prospect.")]
        public bool gatekeeperPresent = false;

        [Tooltip("When this is an in-person visit to a specific city company, its id — so the close " +
                 "advances that company's relationship and brands the managed account.")]
        public string localCompanyId = "";

        [Tooltip("A high-stakes 'key account': tougher gatekeeper, bigger book, bonus rewards.")]
        public bool isKeyAccount = false;

        [Header("Success Bar")]
        [Tooltip("Weekly gross-margin target (on the primary lane) considered a strong win.")]
        [Min(0f)] public float targetWeeklyMargin = 400f;

        /// <summary>Convenience pass-through to the prospect's primary lane.</summary>
        public Lane PrimaryLane => prospect != null ? prospect.PrimaryLane : null;

        public bool IsValid => prospect != null && prospect.PrimaryLane != null;
    }
}
