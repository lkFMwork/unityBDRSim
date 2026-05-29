using System.Collections.Generic;
using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// The Gatekeeper Gauntlet screen: a turn-based verbal duel. Reads the
    /// scenario's difficulty for the gatekeeper's strength and the player's
    /// attributes for damage, runs a <see cref="GatekeeperDuel"/>, and routes on
    /// win (into the call) or loss (back to the hub). Attach to a GameObject in the
    /// GatekeeperDuel scene.
    /// </summary>
    public class GatekeeperDuelController : MonoBehaviour
    {
        private GatekeeperDuel _duel;
        private Canvas _canvas;
        private UiFactory.Meter _resolveMeter;
        private UiFactory.Meter _composureMeter;
        private Text _tellLabel;
        private UiFactory.ScrollLog _log;
        private readonly List<(GatekeeperMove move, Button button)> _moveButtons = new();

        private void Start()
        {
            var scenario = GameManager.Instance.ResolveActiveScenario();
            if (scenario == null)
            {
                BuildErrorUi();
                return;
            }

            int resolve; float scale;
            switch (scenario.difficulty)
            {
                case DifficultyTier.Hard: resolve = 95; scale = 1.2f; break;
                case DifficultyTier.Medium: resolve = 75; scale = 1.0f; break;
                default: resolve = 55; scale = 0.8f; break;
            }

            var attrs = GameManager.Instance.Profile != null ? GameManager.Instance.Profile.attributes : null;
            int seed = unchecked(System.Environment.TickCount);
            _duel = new GatekeeperDuel(resolve, attrs, scale, seed);

            BuildUi(scenario);

            _duel.Log += OnLog;
            _duel.StateChanged += UpdateHud;
            _duel.Ended += OnEnded;

            _duel.Begin();
            UpdateHud();
        }

        private void OnDestroy()
        {
            if (_duel == null) return;
            _duel.Log -= OnLog;
            _duel.StateChanged -= UpdateHud;
            _duel.Ended -= OnEnded;
        }

        // ---- layout ---------------------------------------------------------

        private void BuildUi(ScenarioDefinition scenario)
        {
            _canvas = UiFactory.CreateScreenCanvas("DuelCanvas");
            var root = UiFactory.Panel(_canvas.transform, UiTheme.Background, "Root");
            UiFactory.Stretch(root.rectTransform);
            UiFactory.VLayout(root.gameObject, pad: 16, spacing: 10, expandH: false);

            UiFactory.Label(root.transform, "GATEKEEPER GAUNTLET", 30, UiTheme.Danger,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            string company = scenario.prospect != null ? scenario.prospect.companyName : scenario.title;
            UiFactory.Label(root.transform, $"Front desk at {company} — get past them.", 15,
                UiTheme.TextMuted, TextAnchor.MiddleCenter, FontStyle.Italic);

            // Gatekeeper resolve
            UiFactory.Label(root.transform, "Gatekeeper Resolve", 13, UiTheme.TextMuted,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            _resolveMeter = UiFactory.MakeMeter(root.transform, "", UiTheme.Danger, 26f);

            // The "tell" the player reads
            var tellPanel = UiFactory.Panel(root.transform, UiTheme.Panel, "Tell").gameObject;
            UiFactory.VLayout(tellPanel, pad: 12, spacing: 0, expandH: false);
            UiFactory.Size(tellPanel, prefH: 70f);
            _tellLabel = UiFactory.Label(tellPanel.transform, "", 17, UiTheme.TextPrimary,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            // Transcript
            _log = UiFactory.MakeScrollLog(root.transform, UiTheme.PanelDark, UiTheme.TextPrimary);
            UiFactory.Size(_log.Scroll.gameObject, flexH: 1f);

            // Player composure
            UiFactory.Label(root.transform, "Your Composure", 13, UiTheme.TextMuted,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            _composureMeter = UiFactory.MakeMeter(root.transform, "", UiTheme.Positive, 26f);

            // Moves
            var moves = UiFactory.Panel(root.transform, UiTheme.Background, "Moves").gameObject;
            UiFactory.HLayout(moves, spacing: 8, expandW: true, expandH: true);
            UiFactory.Size(moves, prefH: 84f);

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

        // ---- events ---------------------------------------------------------

        private void OnLog(string line) => _log.Append(line);

        private void UpdateHud()
        {
            if (_duel == null) return;

            _resolveMeter.Set(_duel.GatekeeperMaxResolve > 0
                ? (float)_duel.GatekeeperResolve / _duel.GatekeeperMaxResolve : 0f);
            _resolveMeter.Caption.text = $"{_duel.GatekeeperResolve} / {_duel.GatekeeperMaxResolve}";

            _composureMeter.Set(_duel.PlayerMaxComposure > 0
                ? (float)_duel.PlayerComposure / _duel.PlayerMaxComposure : 0f);
            _composureMeter.Caption.text = $"{_duel.PlayerComposure} / {_duel.PlayerMaxComposure}";

            _tellLabel.text = _duel.IsOver ? "" : GatekeeperCombatData.StanceTell(_duel.CurrentStance);

            foreach (var entry in _moveButtons)
                entry.button.interactable = !_duel.IsOver;
        }

        private void OnEnded(bool won)
        {
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
