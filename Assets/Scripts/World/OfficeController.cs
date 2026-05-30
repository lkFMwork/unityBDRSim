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
    /// The FitzMark office hub: walk around, talk to senior-rep mentors for advice,
    /// make calls from your desk, and head out to the city. Spawns the player and
    /// the mentor NPCs at runtime; static geometry (rooms, furniture, desk and exit
    /// markers) is built by the setup tool.
    /// </summary>
    public class OfficeController : MonoBehaviour
    {
        private GameObject _player;
        private PlayerController _playerCtrl;
        private FollowCamera _cam;
        private Interactable[] _interactables;

        private TMP_Text _prompt;
        private TMP_Text _callsLabel;
        private GameObject _adviceOverlay;
        private readonly Dictionary<int, int> _tipIndex = new();
        private float _flashUntil;
        private string _flashText = "";

        private void Start()
        {
            GameManager.Instance.HubScene = SceneNames.Office;

            SetupCamera();
            SpawnPlayer();
            SpawnMentors();

            _interactables = Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None);

            BuildHud();
            RefreshCalls();
        }

        private void Update()
        {
            if (_player == null || _adviceOverlay != null) return; // modal advice pauses interaction

            var near = FindNearest(_player.transform.position);
            if (Input.GetKeyDown(KeyCode.E) && near != null) Interact(near);
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
            _cam.Offset = new Vector3(0f, 8f, -8f);
        }

        private void SpawnPlayer()
        {
            _player = new GameObject("Player");
            _player.transform.position = new Vector3(0f, 0.2f, -12f);

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

        private void SpawnMentors()
        {
            Vector3[] spots =
            {
                new Vector3(-10f, 0.2f, 9f),
                new Vector3(-6f, 0.2f, 11f),
                new Vector3(8f, 0.2f, 10f)
            };

            for (int i = 0; i < MentorLibrary.Count; i++)
            {
                var mentor = MentorLibrary.Get(i);
                var go = new GameObject("Mentor_" + mentor.Id);
                go.transform.position = i < spots.Length ? spots[i] : new Vector3(-8f + i * 2f, 0.2f, 9f);
                go.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // face the room

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
            var canvas = UiFactory.CreateScreenCanvas("OfficeHud");

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

            var menu = UiFactory.Button(top.transform, "Menu",
                () => GameManager.Instance.ReturnToMenu(), UiTheme.Panel, UiTheme.TextMuted,
                14, TextAnchor.MiddleCenter);
            UiFactory.Size(menu.gameObject, prefW: 120f);

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
                                   $"·  talk to the team, or work your desk";
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
            switch (target.kind)
            {
                case Interactable.Kind.Npc:
                    ShowAdvice(target.seed);
                    break;
                case Interactable.Kind.Desk:
                    MakeDeskCall();
                    break;
                case Interactable.Kind.Exit:
                    GameManager.Instance.GoToCity();
                    break;
            }
        }

        private void MakeDeskCall()
        {
            var c = GameManager.Instance.Profile;
            if (c == null) { GameManager.Instance.ReturnToMenu(); return; }

            CareerSystem.EnsureStarted(c);
            if (!CareerSystem.HasCallsLeft(c))
            {
                Flash("No calls left today — head to the Menu and End Day.");
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

            var profile = GameManager.Instance.Profile;
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

            var canvas = Object.FindFirstObjectByType<Canvas>();
            Transform parent = canvas != null ? canvas.transform : null;
            if (parent == null) parent = UiFactory.CreateScreenCanvas("AdviceCanvas").transform;

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
            if (_playerCtrl != null) _playerCtrl.SetControlEnabled(true);
        }

        private void Flash(string message)
        {
            _flashText = message;
            _flashUntil = Time.time + 2.5f;
        }
    }
}
