using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// A simple smoothed chase camera at a fixed world-space offset (a gentle
    /// isometric angle). Keeping the angle constant makes on-foot WASD movement
    /// predictable in the greybox city.
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        public Transform Target;
        public Vector3 Offset = new Vector3(0f, 9f, -9f);
        public float FollowLerp = 8f;
        public float LookHeight = 1.2f;

        private void LateUpdate()
        {
            if (Target == null) return;

            Vector3 desired = Target.position + Offset;
            float t = 1f - Mathf.Exp(-FollowLerp * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);
            transform.LookAt(Target.position + Vector3.up * LookHeight);
        }
    }
}
