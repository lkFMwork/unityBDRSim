using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using Fitzmark.BDRSim.UI;
using UnityEngine;
using UnityEngine.UI;
using AvatarBuilder = Fitzmark.BDRSim.UI.AvatarBuilder; // disambiguate from UnityEngine.AvatarBuilder

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Runs the open-world city hub: spawns the player (on foot) and a car,
    /// drives the chase camera, handles walk/drive and proximity interaction, and
    /// launches in-person meetings at client sites. Attach to a GameObject in the
    /// City scene; the static geometry + Interactables are built by the setup tool.
    /// </summary>
    public class CityController : MonoBehaviour
    {
        private const float CarEnterRange = 3.6f;

        private GameObject _player;
        private PlayerController _playerCtrl;
        private GameObject _car;
        private CarController _carCtrl;
        private FollowCamera _cam;
        private Interactable[] _interactables;
        private bool _inCar;

        private Text _prompt;
        private Text _callsLabel;
        private float _flashUntil;
        private string _flashText = "";

        private void Start()
        {
            GameManager.Instance.HubScene = SceneNames.City;

            SetupCamera();
            SpawnPlayer();
            SpawnCar();

            _interactables = Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None);

            BuildHud();
            RefreshCalls();
        }

        private void Update()
        {
            if (_player == null) return;

            Vector3 pos = _inCar ? _car.transform.position : _player.transform.position;
            var near = FindNearest(pos);

            if (Input.GetKeyDown(KeyCode.F)) ToggleCar();
            else if (Input.GetKeyDown(KeyCode.E) && near != null) Interact(near);

            UpdatePrompt(near);
        }

        // ---- spawn ----------------------------------------------------------

        private void SetupCamera()
        {
            GameObject camGo = Camera.main != null ? Camera.main.gameObject : null;
            if (camGo == null)
            {
                camGo = new GameObject("Main Camera", typeof(Camera));
                camGo.tag = "MainCamera";
            }
            _cam = camGo.GetComponent<FollowCamera>();
            if (_cam == null) _cam = camGo.AddComponent<FollowCamera>();
        }

        private void SpawnPlayer()
        {
            _player = new GameObject("Player");
            _player.transform.position = new Vector3(0f, 0.2f, -3f);

            var cc = _player.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 1f, 0f);

            _playerCtrl = _player.AddComponent<PlayerController>();

            var body = new GameObject("Body");
            body.transform.SetParent(_player.transform, false);
            var avatar = body.AddComponent<AvatarBuilder>();
            avatar.SetConfig(GameManager.Instance.Profile != null
                ? GameManager.Instance.Profile.avatar
                : new AvatarConfig());

            _cam.Target = _player.transform;
        }

        private void SpawnCar()
        {
            _car = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _car.name = "Car";
            _car.transform.localScale = new Vector3(1.7f, 0.8f, 3.4f);
            _car.transform.position = new Vector3(4f, 0.4f, -3f);
            var r = _car.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = Mat(new Color(0.72f, 0.22f, 0.22f));
            _carCtrl = _car.AddComponent<CarController>();
        }

        // ---- HUD ------------------------------------------------------------

        private void BuildHud()
        {
            var canvas = UiFactory.CreateScreenCanvas("CityHud");

            var top = UiFactory.Panel(canvas.transform, new Color(0f, 0f, 0f, 0.5f), "TopBar");
            var trt = top.rectTransform;
            trt.anchorMin = new Vector2(0f, 0.92f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            UiFactory.HLayout(top.gameObject, pad: 12, spacing: 10, expandW: true, expandH: true);

            _callsLabel = UiFactory.Label(top.transform, "", 15, UiTheme.TextPrimary,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Size(_callsLabel.gameObject, flexW: 1f);

            var office = UiFactory.Button(top.transform, "Office (Menu)",
                () => GameManager.Instance.ReturnToMenu(), UiTheme.Panel, UiTheme.TextMuted,
                14, TextAnchor.MiddleCenter);
            UiFactory.Size(office.gameObject, prefW: 160f);

            var bar = UiFactory.Panel(canvas.transform, new Color(0f, 0f, 0f, 0.55f), "Prompt");
            var brt = bar.rectTransform;
            brt.anchorMin = new Vector2(0.08f, 0.03f);
            brt.anchorMax = new Vector2(0.92f, 0.11f);
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            _prompt = UiFactory.Label(bar.transform, "", 16, UiTheme.TextPrimary, TextAnchor.MiddleCenter);
            UiFactory.Stretch(_prompt.rectTransform);
        }

        private void RefreshCalls()
        {
            var c = GameManager.Instance.Profile;
            if (c != null)
            {
                CareerSystem.EnsureStarted(c);
                _callsLabel.text = $"Day {c.career.day}  ·  Calls left: " +
                                   $"{c.career.callsRemainingToday}/{CareerSystem.CallsPerDay(c)}  " +
                                   $"·  drive to a client site to set a meeting";
            }
            else
            {
                _callsLabel.text = "No BDR loaded — head to the office.";
            }
        }

        private void UpdatePrompt(Interactable near)
        {
            if (Time.time < _flashUntil)
            {
                _prompt.text = _flashText;
                return;
            }

            string controls = $"WASD move   ·   F {( _inCar ? "exit car" : "drive")}   ·   E interact";
            if (near != null) controls = $"[E] {near.label}      " + controls;
            _prompt.text = controls;
        }

        // ---- interaction ----------------------------------------------------

        private Interactable FindNearest(Vector3 pos)
        {
            Interactable best = null;
            float bestDist = float.MaxValue;
            if (_interactables == null) return null;
            foreach (var it in _interactables)
            {
                if (it == null) continue;
                float d = Vector3.Distance(pos, it.transform.position);
                if (d <= it.range && d < bestDist)
                {
                    bestDist = d;
                    best = it;
                }
            }
            return best;
        }

        private void ToggleCar()
        {
            if (_inCar)
            {
                _inCar = false;
                _carCtrl.SetDriving(false);
                _player.transform.position = _car.transform.position + _car.transform.right * 2.2f + Vector3.up * 0.2f;
                _player.SetActive(true);
                _playerCtrl.SetControlEnabled(true);
                _cam.Target = _player.transform;
            }
            else
            {
                if (Vector3.Distance(_player.transform.position, _car.transform.position) > CarEnterRange)
                {
                    Flash("Get closer to the car to drive.");
                    return;
                }
                _inCar = true;
                _playerCtrl.SetControlEnabled(false);
                _player.SetActive(false);
                _carCtrl.SetDriving(true);
                _cam.Target = _car.transform;
            }
        }

        private void Interact(Interactable target)
        {
            if (target.kind == Interactable.Kind.Office)
            {
                GameManager.Instance.GoToOffice();
                return;
            }

            var c = GameManager.Instance.Profile;
            if (c == null)
            {
                GameManager.Instance.ReturnToMenu();
                return;
            }

            CareerSystem.EnsureStarted(c);
            if (!CareerSystem.HasCallsLeft(c))
            {
                Flash("No calls left today — head to the office (top-right) and End Day.");
                return;
            }

            CareerSystem.ConsumeCall(c);
            c.inPersonMeetings++;
            var doneQuests = QuestSystem.Sync(c);
            if (doneQuests.Count > 0) GameManager.Instance.CareerFlash = QuestSystem.FlashFor(doneQuests);
            GameManager.Instance.SaveProfile();

            int week = CareerSystem.Week(c.career.day);
            int difficulty = Mathf.Clamp(1 + (c.level - 1) / 2 + (week - 1), 1, 10);
            int seed = unchecked(target.seed * 101 + c.career.day * 13 + c.callsMade);
            var scenario = ProspectGenerator.Generate(difficulty, seed);

            GameManager.Instance.HubScene = SceneNames.City;
            GameManager.Instance.StartTravel(scenario, true); // beat the commute level, then meet in person
        }

        private void Flash(string message)
        {
            _flashText = message;
            _flashUntil = Time.time + 2.5f;
        }

        private static Material Mat(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var m = new Material(shader) { color = color };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            return m;
        }
    }
}
