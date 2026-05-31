using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Platformer reachability math — the genre's cardinal rule: derive level geometry from
    /// the jump arc, never from magic numbers. Given the controller's run speed, jump speed
    /// and gravity, this computes the real limits a player can clear, so the level generator
    /// can guarantee every gap and every step is beatable.
    ///
    /// timeToApex = jumpSpeed / |gravity|
    /// maxJumpHeight = jumpSpeed² / (2|gravity|)
    /// maxJumpDistance = runSpeed × (2 × timeToApex)      (full hang time = up + down)
    ///
    /// Sources: error454 "3 fundamental equations of platformers", gamedeveloper.com
    /// "Designing a 2D Jump".
    /// </summary>
    public readonly struct JumpArc
    {
        public readonly float RunSpeed;
        public readonly float JumpSpeed;
        public readonly float Gravity;       // magnitude (positive)

        public JumpArc(float runSpeed, float jumpSpeed, float gravity)
        {
            RunSpeed = runSpeed;
            JumpSpeed = jumpSpeed;
            Gravity = Mathf.Abs(gravity);
        }

        public float TimeToApex => JumpSpeed / Gravity;
        public float MaxJumpHeight => (JumpSpeed * JumpSpeed) / (2f * Gravity);
        public float MaxJumpDistance => RunSpeed * (2f * TimeToApex);

        /// <summary>Safe gap width to clear comfortably (margin so timing needn't be frame-perfect).</summary>
        public float SafeGap(float margin = 0.75f) => MaxJumpDistance * margin;

        /// <summary>Safe step-up height to land on a higher platform (margin for the arc's width).</summary>
        public float SafeStepUp(float margin = 0.7f) => MaxJumpHeight * margin;

        /// <summary>Safe vertical spacing between stacked platforms you jump up through.</summary>
        public float SafePlatformRise(float margin = 0.6f) => MaxJumpHeight * margin;
    }
}
