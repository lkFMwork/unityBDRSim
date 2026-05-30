using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Ambient pedestrian: strolls to random points within a radius, faces where it's
    /// going, pauses, repeats — driving its rigged avatar's walk animation while moving.
    /// Pure transform movement (no physics) so it's cheap background life for the hubs.
    /// </summary>
    public class WanderingNpc : MonoBehaviour
    {
        public Vector3 center;
        public float radius = 12f;
        public float speed = 2.2f;

        private Fitzmark.BDRSim.UI.AvatarBuilder _avatar; // procedural fallback
        private OfficeWorker _worker;                     // real Mixamo body, when present
        private Vector3 _target;
        private float _pause;

        private void Start()
        {
            _avatar = GetComponentInChildren<Fitzmark.BDRSim.UI.AvatarBuilder>();
            _worker = GetComponentInChildren<OfficeWorker>();
            PickTarget();
        }

        private void SetWalk(float amount)
        {
            if (_worker != null) _worker.SetWalk(amount);
            else if (_avatar != null) _avatar.SetWalk(amount);
        }

        private void PickTarget()
        {
            Vector2 p = Random.insideUnitCircle * radius;
            _target = center + new Vector3(p.x, 0f, p.y);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (_pause > 0f)
            {
                _pause -= dt;
                SetWalk(0f);
                return;
            }

            Vector3 pos = transform.position;
            Vector3 to = _target - pos;
            to.y = 0f;
            float dist = to.magnitude;

            if (dist < 0.3f)
            {
                _pause = Random.Range(0.8f, 2.6f);
                PickTarget();
                SetWalk(0f);
                return;
            }

            Vector3 dir = to / dist;
            transform.position = pos + dir * speed * dt;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * dt);
            SetWalk(1f);
        }
    }
}
