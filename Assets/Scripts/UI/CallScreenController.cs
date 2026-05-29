using System.Collections.Generic;
using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// Drives the call floor: builds the HUD in code, runs a <see cref="CallSession"/>,
    /// streams the transcript, updates the trust/patience meters, renders the
    /// dialogue choices, and shows the graded results when the call ends.
    /// Attach to a GameObject in the CallFloor scene (the setup tool does this).
    /// </summary>
    public class CallScreenController : MonoBehaviour
    {
        private CallSession _session;

        private Canvas _canvas;
        private Text _headerLabel;
        private Text _stageLabel;
        private Text _moodLabel;
        private Text _laneLabel;
        private Text _objectionLabel;
        private UiFactory.Meter _trustMeter;
        private UiFactory.Meter _patienceMeter;
        private UiFactory.ScrollLog _log;
        private GameObject _choicesPanel;

        private void Start()
        {
            var scenario = GameManager.Instance.ResolveActiveScenario();
            if (scenario == null)
            {
                BuildErrorUi();
                return;
            }

            BuildUi(scenario);

            var modifiers = CharacterModifiers.FromCharacter(GameManager.Instance.Profile);
            _session = new CallSession(scenario, modifiers);
            _session.NarratorLine += OnNarrator;
            _session.RepLine += OnRep;
            _session.ProspectLine += OnProspect;
            _session.StateChanged += UpdateHud;
            _session.ChoicesChanged += RebuildChoices;
            _session.CallEnded += ShowResults;

            _session.Begin();
        }

        private void OnDestroy()
        {
            if (_session == null) return;
            _session.NarratorLine -= OnNarrator;
            _session.RepLine -= OnRep;
            _session.ProspectLine -= OnProspect;
            _session.StateChanged -= UpdateHud;
            _session.ChoicesChanged -= RebuildChoices;
            _session.CallEnded -= ShowResults;
        }

        // ---- layout ---------------------------------------------------------

        private void BuildUi(ScenarioDefinition scenario)
        {
            _canvas = UiFactory.CreateScreenCanvas("CallCanvas");
            var root = UiFactory.Panel(_canvas.transform, UiTheme.Background, "Root");
            UiFactory.Stretch(root.rectTransform);
            UiFactory.VLayout(root.gameObject, pad: 12, spacing: 10, expandH: false);

            BuildTopBar(root.transform, scenario);

            // Middle: prospect info (left) + transcript (right)
            var middle = UiFactory.Panel(root.transform, UiTheme.Background, "Middle").gameObject;
            UiFactory.HLayout(middle, spacing: 10, expandH: true);
            UiFactory.Size(middle, flexH: 1f);

            BuildInfoPanel(middle.transform, scenario);

            _log = UiFactory.MakeScrollLog(middle.transform, UiTheme.PanelDark, UiTheme.TextPrimary);
            UiFactory.Size(_log.Scroll.gameObject, prefW: 400f, flexW: 1f);

            BuildChoicesPanel(root.transform);
        }

        private void BuildTopBar(Transform parent, ScenarioDefinition scenario)
        {
            var bar = UiFactory.Panel(parent, UiTheme.PanelDark, "TopBar").gameObject;
            UiFactory.HLayout(bar, pad: 10, spacing: 10, expandH: true);
            UiFactory.Size(bar, prefH: 60f, flexH: 0f);

            var back = UiFactory.Button(bar.transform, "‹ Menu",
                () => GameManager.Instance.ReturnToMenu(), UiTheme.Panel, UiTheme.TextMuted,
                16, TextAnchor.MiddleCenter);
            UiFactory.Size(back.gameObject, prefW: 90f);

            _headerLabel = UiFactory.Label(bar.transform,
                scenario.prospect != null ? scenario.prospect.DisplayHeadline : scenario.title,
                20, UiTheme.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Bold, "Header");
            UiFactory.Size(_headerLabel.gameObject, flexW: 1f);

            _stageLabel = UiFactory.Label(bar.transform, "Stage: Opening", 16, UiTheme.AccentStrong,
                TextAnchor.MiddleRight, FontStyle.Bold, "Stage");
            UiFactory.Size(_stageLabel.gameObject, prefW: 240f);
        }

        private void BuildInfoPanel(Transform parent, ScenarioDefinition scenario)
        {
            var panel = UiFactory.Panel(parent, UiTheme.Panel, "Info").gameObject;
            UiFactory.VLayout(panel, pad: 14, spacing: 10);
            UiFactory.Size(panel, prefW: 320f, flexW: 0f);

            var p = scenario.prospect;
            UiFactory.Label(panel.transform, "PROSPECT", 13, UiTheme.TextMuted,
                TextAnchor.UpperLeft, FontStyle.Bold);
            UiFactory.Label(panel.transform,
                p != null
                    ? $"<b>{p.contactName}</b>\n{p.title}\n{p.companyName}\n{p.location}"
                    : "Unknown",
                16, UiTheme.TextPrimary);

            var lane = scenario.PrimaryLane;
            UiFactory.Label(panel.transform, "PRIMARY LANE", 13, UiTheme.TextMuted,
                TextAnchor.UpperLeft, FontStyle.Bold);
            _laneLabel = UiFactory.Label(panel.transform,
                lane != null
                    ? $"{lane.label}\n{lane.mode} / {lane.equipment}\n{lane.miles} mi · " +
                      $"{lane.loadsPerWeek}/wk · now ${lane.currentRatePerMile:0.00}/mi"
                    : "No lane on file",
                15, UiTheme.TextPrimary);

            _trustMeter = UiFactory.MakeMeter(panel.transform, "Trust", UiTheme.Positive);
            _patienceMeter = UiFactory.MakeMeter(panel.transform, "Patience", UiTheme.Warning);

            _moodLabel = UiFactory.Label(panel.transform, "Mood: —", 15, UiTheme.TextPrimary,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            _objectionLabel = UiFactory.Label(panel.transform, string.Empty, 14, UiTheme.Danger,
                TextAnchor.UpperLeft, FontStyle.Bold);
        }

        private void BuildChoicesPanel(Transform parent)
        {
            var panel = UiFactory.Panel(parent, UiTheme.PanelDark, "Choices").gameObject;
            UiFactory.VLayout(panel, pad: 12, spacing: 8);
            UiFactory.Size(panel, prefH: 248f, flexH: 0f);

            UiFactory.Label(panel.transform, "What do you say?", 16, UiTheme.TextMuted,
                TextAnchor.UpperLeft, FontStyle.Bold);

            _choicesPanel = UiFactory.Panel(panel.transform, UiTheme.PanelDark, "ChoiceList").gameObject;
            UiFactory.VLayout(_choicesPanel, pad: 0, spacing: 8);
            UiFactory.Size(_choicesPanel, flexH: 1f);
        }

        // ---- event handlers -------------------------------------------------

        private void OnNarrator(string s) => _log.Append(UiTheme.RichNarrator(s));
        private void OnRep(string s) => _log.Append(UiTheme.RichRep(s));
        private void OnProspect(string s) => _log.Append(UiTheme.RichProspect(s));

        private void UpdateHud()
        {
            if (_session == null) return;
            var prospect = _session.Prospect;

            _stageLabel.text = $"Stage: {_session.Stage}";
            _moodLabel.text = $"Mood: {prospect.MoodLabel}";

            _trustMeter.Set(prospect.Trust);
            _trustMeter.Caption.text = $"Trust  {Mathf.RoundToInt(prospect.Trust * 100f)}%";
            _patienceMeter.Set(prospect.Patience);
            _patienceMeter.Caption.text = $"Patience  {Mathf.RoundToInt(prospect.Patience * 100f)}%";

            _objectionLabel.text = prospect.ActiveObjection.HasValue
                ? $"⚠ Objection: {ObjectionCatalog.Get(prospect.ActiveObjection.Value).Title}"
                : string.Empty;
        }

        private void RebuildChoices(IReadOnlyList<DialogueChoice> choices)
        {
            for (int i = _choicesPanel.transform.childCount - 1; i >= 0; i--)
                Destroy(_choicesPanel.transform.GetChild(i).gameObject);

            if (choices == null) return;

            foreach (var choice in choices)
            {
                var btn = UiFactory.Button(_choicesPanel.transform, choice.Text,
                    () => _session.Choose(choice), UiTheme.Accent, UiTheme.TextPrimary,
                    15, TextAnchor.MiddleLeft);
                UiFactory.Size(btn.gameObject, minH: 44f, prefH: 56f, flexW: 1f, flexH: 1f);
            }
        }

        // ---- results --------------------------------------------------------

        private void ShowResults(CallOutcome outcome)
        {
            var report = CallEvaluator.Evaluate(_session);
            GameManager.Instance.LastReport = report;

            // Award career progress and persist it.
            ProgressionResult prog = default;
            var profile = GameManager.Instance.Profile;
            bool career = GameManager.Instance.IsCareerCall;
            if (profile != null)
            {
                prog = ProgressionSystem.ApplyCall(profile, report, _session);
                if (career) CareerSystem.RecordResult(profile, report);
                GameManager.Instance.SaveProfile();
            }

            var overlay = UiFactory.Panel(_canvas.transform, new Color(0f, 0f, 0f, 0.78f), "ResultsOverlay");
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.VLayout(overlay.gameObject, pad: 40, spacing: 0, expandH: true,
                align: TextAnchor.MiddleCenter);

            var card = UiFactory.Panel(overlay.transform, UiTheme.Panel, "Card").gameObject;
            UiFactory.VLayout(card, pad: 24, spacing: 12, expandH: false, align: TextAnchor.UpperCenter);
            UiFactory.Size(card, prefW: 760f, minH: 360f);

            UiFactory.Label(card.transform, report.Headline, 30, UiTheme.AccentStrong,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UiFactory.Label(card.transform,
                $"Grade  <b>{report.LetterGrade}</b>   ·   {Mathf.RoundToInt(report.OverallPercent * 100f)}%",
                24, UiTheme.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);

            UiFactory.Label(card.transform, report.OutcomeNote, 16, UiTheme.TextMuted,
                TextAnchor.MiddleCenter, FontStyle.Italic);

            if (profile != null)
            {
                string xpLine = $"+{prog.XpGained} XP";
                if (prog.LeveledUp)
                    xpLine += $"   —   LEVEL UP!  Now level {prog.NewLevel}  (+{prog.SkillPointsGained} SP)";
                UiFactory.Label(card.transform, xpLine, 18,
                    prog.LeveledUp ? UiTheme.Positive : UiTheme.AccentStrong,
                    TextAnchor.MiddleCenter, FontStyle.Bold);
            }

            UiFactory.Label(card.transform, BuildBreakdown(report), 15, UiTheme.TextPrimary);
            UiFactory.Label(card.transform, "What went well", 15, UiTheme.Positive,
                TextAnchor.UpperLeft, FontStyle.Bold);
            UiFactory.Label(card.transform, "• " + string.Join("\n• ", report.Strengths), 15,
                UiTheme.TextPrimary);
            UiFactory.Label(card.transform, "Coaching", 15, UiTheme.Warning,
                TextAnchor.UpperLeft, FontStyle.Bold);
            UiFactory.Label(card.transform, "• " + string.Join("\n• ", report.Improvements), 15,
                UiTheme.TextPrimary);

            var buttons = UiFactory.Panel(card.transform, UiTheme.Panel, "Buttons").gameObject;
            UiFactory.HLayout(buttons, spacing: 12, expandW: true, expandH: true);
            UiFactory.Size(buttons, prefH: 56f);

            if (career)
            {
                UiFactory.Button(buttons.transform, "Continue ▶", () => GameManager.Instance.ReturnToMenu(),
                    UiTheme.Positive, Color.white, 18, TextAnchor.MiddleCenter);
            }
            else
            {
                UiFactory.Button(buttons.transform, "Retry", () => GameManager.Instance.ReplayCurrent(),
                    UiTheme.Accent, UiTheme.TextPrimary, 18, TextAnchor.MiddleCenter);
                UiFactory.Button(buttons.transform, "Back to Menu", () => GameManager.Instance.ReturnToMenu(),
                    UiTheme.PanelDark, UiTheme.TextPrimary, 18, TextAnchor.MiddleCenter);
            }
        }

        private static string BuildBreakdown(CallReport report)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kvp in report.CategoryPercents)
                sb.AppendLine($"{kvp.Key,-20} {Mathf.RoundToInt(kvp.Value * 100f),3}%");
            return sb.ToString().TrimEnd();
        }

        // ---- error fallback -------------------------------------------------

        private void BuildErrorUi()
        {
            var canvas = UiFactory.CreateScreenCanvas("ErrorCanvas");
            var root = UiFactory.Panel(canvas.transform, UiTheme.Background, "Root");
            UiFactory.Stretch(root.rectTransform);
            UiFactory.VLayout(root.gameObject, pad: 40, spacing: 16, expandH: true,
                align: TextAnchor.MiddleCenter);

            UiFactory.Label(root.transform,
                "No scenario to play.\n\nReturn to the menu and pick a scenario, or run\n" +
                "Tools → Fitzmark BDR → Setup Project (One-Click) in the Editor.",
                18, UiTheme.Warning, TextAnchor.MiddleCenter, FontStyle.Bold);

            var back = UiFactory.Button(root.transform, "Back to Menu",
                () => GameManager.Instance.ReturnToMenu(), UiTheme.Accent, UiTheme.TextPrimary,
                18, TextAnchor.MiddleCenter);
            UiFactory.Size(back.gameObject, prefW: 220f, prefH: 52f);
        }
    }
}
