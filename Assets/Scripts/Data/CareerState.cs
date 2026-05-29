using System;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// The player's career meta-progress: which workday they're on, how many
    /// calls they have left today, and progress toward the week's quota.
    /// Serialized as part of <see cref="BDRCharacter"/>.
    /// </summary>
    [Serializable]
    public class CareerState
    {
        public bool initialized;
        public int day = 1;
        public int callsRemainingToday = 5;
        public int weekDealsWon = 0;
        public int weekDealsGoal = 3;

        // Multi-channel outreach: touches you can spend warming leads each day.
        public int outreachRemainingToday = 8;
    }
}
