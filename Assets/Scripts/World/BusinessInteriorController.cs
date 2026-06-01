using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AvatarBuilder = Fitzmark.BDRSim.UI.AvatarBuilder; // disambiguate from UnityEngine.AvatarBuilder

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// A business's walk-in interior. Instead of cutting straight from the street to the
    /// gatekeeper duel, the player enters a real lobby (built with <see cref="RoomBuilder"/>),
    /// sees a receptionist and other people, and walks up to the gatekeeper guarding the back
    /// office; talking to them starts the meeting. The pending meeting is carried in via
    /// <see cref="GameManager.SelectedScenario"/>.
    ///
    /// Phase 1: the gatekeeper duel and the meeting still run in their existing scenes (this
    /// scene just precedes them and makes the visit physical). A later phase can host both
    /// in-place. Move with WASD + mouse, interact with E, toggle view with V.
    /// </summary>
    public class BusinessInteriorController : MonoBehaviour
    {
        private const float H = 3.4f; // room height
        private static readonly Color WallCol = new(0.80f, 0.81f, 0.85f);
        private static readonly Color FloorCol = new(0.32f, 0.34f, 0.40f);
        private static readonly Color CeilCol = new(0.88f, 0.89f, 0.93f);
        private static readonly Color LightCol = new(1f, 0.96f, 0.86f);

        private GameObject _player;
        private FirstPersonController _fp;
        private Transform _root;
        private Interactable[] _interactables;
        private TMP_Text _prompt;

        private void Start()
        {
            GameManager.Instance.HubScene = SceneNames.City; // leaving / finishing returns to the street

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.44f, 0.45f, 0.50f);
            RenderSettings.fog = false;

            _root = new GameObject("BusinessRooms").transform;
            SetupCamera();
            BuildInterior();
            SpawnPlayer(new Vector3(0f, 0f, -9f));
            SpawnPeople();

            _interactables = Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None);
            BuildHud();
        }

        private void Update()
        {
            if (_player == null) return;
            var near = FindNearest(_player.transform.position);
            if (Input.GetKeyDown(KeyCode.E) && near != null) Interact(near);
            UpdatePrompt(near);
        }

        // ---- build -------------------------------------------------------------

        private void BuildInterior()
        {
            // Lobby (enter here) to the south; back office to the north, a doorway between them.
            RoomBuilder.Room(_root, new Vector3(-7f, 0f, -11f), new Vector3(7f, 0f, 0f), H,
                WallCol, FloorCol, CeilCol, RoomBuilder.Side.N, LightCol);
            RoomBuilder.Room(_root, new Vector3(-7f, 0f, 0f), new Vector3(7f, 0f, 9f), H,
                WallCol, new Color(0.30f, 0.33f, 0.40f), CeilCol, RoomBuilder.Side.S, LightCol);

            // Reception desk in the lobby (furniture is greybox for now).
            var desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            desk.name = "ReceptionDesk";
            desk.transform.SetParent(_root, false);
            desk.transform.localScale = new Vector3(3.2f, 1.1f, 1.1f);
            desk.transform.localPosition = new Vector3(3.4f, 0.55f, -6.5f);
            MaterialLibrary.Paint(desk, new Color(0.50f, 0.36f, 0.24f));
        }

        private void SpawnPeople()
        {
            // Receptionist behind the desk and a worker in the back office — ambiance ("see people").
            SpawnNpc(new Vector3(4.8f, 0f, -6.5f), 200f, "talking", null, "");
            SpawnNpc(new Vector3(-4.5f, 0f, 6f), 160f, "typing", null, "");

            // Gatekeeper blocking the doorway to the back office — talk to start the meeting.
            var scenario = GameManager.Instance.SelectedScenario;
            SpawnNpc(new Vector3(0f, 0f, -0.7f), 180f, "idle",
                () => GameManager.Instance.StartCareerCall(scenario),
                "Talk to the gatekeeper (start the meeting)");

            // Exit back to the street, by the entrance.
            var exit = new GameObject("Exit");
            exit.transform.SetParent(_root, false);
            exit.transform.localPosition = new Vector3(0f, 0f, -10.4f);
            var it = exit.AddComponent<Interactable>();
            it.kind = Interactable.Kind.Exit;
            it.label = "Leave (back to the street)";
            it.range = 3.2f;
            it.onInteract = () => GameManager.Instance.GoToCity();
        }

        private void SpawnNpc(Vector3 pos, float yaw, string clip, System.Action onE, string label)
        {
            var worker = OfficeWorker.Spawn(_root, pos, yaw, clip);
            GameObject go;
            if (worker != null)
            {
                go = worker.gameObject;
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.transform.SetParent(_root, false);
                go.transform.position = pos + Vector3.up * 1f;
                MaterialLibrary.Paint(go, new Color(0.46f, 0.42f, 0.38f));
            }
            if (onE == null) return; // pure ambiance, not interactable
            var it = go.AddComponent<Interactable>();
            it.kind = Interactable.Kind.Npc;
            it.label = label;
            it.range = 3.0f;
            it.onInteract = onE;
        }

        // ---- player + camera (mirrors the city/office) -------------------------

        private void SetupCamera()
        {
            GameObject camGo = Camera.main != null ? Camera.main.gameObject : null;
            if (camGo == null)
            {
                camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camGo.tag = "MainCamera";
            }
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
            cam.nearClipPlane = 0.05f;
            var fc = camGo.GetComponent<FollowCamera>();
            if (fc != null) fc.enabled = false; // first-person controller drives the camera

            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = 0.7f;
                sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }

        private void SpawnPlayer(Vector3 pos)
        {
            _player = new GameObject("Player");
            _player.transform.position = pos;

            var cc = _player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            // Real Mixamo body when imported; else the procedural avatar.
            if (OfficeWorker.Attach(_player.transform) == null)
            {
                var body = new GameObject("Body");
                body.transform.SetParent(_player.transform, false);
                body.AddComponent<AvatarBuilder>().SetConfig(GameManager.Instance.Profile != null
                    ? GameManager.Instance.Profile.avatar : new AvatarConfig());
            }

            _fp = _player.AddComponent<FirstPersonController>();
        }

        // ---- HUD ----------------------------------------------------------------

        private void BuildHud()
        {
            var canvas = UiFactory.CreateScreenCanvas("BusinessHud");
            var bar = UiFactory.Panel(canvas.transform, new Color(0f, 0f, 0f, 0.55f), "Prompt");
            var rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0.06f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _prompt = UiFactory.Label(bar.transform, "", 16, UiTheme.TextPrimary, TextAnchor.MiddleCenter);
            UiFactory.Stretch(_prompt.rectTransform);
        }

        private void UpdatePrompt(Interactable near)
        {
            if (_prompt == null) return;
            const string controls = "WASD move    Mouse look    [V] view";
            _prompt.text = near != null ? $"[E] {near.label}      {controls}" : controls;
        }

        private void Interact(Interactable target)
        {
            if (target.onInteract != null) target.onInteract();
        }

        private Interactable FindNearest(Vector3 pos)
        {
            if (_interactables == null) return null;
            Interactable best = null;
            float bestDist = float.MaxValue;
            foreach (var it in _interactables)
            {
                if (it == null) continue;
                float d = Vector3.Distance(pos, it.transform.position);
                if (d <= it.range && d < bestDist) { best = it; bestDist = d; }
            }
            return best;
        }
    }
}
