using UnityEngine;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// Procedural fighter visuals driven in code (no rig/clips): idle bounce, a
    /// lunge on attack, knockback + red flash on a hit, a guard lean while blocking,
    /// and victory/defeat poses. Animates <b>localPosition</b> so it can sit on a
    /// moving (walking) fighter root and still lunge/recoil relative to it. Swap in a
    /// real Animator later and keep these calls.
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
        private bool _blocking;
        private Renderer[] _renderers;
        private Color[] _baseColors;
        private bool _ready;

        private void Start()
        {
            _home = transform.localPosition;
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
        public void SetBlocking(bool blocking) { _blocking = blocking; }

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
                    if (_blocking) { pos.x -= dir * 0.14f; pos.y += 0.02f; } // guard lean back
                    else pos.y += bob;
                    break;
                case St.Attack:
                {
                    const float dur = 0.22f;
                    float p = Mathf.Sin(Mathf.Clamp01(_t / dur) * Mathf.PI);
                    pos.x += dir * 0.6f * p;
                    if (_t >= dur) _state = St.Idle;
                    break;
                }
                case St.Hit:
                {
                    const float dur = 0.28f;
                    float p = Mathf.Sin(Mathf.Clamp01(_t / dur) * Mathf.PI);
                    pos.x -= dir * 0.35f * p;
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
                    break;
                }
            }
            transform.localPosition = pos;

            _flash = Mathf.MoveTowards(_flash, 0f, dt * 3.5f);
            for (int i = 0; i < _renderers.Length; i++)
            {
                var m = _renderers[i].sharedMaterial;
                if (m != null) m.color = Color.Lerp(_baseColors[i], Color.red, _flash * 0.7f);
            }
        }
    }
}
