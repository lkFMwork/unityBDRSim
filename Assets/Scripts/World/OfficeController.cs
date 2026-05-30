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
    /// The FitzMark office — a first-person, walkable building of enclosed rooms off a
    /// central corridor, each room an activity: Reception (leave to the map, trophies,
    /// rankings, quests), Bullpen (cold call), Operations (CRM), Training (skill tree),
    /// Outreach, Supply &amp; IT (upgrades + market), Freight Desk, and a break room. Rooms,
    /// doorways and lighting are built at runtime via <see cref="RoomBuilder"/>; furniture
    /// and people are real models via <see cref="ModelLibrary"/> / <see cref="OfficeWorker"/>.
    /// Move with WASD + mouse, interact with E, toggle first/third person with V.
    /// </summary>
    public class OfficeController : MonoBehaviour
    {
        private const float H = 3.2f;                          // room height
        private static readonly Color WallCol = new(0.82f, 0.83f, 0.87f);
        private static readonly Color CeilCol = new(0.90f, 0.91f, 0.94f);
        private static readonly Color LightCol = new(1f, 0.96f, 0.86f);
        private static readonly Color DeskBrown = new(0.50f, 0.36f, 0.24f);
        private static readonly Color Dark = new(0.18f, 0.20f, 0.24f);

        private GameObject _player;
        private FirstPersonController _fp;
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
        private int _mentorsPlaced;

        private void Start()
        {
            GameManager.Instance.HubScene = SceneNames.Office;

            _root = new GameObject("OfficeRooms").transform;
            SetupCamera();
            BuildShell();
            BuildRooms();
            SpawnPlayer();

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

        // ---- shell (corridor + lighting) ------------------------------------

        private void BuildShell()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.43f, 0.48f);
            RenderSettings.fog = false;

            // Central corridor running south(-z, reception) to north(+z, break room).
            var cMin = new Vector3(-3f, 0f, -16f);
            var cMax = new Vector3(3f, 0f, 16f);
            RoomBuilder.Floor(_root, cMin, cMax, new Color(0.30f, 0.33f, 0.40f));
            RoomBuilder.Ceiling(_root, cMin, cMax, H, CeilCol);

            float t = RoomBuilder.WallThickness;
            // South wall with the exit doorway; solid north wall.
            RoomBuilder.WallX(_root, -3f, 3f, -16f + t / 2f, 0f, H, WallCol, 0f);
            RoomBuilder.WallX(_root, -3f, 3f, 16f - t / 2f, 0f, H, WallCol);
            // Corridor side walls only in the reception (south) and break (north) caps;
            // the six rooms supply the side walls (with doors) in between.
            RoomBuilder.WallZ(_root, -16f, -12f, -3f + t / 2f, 0f, H, WallCol);
            RoomBuilder.WallZ(_root, -16f, -12f, 3f - t / 2f, 0f, H, WallCol);
            RoomBuilder.WallZ(_root, 12f, 16f, -3f + t / 2f, 0f, H, WallCol);
            RoomBuilder.WallZ(_root, 12f, 16f, 3f - t / 2f, 0f, H, WallCol);

            RoomBuilder.CeilingLight(_root, new Vector3(0f, H - 0.15f, -13f), LightCol);
            RoomBuilder.CeilingLight(_root, new Vector3(0f, H - 0.15f, -4f), LightCol);
            RoomBuilder.CeilingLight(_root, new Vector3(0f, H - 0.15f, 4f), LightCol);
            RoomBuilder.CeilingLight(_root, new Vector3(0f, H - 0.15f, 14f), LightCol);
        }

        // ---- rooms ----------------------------------------------------------

        private void BuildRooms()
        {
            // West wing (doors face east, onto the corridor).
            BuildBullpen(new Vector3(-13f, 0f, -12f), new Vector3(-3f, 0f, -4f));
            BuildOperations(new Vector3(-13f, 0f, -4f), new Vector3(-3f, 0f, 4f));
            BuildTraining(new Vector3(-13f, 0f, 4f), new Vector3(-3f, 0f, 12f));

            // East wing (doors face west).
            BuildOutreach(new Vector3(3f, 0f, -12f), new Vector3(13f, 0f, -4f));
            BuildSupply(new Vector3(3f, 0f, -4f), new Vector3(13f, 0f, 4f));
            BuildFreight(new Vector3(3f, 0f, 4f), new Vector3(13f, 0f, 12f));

            BuildReception();
            BuildBreakRoom();
        }

        private void BuildBullpen(Vector3 min, Vector3 max)
        {
            RoomBuilder.Room(_root, min, max, H, WallCol, new Color(0.34f, 0.36f, 0.42f), CeilCol,
                RoomBuilder.Side.E, LightCol);
            Workstation(new Vector3(-11.5f, 0f, -10f), 90f);
            Workstation(new Vector3(-11.5f, 0f, -6f), 90f);
            Workstation(new Vector3(-5f, 0f, -10.5f), 0f);
            Prop("plant", new Vector3(-4f, 0f, -5f), 0f, new Vector3(0.6f, 1.3f, 0.6f), Dark);
            Zone("Work a cold call", new Vector3(-8f, 0f, -8f), 3f, MakeDeskCall, default);
            if (MixamoLibrary.Available)
            {
                OfficeWorker.Spawn(_root, new Vector3(-11f, 0f, -10f), 90f, "typing");
                OfficeWorker.Spawn(_root, new Vector3(-11f, 0f, -6f), 90f, "phone");
            }
            SpawnMentor(new Vector3(-6f, 0f, -6f), 120f);
        }

        private void BuildOperations(Vector3 min, Vector3 max)
        {
            RoomBuilder.Room(_root, min, max, H, WallCol, new Color(0.30f, 0.34f, 0.40f), CeilCol,
                RoomBuilder.Side.E, LightCol);
            Prop("office/exec_desk", new Vector3(-11f, 0f, 0f), 90f, new Vector3(1.8f, 0.78f, 0.9f), DeskBrown);
            Prop("computer", new Vector3(-11f, 0.78f, 0f), 90f, new Vector3(0.45f, 0.42f, 0.12f), Dark);
            Prop("office/shelf", new Vector3(-12.6f, 0f, 3f), 90f, new Vector3(0.5f, 2f, 1.2f), DeskBrown);
            Zone("CRM dashboard", new Vector3(-8f, 0f, 0f), 3f,
                () => OpenModal((cv, c) => new CrmView(cv, Profile, c).Open()), default);
            Zone("Quest board", new Vector3(-5.5f, 0f, 2.5f), 2.6f,
                () => OpenModal((cv, c) => new QuestLogView(cv, Profile, c).Open()), default);
            SpawnMentor(new Vector3(-6f, 0f, 2f), 120f);
        }

        private void BuildTraining(Vector3 min, Vector3 max)
        {
            RoomBuilder.Room(_root, min, max, H, WallCol, new Color(0.32f, 0.36f, 0.34f), CeilCol,
                RoomBuilder.Side.E, LightCol);
            Prop("office/whiteboard", new Vector3(-12.7f, 1.2f, 8f), 90f, new Vector3(0.1f, 1.4f, 3f),
                new Color(0.9f, 0.92f, 0.95f));
            Prop("table", new Vector3(-8f, 0f, 8f), 0f, new Vector3(1.8f, 0.78f, 1.1f), DeskBrown);
            Prop("chair", new Vector3(-8f, 0f, 6.6f), 0f, new Vector3(0.55f, 1f, 0.55f), Dark);
            Prop("chair", new Vector3(-8f, 0f, 9.4f), 180f, new Vector3(0.55f, 1f, 0.55f), Dark);
            Zone("Train — skill tree", new Vector3(-8f, 0f, 8f), 3f,
                () => OpenModal((cv, c) => new SkillTreeView(cv, Profile, c).Open()), default);
            SpawnMentor(new Vector3(-6f, 0f, 10f), 120f);
        }

        private void BuildOutreach(Vector3 min, Vector3 max)
        {
            RoomBuilder.Room(_root, min, max, H, WallCol, new Color(0.30f, 0.33f, 0.42f), CeilCol,
                RoomBuilder.Side.W, LightCol);
            Workstation(new Vector3(11.5f, 0f, -10f), 270f);
            Workstation(new Vector3(11.5f, 0f, -6f), 270f);
            Prop("plant", new Vector3(4f, 0f, -5f), 0f, new Vector3(0.6f, 1.3f, 0.6f), Dark);
            Zone("Email & social outreach", new Vector3(8f, 0f, -8f), 3f,
                () => OpenModal((cv, c) => new OutreachView(cv, Profile, c).Open()), default);
            if (MixamoLibrary.Available)
                OfficeWorker.Spawn(_root, new Vector3(11f, 0f, -10f), 270f, "typing");
        }

        private void BuildSupply(Vector3 min, Vector3 max)
        {
            RoomBuilder.Room(_root, min, max, H, WallCol, new Color(0.36f, 0.34f, 0.30f), CeilCol,
                RoomBuilder.Side.W, LightCol);
            Prop("office/shelf", new Vector3(12.6f, 0f, -1f), 270f, new Vector3(0.5f, 2f, 1.4f), DeskBrown);
            Prop("office/server", new Vector3(12.6f, 0f, 2.5f), 270f, new Vector3(0.7f, 2f, 0.7f), Dark);
            Zone("Buy upgrades", new Vector3(8f, 0f, -1f), 3f,
                () => OpenModal((cv, c) => new UpgradesView(cv, Profile, c).Open()), default);
            Zone("Freight market", new Vector3(8f, 0f, 2.5f), 2.6f,
                () => OpenModal((cv, c) => new MarketView(cv, Profile, c).Open()), default);
        }

        private void BuildFreight(Vector3 min, Vector3 max)
        {
            RoomBuilder.Room(_root, min, max, H, WallCol, new Color(0.30f, 0.36f, 0.36f), CeilCol,
                RoomBuilder.Side.W, LightCol);
            Prop("desk", new Vector3(11.5f, 0f, 8f), 270f, new Vector3(1.6f, 0.78f, 0.85f), DeskBrown);
            Prop("computer", new Vector3(11.5f, 0.78f, 8f), 270f, new Vector3(0.45f, 0.42f, 0.12f), Dark);
            Prop("office/board", new Vector3(12.7f, 1.3f, 10.5f), 270f, new Vector3(0.1f, 1.2f, 1.6f),
                new Color(0.36f, 0.62f, 0.64f));
            Zone("Your freight book ▶", new Vector3(8f, 0f, 8f), 3f,
                () => GameManager.Instance.GoToFreightDesk(), default);
            if (MixamoLibrary.Available)
                OfficeWorker.Spawn(_root, new Vector3(11f, 0f, 8f), 270f, "typing");
        }

        private void BuildReception()
        {
            Prop("office/reception", new Vector3(-1f, 0f, -12.8f), 0f, new Vector3(2.2f, 1.1f, 0.8f),
                new Color(0.55f, 0.42f, 0.30f));
            Prop("plant", new Vector3(-2.4f, 0f, -15.2f), 0f, new Vector3(0.6f, 1.4f, 0.6f), Dark);
            Prop("plant", new Vector3(2.4f, 0f, -15.2f), 0f, new Vector3(0.6f, 1.4f, 0.6f), Dark);
            Prop("office/trophycase", new Vector3(-2.6f, 0f, -13.5f), 90f, new Vector3(0.4f, 1.6f, 1f),
                new Color(0.85f, 0.72f, 0.30f));
            Prop("office/rankings_board", new Vector3(2.6f, 0f, -13.5f), 270f, new Vector3(0.4f, 1.6f, 1f),
                new Color(0.70f, 0.74f, 0.80f));

            Zone("Leave to the overworld ▶", new Vector3(0f, 0f, -15.4f), 2.6f,
                () => GameManager.Instance.GoToTexas(), default);
            Zone("Trophies", new Vector3(-2.2f, 0f, -13.5f), 2f,
                () => OpenModal((cv, c) => new AchievementsView(cv, Profile, c).Open()), default);
            Zone("Rankings", new Vector3(2.2f, 0f, -13.5f), 2f,
                () => OpenModal((cv, c) => new LeaderboardView(cv, Profile, c).Open()), default);
        }

        private void BuildBreakRoom()
        {
            Prop("office/couch", new Vector3(0f, 0f, 15f), 180f, new Vector3(2.4f, 0.8f, 0.9f),
                new Color(0.30f, 0.42f, 0.40f));
            Prop("table", new Vector3(0f, 0f, 13.4f), 0f, new Vector3(1.2f, 0.6f, 0.8f), DeskBrown);
            Prop("office/watercooler", new Vector3(2.2f, 0f, 14.8f), 0f, new Vector3(0.6f, 1.5f, 0.6f),
                new Color(0.40f, 0.60f, 0.80f));
            Zone("Grab coffee", new Vector3(0f, 0f, 13.2f), 2.6f,
                () => Flash("☕  Coffee break. Back to the grind."), default);
        }

        // A desk + monitor + chair workstation facing `yaw` (degrees).
        private void Workstation(Vector3 deskPos, float yaw)
        {
            Prop("desk", deskPos, yaw, new Vector3(1.5f, 0.78f, 0.8f), DeskBrown);
            Prop("computer", deskPos + new Vector3(0f, 0.78f, 0f), yaw, new Vector3(0.45f, 0.42f, 0.12f), Dark);
            Vector3 fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Prop("chair", deskPos - fwd * 0.8f, yaw + 180f, new Vector3(0.55f, 1f, 0.55f), Dark);
        }

        // Real model auto-fitted to `size` metres; clean unlabeled placeholder if missing.
        private GameObject Prop(string key, Vector3 pos, float yaw, Vector3 size, Color color)
            => ModelLibrary.Spawn(key, _root, pos, yaw, 1f, size, color, false, fitSize: size);

        private Interactable Zone(string label, Vector3 pos, float range, System.Action action, Color _)
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

        // ---- player / mentors ----------------------------------------------

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
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.07f);
            var fc = camGo.GetComponent<FollowCamera>();
            if (fc != null) fc.enabled = false; // first-person controller drives the camera
        }

        private void SpawnPlayer()
        {
            _player = new GameObject("Player");
            _player.transform.position = new Vector3(0f, 0.2f, -14.5f);
            _player.transform.rotation = Quaternion.identity; // face north, up the corridor

            var cc = _player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            if (OfficeWorker.Attach(_player.transform) == null)
            {
                var body = new GameObject("Body");
                body.transform.SetParent(_player.transform, false);
                body.AddComponent<AvatarBuilder>().SetConfig(Profile != null ? Profile.avatar : new AvatarConfig());
            }

            _fp = _player.AddComponent<FirstPersonController>();
        }

        private void SpawnMentor(Vector3 pos, float yaw)
        {
            int i = _mentorsPlaced++;
            if (i >= MentorLibrary.Count) return;
            var mentor = MentorLibrary.Get(i);

            var go = new GameObject("Mentor_" + mentor.Id);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (OfficeWorker.Spawn(go.transform, pos, yaw, "talking") == null)
            {
                var body = new GameObject("Body");
                body.transform.SetParent(go.transform, false);
                body.AddComponent<AvatarBuilder>().SetConfig(MentorAvatar(i));
            }

            var it = go.AddComponent<Interactable>();
            it.kind = Interactable.Kind.Npc;
            it.label = mentor.Name;
            it.seed = i;
            it.range = 3f;
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
            brt.anchorMin = new Vector2(0.06f, 0.03f);
            brt.anchorMax = new Vector2(0.94f, 0.10f);
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
                                   $"{c.career.callsRemainingToday}/{CareerSystem.CallsPerDay(c)}";
            }
            else
            {
                _callsLabel.text = "No BDR loaded.";
            }
        }

        private void UpdatePrompt(Interactable near)
        {
            if (Time.time < _flashUntil) { _prompt.text = _flashText; return; }
            string controls = "Mouse look · WASD move · E interact · V view";
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
            if (_fp != null) _fp.SetControlEnabled(false);
            open(_hud.transform, CloseModal);
        }

        private void CloseModal()
        {
            _modalOpen = false;
            if (_fp != null) _fp.SetControlEnabled(true);
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

            if (_fp != null) _fp.SetControlEnabled(false);
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
            if (_fp != null) _fp.SetControlEnabled(true);
        }

        private void Flash(string message)
        {
            _flashText = message;
            _flashUntil = Time.time + 2.5f;
        }

        private static BDRCharacter Profile => GameManager.Instance.Profile;
    }
}
