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
        public float walkSpeed = 5f;
        public float runSpeed = 8.5f;            // top speed while holding the run button
        public float maxJumpHeight = 3.2f;
        public float timeToApex = 0.40f;
        public float coyoteTime = 0.1f;
        public float jumpBuffer = 0.12f;
        // Arc shape. 1 = fully symmetric (rise and fall ramp equally — a clean parabola).
        // Raise slightly (e.g. 1.15) only if you want a touch more weight on the way down.
        public float fallGravityMult = 1f;
        // Horizontal speed RAMPS toward the target (the technique that makes running + jumping
        // feel good, per cjddmut's PlatformerMotor2D) rather than snapping on/off. Air is grippier
        // (slower accel) so you keep momentum mid-jump instead of stopping dead — the fix for the
        // "weird jump-while-running" feel.
        public float timeToTopSpeedGround = 0.10f;
        public float timeToTopSpeedAir = 0.22f;
        public float timeToStopGround = 0.08f;   // skid-to-stop on release
        public float killY = -8f;
        public int startLives = 3;
        public float halfWidth = 0.35f;
        public float halfHeight = 0.5f;

        // Live state for the on-screen debug readout (diagnosing the jump/ground issue).
        public int DbgNudges, DbgVCol;
        public string DebugLine =>
            $"grnd:{(_grounded ? 1 : 0)}  vY:{_vel.y:F1}  lock:{_jumpLock:F2}  y:{transform.position.y:F2}  " +
            $"nudge:{DbgNudges}  vcol:{DbgVCol}";

        public event Action Won;
        public event Action Failed;
        public event Action<int> LivesChanged;
        public event Action<int> CoinsChanged;
        public event Action<bool> PowerChanged; // true = grew big, false = shrank

        public int Lives { get; private set; }
        public int Coins { get; private set; }
        public bool Big { get; private set; }   // Mario-style: big absorbs one hit → shrink
        public bool IsActive = true;

        public JumpArc Arc => new JumpArc(runSpeed, JumpVelocity, Gravity);
        public float Gravity => (2f * maxJumpHeight) / (timeToApex * timeToApex);
        public float JumpVelocity => (2f * maxJumpHeight) / timeToApex;

        private LayerMask _solidMask;
        private Vector2 _vel;
        private Vector3 _start;
        private SpriteAnimator _anim;
        private int _facing = 1;
        private float _coyote, _buffer, _invuln, _jumpLock;
        private bool _grounded;
        private bool _ended;

        public void Init(Vector3 start, SpriteAnimator anim, LayerMask solidMask)
        {
            _start = start;
            transform.position = start;
            _anim = anim;
            _solidMask = solidMask;
            // Movement casts use an EXPLICIT box at the live position (see CastSelf), so they don't
            // depend on collider-sync timing. Keep autoSync on so the pickup trigger stays current
            // for coins/enemies; it's cheap because only the player transform is ever dirty.
            Physics2D.autoSyncTransforms = true;
            // Don't report a collider the cast box already touches/overlaps as a distance-0 hit —
            // that false "you're already against ground" read is a classic cause of phantom
            // landings (and the resulting grounded flicker / feet bob).
            Physics2D.queriesStartInColliders = false;
            Physics2D.SyncTransforms(); // register the freshly-built ground for queries
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
            if (Mathf.Abs(h) > 0.01f) _facing = h > 0 ? 1 : -1;

            // Hold Shift (or controller B) to RUN; otherwise walk. Both ramp toward their target
            // speed (skid-to-stop on release) so movement and jump-while-moving feel smooth, not
            // switch-like. Reachability is guaranteed at walk speed, so running is optional flair.
            bool running = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
                           || Input.GetKey(KeyCode.JoystickButton1);
            float topSpeed = running ? runSpeed : walkSpeed;
            float target = h * topSpeed;
            float accel = runSpeed / Mathf.Max(0.001f, _grounded ? timeToTopSpeedGround : timeToTopSpeedAir);
            if (Mathf.Abs(h) <= 0.01f) accel = runSpeed / Mathf.Max(0.001f, _grounded ? timeToStopGround : timeToTopSpeedAir);
            _vel.x = Mathf.MoveTowards(_vel.x, target, accel * dt);

            // Air-lock: for a short window after jumping, you are NOT considered grounded and
            // coyote can't refresh — a hard guarantee against re-triggering the same jump (no
            // infinite jump) even if a ground cast momentarily reads true.
            _jumpLock = Mathf.Max(0f, _jumpLock - dt);
            bool canGround = _grounded && _jumpLock <= 0f;
            if (canGround) _coyote = coyoteTime; else _coyote = Mathf.Max(0f, _coyote - dt);

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W))
                _buffer = jumpBuffer;
            else _buffer = Mathf.Max(0f, _buffer - dt);

            if (_buffer > 0f && _coyote > 0f)
            {
                _vel.y = JumpVelocity;
                _buffer = 0f; _coyote = 0f;
                _grounded = false; _jumpLock = 0.14f; // leave the ground; lock out re-grounding briefly
                _anim?.PlayJump();
            }
            if ((Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W)) && _vel.y > 0f)
                _vel.y *= 0.5f; // variable-height jump

            // Symmetric arc: ONE constant gravity for rise and fall, so the jump ramps up and
            // ramps down at the same rate — a clean parabola (inherently a smooth U, no stall).
            // fallGravityMult defaults to 1 (fully symmetric); nudge it >1 only if you later want
            // a slightly snappier descent.
            float g = Gravity * (_vel.y < 0f ? fallGravityMult : 1f);
            _vel.y -= g * dt;
            _vel.y = Mathf.Max(_vel.y, -JumpVelocity * 1.6f); // terminal (a bit past launch speed)

            MoveCollide(dt);
            Animate(h);

            if (transform.position.y < killY) LoseLife(true);
        }

        // Axis-separated movement that casts the player's OWN collider (the single source of
        // truth for where the player is), so there is no hand-rolled center/feet offset to get
        // wrong — eliminating the "rests a tile too high" class of bug.
        private void MoveCollide(float dt)
        {
            Vector3 p = transform.position;
            const float skin = 0.02f;

            // Horizontal — cast from the working position p.
            float dx = _vel.x * dt;
            if (Mathf.Abs(dx) != 0f)
            {
                int dir = dx > 0 ? 1 : -1;
                float dist = CastSelf(p, Vector2.right * dir, Mathf.Abs(dx) + skin);
                if (dist < Mathf.Abs(dx) + skin) { dx = Mathf.Max(0f, dist - skin) * dir; _vel.x = 0f; }
                p.x += dx;
            }

            // Vertical — cast from p (now x-updated).
            float dy = _vel.y * dt;
            _grounded = false;
            if (Mathf.Abs(dy) != 0f)
            {
                int dir = dy > 0 ? 1 : -1;
                float dist = CastSelf(p, Vector2.up * dir, Mathf.Abs(dy) + skin);
                if (dist < Mathf.Abs(dy) + skin)
                {
                    // Corner correction (rising): if only a corner of your head clips a block,
                    // nudge sideways so you slide past instead of stopping dead.
                    if (dir > 0 && TryCornerNudge(ref p))
                    {
                        DbgNudges++;
                        p.y += dy; // continue rising this frame
                    }
                    else
                    {
                        DbgVCol++;
                        dy = Mathf.Max(0f, dist - skin) * dir;
                        if (dir < 0) { _grounded = true; if (_vel.y < -8f) _anim?.Squash(); }
                        _vel.y = 0f;
                        p.y += dy;
                    }
                }
                else p.y += dy;
            }

            // No separate grounded probe: the vertical block above already sets _grounded on any
            // downward contact, and gravity makes dy negative every frame, so a grounded player is
            // re-confirmed each frame. A second probe was an independent source that could flicker.

            p.z = 0f;
            transform.position = p;
        }

        // If the head is clipping only a corner of a block above, try nudging left/right by up
        // to ~a third of a tile to slide past. Uses BoxCast at offset positions (no transform
        // writes / no per-step syncs), so it's cheap. Returns true (and shifts p) if a nudge clears it.
        private bool TryCornerNudge(ref Vector3 p)
        {
            Vector2 box = new Vector2(halfWidth * 2f - 0.04f, halfHeight * 2f - 0.04f);
            const float maxNudge = 0.34f, step = 0.08f;
            for (float n = step; n <= maxNudge; n += step)
            {
                for (int si = 0; si < 2; si++)
                {
                    float s = si == 0 ? 1f : -1f;
                    Vector2 c = new Vector2(p.x + s * n, p.y); // box is centered on the transform
                    var hit = Physics2D.BoxCast(c, box, 0f, Vector2.up, 0.15f, _solidMask);
                    if (hit.collider == null) { p.x += s * n; return true; } // clear above at this offset
                }
            }
            return false;
        }

        // Distance the player box can travel from `origin` along `dir` before hitting a surface
        // that actually OPPOSES that direction (full `max` if clear). Two essentials:
        //  • Box is cast from an EXPLICIT origin (the live working position), NOT the collider's
        //    physics-synced state — so results can't lag sync timing. That lag was making the
        //    downward cast falsely "land" each frame, resetting fall velocity (the slow fall).
        //  • Normal check: the floor you're standing on reports a distance-0 hit with its normal
        //    pointing up; that must NOT block an upward jump. Only surfaces whose normal faces
        //    back against travel (dot < 0) are real obstacles.
        private static readonly RaycastHit2D[] _castHits = new RaycastHit2D[8];
        private float CastSelf(Vector2 origin, Vector2 dir, float max)
        {
            Vector2 size = new Vector2(halfWidth * 2f - 0.04f, halfHeight * 2f - 0.04f);
            int n = Physics2D.BoxCastNonAlloc(origin, size, 0f, dir, _castHits, max, _solidMask);
            float best = max;
            for (int i = 0; i < n; i++)
            {
                var h = _castHits[i];
                if (h.collider == null) continue;
                if (Vector2.Dot(h.normal, dir) >= -0.01f) continue; // not opposing → ignore (floor when jumping)
                if (h.distance < best) best = h.distance;
            }
            return best;
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
                    _anim?.Squash();
                    break;
                case PlatformerProp.Kind.Mushroom:
                    if (!Big) { Big = true; PowerChanged?.Invoke(true); _anim?.SetBig(true); }
                    Destroy(other.gameObject);
                    break;
                case PlatformerProp.Kind.Enemy:
                    // Stomp if descending and your feet are above the enemy's center — generous
                    // window (any downward motion onto it), the standard Mario rule.
                    bool stomp = _vel.y <= 0.1f && transform.position.y > other.transform.position.y - 0.1f;
                    if (stomp)
                    {
                        Destroy(other.gameObject);
                        _vel.y = JumpVelocity * 0.55f; // bounce
                    }
                    else if (_invuln <= 0f)
                    {
                        TakeDamage();
                        _vel = new Vector2(-_facing * 5f, JumpVelocity * 0.5f);
                    }
                    break;
            }
        }

        // Mario damage rule: if big, shrink and survive with brief invuln; if small, lose a life.
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
