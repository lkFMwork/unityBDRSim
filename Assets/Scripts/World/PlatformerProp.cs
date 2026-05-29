using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// A trigger object in a platformer level: a patrolling enemy, a collectible
    /// coin, or the goal. Kept as one small component so the generator can stamp
    /// out levels easily. The player controller reads <see cref="kind"/> on contact.
    /// </summary>
    public class PlatformerProp : MonoBehaviour
    {
        public enum Kind { Enemy, Coin, Goal }

        public Kind kind = Kind.Enemy;
        public float minX;
        public float maxX;
        public float speed = 2.2f;

        private int _dir = 1;

        private void Update()
        {
            if (kind == Kind.Enemy)
            {
                var p = transform.position;
                p.x += _dir * speed * Time.deltaTime;
                if (p.x >= maxX) { p.x = maxX; _dir = -1; }
                else if (p.x <= minX) { p.x = minX; _dir = 1; }
                transform.position = p;
            }
            else if (kind == Kind.Coin)
            {
                transform.Rotate(0f, 200f * Time.deltaTime, 0f);
            }
        }
    }
}
