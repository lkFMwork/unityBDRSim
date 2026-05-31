using System;
using UnityEngine;
using Fitzmark.BDRSim.UI;

namespace Fitzmark.BDRSim.World
{
    public enum AttackType { None, Light, Heavy, Launcher, Special, Projectile }

    /// <summary>Per-frame intent for a fighter, supplied by the player input or the AI.</summary>
    public struct FighterIntent
    {
        public float move;     // -1..1
        public bool jump;
        public AttackType attack;
        public bool block;
    }

    /// <summary>
    /// A real-time 2.5D fighter on a CharacterController: walk, jump, three attacks
    /// with startup/active/recovery windows and manual hit detection vs the opponent,
    /// blocking (chip damage, no stun), hitstun (enables combos), and KO. Control is
    /// supplied each frame via <see cref="Tick"/> (player input or AI), so the same
    /// class powers both fighters. Drives a <see cref="FighterRig"/> child for visuals.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class Fighter : MonoBehaviour
    {
        public float walkSpeed = 4.5f;
        public float jumpSpeed = 11f;
        public float gravity = -30f;
        public float stageBound = 6.5f;

        public float MaxHealth { get; private set; } = 100f;
        public float Health { get; private set; } = 100f;
        public float DamageMultiplier = 1f;
        public bool FacingRight { get; private set; } = true;
        public Fighter Opponent;

        public bool IsKO => _state == St.KO;
        public bool IsAttacking => _state == St.Attacking;
        public bool IsHitStunned => _state == St.HitStun;

        public event Action HealthChanged;
        public event Action Defeated;
        public event Action<bool> HitConnected; // true = opponent was already in hitstun (combo)

        private enum St { Idle, Attacking, HitStun, BlockStun, KO }
        private St _state = St.Idle;
        private CharacterController _cc;
        private FighterRig _rig;
        private Vector3 _vel;
        private bool _blocking;
        private AttackType _attack;
        private int _attackFrame;
        private bool _attackHit;
        private bool _projectileFired;
        private int _comboHits;
        private float _stun;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _rig = GetComponentInChildren<FighterRig>();
        }

        public void Setup(float maxHealth, float damageMultiplier)
        {
            MaxHealth = maxHealth;
            DamageMultiplier = damageMultiplier;
            Health = maxHealth;
            HealthChanged?.Invoke();
        }

        public void ResetForRound(Vector3 position, bool faceRight)
        {
            transform.position = position;
            _vel = Vector3.zero;
            _state = St.Idle;
            FacingRight = faceRight;
            Health = MaxHealth;
            HealthChanged?.Invoke();
        }

        public void Tick(FighterIntent intent)
        {
            float dt = Time.deltaTime;
            if (Opponent != null && _state != St.KO)
                FacingRight = Opponent.transform.position.x >= transform.position.x;

            bool grounded = _cc.isGrounded;

            switch (_state)
            {
                case St.KO:
                    Fall(dt, grounded);
                    break;
                case St.HitStun:
                case St.BlockStun:
                    _stun -= dt;
                    _vel.x = Mathf.MoveTowards(_vel.x, 0f, 18f * dt);
                    Fall(dt, grounded);
                    if (_stun <= 0f && grounded) _state = St.Idle; // stay stunned until you land
                    break;
                case St.Attacking:
                    RunAttack(dt);
                    _vel.x = Mathf.MoveTowards(_vel.x, 0f, 14f * dt);
                    Fall(dt, grounded);
                    break;
                default: // Idle / free
                    _blocking = intent.block && grounded;
                    _rig?.SetBlocking(_blocking);
                    if (!_blocking && intent.attack != AttackType.None && grounded)
                    {
                        StartAttack(intent.attack);
                    }
                    else
                    {
                        _vel.x = intent.move * (_blocking ? walkSpeed * 0.4f : walkSpeed);
                        if (intent.jump && grounded) _vel.y = jumpSpeed;
                    }
                    Fall(dt, grounded);
                    break;
            }

            ApplyMove(dt);
            if (_rig != null) _rig.facingRight = FacingRight;
            FaceVisual();
        }

        private void StartAttack(AttackType type)
        {
            _state = St.Attacking;
            _attack = type;
            _attackFrame = 0;
            _attackHit = false;
            _vel.x = 0f;
            _rig?.Attack(RigMove(type));
            if (type == AttackType.Projectile) _projectileFired = false;
        }

        private void RunAttack(float dt)
        {
            _attackFrame++;
            var d = Data(_attack);

            // Projectile: spawn a travelling hitbox once, at the end of startup.
            if (_attack == AttackType.Projectile)
            {
                if (!_projectileFired && _attackFrame >= d.startup)
                {
                    _projectileFired = true;
                    FightProjectile.Spawn(this, FacingRight ? 1 : -1, d.dmg * DamageMultiplier, d.hitstun * FrameSec);
                }
                if (_attackFrame >= d.startup + d.recovery) _state = St.Idle;
                return;
            }

            // Active frames: test this move's hitbox against the opponent's hurtbox.
            bool active = _attackFrame > d.startup && _attackFrame <= d.startup + d.active;
            if (active && !_attackHit && Opponent != null && !Opponent.IsKO && HitboxOverlaps(d))
            {
                _attackHit = true;
                bool combo = Opponent.IsHitStunned;
                _comboHits = combo ? _comboHits + 1 : 0;
                float scale = Mathf.Max(0.4f, 1f - _comboHits * 0.15f); // combo damage scaling
                Opponent.ReceiveAttack(d, FacingRight ? 1 : -1, DamageMultiplier * scale);
                HitConnected?.Invoke(combo);
            }

            if (_attackFrame >= d.startup + d.active + d.recovery) _state = St.Idle;
        }

        // The move's hitbox (a reach×height box in front of the attacker) vs the opponent's
        // body hurtbox. This is the spatial half of fighting-game collision (frame data is the
        // temporal half, handled by the active-frame window above).
        private bool HitboxOverlaps(FrameData d)
        {
            Vector3 me = transform.position, foe = Opponent.transform.position;
            float dx = foe.x - me.x;
            bool inFront = (dx >= 0f) == FacingRight || Mathf.Abs(dx) < 0.3f;
            bool xOk = Mathf.Abs(dx) <= d.reach + 0.5f;            // + foe half-width
            bool yOk = Mathf.Abs(foe.y - me.y) <= d.height;
            return inFront && xOk && yOk;
        }

        /// <summary>Apply a melee hit defined by frame data (called by the attacker).</summary>
        public void ReceiveAttack(FrameData d, int dir, float dmgScale)
        {
            ApplyHit(d.dmg * dmgScale, dir, d.hitstun * FrameSec, d.blockstun * FrameSec,
                d.knockX, d.knockY, d.launch);
        }

        /// <summary>Apply a hit from a projectile (called by <see cref="FightProjectile"/>).</summary>
        public void ReceiveProjectile(float damage, int dir, float stun)
        {
            ApplyHit(damage, dir, stun, stun * 0.7f, 4f, 3f, false);
        }

        private void ApplyHit(float damage, int dir, float hitStun, float blockStun,
            float knockX, float knockY, bool launch)
        {
            if (_state == St.KO) return;
            if (_blocking)
            {
                Health -= damage * 0.12f;           // chip damage
                HealthChanged?.Invoke();
                _state = St.BlockStun;
                _stun = blockStun;                   // blockstun < hitstun → you recover sooner
                _vel.x = dir * 1.5f;                 // pushback only
                _rig?.TakeHit();
                if (Health <= 0f) Die();
                return;
            }
            Health -= damage;
            HealthChanged?.Invoke();
            if (Health <= 0f) { Die(); return; }
            _state = St.HitStun;
            _stun = hitStun;
            _vel = new Vector3(dir * knockX, knockY, 0f);
            _rig?.TakeHit();
        }

        public void PlayVictory() => _rig?.Victory();

        private void Die()
        {
            Health = 0f;
            _state = St.KO;
            _vel = Vector3.zero;
            _rig?.Defeat();
            Defeated?.Invoke();
        }

        private void Fall(float dt, bool grounded)
        {
            if (grounded && _vel.y < 0f) _vel.y = -2f;
            _vel.y += gravity * dt;
        }

        private void ApplyMove(float dt)
        {
            _cc.Move(new Vector3(_vel.x, _vel.y, 0f) * dt);
            var pos = transform.position;
            pos.z = 0f;
            pos.x = Mathf.Clamp(pos.x, -stageBound, stageBound);
            transform.position = pos;
        }

        private void FaceVisual()
        {
            if (_rig != null)
                _rig.transform.localRotation = Quaternion.Euler(0f, FacingRight ? 90f : -90f, 0f);
        }

        private static FighterRig.Move RigMove(AttackType t) => t switch
        {
            AttackType.Heavy => FighterRig.Move.Heavy,
            AttackType.Launcher => FighterRig.Move.Launcher,
            AttackType.Special => FighterRig.Move.Special,
            AttackType.Projectile => FighterRig.Move.Projectile,
            _ => FighterRig.Move.Light,
        };

        /// <summary>
        /// Move definition in real fighting-game terms (60fps frame data + boxes). Startup =
        /// frames before the hitbox appears, active = frames it can hit, recovery = frames
        /// you're locked after. hitstun/blockstun are the frames the foe is frozen on hit/block;
        /// hitstun &gt; recovery means you're "plus on hit" → a faster follow-up combos (the
        /// core of fighting-game offense). reach/height define the hitbox, blockstun the chip.
        /// (Sources: Capcom SF frame data, Dream Cancel hitstun/blockstun, fighter fundamentals.)
        /// </summary>
        public struct FrameData
        {
            public int startup, active, recovery, hitstun, blockstun;
            public float dmg, reach, height, knockX, knockY;
            public bool launch;
        }

        private const float FrameSec = 1f / 60f;

        // Light: fast poke, plus on hit (links into another light → combo). Heavy: slow, big,
        // unsafe if whiffed. Launcher: pops up for juggles. Special/Projectile: high commit.
        public static FrameData Data(AttackType type) => type switch
        {
            AttackType.Light => new FrameData { startup = 4, active = 3, recovery = 8, hitstun = 16, blockstun = 11, dmg = 5f, reach = 1.5f, height = 1.6f, knockX = 3f, knockY = 3f },
            AttackType.Heavy => new FrameData { startup = 11, active = 4, recovery = 20, hitstun = 24, blockstun = 14, dmg = 11f, reach = 1.9f, height = 1.7f, knockX = 5f, knockY = 4f },
            AttackType.Launcher => new FrameData { startup = 9, active = 4, recovery = 26, hitstun = 34, blockstun = 12, dmg = 9f, reach = 1.6f, height = 1.8f, knockX = 1.5f, knockY = 11f, launch = true },
            AttackType.Special => new FrameData { startup = 16, active = 6, recovery = 28, hitstun = 28, blockstun = 16, dmg = 18f, reach = 2.2f, height = 1.8f, knockX = 7f, knockY = 5f },
            AttackType.Projectile => new FrameData { startup = 14, active = 0, recovery = 26, hitstun = 20, blockstun = 12, dmg = 9f, reach = 0f, height = 0f, knockX = 4f, knockY = 3f },
            _ => new FrameData { startup = 6, active = 3, recovery = 10, hitstun = 14, blockstun = 8, dmg = 4f, reach = 1.4f, height = 1.6f, knockX = 3f, knockY = 3f },
        };
    }
}
