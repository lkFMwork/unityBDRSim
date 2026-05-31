using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// A 2D trigger object in the sprite platformer: enemy (patrols), coin/heart (spin-bob),
    /// spring, or goal flag. The 2D player controller reads <see cref="kind"/> on contact.
    /// Reuses <see cref="PlatformerProp.Kind"/> so difficulty/section logic is shared.
    /// </summary>
    public class Platformer2DProp : MonoBehaviour
    {
        public PlatformerProp.Kind kind = PlatformerProp.Kind.Coin;
        public float minX, maxX, speed = 2f;

        private int _dir = 1;
        private float _bobT;

        private void Update()
        {
            if (kind == PlatformerProp.Kind.Enemy)
            {
                var p = transform.position;
                p.x += _dir * speed * Time.deltaTime;
                if (p.x >= maxX) { p.x = maxX; _dir = -1; }
                else if (p.x <= minX) { p.x = minX; _dir = 1; }
                transform.position = p;
                var s = transform.localScale; s.x = Mathf.Abs(s.x) * _dir; transform.localScale = s;
            }
            else if (kind == PlatformerProp.Kind.Coin || kind == PlatformerProp.Kind.Heart)
            {
                _bobT += Time.deltaTime * 3f;
                var p = transform.position;
                transform.position = new Vector3(p.x, p.y, 0f);
                transform.localScale = new Vector3(Mathf.Cos(_bobT) * 0.9f + 0.1f, 1f, 1f); // spin
            }
        }
    }
}
