using Fitzmark.BDRSim.Data;
using UnityEngine;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// Builds a stylized 3D BDR figure out of Unity primitives, colored from
    /// <see cref="AvatarPalette"/> per an <see cref="AvatarConfig"/>. It needs no
    /// imported art, so character customization works immediately. A gentle idle
    /// sway + bob sells the "3D + animated" feel.
    ///
    /// Upgrade path to AAA: replace <see cref="Build"/> with an instantiated
    /// rigged prefab and drive a real Animator — <see cref="SetConfig"/> stays the
    /// public seam the rest of the game talks to.
    /// </summary>
    public class AvatarBuilder : MonoBehaviour
    {
        [SerializeField] private float swayDegrees = 22f;
        [SerializeField] private float swaySpeed = 0.8f;
        [SerializeField] private float bobHeight = 0.02f;

        /// <summary>When false, the default idle sway is off (e.g. fighters drive their own motion).</summary>
        public bool AnimateIdle = true;

        private Transform _pivot;
        private AvatarConfig _config;
        private float _baseY;

        public void SetConfig(AvatarConfig config)
        {
            _config = config != null ? config.Clone() : new AvatarConfig();
            Build();
        }

        private void Update()
        {
            if (_pivot == null || !AnimateIdle) return;
            float t = Time.time;
            _pivot.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * swaySpeed) * swayDegrees, 0f);
            var p = _pivot.localPosition;
            p.y = _baseY + Mathf.Sin(t * 2f) * bobHeight;
            _pivot.localPosition = p;
        }

        private void Build()
        {
            if (_pivot == null)
            {
                _pivot = new GameObject("Pivot").transform;
                _pivot.SetParent(transform, false);
                _baseY = 0f;
            }
            else
            {
                for (int i = _pivot.childCount - 1; i >= 0; i--)
                    Destroy(_pivot.GetChild(i).gameObject);
            }

            float buildWidth = _config.build switch { 0 => 0.85f, 2 => 1.22f, _ => 1.0f };
            float h = Mathf.Clamp(_config.height, 0.9f, 1.12f);
            _pivot.localScale = new Vector3(1f, h, 1f);

            Color skin = AvatarPalette.Skin(_config.skinTone);
            Color outfit = AvatarPalette.Outfit(_config.outfitColor);
            Color accent = AvatarPalette.Accent(_config.accentColor);
            Color hair = AvatarPalette.Hair(_config.hairColor);
            Color trousers = outfit * 0.55f; trousers.a = 1f;

            // Legs
            Part("LegL", PrimitiveType.Capsule, new Vector3(-0.13f, 0.45f, 0f),
                new Vector3(0.18f, 0.45f, 0.18f), trousers);
            Part("LegR", PrimitiveType.Capsule, new Vector3(0.13f, 0.45f, 0f),
                new Vector3(0.18f, 0.45f, 0.18f), trousers);

            // Torso
            Part("Torso", PrimitiveType.Cube, new Vector3(0f, 1.15f, 0f),
                new Vector3(0.52f * buildWidth, 0.72f, 0.30f), outfit);

            // Tie / lanyard accent
            Part("Accent", PrimitiveType.Cube, new Vector3(0f, 1.12f, 0.16f),
                new Vector3(0.08f, 0.46f, 0.03f), accent);

            // Arms
            Part("ArmL", PrimitiveType.Capsule, new Vector3(-0.34f * buildWidth, 1.18f, 0f),
                new Vector3(0.13f, 0.4f, 0.13f), outfit);
            Part("ArmR", PrimitiveType.Capsule, new Vector3(0.34f * buildWidth, 1.18f, 0f),
                new Vector3(0.13f, 0.4f, 0.13f), outfit);

            // Hands
            Part("HandL", PrimitiveType.Sphere, new Vector3(-0.34f * buildWidth, 0.92f, 0f),
                new Vector3(0.14f, 0.14f, 0.14f), skin);
            Part("HandR", PrimitiveType.Sphere, new Vector3(0.34f * buildWidth, 0.92f, 0f),
                new Vector3(0.14f, 0.14f, 0.14f), skin);

            // Head + hair
            Part("Head", PrimitiveType.Sphere, new Vector3(0f, 1.7f, 0f),
                new Vector3(0.32f, 0.34f, 0.32f), skin);
            Part("Hair", PrimitiveType.Sphere, new Vector3(0f, 1.8f, -0.02f),
                new Vector3(0.36f, 0.22f, 0.36f), hair);
        }

        private void Part(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(_pivot, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = MakeMaterial(color);
        }

        private static Material MakeMaterial(Color color) => MaterialLibrary.Get(color);
    }
}
