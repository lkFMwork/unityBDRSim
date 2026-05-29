namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>
    /// The phases of a sales call, in the order a healthy call flows through.
    /// The <see cref="CallSession"/> is a small state machine over these.
    /// </summary>
    public enum CallStage
    {
        Opening,
        Gatekeeper,
        Discovery,
        ValuePitch,
        ObjectionHandling,
        Negotiation,
        Closing,
        Wrap,
        Completed
    }

    /// <summary>
    /// The dimensions a BDR is graded on. The post-call report breaks the score
    /// down by category and turns the weakest ones into coaching tips.
    /// </summary>
    public enum ScoreCategory
    {
        Rapport,
        Discovery,
        ValueArticulation,
        ObjectionHandling,
        Negotiation,
        Close
    }

    /// <summary>How a call ended.</summary>
    public enum CallOutcome
    {
        InProgress,
        WonCommitment,   // prospect tenders a first load / commits
        WonTrial,        // prospect agrees to a trial / next meeting
        NoSaleFollowUp,  // polite no, door left open
        Rejected,        // hard no
        HungUp           // prospect bailed (patience ran out)
    }

    /// <summary>Quality tier of a dialogue option, used for inline feedback.</summary>
    public enum ChoiceQuality
    {
        Strong,
        Adequate,
        Weak
    }
}
