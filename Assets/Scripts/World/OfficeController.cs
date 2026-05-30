using System.Collections.Generic;
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
    /// The FitzMark office — the working hub, built at runtime as a set of walkable rooms,
    /// each one an activity: Reception (leave to the map), the Bullpen (prospect / cold
    /// call), Outreach, the Freight Desk, Operations/CRM, Training (skill tree), Supply &amp;
    /// IT (upgrades + market), records boards, and a break room. Furniture comes from
    /// <see cref="ModelLibrary"/>, so labeled stand-ins today become real models the moment
    /// you drop them into Resources/Models. Walk with WASD, interact with E.
    /// </summary>
    public class OfficeController : MonoBehaviour
    {
        private GameObject _player;
        private PlayerController _playerCtrl;
        private FollowCamera _cam;
        private Transform _root;
        private Canvas _hud;
        private Interactable[] _interactables;

        private TMP_Text _prompt;
        private TMP_Text _callsLabel;
        private GameObject _adviceOverlay;
        private bool _modalOpen;
        private readonly Dictionary<int, int> _tipIndex = new();
        private float _flashUntil;
        private string _flashText = "";

        private void Start()
        {
            GameManager.Instance.HubScene = SceneNames.Office;

            _root = new GameObject("OfficeRooms").transform;
            SetupCamera();
            BuildEnvironment();
            BuildRooms();
            SpawnPlayer();
            SpawnMentors();

            _interactables = Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None);

            BuildHud();
            RefreshCalls();
        }

        private void Update()
        {
            if (_player == null || _adviceOverlay != null || _modalOpen) return;
            var near = FindNearest(_player.transform.position);
            if (Input.GetKeyDown(KeyCode.E) && near != null) Interact(near);
            UpdatePrompt(near);
        }

        // ---- environment ----------------------------------------------------

        private void BuildEnvironment()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(_root, false);
            floor.transform.localScale = new Vector3(3f, 1f, 3f); // 30 x 30
            SetMat(floor, Mat(new Color(0.32f, 0.31f, 0.29f)));

            var carpet = GameObject.CreatePrimitive(PrimitiveType.Plane);
            carpet.name = "Carpet";
            carpet.transform.SetParent(_root, false);
            carpet.transform.localScale = new Vector3(1.4f, 1f, 1.4f);
            carpet.transform.position = new Vector3(0f, 0.01f, 3f);
            var cc = carpet.GetComponent<Collider>(); if (cc != null) Destroy(cc);
            SetMat(carpet, Mat(new Color(0.22f, 0.27f, 0.34f)));

            Wall("Wall_N", new Vector3(0f, 1.6f, 15f), new Vector3(30f, 3.2f, 0.4f));
            Wall("Wall_S", new Vector3(0f, 1.6f, -15f), new Vector3(30f, 3.2f, 0.4f));
            Wall("Wall_E", new Vector3(15f, 1.6f, 0f), new Vector3(0.4f, 3.2f, 30f));
            Wall("Wall_W", new Vector3(-15f, 1.6f, 0f), new Vector3(0.4f, 3.2f, 30f));

            // Partial dividers suggest wings without trapping the player.
            Divider(new Vector3(4.5f, 0.7f, 2f), new Vector3(0.3f, 1.4f, 9f));
            Divider(new Vector3(-4.5f, 0.7f, 3f), new Vector3(0.3f, 1.4f, 9f));
            Divider(new Vector3(-9.5f, 0.7f, -2f), new Vector3(7f, 1.4f, 0.3f));
        }

        private void Wall(string name, Vector3 pos, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            SetMat(go, Mat(new Color(0.34f, 0.36f, 0.42f)));
        }

        private void Divider(Vector3 pos, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Divider";
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            SetMat(go, Mat(new Color(0.40f, 0.42f, 0.48f)));
        }

        // ---- rooms ----------------------------------------------------------

        private void BuildRooms()
        {
            // Reception (south) — leave, plus the records boards.
            Prop("office/reception", new Vector3(0f, 0f, -13.2f), 0f, new Vector3(3f, 1.1f, 1.2f), new Color(0.55f, 0.42f, 0.30f));
            Prop("office/plant", new Vector3(-3.2f, 0f, -13.6f), 0f, new Vector3(0.6f, 1.4f, 0.6f), new Color(0.26f, 0.50f, 0.28f));
            Prop("office/plant", new Vector3(3.2f, 0f, -13.6f), 0f, new Vector3(0.6f, 1.4f, 0.6f), new Color(0.26f, 0.50f, 0.28f), label: false);
            Prop("office/trophycase", new Vector3(6f, 0f, -13.4f), 0f, new Vector3(1.2f, 1.6f, 0.5f), new Color(0.85f, 0.72f, 0.30f));
            Prop("office/rankings_board", new Vector3(-6f, 0f, -13.4f), 0f, new Vector3(1.4f, 1.6f, 0.2f), new Color(0.70f, 0.74f, 0.80f));
            RoomSign("RECEPTION", new Vector3(0f, 3.2f, -13f));
            Zone("Leave to the overworld ▶", new Vector3(0f, 0f, -14.2f), 3.6f,
                () => GameManager.Instance.GoToTexas(), new Color(0.30f, 0.55f, 0.95f));
            Zone("Trophies", new Vector3(6f, 0f, -12.3f), 2.6f,
                () => OpenModal((cv, close) => new AchievementsView(cv, Profile, close).Open()), new Color(0.90f, 0.78f, 0.34f));
            Zone("Rankings", new Vector3(-6f, 0f, -12.3f), 2.6f,
                () => OpenModal((cv, close) => new LeaderboardView(cv, Profile, close).Open()), new Color(0.74f, 0.78f, 0.84f));

            // Freight desk + assignments (center).
            Prop("office/desk", new Vector3(0f, 0f, 3f), 180f, new Vector3(2.6f, 1f, 1.3f), new Color(0.46f, 0.34f, 0.26f));
            Prop("office/board", new Vector3(-3.6f, 0f, 6.4f), 0f, new Vector3(1.4f, 1.6f, 0.2f), new Color(0.36f, 0.62f, 0.64f));
            RoomSign("FREIGHT DESK", new Vector3(0f, 3.0f, 3f));
            Zone("Your freight book ▶", new Vector3(0f, 0f, 1.4f), 3f,
                () => GameManager.Instance.GoToFreightDesk(), new Color(0.34f, 0.78f, 0.56f));
            Zone("Quest board", new Vector3(-3.6f, 0f, 5.2f), 2.6f,
                () => OpenModal((cv, close) => new QuestLogView(cv, Profile, close).Open()), new Color(0.40f, 0.78f, 0.80f));

            // Bullpen / prospecting (east).
            var deskBrown = new Color(0.46f, 0.34f, 0.26f);
            Prop("office/desk", new Vector3(7f, 0f, -2f), 0f, new Vector3(2.2f, 1f, 1.2f), deskBrown);
            Prop("office/desk", new Vector3(11f, 0f, -2f), 0f, new Vector3(2.2f, 1f, 1.2f), deskBrown, label: false);
            Prop("office/desk", new Vector3(7f, 0f, 3f), 0f, new Vector3(2.2f, 1f, 1.2f), deskBrown, label: false);
            Prop("office/desk", new Vector3(11f, 0f, 3f), 0f, new Vector3(2.2f, 1f, 1.2f), deskBrown, label: false);
            Prop("office/computer", new Vector3(9f, 0f, -3.4f), 0f, new Vector3(0.7f, 0.6f, 0.5f), new Color(0.20f, 0.22f, 0.26f));
            RoomSign("BULLPEN — PROSPECTING", new Vector3(9f, 3.0f, 2.5f));
            Zone("Work a cold call", new Vector3(9f, 0f, -2.6f), 3f, MakeDeskCall, new Color(0.34f, 0.80f, 0.44f));

            // Outreach (north-east).
            Prop("office/outreach_desk", new Vector3(9f, 0f, 10.5f), 0f, new Vector3(2.4f, 1f, 1.2f), new Color(0.30f, 0.45f, 0.70f));
            RoomSign("OUTREACH", new Vector3(9f, 3.0f, 10.5f));
            Zone("Email & social outreach", new Vector3(9f, 0f, 9f), 3f,
                () => OpenModal((cv, close) => new OutreachView(cv, Profile, close).Open()), new Color(0.30f, 0.62f, 1f));

            // Operations / CRM (north-west).
            Prop("office/exec_desk", new Vector3(-9f, 0f, 10.5f), 0f, new Vector3(3f, 1f, 1.5f), new Color(0.38f, 0.30f, 0.24f));
            RoomSign("OPERATIONS — CRM", new Vector3(-9f, 3.0f, 10.5f));
            Zone("CRM dashboard", new Vector3(-9f, 0f, 8.8f), 3f,
                () => OpenModal((cv, close) => new CrmView(cv, Profile, close).Open()), new Color(0.45f, 0.60f, 0.95f));

            // Training (west).
            Prop("office/whiteboard", new Vector3(-12.6f, 0f, 2.5f), 90f, new Vector3(0.3f, 2f, 3f), new Color(0.86f, 0.88f, 0.90f));
            RoomSign("TRAINING", new Vector3(-9.5f, 3.0f, 2.5f));
            Zone("Train — skill tree", new Vector3(-9.5f, 0f, 1.5f), 3f,
                () => OpenModal((cv, close) => new SkillTreeView(cv, Profile, close).Open()), new Color(0.66f, 0.50f, 0.90f));

            // Supply & IT (south-west).
            Prop("office/shelf", new Vector3(-12.6f, 0f, -7.5f), 90f, new Vector3(0.5f, 2f, 3f), new Color(0.50f, 0.52f, 0.56f));
            Prop("office/server", new Vector3(-9.5f, 0f, -9.5f), 0f, new Vector3(1f, 2f, 1f), new Color(0.20f, 0.22f, 0.26f));
            RoomSign("SUPPLY & IT", new Vector3(-10.5f, 3.0f, -8f));
            Zone("Buy upgrades", new Vector3(-9.5f, 0f, -6.5f), 3f,
                () => OpenModal((cv, close) => new UpgradesView(cv, Profile, close).Open()), new Color(0.93f, 0.70f, 0.28f));
            Zone("Freight market", new Vector3(-12.6f, 0f, -4.5f), 2.6f,
                () => OpenModal((cv, close) => new MarketView(cv, Profile, close).Open()), new Color(0.92f, 0.52f, 0.34f));

            // Break room (north-center).
            Prop("office/couch", new Vector3(0f, 0f, 12.8f), 0f, new Vector3(2.6f, 0.8f, 1f), new Color(0.30f, 0.42f, 0.40f));
            Prop("office/watercooler", new Vector3(2.8f, 0f, 12.8f), 0f, new Vector3(0.6f, 1.5f, 0.6f), new Color(0.40f, 0.60f, 0.80f));
            RoomSign("BREAK ROOM", new Vector3(0f, 3.0f, 12.8f));
            Zone("Grab coffee", new Vector3(0f, 0f, 11.2f), 3f,
                () => Flash("☕  Coffee break. Back to the grind."), new Color(0.62f, 0.46f, 0.32f));
        }

        private GameObject Prop(string key, Vector3 pos, float yaw, Vector3 size, Color color, bool label = false)
            => ModelLibrary.Spawn(key, _root, pos, yaw, 1f, size, color, false); // labels off (debug clutter)

        private Interactable Zone(string label, Vector3 pos, float range, System.Action action, Color markerColor)
        {
            var go = new GameObject("Zone:" + label);
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            var it = go.AddComponent<Interactable>();
            it.label = label;
            it.range = range;
            it.onInteract = action;
            return it;
        }

        private void RoomSign(string text, Vector3 pos)
        {
            // Disabled: the big floating signs read as debug clutter. Real wall-mounted
            // signage comes when the office is rebuilt (first-person) around imported models.
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
            _cam.Offset = new Vector3(0f, 11f, -10f);
        }

        private void SpawnPlayer()
        {
            _player = new GameObject("Player");
            _player.transform.position = new Vector3(0f, 0.2f, -11f);

            var cc = _player.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 1f, 0f);

            _playerCtrl = _player.AddComponent<PlayerController>();

            var body = new GameObject("Body");
            body.transform.SetParent(_player.transform, false);
            var avatar = body.AddComponent<AvatarBuilder>();
            avatar.SetConfig(Profile != null ? Profile.avatar : new AvatarConfig());

            _cam.Target = _player.transform;
        }

        private void SpawnMentors()
        {
            Vector3[] spots =
            {
                new Vector3(7f, 0.2f, 5f),    // bullpen
                new Vector3(11f, 0.2f, 5f),
                new Vector3(-9f, 0.2f, 4f)    // training
            };

            for (int i = 0; i < MentorLibrary.Count; i++)
            {
                var mentor = MentorLibrary.Get(i);
                var go = new GameObject("Mentor_" + mentor.Id);
                go.transform.position = i < spots.Length ? spots[i] : new Vector3(-8f + i * 2f, 0.2f, 7f);
                go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                var body = new GameObject("Body");
                body.transform.SetParent(go.transform, false);
                var avatar = body.AddComponent<AvatarBuilder>();
                avatar.SetConfig(MentorAvatar(i));

                var it = go.AddComponent<Interactable>();
                it.kind = Interactable.Kind.Npc;
                it.label = mentor.Name;
                it.seed = i;
                it.range = 3.5f;
            }
        }

        private static AvatarConfig MentorAvatar(int i) => new AvatarConfig
        {
            skinTone = i % AvatarPalette.SkinCount,
            outfitColor = (i + 2) % AvatarPalette.OutfitCount,
            accentColor = i % AvatarPalette.AccentCount,
            hairColor = (i + 1) % AvatarPalette.HairCount,
            build = 1,
            height = 1f
        };

        // ---- HUD ------------------------------------------------------------

        private void BuildHud()
        {
            _hud = UiFactory.CreateScreenCanvas("OfficeHud");

            var top = UiFactory.Panel(_hud.transform, new Color(0f, 0f, 0f, 0.5f), "TopBar");
            var trt = top.rectTransform;
            trt.anchorMin = new Vector2(0f, 0.92f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            UiFactory.HLayout(top.gameObject, pad: 12, spacing: 10, expandW: true, expandH: true);

            _callsLabel = UiFactory.Label(top.transform, "", 15, UiTheme.TextPrimary,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Size(_callsLabel.gameObject, flexW: 1f);

            var leave = UiFactory.Button(top.transform, "Leave ▶",
                () => GameManager.Instance.GoToTexas(), UiTheme.Panel, UiTheme.TextMuted,
                14, TextAnchor.MiddleCenter);
            UiFactory.Size(leave.gameObject, prefW: 120f);

            var bar = UiFactory.Panel(_hud.transform, new Color(0f, 0f, 0f, 0.55f), "Prompt");
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
            var c = Profile;
            if (c != null)
            {
                CareerSystem.EnsureStarted(c);
                _callsLabel.text = $"Day {c.career.day}  ·  ${c.cash:N0}  ·  Calls left: " +
                                   $"{c.career.callsRemainingToday}/{CareerSystem.CallsPerDay(c)}  " +
                                   $"·  walk the rooms (WASD), interact (E)";
            }
            else
            {
                _callsLabel.text = "No BDR loaded.";
            }
        }

        private void UpdatePrompt(Interactable near)
        {
            if (Time.time < _flashUntil) { _prompt.text = _flashText; return; }
            string controls = "WASD move   ·   E interact";
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
                if (d <= it.range && d < bestDist) { bestDist = d; best = it; }
            }
            return best;
        }

        private void Interact(Interactable target)
        {
            if (target.onInteract != null) { target.onInteract(); return; }
            switch (target.kind)
            {
                case Interactable.Kind.Npc: ShowAdvice(target.seed); break;
                case Interactable.Kind.Desk: MakeDeskCall(); break;
                case Interactable.Kind.Exit: GameManager.Instance.GoToTexas(); break;
            }
        }

        private void OpenModal(System.Action<Transform, System.Action> open)
        {
            if (_modalOpen || _hud == null) return;
            _modalOpen = true;
            if (_playerCtrl != null) _playerCtrl.SetControlEnabled(false);
            open(_hud.transform, CloseModal);
        }

        private void CloseModal()
        {
            _modalOpen = false;
            if (_playerCtrl != null) _playerCtrl.SetControlEnabled(true);
            RefreshCalls();
        }

        private void MakeDeskCall()
        {
            var c = Profile;
            if (c == null) { GameManager.Instance.GoToTexas(); return; }

            CareerSystem.EnsureStarted(c);
            if (!CareerSystem.HasCallsLeft(c))
            {
                Flash("No calls left today — leave to the map and turn in the day.");
                return;
            }

            CareerSystem.ConsumeCall(c);
            GameManager.Instance.SaveProfile();

            int week = CareerSystem.Week(c.career.day);
            int difficulty = Mathf.Clamp(1 + (c.level - 1) / 2 + (week - 1), 1, 10);
            int seed = unchecked(c.career.day * 17 + c.callsMade * 3 + 91);
            var scenario = ProspectGenerator.Generate(difficulty, seed);

            GameManager.Instance.HubScene = SceneNames.Office;
            GameManager.Instance.StartCareerCall(scenario);
        }

        private void ShowAdvice(int mentorIndex)
        {
            var mentor = MentorLibrary.Get(mentorIndex);
            if (mentor == null) return;

            var profile = Profile;
            if (profile != null)
            {
                profile.mentorTalks++;
                var done = QuestSystem.Sync(profile);
                if (done.Count > 0) GameManager.Instance.CareerFlash = QuestSystem.FlashFor(done);
                GameManager.Instance.SaveProfile();
            }

            int tip = _tipIndex.TryGetValue(mentorIndex, out var t) ? t : 0;
            _tipIndex[mentorIndex] = tip + 1;
            string advice = mentor.Tips.Length > 0 ? mentor.Tips[tip % mentor.Tips.Length] : "...";

            _playerCtrl.SetControlEnabled(false);
            _modalOpen = true;

            Transform parent = _hud != null ? _hud.transform : UiFactory.CreateScreenCanvas("AdviceCanvas").transform;

            var overlay = UiFactory.Panel(parent, new Color(0f, 0f, 0f, 0.8f), "AdviceOverlay");
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.VLayout(overlay.gameObject, pad: 40, spacing: 14, expandH: true,
                align: TextAnchor.MiddleCenter);
            _adviceOverlay = overlay.gameObject;

            var card = UiFactory.Panel(overlay.transform, UiTheme.Panel, "Card").gameObject;
            UiFactory.VLayout(card, pad: 24, spacing: 12, expandH: false, align: TextAnchor.UpperCenter);
            UiFactory.Size(card, prefW: 640f, minH: 220f);

            UiFactory.Label(card.transform, mentor.Name, 24, UiTheme.AccentStrong,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Label(card.transform, mentor.Title, 14, UiTheme.TextMuted,
                TextAnchor.MiddleCenter, FontStyle.Italic);
            UiFactory.Label(card.transform, $"“{advice}”", 17, UiTheme.TextPrimary,
                TextAnchor.MiddleCenter);

            var close = UiFactory.Button(card.transform, "Thanks",
                CloseAdvice, UiTheme.Accent, UiTheme.TextPrimary, 16, TextAnchor.MiddleCenter);
            UiFactory.Size(close.gameObject, prefW: 200f, prefH: 48f);
        }

        private void CloseAdvice()
        {
            if (_adviceOverlay != null) Destroy(_adviceOverlay);
            _adviceOverlay = null;
            _modalOpen = false;
            if (_playerCtrl != null) _playerCtrl.SetControlEnabled(true);
        }

        private void Flash(string message)
        {
            _flashText = message;
            _flashUntil = Time.time + 2.5f;
        }

        private static BDRCharacter Profile => GameManager.Instance.Profile;

        private static Material Mat(Color c) => MaterialLibrary.Get(c);

        private static void SetMat(GameObject go, Material m)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null && m != null) r.sharedMaterial = m;
        }
    }
}
