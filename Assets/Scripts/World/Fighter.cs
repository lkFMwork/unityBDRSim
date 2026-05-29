using System;
using UnityEngine;
using Fitzmark.BDRSim.UI;

namespace Fitzmark.BDRSim.World
{
    public enum AttackType { None, Light, Heavy, Special }

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

        private enum St { Idle, Attacking, HitStun, KO }
        private St _state = St.Idle;
        private CharacterController _cc;
        private FighterRig _rig;
        private Vector3 _vel;
        private bool _blocking;
        private AttackType _attack;
        private float _attackTime;
        private bool _attackHit;
        private float _hitStun;

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
                    _hitStun -= dt;
                    _vel.x = Mathf.MoveTowards(_vel.x, 0f, 18f * dt);
                    Fall(dt, grounded);
                    if (_hitStun <= 0f) _state = St.Idle;
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
            _attackTime = 0f;
            _attackHit = false;
            _vel.x = 0f;
            _rig?.Attack();
        }

        private void RunAttack(float dt)
        {
            _attackTime += dt;
            var p = Params(_attack);

            if (!_attackHit && _attackTime >= p.startup && _attackTime <= p.startup + p.active
                && Opponent != null && !Opponent.IsKO)
            {
                float dx = Opponent.transform.position.x - transform.position.x;
                bool inFront = (dx >= 0f) == FacingRight || Mathf.Abs(dx) < 0.3f;
                bool reachOk = Mathf.Abs(dx) <= p.reach
                               && Mathf.Abs(Opponent.transform.position.y - transform.position.y) < 1.6f;
                if (inFront && reachOk)
                {
                    _attackHit = true;
                    bool combo = Opponent.IsHitStunned;
                    Opponent.ReceiveHit(p.dmg * DamageMultiplier, FacingRight ? 1 : -1, p.stun);
                    HitConnected?.Invoke(combo);
                }
            }

            if (_attackTime >= p.startup + p.active + p.recovery) _state = St.Idle;
        }

        public void ReceiveHit(float damage, int dir, float stun)
        {
            if (_state == St.KO) return;
            if (_blocking)
            {
                Health -= damage * 0.15f; // chip
                HealthChanged?.Invoke();
                _rig?.TakeHit();
                if (Health <= 0f) Die();
                return;
            }
            Health -= damage;
            HealthChanged?.Invoke();
            if (Health <= 0f) { Die(); return; }
            _state = St.HitStun;
            _hitStun = stun;
            _vel = new Vector3(dir * 4f, 5f, 0f);
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

        private struct AtkParams
        {
            public float startup, active, recovery, dmg, reach, stun;
        }

        private static AtkParams Params(AttackType type) => type switch
        {
            AttackType.Light => new AtkParams { startup = 0.07f, active = 0.08f, recovery = 0.17f, dmg = 6f, reach = 1.5f, stun = 0.30f },
            AttackType.Heavy => new AtkParams { startup = 0.18f, active = 0.10f, recovery = 0.34f, dmg = 12f, reach = 1.8f, stun = 0.42f },
            AttackType.Special => new AtkParams { startup = 0.30f, active = 0.12f, recovery = 0.50f, dmg = 22f, reach = 2.1f, stun = 0.60f },
            _ => new AtkParams { startup = 0.1f, active = 0.1f, recovery = 0.2f, dmg = 4f, reach = 1.4f, stun = 0.3f }
        };
    }
}
