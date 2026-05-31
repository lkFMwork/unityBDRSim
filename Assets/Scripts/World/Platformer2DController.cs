using System;
using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// 2D platformer character built on Unity's own physics solver. The player is a DYNAMIC
    /// Rigidbody2D + CapsuleCollider2D, so every wall/floor/ceiling collision is resolved by the
    /// engine — no hand-rolled raycasts, which is where the phantom-ceiling / false-grounding /
    /// drift bugs all lived. We only set velocity (run, jump) and read a tiny ground-overlap box
    /// at the feet (which physically cannot detect a ceiling). Coyote time, jump buffer, variable
    /// jump height, run button, stomp, and the grow/shrink power-up are layered on top. Jump
    /// physics derive from a <see cref="JumpArc"/> so the level generator can guarantee reachability.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Platformer2DController : MonoBehaviour
    {
        public float walkSpeed = 5f;
        public float runSpeed = 8.5f;
        public float maxJumpHeight = 3.2f;
        public float timeToApex = 0.40f;
        public float coyoteTime = 0.1f;
        public float jumpBuffer = 0.12f;
        public float accelTime = 0.08f;   // time to reach top speed (ground)
        public float airAccelTime = 0.18f;
        public float killY = -8f;
        public int startLives = 3;
        public float halfWidth = 0.35f;
        public float halfHeight = 0.5f;

        public event Action Won;
        public event Action Failed;
        public event Action<int> LivesChanged;
        public event Action<int> CoinsChanged;
        public event Action<bool> PowerChanged;

        public int Lives { get; private set; }
        public int Coins { get; private set; }
        public bool Big { get; private set; }
        public bool IsActive = true;

        public JumpArc Arc => new JumpArc(walkSpeed, JumpVelocity, Gravity);
        public float Gravity => (2f * maxJumpHeight) / (timeToApex * timeToApex);
        public float JumpVelocity => (2f * maxJumpHeight) / timeToApex;

        private Rigidbody2D _rb;
        private LayerMask _solidMask;
        private SpriteAnimator _anim;
        private Vector3 _start;
        private int _facing = 1;
        private float _coyote, _buffer, _invuln;
        private bool _grounded, _wasGrounded, _ended;
        private bool _cutJump;

        public void Init(Vector3 start, SpriteAnimator anim, LayerMask solidMask, Rigidbody2D rb)
        {
            _anim = anim;
            _solidMask = solidMask;
            _rb = rb;
            _start = start;

            // Dynamic body, no spin, continuous (no tunnelling through thin platforms). Constant
            // gravity scale → a symmetric arc that ramps up and down equally. Derived from the
            // designed jump height/apex so it matches JumpArc.
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.freezeRotation = true;
            // Discrete is fine (and much cheaper than Continuous): at jump speed the body moves a
            // small fraction of a tile per step, so there's no tunnelling — Continuous's per-step
            // sweep tests were the framerate hitch during fast jump/fall.
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.gravityScale = Gravity / Mathf.Abs(Physics2D.gravity.y);
            _rb.position = start;
            _rb.velocity = Vector2.zero;

            Lives = startLives;
            Coins = 0;
            LivesChanged?.Invoke(Lives);
            CoinsChanged?.Invoke(Coins);
        }

        private void Update()
        {
            if (!IsActive || _ended) return;

            float h = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(h) > 0.01f) _facing = h > 0 ? 1 : -1;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W))
                _buffer = jumpBuffer;
            if (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W) || Input.GetButtonUp("Jump"))
                _cutJump = true;

            Animate(h);
        }

        private void FixedUpdate()
        {
            if (!IsActive || _ended) { return; }
            float dt = Time.fixedDeltaTime;
            _invuln = Mathf.Max(0f, _invuln - dt);

            // Ground check: a small overlap box just under the feet on the SOLID layer only. An
            // overlap at the feet cannot be confused with a ceiling, and the player's own collider
            // is on a different layer so it isn't detected.
            Vector2 feet = _rb.position + Vector2.down * halfHeight;
            _grounded = Physics2D.OverlapBox(feet, new Vector2(halfWidth * 1.7f, 0.18f), 0f, _solidMask) != null
                        && _rb.velocity.y <= 0.05f;

            if (_grounded && !_wasGrounded && _rb.velocity.y < -6f) _anim?.Squash(); // land squash
            _wasGrounded = _grounded;

            if (_grounded) _coyote = coyoteTime; else _coyote = Mathf.Max(0f, _coyote - dt);
            _buffer = Mathf.Max(0f, _buffer - dt);

            // Horizontal: ramp toward walk/run target (smooth, not switch-like).
            bool running = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
                           || Input.GetKey(KeyCode.JoystickButton1);
            float h = Input.GetAxisRaw("Horizontal");
            float top = running ? runSpeed : walkSpeed;
            float targetX = h * top;
            float rate = top / Mathf.Max(0.001f, _grounded ? accelTime : airAccelTime);
            float vx = Mathf.MoveTowards(_rb.velocity.x, targetX, rate * dt);
            float vy = _rb.velocity.y;

            // Jump (buffered + coyote).
            if (_buffer > 0f && _coyote > 0f)
            {
                vy = JumpVelocity;
                _buffer = 0f; _coyote = 0f; _grounded = false;
                _anim?.PlayJump();
            }
            // Variable height: releasing jump while rising cuts the climb.
            if (_cutJump) { if (vy > 0f) vy *= 0.5f; _cutJump = false; }

            _rb.velocity = new Vector2(vx, vy);

            if (_rb.position.y < killY) LoseLife(true);
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
                    _rb.velocity = new Vector2(_rb.velocity.x, JumpVelocity * 1.5f);
                    _anim?.Squash();
                    break;
                case PlatformerProp.Kind.Mushroom:
                    if (!Big) { Big = true; PowerChanged?.Invoke(true); _anim?.SetBig(true); }
                    Destroy(other.gameObject);
                    break;
                case PlatformerProp.Kind.Enemy:
                    // Stomp if descending onto the enemy (feet above its centre) — the Mario rule.
                    bool stomp = _rb.velocity.y <= 0.1f && _rb.position.y > other.transform.position.y - 0.1f;
                    if (stomp)
                    {
                        Destroy(other.gameObject);
                        _rb.velocity = new Vector2(_rb.velocity.x, JumpVelocity * 0.55f);
                    }
                    else if (_invuln <= 0f)
                    {
                        TakeDamage();
                        _rb.velocity = new Vector2(-_facing * 5f, JumpVelocity * 0.5f);
                    }
                    break;
            }
        }

        private void TakeDamage()
        {
            _invuln = 1.4f;
            if (Big)
            {
                Big = false;
                PowerChanged?.Invoke(false);
                _anim?.SetBig(false);
                return;
            }
            LoseLife(false);
        }

        private void LoseLife(bool respawn)
        {
            Lives--;
            LivesChanged?.Invoke(Lives);
            if (Lives <= 0) { End(false); return; }
            if (respawn) { _rb.velocity = Vector2.zero; _rb.position = _start; _invuln = 1f; }
        }

        private void End(bool won)
        {
            if (_ended) return;
            _ended = true; IsActive = false;
            if (_rb != null) _rb.velocity = Vector2.zero;
            if (won) Won?.Invoke(); else Failed?.Invoke();
        }
    }
}
