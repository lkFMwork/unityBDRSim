using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// A 2D trigger object: enemy (patrols), coin/heart (spin-bob), spring, or goal. The spawner
    /// adds a KINEMATIC Rigidbody2D so moving the collider (patrolling enemies) stays cheap, and
    /// all spin/flip animates a child VISUAL transform so the collider's shape never changes —
    /// both avoid the per-frame static-collider tree rebuilds that stutter Unity 2D physics.
    /// </summary>
    public class Platformer2DProp : MonoBehaviour
    {
        public PlatformerProp.Kind kind = PlatformerProp.Kind.Coin;
        public float minX, maxX, speed = 2f;
        public Transform visual;          // sprite child, animated without touching the collider

        private int _dir = 1;
        private float _bobT;
        private Vector3 _visBase = Vector3.one;

        private void Start() { if (visual != null) _visBase = visual.localScale; }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (kind == PlatformerProp.Kind.Enemy)
            {
                var p = transform.position;
                p.x += _dir * speed * dt;
                if (p.x >= maxX) { p.x = maxX; _dir = -1; }
                else if (p.x <= minX) { p.x = minX; _dir = 1; }
                transform.position = p;
                if (visual != null) { var s = _visBase; s.x = Mathf.Abs(_visBase.x) * _dir; visual.localScale = s; }
            }
            else if (kind == PlatformerProp.Kind.Coin || kind == PlatformerProp.Kind.Heart)
            {
                _bobT += dt * 3f;
                if (visual != null) { var s = _visBase; s.x = _visBase.x * (Mathf.Cos(_bobT) * 0.9f + 0.1f); visual.localScale = s; }
            }
        }
    }
}
