using UnityEngine;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// Drives an old-school-fighter feel on a procedural avatar with pure code: an
    /// idle bounce, a lunge on attack, a knockback + red flash on a hit, and
    /// victory/defeat poses. No rig or animation clips — swap in a real Animator
    /// later and keep these same calls.
    /// </summary>
    public class FighterRig : MonoBehaviour
    {
        public bool facingRight = true;
        public float bobAmplitude = 0.05f;
        public float bobSpeed = 4.5f;

        private enum St { Idle, Attack, Hit, Victory, Defeat }
        private St _state = St.Idle;
        private float _t;
        private Vector3 _home;
        private float _flash;
        private Renderer[] _renderers;
        private Color[] _baseColors;
        private bool _ready;

        private void Start()
        {
            _home = transform.position;
            _renderers = GetComponentsInChildren<Renderer>();
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                var m = _renderers[i].sharedMaterial;
                _baseColors[i] = m != null ? m.color : Color.white;
            }
            _ready = true;
        }

        public void Attack() { _state = St.Attack; _t = 0f; }
        public void TakeHit() { _state = St.Hit; _t = 0f; _flash = 1f; }
        public void Victory() { _state = St.Victory; _t = 0f; }
        public void Defeat() { _state = St.Defeat; _t = 0f; }

        private void Update()
        {
            if (!_ready) return;
            float dt = Time.deltaTime;
            _t += dt;
            float dir = facingRight ? 1f : -1f;
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;

            Vector3 pos = _home;
            switch (_state)
            {
                case St.Idle:
                    pos.y += bob;
                    break;
                case St.Attack:
                {
                    const float dur = 0.25f;
                    float p = Mathf.Sin(Mathf.Clamp01(_t / dur) * Mathf.PI);
                    pos.x += dir * 0.7f * p;
                    pos.y += bob * 0.5f;
                    if (_t >= dur) _state = St.Idle;
                    break;
                }
                case St.Hit:
                {
                    const float dur = 0.3f;
                    float p = Mathf.Sin(Mathf.Clamp01(_t / dur) * Mathf.PI);
                    pos.x -= dir * 0.45f * p;
                    pos.y += Mathf.Sin(_t * 60f) * 0.03f * (1f - Mathf.Clamp01(_t / dur));
                    if (_t >= dur) _state = St.Idle;
                    break;
                }
                case St.Victory:
                    pos.y += Mathf.Abs(Mathf.Sin(Time.time * 8f)) * 0.12f;
                    break;
                case St.Defeat:
                {
                    float sink = Mathf.Min(_t / 0.5f, 1f);
                    pos.y -= sink * 0.5f;
                    pos.x -= dir * sink * 0.3f;
                    break;
                }
            }
            transform.position = pos;

            _flash = Mathf.MoveTowards(_flash, 0f, dt * 3.5f);
            ApplyFlash();
        }

        private void ApplyFlash()
        {
            if (_renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                var m = _renderers[i].sharedMaterial;
                if (m != null) m.color = Color.Lerp(_baseColors[i], Color.red, _flash * 0.7f);
            }
        }
    }
}
