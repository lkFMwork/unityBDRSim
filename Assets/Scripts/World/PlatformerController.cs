using System;
using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// A 2.5D Mario-style platformer character on a CharacterController: run,
    /// variable-height jump (with coyote time + input buffer), gravity, pits, and
    /// stomp-the-enemy. Movement is locked to the X/Y plane. Tunable via the public
    /// fields. Drives a visual child (faces the direction of travel).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlatformerController : MonoBehaviour
    {
        public float runSpeed = 7f;
        public float jumpSpeed = 13f;
        public float gravity = -32f;
        public float maxFall = -26f;
        public float killY = -6f;
        public int startLives = 3;

        public event Action Won;
        public event Action Failed;
        public event Action<int> LivesChanged;
        public event Action<int> CoinsChanged;

        public int Lives { get; private set; }
        public int Coins { get; private set; }
        public bool IsActive = true;

        private CharacterController _cc;
        private Vector3 _velocity;
        private Vector3 _start;
        private Transform _visual;
        private int _facing = 1;
        private float _coyote;
        private float _buffer;
        private float _invuln;
        private bool _ended;

        private void Awake() => _cc = GetComponent<CharacterController>();

        public void Init(Vector3 start, Transform visual)
        {
            _start = start;
            transform.position = start;
            _visual = visual;
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
            _velocity.x = h * runSpeed;
            if (Mathf.Abs(h) > 0.01f) _facing = h > 0f ? 1 : -1;

            bool grounded = _cc.isGrounded;
            if (grounded)
            {
                _coyote = 0.1f;
                if (_velocity.y < 0f) _velocity.y = -2f;
            }
            else _coyote = Mathf.Max(0f, _coyote - dt);

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump")) _buffer = 0.12f;
            else _buffer = Mathf.Max(0f, _buffer - dt);

            if (_buffer > 0f && _coyote > 0f)
            {
                _velocity.y = jumpSpeed;
                _buffer = 0f;
                _coyote = 0f;
            }
            if (Input.GetKeyUp(KeyCode.Space) && _velocity.y > 0f) _velocity.y *= 0.5f; // variable jump

            _velocity.y += gravity * dt;
            if (_velocity.y < maxFall) _velocity.y = maxFall;

            _cc.Move(new Vector3(_velocity.x, _velocity.y, 0f) * dt);

            var p = transform.position;
            if (Mathf.Abs(p.z) > 0.001f) { p.z = 0f; transform.position = p; } // stay on the plane

            if (_visual != null && Mathf.Abs(h) > 0.01f)
                _visual.localRotation = Quaternion.Euler(0f, _facing > 0 ? 90f : -90f, 0f);

            if (transform.position.y < killY)
                LoseLife(respawn: true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_ended) return;
            var prop = other.GetComponent<PlatformerProp>();
            if (prop == null) return;

            switch (prop.kind)
            {
                case PlatformerProp.Kind.Goal:
                    End(true);
                    break;
                case PlatformerProp.Kind.Coin:
                    Coins++;
                    CoinsChanged?.Invoke(Coins);
                    Destroy(other.gameObject);
                    break;
                case PlatformerProp.Kind.Enemy:
                    bool stomp = _velocity.y < 0f && transform.position.y > other.transform.position.y + 0.25f;
                    if (stomp)
                    {
                        Destroy(other.gameObject);
                        _velocity.y = jumpSpeed * 0.6f; // bounce
                    }
                    else if (_invuln <= 0f)
                    {
                        _invuln = 1.2f;
                        _velocity = new Vector3(-_facing * 5f, 7f, 0f); // knockback
                        LoseLife(respawn: false);
                    }
                    break;
            }
        }

        private void LoseLife(bool respawn)
        {
            Lives--;
            LivesChanged?.Invoke(Lives);
            if (Lives <= 0) { End(false); return; }
            if (respawn)
            {
                _velocity = Vector3.zero;
                transform.position = _start;
                _invuln = 1f;
            }
        }

        private void End(bool won)
        {
            if (_ended) return;
            _ended = true;
            IsActive = false;
            if (won) Won?.Invoke(); else Failed?.Invoke();
        }
    }
}
