using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// First-person walker with a tight third-person toggle (press V). Mouse looks,
    /// WASD moves relative to facing, gravity keeps you on the floor. Owns the main
    /// camera directly (no FollowCamera). Drives the attached Mixamo body's walk/idle
    /// and hides it in first person so you don't see inside the mesh. The cursor locks
    /// for mouse-look during play and frees while a menu/modal is open.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        public float MoveSpeed = 4.2f;
        public float MouseSensitivity = 2.4f;
        public float Gravity = -22f;
        public float EyeHeight = 1.62f;

        private CharacterController _cc;
        private Camera _cam;
        private OfficeWorker _worker;
        private SkinnedMeshRenderer[] _body;
        private float _yaw, _pitch, _vy;
        private bool _firstPerson = true;
        private bool _controlEnabled = true;
        private bool _resolved;

        public void SetControlEnabled(bool value)
        {
            _controlEnabled = value;
            ApplyCursor();
        }

        private void Start()
        {
            _cc = GetComponent<CharacterController>();
            _yaw = transform.eulerAngles.y;
            _cam = Camera.main;
            if (_cam != null)
            {
                var fc = _cam.GetComponent<FollowCamera>();
                if (fc != null) fc.enabled = false; // we drive the camera
                _cam.nearClipPlane = 0.05f;          // don't clip when close to walls
                _cam.fieldOfView = 63f;
            }
            Resolve();
            ApplyBodyVisibility();
            ApplyCursor();
        }

        private void Resolve()
        {
            _worker = GetComponentInChildren<OfficeWorker>();
            _body = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            _resolved = true;
        }

        private void Update()
        {
            if (!_resolved) Resolve();
            if (Input.GetKeyDown(KeyCode.V)) { _firstPerson = !_firstPerson; ApplyBodyVisibility(); }

            if (_controlEnabled)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) FreeCursor();
                if (Input.GetMouseButtonDown(0)) LockCursor();
                Look();
                Move();
            }
            else
            {
                SetWalk(0f);
            }
            PositionCamera();
        }

        private void Look()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;
            _yaw += Input.GetAxisRaw("Mouse X") * MouseSensitivity;
            _pitch -= Input.GetAxisRaw("Mouse Y") * MouseSensitivity;
            _pitch = Mathf.Clamp(_pitch, -75f, 75f);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        private void Move()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 dir = transform.right * h + transform.forward * v;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            SetWalk(dir.magnitude);

            if (_cc.isGrounded && _vy < 0f) _vy = -2f;
            _vy += Gravity * Time.deltaTime;
            _cc.Move((dir * MoveSpeed + Vector3.up * _vy) * Time.deltaTime);
        }

        private void SetWalk(float amount)
        {
            if (_worker != null) _worker.SetWalk(amount);
        }

        private void ApplyBodyVisibility()
        {
            if (_body == null) return;
            foreach (var r in _body) if (r != null) r.enabled = !_firstPerson;
        }

        private void PositionCamera()
        {
            if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }
            Vector3 head = transform.position + Vector3.up * EyeHeight;

            if (_firstPerson)
            {
                _cam.transform.SetPositionAndRotation(head, Quaternion.Euler(_pitch, _yaw, 0f));
                return;
            }

            // Tight third-person: behind and slightly above the head, pulled in if a wall is between.
            Quaternion rot = Quaternion.Euler(Mathf.Clamp(_pitch, -25f, 50f), _yaw, 0f);
            Vector3 want = head + rot * new Vector3(0f, 0.35f, -3.0f);
            if (Physics.Linecast(head, want, out var hit)) want = hit.point + hit.normal * 0.2f;
            _cam.transform.position = want;
            _cam.transform.rotation = Quaternion.LookRotation(head + Vector3.up * 0.2f - want);
        }

        private void ApplyCursor()
        {
            if (_controlEnabled) LockCursor(); else FreeCursor();
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void FreeCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDisable() => FreeCursor();
    }
}
