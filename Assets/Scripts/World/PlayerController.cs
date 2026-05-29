using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// On-foot third-person movement via a CharacterController. World-axis WASD
    /// (matched to the fixed camera angle) with gravity; the body rotates to face
    /// the direction of travel. Uses the classic Input Manager (no Input System
    /// package required).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float MoveSpeed = 6f;
        public float RotateSpeed = 12f;
        public float Gravity = -22f;

        private CharacterController _cc;
        private Fitzmark.BDRSim.UI.AvatarBuilder _avatar;
        private float _verticalVelocity;
        private bool _controlEnabled = true;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _avatar = GetComponentInChildren<Fitzmark.BDRSim.UI.AvatarBuilder>();
        }

        public void SetControlEnabled(bool value) => _controlEnabled = value;

        private void Update()
        {
            if (!_controlEnabled) { if (_avatar != null) _avatar.SetWalk(0f); return; }

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 move = new Vector3(h, 0f, v);
            if (move.sqrMagnitude > 1f) move.Normalize();
            if (_avatar != null) _avatar.SetWalk(move.magnitude);

            if (_cc.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += Gravity * Time.deltaTime;

            Vector3 velocity = move * MoveSpeed + Vector3.up * _verticalVelocity;
            _cc.Move(velocity * Time.deltaTime);

            if (move.sqrMagnitude > 0.01f)
            {
                Quaternion target = Quaternion.LookRotation(new Vector3(h, 0f, v));
                transform.rotation = Quaternion.Slerp(transform.rotation, target, RotateSpeed * Time.deltaTime);
            }
        }
    }
}
