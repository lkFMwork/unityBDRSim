using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Runs a single "commute" platformer level: you must beat the level to reach a
    /// local client in person. Generates the level, spawns the player, follows with
    /// a side camera, and on success hands off to the meeting (gatekeeper/call); on
    /// failure you didn't make it. Attach to a GameObject in the Platformer scene.
    /// </summary>
    public class PlatformerSceneController : MonoBehaviour
    {
        private Canvas _canvas;
        private Camera _cam;
        private GameObject _player;
        private PlatformerController _ctrl;
        private TMP_Text _statusLabel;
        private string _company = "the client";
        private bool _over;

        private void Start()
        {
            var scenario = GameManager.Instance.SelectedScenario;
            _company = scenario != null && scenario.prospect != null ? scenario.prospect.companyName : "the client";

            int difficulty = 3;
            if (scenario != null)
            {
                difficulty = scenario.difficulty == DifficultyTier.Hard ? 8
                    : scenario.difficulty == DifficultyTier.Medium ? 5 : 2;
                if (scenario.isKeyAccount) difficulty = Mathf.Min(difficulty + 2, 10);
            }

            BuildArena();

            var level = new GameObject("Level");
            PlatformerLevelGenerator.Build(difficulty, unchecked(System.Environment.TickCount), level.transform,
                out Vector3 start);

            SpawnPlayer(start);
            BuildHud();
        }

        private void BuildArena()
        {
            _cam = Camera.main;
            if (_cam != null)
            {
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.backgroundColor = new Color(0.45f, 0.62f, 0.85f); // sky
                _cam.fieldOfView = 42f;
                _cam.farClipPlane = 500f;
            }
            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var lgo = new GameObject("Sun");
                var l = lgo.AddComponent<Light>();
                l.type = LightType.Directional;
                l.intensity = 1.1f;
                lgo.transform.rotation = Quaternion.Euler(50f, -20f, 0f);
            }
        }

        private void SpawnPlayer(Vector3 start)
        {
            _player = new GameObject("Player");
            _player.transform.position = start;

            var cc = _player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            _ctrl = _player.AddComponent<PlatformerController>();

            // Kenney platformer character (a clean static model that suits the arcade
            // minigame); a hop bob is driven procedurally by the controller's visual.
            var body = ModelLibrary.Spawn(
                "Models/kenney_platformer-kit/Models/FBX format/character-oopi",
                _player.transform, Vector3.zero, 0f, 1f,
                placeholderColor: new Color(0.30f, 0.55f, 0.85f), placeholderLabel: false,
                fitHeight: 1.6f, ground: false); // feet at the controller's base, not re-grounded

            _ctrl.Init(start, body.transform);
            _ctrl.Won += OnWon;
            _ctrl.Failed += OnFailed;
            _ctrl.LivesChanged += _ => RefreshStatus();
            _ctrl.CoinsChanged += _ => RefreshStatus();
        }

        private void LateUpdate()
        {
            if (_cam == null || _player == null) return;
            float t = 10f * Time.deltaTime;
            float camX = Mathf.Lerp(_cam.transform.position.x, _player.transform.position.x + 1.5f, t);
            // Follow height too (levels now climb via stairs/springs), but never below the
            // baseline so flat sections still frame the ground nicely.
            float targetY = Mathf.Max(3.2f, _player.transform.position.y + 2.2f);
            float camY = Mathf.Lerp(_cam.transform.position.y, targetY, t);
            _cam.transform.position = new Vector3(camX, camY, -12f);
            _cam.transform.LookAt(new Vector3(camX, camY - 0.6f, 0f));
        }

        // ---- HUD ------------------------------------------------------------

        private void BuildHud()
        {
            _canvas = UiFactory.CreateScreenCanvas("PlatformerHud");

            var top = UiFactory.Panel(_canvas.transform, new Color(0f, 0f, 0f, 0.5f), "Top");
            Anchor(top.rectTransform, new Vector2(0f, 0.92f), new Vector2(1f, 1f));
            _statusLabel = UiFactory.Label(top.transform, "", 16, UiTheme.TextPrimary,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Stretch(_statusLabel.rectTransform);

            var bottom = UiFactory.Panel(_canvas.transform, new Color(0f, 0f, 0f, 0.45f), "Hint");
            Anchor(bottom.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.07f));
            var hint = UiFactory.Label(bottom.transform,
                "← →  move      Space  jump (hold for higher)      stomp enemies, mind the gaps",
                14, UiTheme.TextMuted, TextAnchor.MiddleCenter, FontStyle.Normal);
            UiFactory.Stretch(hint.rectTransform);

            RefreshStatus();
        }

        private void RefreshStatus()
        {
            if (_statusLabel == null || _ctrl == null) return;
            _statusLabel.text = $"Lives: {_ctrl.Lives}    Coins: {_ctrl.Coins}    →   Get to {_company}";
        }

        // ---- outcomes -------------------------------------------------------

        private void OnWon()
        {
            if (_over) return;
            _over = true;
            ShowResult(true, $"You made it to {_company}!", "Into the meeting ▶",
                () => GameManager.Instance.OnTravelComplete());
        }

        private void OnFailed()
        {
            if (_over) return;
            _over = true;
            var overlay = ResultOverlay("You didn't make the meeting.", UiTheme.Danger);
            var row = UiFactory.Panel(overlay.transform, new Color(0f, 0f, 0f, 0f), "Buttons").gameObject;
            UiFactory.HLayout(row, spacing: 12, expandW: true, expandH: true);
            UiFactory.Size(row, prefH: 56f);
            var retry = UiFactory.Button(row.transform, "Retry the commute",
                () => SceneManager.LoadScene(SceneNames.Platformer), UiTheme.Accent, UiTheme.TextPrimary,
                17, TextAnchor.MiddleCenter);
            UiFactory.Size(retry.gameObject, flexW: 1f);
            var back = UiFactory.Button(row.transform, "Give up",
                () => GameManager.Instance.OnTravelFailed(), UiTheme.PanelDark, UiTheme.TextPrimary,
                17, TextAnchor.MiddleCenter);
            UiFactory.Size(back.gameObject, prefW: 160f);
        }

        private void ShowResult(bool good, string headline, string buttonLabel, System.Action onClick)
        {
            var overlay = ResultOverlay(headline, good ? UiTheme.Positive : UiTheme.Danger);
            var btn = UiFactory.Button(overlay.transform, buttonLabel, () => onClick(),
                good ? UiTheme.Positive : UiTheme.Accent, Color.white, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(btn.gameObject, prefW: 280f, prefH: 54f);
        }

        private Transform ResultOverlay(string headline, Color color)
        {
            var overlay = UiFactory.Panel(_canvas.transform, new Color(0f, 0f, 0f, 0.8f), "Result");
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.VLayout(overlay.gameObject, pad: 40, spacing: 16, expandH: true,
                align: TextAnchor.MiddleCenter);
            UiFactory.Label(overlay.transform, headline, 34, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            return overlay.transform;
        }

        private static void Anchor(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
