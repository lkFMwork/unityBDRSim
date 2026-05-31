using System;
using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// True 2D platformer character (XY plane, sprite body). Run, variable-height jump with
    /// coyote time + input buffer, gravity, raycast ground/ceiling/wall collision against the
    /// tilemap layer, pits, and stomp-the-enemy. Jump physics are derived from a <see cref="JumpArc"/>
    /// so the level generator can guarantee reachability. Animates a child SpriteRenderer
    /// (walk frames, facing flip, squash on land). Movement is grid-friendly and pivot-free —
    /// which is exactly why 2D sidesteps the 3D model float/burial bugs.
    /// </summary>
    public class Platformer2DController : MonoBehaviour
    {
        // Design-first jump tuning (see JumpArc): pick height + apex time, derive the rest.
        public float runSpeed = 7f;
        public float maxJumpHeight = 3.2f;
        public float timeToApex = 0.38f;
        public float coyoteTime = 0.1f;
        public float jumpBuffer = 0.12f;
        public float killY = -8f;
        public int startLives = 3;
        public float halfWidth = 0.35f;
        public float halfHeight = 0.5f;

        public event Action Won;
        public event Action Failed;
        public event Action<int> LivesChanged;
        public event Action<int> CoinsChanged;

        public int Lives { get; private set; }
        public int Coins { get; private set; }
        public bool IsActive = true;

        public JumpArc Arc => new JumpArc(runSpeed, JumpVelocity, Gravity);
        public float Gravity => (2f * maxJumpHeight) / (timeToApex * timeToApex);
        public float JumpVelocity => (2f * maxJumpHeight) / timeToApex;

        private LayerMask _solidMask;
        private Vector2 _vel;
        private Vector3 _start;
        private SpriteAnimator _anim;
        private int _facing = 1;
        private float _coyote, _buffer, _invuln;
        private bool _grounded;
        private bool _ended;

        public void Init(Vector3 start, SpriteAnimator anim, LayerMask solidMask)
        {
            _start = start;
            transform.position = start;
            _anim = anim;
            _solidMask = solidMask;
            Lives = startLives;
            Coins = 0;
            LivesChanged?.Invoke(Lives);
            CoinsChanged?.Invoke(Coins);
        }

        private void Update()
        {
            if (!IsActive || _ended) return;
            float dt = Time.deltaTime;
            _invuln = Mathf.Max(0f, _invuln - dt);

            float h = Input.GetAxisRaw("Horizontal");
            _vel.x = h * runSpeed;
            if (Mathf.Abs(h) > 0.01f) _facing = h > 0 ? 1 : -1;

            if (_grounded) _coyote = coyoteTime; else _coyote = Mathf.Max(0f, _coyote - dt);
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W))
                _buffer = jumpBuffer;
            else _buffer = Mathf.Max(0f, _buffer - dt);

            if (_buffer > 0f && _coyote > 0f)
            {
                _vel.y = JumpVelocity;
                _buffer = 0f; _coyote = 0f;
                _anim?.PlayJump();
            }
            if ((Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W)) && _vel.y > 0f)
                _vel.y *= 0.5f; // variable-height jump

            _vel.y -= Gravity * dt;
            _vel.y = Mathf.Max(_vel.y, -Gravity * timeToApex * 2f); // terminal

            MoveCollide(dt);
            Animate(h);

            if (transform.position.y < killY) LoseLife(true);
        }

        // Axis-separated raycast collision against the solid tile layer (standard 2D approach).
        private void MoveCollide(float dt)
        {
            Vector3 p = transform.position;

            // Horizontal
            float dx = _vel.x * dt;
            if (Mathf.Abs(dx) > 0f)
            {
                int dir = dx > 0 ? 1 : -1;
                var hit = Physics2D.BoxCast(p, new Vector2(halfWidth * 2f, halfHeight * 1.8f), 0f,
                    Vector2.right * dir, Mathf.Abs(dx) + 0.02f, _solidMask);
                if (hit.collider != null) dx = (hit.distance - 0.02f) * dir;
                p.x += dx;
            }

            // Vertical
            float dy = _vel.y * dt;
            _grounded = false;
            if (Mathf.Abs(dy) > 0f)
            {
                int dir = dy > 0 ? 1 : -1;
                var hit = Physics2D.BoxCast(p, new Vector2(halfWidth * 1.8f, halfHeight * 2f), 0f,
                    Vector2.up * dir, Mathf.Abs(dy) + 0.02f, _solidMask);
                if (hit.collider != null)
                {
                    dy = (hit.distance - 0.02f) * dir;
                    if (dir < 0) { _grounded = true; if (_vel.y < -6f) _anim?.Squash(); }
                    _vel.y = 0f;
                }
                p.y += dy;
            }
            else
            {
                // Probe just below for grounded state when standing still.
                var hit = Physics2D.BoxCast(p, new Vector2(halfWidth * 1.8f, halfHeight * 2f), 0f,
                    Vector2.down, 0.06f, _solidMask);
                _grounded = hit.collider != null;
            }

            p.z = 0f;
            transform.position = p;
        }

        private void Animate(float h)
        {
            if (_anim == null) return;
            _anim.SetFacing(_facing);
            if (!_grounded) _anim.PlayJump();
            else if (Mathf.Abs(h) > 0.01f) _anim.PlayWalk();
            else _anim.PlayIdle();
        }

        private void OnTriggerEnter2D(Collider2D other) => Touch(other);
        private void OnTriggerStay2D(Collider2D other) => Touch(other);

        private void Touch(Collider2D other)
        {
            if (_ended) return;
            var prop = other.GetComponent<Platformer2DProp>();
            if (prop == null) return;

            switch (prop.kind)
            {
                case PlatformerProp.Kind.Goal:
                    End(true);
                    break;
                case PlatformerProp.Kind.Coin:
                    Coins++; CoinsChanged?.Invoke(Coins);
                    Destroy(other.gameObject);
                    break;
                case PlatformerProp.Kind.Heart:
                    Lives++; LivesChanged?.Invoke(Lives);
                    Destroy(other.gameObject);
                    break;
                case PlatformerProp.Kind.Spring:
                    _vel.y = JumpVelocity * 1.5f;
                    break;
                case PlatformerProp.Kind.Enemy:
                    bool stomp = _vel.y < 0f && transform.position.y > other.transform.position.y + 0.2f;
                    if (stomp) { Destroy(other.gameObject); _vel.y = JumpVelocity * 0.6f; }
                    else if (_invuln <= 0f)
                    {
                        _invuln = 1.2f;
                        _vel = new Vector2(-_facing * 5f, JumpVelocity * 0.6f);
                        LoseLife(false);
                    }
                    break;
            }
        }

        private void LoseLife(bool respawn)
        {
            Lives--;
            LivesChanged?.Invoke(Lives);
            if (Lives <= 0) { End(false); return; }
            if (respawn) { _vel = Vector2.zero; transform.position = _start; _invuln = 1f; }
        }

        private void End(bool won)
        {
            if (_ended) return;
            _ended = true; IsActive = false;
            if (won) Won?.Invoke(); else Failed?.Invoke();
        }
    }
}
