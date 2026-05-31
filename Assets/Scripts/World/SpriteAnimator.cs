using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Drives a 2D character SpriteRenderer: cycles walk frames, holds idle/jump frames,
    /// flips by facing, and does a quick land-squash. Frames are resolved from
    /// <see cref="SpriteLibrary"/> by key so they're placeholder squares until the Kenney
    /// Pixel Platformer sprites are imported, then real animation with zero code change.
    /// </summary>
    public class SpriteAnimator : MonoBehaviour
    {
        private enum St { Idle, Walk, Jump }

        public float frameRate = 10f;
        public Color tint = new Color(0.55f, 0.5f, 0.9f);

        private SpriteRenderer _sr;
        private Sprite[] _walk;
        private Sprite _idle, _jump;
        private St _state = St.Idle;
        private float _t;
        private int _facing = 1;
        private float _squash;

        /// <summary>Configure with frame keys (idle, jump, and walk cycle).</summary>
        public void Setup(SpriteRenderer sr, string idleKey, string jumpKey, string[] walkKeys, Color tintColor)
        {
            _sr = sr;
            tint = tintColor;
            _idle = SpriteLibrary.Get(idleKey, tint);
            _jump = SpriteLibrary.GetAny(tint, jumpKey, idleKey);
            _walk = new Sprite[walkKeys.Length];
            for (int i = 0; i < walkKeys.Length; i++) _walk[i] = SpriteLibrary.Get(walkKeys[i], tint);
            // If only placeholders exist, tint the renderer; real sprites carry their own color.
            _sr.color = SpriteLibrary.Has(idleKey) ? Color.white : tint;
            _sr.sprite = _idle;
            _sr.sortingOrder = 10;
        }

        public void PlayIdle() => _state = St.Idle;
        public void PlayWalk() => _state = St.Walk;
        public void PlayJump() => _state = St.Jump;
        public void Squash() => _squash = 1f;
        public void SetFacing(int dir) { if (dir != 0) _facing = dir; }

        private void Update()
        {
            if (_sr == null) return;
            float dt = Time.deltaTime;

            switch (_state)
            {
                case St.Walk:
                    _t += dt * frameRate;
                    if (_walk != null && _walk.Length > 0)
                        _sr.sprite = _walk[Mathf.FloorToInt(_t) % _walk.Length];
                    break;
                case St.Jump:
                    _sr.sprite = _jump;
                    break;
                default:
                    _sr.sprite = _idle;
                    break;
            }

            // Facing flip + land squash on the local transform.
            _squash = Mathf.MoveTowards(_squash, 0f, dt * 5f);
            float sx = _facing * (1f + _squash * 0.25f);
            float sy = 1f - _squash * 0.3f;
            transform.localScale = new Vector3(sx, sy, 1f);
        }
    }
}
