using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Arcade, transform-driven car: forward/back on the vertical axis, steer on
    /// the horizontal axis. Kinematic on purpose — no Rigidbody/WheelCollider to
    /// tune — which keeps it rock-solid for a greybox. (It glides over scenery;
    /// real collisions are a later polish pass.)
    /// </summary>
    public class CarController : MonoBehaviour
    {
        public float Speed = 16f;
        public float TurnSpeed = 85f;

        private bool _driving;

        public void SetDriving(bool value) => _driving = value;

        private void Update()
        {
            if (!_driving) return;

            float throttle = Input.GetAxis("Vertical");
            float steer = Input.GetAxis("Horizontal");

            transform.Translate(Vector3.forward * throttle * Speed * Time.deltaTime, Space.Self);
            // steer scales a little with speed so it feels like driving, not spinning in place
            float steerFactor = Mathf.Clamp(Mathf.Abs(throttle) + 0.35f, 0f, 1f) * Mathf.Sign(throttle == 0 ? 1f : throttle);
            transform.Rotate(0f, steer * TurnSpeed * steerFactor * Time.deltaTime, 0f, Space.Self);
        }
    }
}
