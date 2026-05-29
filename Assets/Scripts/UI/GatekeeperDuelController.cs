using System.Collections.Generic;
using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// The Gatekeeper Gauntlet: a turn-based verbal duel staged like an old-school
    /// fighting game. Two procedural fighters face off in a side-on arena; reading
    /// the gatekeeper correctly makes your fighter land a hit (their Resolve drops),
    /// a wrong read makes them counter (your Composure drops). The HUD frames it
    /// with health bars on top and your four moves on the bottom.
    /// </summary>
    public class GatekeeperDuelController : MonoBehaviour
    {
        private GatekeeperDuel _duel;
        private Canvas _canvas;
        private UiFactory.Meter _resolveMeter;
        private UiFactory.Meter _composureMeter;
        private Text _tellLabel;
        private Text _actionLabel;
        private string _companyName = "Front Desk";
        private readonly List<(GatekeeperMove move, Button button)> _moveButtons = new();

        private FighterRig _playerFighter;
        private FighterRig _gkFighter;

        private void Start()
        {
            var scenario = GameManager.Instance.ResolveActiveScenario();
            if (scenario == null) { BuildErrorUi(); return; }

            int resolve; float scale;
            switch (scenario.difficulty)
            {
                case DifficultyTier.Hard: resolve = 95; scale = 1.2f; break;
                case DifficultyTier.Medium: resolve = 75; scale = 1.0f; break;
                default: resolve = 55; scale = 0.8f; break;
            }
            if (scenario.isKeyAccount) resolve += 25;

            var attrs = GameManager.Instance.Profile != null ? GameManager.Instance.Profile.attributes : null;
            _duel = new GatekeeperDuel(resolve, attrs, scale, unchecked(System.Environment.TickCount));

            BuildArena(scenario);
            BuildHud(scenario);

            _duel.Log += OnLog;
            _duel.StateChanged += UpdateHud;
            _duel.Ended += OnEnded;
            _duel.MoveResolved += OnMoveResolved;

            _duel.Begin();
            UpdateHud();
        }

        private void OnDestroy()
        {
            if (_duel == null) return;
            _duel.Log -= OnLog;
            _duel.StateChanged -= UpdateHud;
            _duel.Ended -= OnEnded;
            _duel.MoveResolved -= OnMoveResolved;
        }

        // ---- arena (3D) -----------------------------------------------------

        private void BuildArena(ScenarioDefinition scenario)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 2.2f, -7f);
                cam.transform.LookAt(new Vector3(0f, 1.1f, 0f));
                cam.fieldOfView = 42f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.10f, 0.06f, 0.08f);
            }

            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var lgo = new GameObject("Arena Light");
                var l = lgo.AddComponent<Light>();
                l.type = LightType.Directional;
                l.intensity = 1.1f;
                lgo.transform.rotation = Quaternion.Euler(50f, -20f, 0f);
            }

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Arena Floor";
            floor.transform.localScale = new Vector3(2f, 1f, 1f);
            var fr = floor.GetComponent<Renderer>();
            if (fr != null) fr.sharedMaterial = Mat(new Color(0.18f, 0.16f, 0.20f));

            var playerCfg = GameManager.Instance.Profile != null
                ? GameManager.Instance.Profile.avatar
                : new AvatarConfig();
            _playerFighter = SpawnFighter(new Vector3(-2.2f, 0f, 0f), true, playerCfg);
            _gkFighter = SpawnFighter(new Vector3(2.2f, 0f, 0f), false, GatekeeperConfig());
        }

        private static FighterRig SpawnFighter(Vector3 pos, bool faceRight, AvatarConfig config)
        {
            var go = new GameObject(faceRight ? "PlayerFighter" : "GatekeeperFighter");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, faceRight ? 90f : -90f, 0f);

            var avatar = go.AddComponent<AvatarBuilder>();
            avatar.AnimateIdle = false; // FighterRig drives motion
            avatar.SetConfig(config);

            var rig = go.AddComponent<FighterRig>();
            rig.facingRight = faceRight;
            return rig;
        }

        private static AvatarConfig GatekeeperConfig() => new AvatarConfig
        {
            skinTone = 2, outfitColor = 1, accentColor = 4, hairColor = 0, build = 2, height = 1.05f
        };

        // ---- HUD ------------------------------------------------------------

        private void BuildHud(ScenarioDefinition scenario)
        {
            _canvas = UiFactory.CreateScreenCanvas("DuelCanvas");
            _companyName = scenario.prospect != null ? scenario.prospect.companyName : scenario.title;

            var title = UiFactory.Panel(_canvas.transform, new Color(0f, 0f, 0f, 0.4f), "Title");
            Anchor(title.rectTransform, new Vector2(0f, 0.93f), new Vector2(1f, 1f));
            UiFactory.Stretch(UiFactory.Label(title.transform,
                scenario.isKeyAccount ? "★ KEY ACCOUNT — GATEKEEPER GAUNTLET" : "GATEKEEPER GAUNTLET",
                20, UiTheme.Danger, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform);

            _composureMeter = MakeBar(new Vector2(0.03f, 0.85f), new Vector2(0.47f, 0.91f), UiTheme.Positive, false);
            _resolveMeter = MakeBar(new Vector2(0.53f, 0.85f), new Vector2(0.97f, 0.91f), UiTheme.Danger, true);

            var tellPanel = UiFactory.Panel(_canvas.transform, new Color(0f, 0f, 0f, 0.5f), "Tell");
            Anchor(tellPanel.rectTransform, new Vector2(0.10f, 0.75f), new Vector2(0.90f, 0.83f));
            _tellLabel = UiFactory.Label(tellPanel.transform, "", 16, UiTheme.TextPrimary,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            var trt = _tellLabel.rectTransform;
            UiFactory.Stretch(trt);
            trt.offsetMin = new Vector2(12f, 4f);
            trt.offsetMax = new Vector2(-12f, -4f);

            var bottom = UiFactory.Panel(_canvas.transform, new Color(0f, 0f, 0f, 0.62f), "Bottom");
            Anchor(bottom.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.30f));
            UiFactory.VLayout(bottom.gameObject, pad: 12, spacing: 8, expandH: false);

            _actionLabel = UiFactory.Label(bottom.transform, "", 14, UiTheme.TextMuted,
                TextAnchor.MiddleCenter, FontStyle.Italic);
            UiFactory.Size(_actionLabel.gameObject, prefH: 26f);

            var moves = UiFactory.Panel(bottom.transform, new Color(0f, 0f, 0f, 0f), "Moves").gameObject;
            UiFactory.HLayout(moves, spacing: 8, expandW: true, expandH: true);
            UiFactory.Size(moves, flexH: 1f);

            foreach (var move in _duel.Moves)
            {
                var captured = move;
                var btn = UiFactory.Button(moves.transform,
                    $"{GatekeeperCombatData.MoveName(move)}\n<size=11>{MovePurpose(move)}</size>",
                    () => _duel.Play(captured), UiTheme.Accent, UiTheme.TextPrimary, 16,
                    TextAnchor.MiddleCenter);
                UiFactory.Size(btn.gameObject, flexW: 1f);
                _moveButtons.Add((captured, btn));
            }
        }

        private UiFactory.Meter MakeBar(Vector2 anchorMin, Vector2 anchorMax, Color color, bool mirror)
        {
            var bg = UiFactory.Panel(_canvas.transform, new Color(0f, 0f, 0f, 0.55f), "Bar");
            Anchor(bg.rectTransform, anchorMin, anchorMax);

            var fill = UiFactory.Panel(bg.transform, color, "Fill");
            var caption = UiFactory.Label(bg.transform, "", 13, Color.white, TextAnchor.MiddleCenter,
                FontStyle.Bold, "Caption");
            UiFactory.Stretch(caption.rectTransform);

            var meter = new UiFactory.Meter { Fill = fill.rectTransform, Caption = caption, Mirror = mirror };
            meter.Set(1f);
            return meter;
        }

        private static void Anchor(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ---- events ---------------------------------------------------------

        private void OnLog(string line) { if (_actionLabel != null) _actionLabel.text = line; }

        private void UpdateHud()
        {
            if (_duel == null) return;

            _resolveMeter.Set(_duel.GatekeeperMaxResolve > 0
                ? (float)_duel.GatekeeperResolve / _duel.GatekeeperMaxResolve : 0f);
            _resolveMeter.Caption.text = $"{_companyName}   {_duel.GatekeeperResolve}/{_duel.GatekeeperMaxResolve}";

            _composureMeter.Set(_duel.PlayerMaxComposure > 0
                ? (float)_duel.PlayerComposure / _duel.PlayerMaxComposure : 0f);
            _composureMeter.Caption.text = $"YOU   {_duel.PlayerComposure}/{_duel.PlayerMaxComposure}";

            _tellLabel.text = _duel.IsOver ? "" : GatekeeperCombatData.StanceTell(_duel.CurrentStance);

            foreach (var entry in _moveButtons)
                entry.button.interactable = !_duel.IsOver;
        }

        private void OnMoveResolved(MoveOutcome outcome)
        {
            switch (outcome)
            {
                case MoveOutcome.Critical:
                case MoveOutcome.Neutral:
                    _playerFighter?.Attack();
                    _gkFighter?.TakeHit();
                    break;
                case MoveOutcome.Backfire:
                    _gkFighter?.Attack();
                    _playerFighter?.TakeHit();
                    break;
            }
        }

        private void OnEnded(bool won)
        {
            if (won) { _gkFighter?.Defeat(); _playerFighter?.Victory(); }
            else { _playerFighter?.Defeat(); _gkFighter?.Victory(); }

            var overlay = UiFactory.Panel(_canvas.transform, new Color(0f, 0f, 0f, 0.82f), "Result");
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.VLayout(overlay.gameObject, pad: 40, spacing: 16, expandH: true,
                align: TextAnchor.MiddleCenter);

            UiFactory.Label(overlay.transform, won ? "YOU'RE THROUGH!" : "SHUT DOWN",
                40, won ? UiTheme.Positive : UiTheme.Danger, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Label(overlay.transform,
                won ? "You charmed your way past the front desk. Now close the decision-maker."
                    : "The gatekeeper held the line. No meeting this time.",
                16, UiTheme.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Italic);

            if (won)
            {
                bool flawless = _duel.PlayerComposure >= _duel.PlayerMaxComposure;
                if (flawless)
                    UiFactory.Label(overlay.transform, "Flawless — they never rattled you.", 14,
                        UiTheme.Warning, TextAnchor.MiddleCenter, FontStyle.Bold);

                var go = UiFactory.Button(overlay.transform, "Talk to them ▶",
                    () => GameManager.Instance.OnGatekeeperCleared(flawless), UiTheme.Positive, Color.white,
                    18, TextAnchor.MiddleCenter);
                UiFactory.Size(go.gameObject, prefW: 260f, prefH: 54f);
            }
            else
            {
                var back = UiFactory.Button(overlay.transform, "Back",
                    () => GameManager.Instance.OnGatekeeperFailed(), UiTheme.Accent, UiTheme.TextPrimary,
                    18, TextAnchor.MiddleCenter);
                UiFactory.Size(back.gameObject, prefW: 260f, prefH: 54f);
            }
        }

        private static string MovePurpose(GatekeeperMove move) => move switch
        {
            GatekeeperMove.Charm => "make a friend",
            GatekeeperMove.Logic => "prove you're legit",
            GatekeeperMove.Pressure => "be brief & bold",
            GatekeeperMove.Curveball => "break the script",
            _ => ""
        };

        private static Material Mat(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var m = new Material(shader) { color = color };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            return m;
        }

        private void BuildErrorUi()
        {
            var canvas = UiFactory.CreateScreenCanvas("DuelErrorCanvas");
            var root = UiFactory.Panel(canvas.transform, UiTheme.Background, "Root");
            UiFactory.Stretch(root.rectTransform);
            UiFactory.VLayout(root.gameObject, pad: 40, spacing: 16, expandH: true,
                align: TextAnchor.MiddleCenter);
            UiFactory.Label(root.transform, "No meeting to start.", 18, UiTheme.Warning,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            var back = UiFactory.Button(root.transform, "Back to Menu",
                () => GameManager.Instance.ReturnToMenu(), UiTheme.Accent, UiTheme.TextPrimary,
                18, TextAnchor.MiddleCenter);
            UiFactory.Size(back.gameObject, prefW: 220f, prefH: 52f);
        }
    }
}
