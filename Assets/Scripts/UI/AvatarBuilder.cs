using Fitzmark.BDRSim.Data;
using UnityEngine;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// A procedurally <b>rigged</b> humanoid: a real bone skeleton (hips → spine →
    /// chest → head, plus two-segment arms and legs) with limb geometry parented to
    /// the bones, so joints actually articulate. Skeletal animation — idle breathing,
    /// a walk cycle, a talking gesture, and a celebrate — is computed in code and
    /// applied to the bone rotations. Everything is generated (no imported models, no
    /// licensing) and recolored per <see cref="AvatarConfig"/>, so every character in
    /// the game is a rigged, animated figure with zero asset imports. Drop in a
    /// purchased/Mixamo model later by swapping what <see cref="SetConfig"/> builds —
    /// the public seam stays the same.
    /// </summary>
    public class AvatarBuilder : MonoBehaviour
    {
        public enum Anim { Idle, Walk, Talk, Celebrate }

        /// <summary>When false, holds a neutral ready-stance instead of the idle loop.</summary>
        public bool AnimateIdle = true;

        private AvatarConfig _config;
        private Transform _root;
        private Anim _state = Anim.Idle;
        private float _walk;   // 0..1 locomotion intensity
        private float _phase;  // walk/celebrate cycle phase
        private float _t;      // general clock
        private float _baseY;

        // Animated bones
        private Transform _hips, _spine, _chest, _head;
        private Transform _armLU, _armLL, _armRU, _armRL;
        private Transform _legLU, _legLL, _legRU, _legRL;

        public void SetConfig(AvatarConfig config)
        {
            _config = config != null ? config.Clone() : new AvatarConfig();
            Build();
        }

        public void Play(Anim anim) => _state = anim;

        /// <summary>Drive the walk cycle from a controller's speed (0 = stand, 1 = full).</summary>
        public void SetWalk(float speed01)
        {
            _walk = Mathf.Clamp01(speed01);
            _state = _walk > 0.05f ? Anim.Walk : Anim.Idle;
        }

        private void Update()
        {
            if (_hips == null) return;
            float dt = Time.deltaTime;
            _t += dt;
            if (!AnimateIdle && _state == Anim.Idle) HoldRest(dt);
            else Animate(_state, dt);
        }

        // ---- build ----------------------------------------------------------

        private void Build()
        {
            if (_root != null) Destroy(_root.gameObject);
            _root = new GameObject("Rig").transform;
            _root.SetParent(transform, false);
            _root.localScale = new Vector3(1f, Mathf.Clamp(_config.height, 0.9f, 1.12f), 1f);
            _baseY = 0f;

            float thick = _config.build == 0 ? 0.85f : (_config.build == 2 ? 1.2f : 1f);

            Color skin = AvatarPalette.Skin(_config.skinTone);
            Color outfit = AvatarPalette.Outfit(_config.outfitColor);
            Color accent = AvatarPalette.Accent(_config.accentColor);
            Color hair = AvatarPalette.Hair(_config.hairColor);
            Color trousers = outfit * 0.55f; trousers.a = 1f;
            Color shoe = new Color(0.12f, 0.12f, 0.14f);

            // Skeleton
            _hips = Bone(_root, "Hips", new Vector3(0f, 0.88f, 0f));
            _spine = Bone(_hips, "Spine", new Vector3(0f, 0.16f, 0f));
            _chest = Bone(_spine, "Chest", new Vector3(0f, 0.20f, 0f));
            _head = Bone(_chest, "Head", new Vector3(0f, 0.30f, 0f));

            _armLU = Bone(_chest, "ArmL_U", new Vector3(-0.20f * thick, 0.16f, 0f));
            _armLL = Bone(_armLU, "ArmL_L", new Vector3(0f, -0.27f, 0f));
            _armRU = Bone(_chest, "ArmR_U", new Vector3(0.20f * thick, 0.16f, 0f));
            _armRL = Bone(_armRU, "ArmR_L", new Vector3(0f, -0.27f, 0f));

            _legLU = Bone(_hips, "LegL_U", new Vector3(-0.10f, -0.02f, 0f));
            _legLL = Bone(_legLU, "LegL_L", new Vector3(0f, -0.42f, 0f));
            _legRU = Bone(_hips, "LegR_U", new Vector3(0.10f, -0.02f, 0f));
            _legRL = Bone(_legRU, "LegR_L", new Vector3(0f, -0.42f, 0f));

            // Limb geometry (parented to bones, so they follow joint rotations)
            Limb(_chest, "Torso", PrimitiveType.Cube, new Vector3(0f, 0.06f, 0f), new Vector3(0.42f * thick, 0.50f, 0.24f * thick), outfit);
            Limb(_chest, "Tie", PrimitiveType.Cube, new Vector3(0f, 0.0f, 0.13f * thick), new Vector3(0.07f, 0.34f, 0.03f), accent);
            Limb(_hips, "Pelvis", PrimitiveType.Cube, new Vector3(0f, -0.05f, 0f), new Vector3(0.36f * thick, 0.22f, 0.24f * thick), trousers);
            Limb(_head, "Head", PrimitiveType.Sphere, new Vector3(0f, 0.04f, 0f), new Vector3(0.26f, 0.30f, 0.27f), skin);
            Limb(_head, "Hair", PrimitiveType.Sphere, new Vector3(0f, 0.12f, -0.01f), new Vector3(0.30f, 0.20f, 0.30f), hair);

            Limb(_armLU, "ArmL_U_m", PrimitiveType.Capsule, new Vector3(0f, -0.13f, 0f), new Vector3(0.13f, 0.16f, 0.13f), outfit);
            Limb(_armLL, "ArmL_L_m", PrimitiveType.Capsule, new Vector3(0f, -0.13f, 0f), new Vector3(0.115f, 0.15f, 0.115f), skin);
            Limb(_armRU, "ArmR_U_m", PrimitiveType.Capsule, new Vector3(0f, -0.13f, 0f), new Vector3(0.13f, 0.16f, 0.13f), outfit);
            Limb(_armRL, "ArmR_L_m", PrimitiveType.Capsule, new Vector3(0f, -0.13f, 0f), new Vector3(0.115f, 0.15f, 0.115f), skin);

            Limb(_legLU, "LegL_U_m", PrimitiveType.Capsule, new Vector3(0f, -0.20f, 0f), new Vector3(0.16f, 0.22f, 0.16f), trousers);
            Limb(_legLL, "LegL_L_m", PrimitiveType.Capsule, new Vector3(0f, -0.20f, 0f), new Vector3(0.14f, 0.21f, 0.14f), trousers);
            Limb(_legRU, "LegR_U_m", PrimitiveType.Capsule, new Vector3(0f, -0.20f, 0f), new Vector3(0.16f, 0.22f, 0.16f), trousers);
            Limb(_legRL, "LegR_L_m", PrimitiveType.Capsule, new Vector3(0f, -0.20f, 0f), new Vector3(0.14f, 0.21f, 0.14f), trousers);

            Limb(_legLL, "FootL", PrimitiveType.Cube, new Vector3(0f, -0.40f, 0.06f), new Vector3(0.14f, 0.08f, 0.26f), shoe);
            Limb(_legRL, "FootR", PrimitiveType.Cube, new Vector3(0f, -0.40f, 0.06f), new Vector3(0.14f, 0.08f, 0.26f), shoe);

            _state = Anim.Idle;
            _armLU.localRotation = ArmRest(-1f);
            _armRU.localRotation = ArmRest(1f);
        }

        private Transform Bone(Transform parent, string name, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            t.localRotation = Quaternion.identity;
            return t;
        }

        private void Limb(Transform bone, string name, PrimitiveType type, Vector3 center, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(bone, false);
            go.transform.localPosition = center;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = MaterialLibrary.Get(color);
        }

        // ---- animation ------------------------------------------------------

        private void Animate(Anim state, float dt)
        {
            float a = 1f - Mathf.Exp(-12f * dt); // rotation smoothing
            float bob = 0f;

            Quaternion hips = Q(0, 0, 0), spine = Q(0, 0, 0), chest = Q(0, 0, 0), head = Q(0, 0, 0);
            Quaternion aLU = ArmRest(-1f), aLL = Q(0, 0, 0), aRU = ArmRest(1f), aRL = Q(0, 0, 0);
            Quaternion lLU = Q(0, 0, 0), lLL = Q(0, 0, 0), lRU = Q(0, 0, 0), lRL = Q(0, 0, 0);

            switch (state)
            {
                case Anim.Walk:
                {
                    float s = Mathf.Max(0.4f, _walk);
                    _phase += dt * (5f + 5f * s);
                    float p = _phase, legSwing = 32f * s, armSwing = 26f * s, knee = 42f * s;
                    lLU = Q(Mathf.Sin(p) * legSwing, 0, 0);
                    lRU = Q(Mathf.Sin(p + Mathf.PI) * legSwing, 0, 0);
                    lLL = Q(Mathf.Max(0f, -Mathf.Sin(p)) * knee, 0, 0);
                    lRL = Q(Mathf.Max(0f, -Mathf.Sin(p + Mathf.PI)) * knee, 0, 0);
                    aLU = ArmRest(-1f) * Q(Mathf.Sin(p + Mathf.PI) * armSwing, 0, 0);
                    aRU = ArmRest(1f) * Q(Mathf.Sin(p) * armSwing, 0, 0);
                    aLL = Q(-18f, 0, 0); aRL = Q(-18f, 0, 0);
                    chest = Q(3f, Mathf.Sin(p) * 4f, 0);
                    bob = Mathf.Abs(Mathf.Sin(p)) * 0.04f * s;
                    break;
                }
                case Anim.Celebrate:
                {
                    _phase += dt * 8f;
                    float w = Mathf.Sin(_phase);
                    aLU = Q(-150f + w * 12f, 0f, 30f);
                    aRU = Q(-150f + w * 12f, 0f, -30f);
                    aLL = Q(-25f, 0, 0); aRL = Q(-25f, 0, 0);
                    head = Q(-8f, 0, 0);
                    bob = Mathf.Abs(Mathf.Sin(_phase)) * 0.12f;
                    break;
                }
                case Anim.Talk:
                {
                    float g = Mathf.Sin(_t * 4f);
                    chest = Q(0, Mathf.Sin(_t) * 3f, 0);
                    aRU = ArmRest(1f) * Q(-45f + g * 18f, 0f, -8f);
                    aRL = Q(-40f + g * 22f, 0, 0);
                    head = Q(Mathf.Sin(_t * 1.6f) * 3f, Mathf.Sin(_t * 1.1f) * 4f, 0);
                    break;
                }
                default: // Idle
                {
                    float s1 = Mathf.Sin(_t * 1.3f), s2 = Mathf.Sin(_t * 0.9f);
                    spine = Q(s1 * 1f, 0, 0);
                    chest = Q(1.5f + s1 * 1.5f, s2 * 3f, 0);
                    head = Q(s1 * 2f, s2 * 3f, 0);
                    aLU = ArmRest(-1f) * Q(s1 * 5f, 0, 0);
                    aRU = ArmRest(1f) * Q(s2 * 5f, 0, 0);
                    bob = Mathf.Sin(_t * 1.8f) * 0.012f;
                    break;
                }
            }

            Slerp(_hips, hips, a); Slerp(_spine, spine, a); Slerp(_chest, chest, a); Slerp(_head, head, a);
            Slerp(_armLU, aLU, a); Slerp(_armLL, aLL, a); Slerp(_armRU, aRU, a); Slerp(_armRL, aRL, a);
            Slerp(_legLU, lLU, a); Slerp(_legLL, lLL, a); Slerp(_legRU, lRU, a); Slerp(_legRL, lRL, a);

            SetBob(bob);
        }

        private void HoldRest(float dt)
        {
            float a = 1f - Mathf.Exp(-12f * dt);
            Slerp(_hips, Q(0, 0, 0), a); Slerp(_spine, Q(2, 0, 0), a); Slerp(_chest, Q(2, 0, 0), a); Slerp(_head, Q(0, 0, 0), a);
            Slerp(_armLU, ArmRest(-1f) * Q(10, 0, 0), a); Slerp(_armRU, ArmRest(1f) * Q(10, 0, 0), a);
            Slerp(_armLL, Q(-22, 0, 0), a); Slerp(_armRL, Q(-22, 0, 0), a);
            Slerp(_legLU, Q(0, 0, 0), a); Slerp(_legRU, Q(0, 0, 0), a); Slerp(_legLL, Q(0, 0, 0), a); Slerp(_legRL, Q(0, 0, 0), a);
            SetBob(0f);
        }

        private void SetBob(float bob)
        {
            if (_root == null) return;
            var p = _root.localPosition;
            p.y = _baseY + bob;
            _root.localPosition = p;
        }

        private static Quaternion Q(float x, float y, float z) => Quaternion.Euler(x, y, z);
        private static Quaternion ArmRest(float side) => Quaternion.Euler(0f, 0f, side * 8f);
        private static void Slerp(Transform t, Quaternion target, float a)
        {
            if (t != null) t.localRotation = Quaternion.Slerp(t.localRotation, target, a);
        }
    }
}
