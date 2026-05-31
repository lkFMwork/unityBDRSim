using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Arcade, transform-driven car: forward/back on the vertical axis, steer on
    /// the horizontal axis. Kinematic on purpose — no Rigidbody/WheelCollider to
    /// tune — which keeps it rock-solid for a greybox. Follows the generated terrain
    /// by sampling the ground each frame so it hugs hills instead of clipping through.
    /// </summary>
    public class CarController : MonoBehaviour
    {
        public float Speed = 16f;
        public float TurnSpeed = 85f;
        public float RideHeight = 0.2f; // metres the wheels sit above the ground

        private bool _driving;
        private CityTerrain _terrain;

        public void SetDriving(bool value) => _driving = value;

        /// <summary>Give the car the city terrain so it rides the surface (set by the city builder).</summary>
        public void SetTerrain(CityTerrain terrain) => _terrain = terrain;

        private void Update()
        {
            if (_driving)
            {
                float throttle = Input.GetAxis("Vertical");
                float steer = Input.GetAxis("Horizontal");

                transform.Translate(Vector3.forward * throttle * Speed * Time.deltaTime, Space.Self);
                // steer scales a little with speed so it feels like driving, not spinning in place
                float steerFactor = Mathf.Clamp(Mathf.Abs(throttle) + 0.35f, 0f, 1f) * Mathf.Sign(throttle == 0 ? 1f : throttle);
                transform.Rotate(0f, steer * TurnSpeed * steerFactor * Time.deltaTime, 0f, Space.Self);
            }

            // Hug the terrain (whether parked or driving) so the car never floats or sinks.
            if (_terrain != null)
            {
                var p = transform.position;
                float groundY = _terrain.SampleHeight(p.x, p.z) + RideHeight;
                // ease so bumps aren't jarring
                p.y = Mathf.Lerp(p.y, groundY, 12f * Time.deltaTime);
                transform.position = p;
            }
        }
    }
}
