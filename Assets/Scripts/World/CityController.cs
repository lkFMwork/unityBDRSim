using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using Fitzmark.BDRSim.UI;
using TMPro;
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
        private FirstPersonController _fp;
        private GameObject _car;
        private CarController _carCtrl;
        private Camera _cam;
        private Transform _camPivot;
        private Interactable[] _interactables;
        private bool _inCar;

        private Transform _root;
        private CityTheme _theme;
        private Vector3 _playerSpawn = new Vector3(0f, 0.2f, -8f);
        private TMP_Text _prompt;
        private TMP_Text _callsLabel;
        private float _flashUntil;
        private string _flashText = "";

        private void Start()
        {
            GameManager.Instance.HubScene = SceneNames.City;

            var c = GameManager.Instance.Profile;
            string cityId = GameManager.Instance.ActiveCityId;
            var client = TerritoryRegistry.Get(cityId);
            _theme = CityThemes.For(cityId, client != null ? client.City : "Texas");

            _root = new GameObject("City").transform;
            BuildCity(cityId);

            SetupCamera();
            SpawnPlayer();
            SpawnCar();

            _interactables = Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None);

            BuildHud();
            RefreshCalls();
        }

        private void BuildCity(string cityId)
        {
            int seed = StableHash(string.IsNullOrEmpty(cityId) ? "texas" : cityId);
            var builder = new CityBuilder(_root, _theme, seed);
            builder.Build();
            _playerSpawn = builder.PlayerSpawn;

            // Each business is an enterable client site (press E → gatekeeper fight → meeting),
            // marked by a top-down spotlight + glowing beacon so it's easy to spot and drive to.
            foreach (var (pos, yaw, index) in builder.Businesses)
            {
                var go = new GameObject("Business_" + index);
                go.transform.SetParent(_root, false);
                go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                var it = go.AddComponent<Interactable>();
                it.kind = Interactable.Kind.Client;
                it.label = "Enter business (fight the gatekeeper)";
                it.seed = index;
                it.range = 4f;

                AddBusinessBeacon(go.transform);
            }
        }

        // A downward spotlight + emissive glow column marking an enterable business.
        private void AddBusinessBeacon(Transform parent)
        {
            var lightGo = new GameObject("Spotlight");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.localPosition = new Vector3(0f, 9f, 0f);
            lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // point straight down
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(1f, 0.93f, 0.55f);
            light.intensity = 14f;
            light.range = 16f;
            light.spotAngle = 48f;
            light.shadows = LightShadows.None;

            // A thin glowing beacon column so it reads even in daylight.
            var beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beacon.name = "Beacon";
            beacon.transform.SetParent(parent, false);
            beacon.transform.localPosition = new Vector3(0f, 4f, 0f);
            beacon.transform.localScale = new Vector3(0.25f, 4f, 0.25f);
            var col = beacon.GetComponent<Collider>(); if (col != null) Destroy(col);
            var mat = MaterialLibrary.Get(new Color(1f, 0.88f, 0.4f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.35f) * 2.2f);
            var r = beacon.GetComponent<Renderer>(); if (r != null) r.sharedMaterial = mat;
        }

        private static int StableHash(string s)
        {
            int h = 17;
            foreach (char ch in s) h = unchecked(h * 31 + ch);
            return h;
        }

        private void Update()
        {
            if (_player == null) return;

            Vector3 pos = _inCar ? _car.transform.position : _player.transform.position;
            var near = FindNearest(pos);

            if (Input.GetKeyDown(KeyCode.F)) ToggleCar();
            else if (Input.GetKeyDown(KeyCode.E) && near != null) Interact(near);

            if (_inCar) DriveCamera();
            UpdatePrompt(near);
        }

        // ---- spawn ----------------------------------------------------------

        private void SetupCamera()
        {
            GameObject camGo = Camera.main != null ? Camera.main.gameObject : null;
            if (camGo == null)
            {
                camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camGo.tag = "MainCamera";
            }
            _cam = camGo.GetComponent<Camera>();
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.55f, 0.66f, 0.82f); // daytime sky
            _cam.farClipPlane = 600f;
            var fc = camGo.GetComponent<FollowCamera>();
            if (fc != null) fc.enabled = false; // first-person controller drives the camera

            // Sun so the city isn't flat.
            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = 1.15f;
                sun.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
            }
        }

        private void SpawnPlayer()
        {
            _player = new GameObject("Player");
            _player.transform.position = _playerSpawn;

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

            // Full 360° mouse-look + tight third-person toggle (V), same as the office.
            _fp = _player.AddComponent<FirstPersonController>();
        }

        private void SpawnCar()
        {
            // A real Kenney sedan, auto-scaled (by height so it keeps its proportions),
            // parked one lane to the right of the spawn.
            _car = ModelLibrary.Spawn(CityThemes.CarModel, _root,
                _playerSpawn + new Vector3(3f, 0f, 0f), 0f, 1f,
                placeholderColor: new Color(0.72f, 0.22f, 0.22f), placeholderLabel: false,
                fitHeight: 1.5f);
            _car.name = "Car";
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

            var back = UiFactory.Button(top.transform, "◀ Back to Map",
                () => GameManager.Instance.GoToTexas(), UiTheme.Panel, UiTheme.TextMuted,
                14, TextAnchor.MiddleCenter);
            UiFactory.Size(back.gameObject, prefW: 160f);

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
                _callsLabel.text = $"<b>{_theme.DisplayName}</b>  ·  Day {c.career.day}  ·  " +
                                   $"Calls {c.career.callsRemainingToday}/{CareerSystem.CallsPerDay(c)}  ·  " +
                                   $"drive to a business and press E";
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
                if (_fp != null) _fp.enabled = true;
            }
            else
            {
                if (Vector3.Distance(_player.transform.position, _car.transform.position) > CarEnterRange)
                {
                    Flash("Get closer to the car to drive.");
                    return;
                }
                _inCar = true;
                if (_fp != null) _fp.enabled = false; // FP releases the camera; we chase the car
                _player.SetActive(false);
                _carCtrl.SetDriving(true);
            }
        }

        // While driving, chase the car from behind-and-above (full turning with the car).
        private void DriveCamera()
        {
            if (_cam == null || _car == null) return;
            Vector3 back = _car.transform.forward;
            Vector3 want = _car.transform.position - back * 8f + Vector3.up * 4.5f;
            float t = 1f - Mathf.Exp(-8f * Time.deltaTime);
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, want, t);
            _cam.transform.rotation = Quaternion.LookRotation(
                (_car.transform.position + Vector3.up * 1.2f) - _cam.transform.position);
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
                GameManager.Instance.GoToTexas();
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
            scenario.gatekeeperPresent = true; // a gatekeeper guards the meeting → fight to get in

            // Enter the business: fight the gatekeeper (MK duel), then the meeting.
            GameManager.Instance.HubScene = SceneNames.City;
            GameManager.Instance.StartCareerCall(scenario);
        }

        private void Flash(string message)
        {
            _flashText = message;
            _flashUntil = Time.time + 2.5f;
        }
    }
}
