using UnityEngine;
using Fitzmark.BDRSim.UI;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// A visible special-move projectile (a glowing "pitch" orb) that travels horizontally
    /// from the attacker and damages the opponent on contact. This is the special you can
    /// see fly across the arena — fired by <see cref="Fighter"/> on a back+Special input.
    /// </summary>
    public class FightProjectile : MonoBehaviour
    {
        private Fighter _owner;
        private int _dir;
        private float _damage;
        private float _stun;
        private float _life = 2.2f;
        private const float Speed = 9f;

        public static FightProjectile Spawn(Fighter owner, int dir, float damage, float stun)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile";
            go.transform.position = owner.transform.position + new Vector3(dir * 0.9f, 1.1f, 0f);
            go.transform.localScale = Vector3.one * 0.55f;
            var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);

            var mat = MaterialLibrary.Get(new Color(1f, 0.7f, 0.2f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.15f) * 3f);
            var r = go.GetComponent<Renderer>(); if (r != null) r.sharedMaterial = mat;

            var p = go.AddComponent<FightProjectile>();
            p._owner = owner; p._dir = dir; p._damage = damage; p._stun = stun;
            return p;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _life -= dt;
            transform.position += new Vector3(_dir * Speed * dt, 0f, 0f);
            transform.Rotate(0f, 0f, 540f * dt);

            var foe = _owner != null ? _owner.Opponent : null;
            if (foe != null && !foe.IsKO)
            {
                Vector3 d = foe.transform.position + Vector3.up * 1.1f - transform.position;
                if (d.sqrMagnitude < 1.0f)
                {
                    foe.ReceiveProjectile(_damage, _dir, _stun);
                    Destroy(gameObject);
                    return;
                }
            }
            if (_life <= 0f || Mathf.Abs(transform.position.x) > 9f) Destroy(gameObject);
        }
    }
}
