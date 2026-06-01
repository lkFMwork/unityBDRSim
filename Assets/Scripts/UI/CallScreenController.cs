using System.Collections.Generic;
using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using TMPro;
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
        private TMP_Text _headerLabel;
        private TMP_Text _stageLabel;
        private TMP_Text _moodLabel;
        private TMP_Text _laneLabel;
        private TMP_Text _objectionLabel;
        private UiFactory.Meter _trustMeter;
        private UiFactory.Meter _patienceMeter;
        private UiFactory.ScrollLog _log;
        private GameObject _choicesPanel;
        private GameObject _abilitiesPanel;
        private readonly List<(AbilityDefinition def, Button button, TMP_Text label)> _abilityButtons = new();

        private void Start()
        {
            var scenario = GameManager.Instance.ResolveActiveScenario();
            if (scenario == null)
            {
                BuildErrorUi();
                return;
            }

            BuildUi(scenario);

            var profile = GameManager.Instance.Profile;
            var modifiers = CharacterModifiers.FromCharacter(profile);
            _session = new CallSession(scenario, modifiers);
            _session.ConfigureAbilities(AbilitySystem.AvailableAbilities(profile));
            _session.NarratorLine += OnNarrator;
            _session.RepLine += OnRep;
            _session.ProspectLine += OnProspect;
            _session.StateChanged += UpdateHud;
            _session.ChoicesChanged += RebuildChoices;
            _session.CallEnded += ShowResults;
            _session.AbilitiesChanged += RefreshAbilities;

            BuildAbilityButtons();
            _session.Begin();
            RefreshAbilities();

            // A random opening situation colors the call.
            var openingEvent = CallEventLibrary.Roll(new System.Random(), 0.45f);
            if (openingEvent != null) _session.ApplyEvent(openingEvent);
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
            _session.AbilitiesChanged -= RefreshAbilities;
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

            BuildAbilitiesBar(root.transform);
            BuildChoicesPanel(root.transform);
        }

        private void BuildAbilitiesBar(Transform parent)
        {
            var bar = UiFactory.Panel(parent, UiTheme.PanelDark, "AbilitiesBar").gameObject;
            UiFactory.HLayout(bar, pad: 8, spacing: 8, expandW: false, expandH: true);
            UiFactory.Size(bar, prefH: 46f, flexH: 0f);

            var caption = UiFactory.Label(bar.transform, "Abilities:", 13, UiTheme.TextMuted,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Size(caption.gameObject, prefW: 78f);

            _abilitiesPanel = UiFactory.Panel(bar.transform, new Color(0f, 0f, 0f, 0f), "AbilityButtons")
                .gameObject;
            UiFactory.HLayout(_abilitiesPanel, pad: 0, spacing: 8, expandW: false, expandH: true);
            UiFactory.Size(_abilitiesPanel, flexW: 1f);
        }

        private void BuildAbilityButtons()
        {
            _abilityButtons.Clear();
            if (_abilitiesPanel == null || _session == null) return;

            for (int i = _abilitiesPanel.transform.childCount - 1; i >= 0; i--)
                Destroy(_abilitiesPanel.transform.GetChild(i).gameObject);

            if (_session.Abilities.Count == 0)
            {
                UiFactory.Label(_abilitiesPanel.transform, "none for this build", 12, UiTheme.TextMuted,
                    TextAnchor.MiddleLeft, FontStyle.Italic);
                return;
            }

            foreach (var ability in _session.Abilities)
            {
                var captured = ability;
                var btn = UiFactory.Button(_abilitiesPanel.transform, captured.Name,
                    () => _session.UseAbility(captured), UiTheme.Accent, UiTheme.TextPrimary,
                    13, TextAnchor.MiddleCenter);
                UiFactory.Size(btn.gameObject, prefW: 150f, prefH: 32f);
                var label = btn.GetComponentInChildren<TMP_Text>();
                _abilityButtons.Add((captured, btn, label));
            }
        }

        private void RefreshAbilities()
        {
            if (_session == null) return;
            foreach (var entry in _abilityButtons)
            {
                int left = _session.AbilityUsesLeft(entry.def.Id);
                if (entry.label != null) entry.label.text = $"{entry.def.Name} ({left})";
                if (entry.button != null) entry.button.interactable = left > 0 && !_session.IsOver;
            }
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

            string headerText = scenario.prospect != null ? scenario.prospect.DisplayHeadline : scenario.title;
            if (scenario.isKeyAccount) headerText = "★ KEY ACCOUNT  ·  " + headerText;
            _headerLabel = UiFactory.Label(bar.transform, headerText, 20,
                scenario.isKeyAccount ? UiTheme.Warning : UiTheme.TextPrimary,
                TextAnchor.MiddleLeft, FontStyle.Bold, "Header");
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

            // Award career progress, update quests, and persist it.
            ProgressionResult prog = default;
            var profile = GameManager.Instance.Profile;
            bool career = GameManager.Instance.IsCareerCall;
            List<QuestDefinition> doneQuests = new List<QuestDefinition>();
            List<Achievement> doneAch = new List<Achievement>();
            if (profile != null)
            {
                prog = ProgressionSystem.ApplyCall(profile, report, _session);
                if (career) CareerSystem.RecordResult(profile, report);
                doneQuests = QuestSystem.Sync(profile);
                doneAch = AchievementSystem.Sync(profile);
                string flash = doneQuests.Count > 0
                    ? QuestSystem.FlashFor(doneQuests)
                    : AchievementSystem.FlashFor(doneAch);
                if (!string.IsNullOrEmpty(flash)) GameManager.Instance.CareerFlash = flash;

                // Closing a deal isn't the end — it opens a real account that tenders
                // freight on the desk. Local (Texas) visits advance a meeting stage and
                // only open freight on the final close; remote wins open it immediately.
                bool won = report.Outcome == CallOutcome.WonCommitment
                           || report.Outcome == CallOutcome.WonTrial;
                if (won) Vfx.Celebrate();
                var wonScenario = GameManager.Instance.ResolveActiveScenario();
                string companyId = wonScenario != null ? wonScenario.localCompanyId : "";
                if (!string.IsNullOrEmpty(companyId))
                {
                    if (wonScenario.fieldVisit)
                    {
                        // In-person field visit: full progression (cold → … → managed). Winning also
                        // clears the city on the SMW path, and the final close opens a managed freight
                        // account branded with the company.
                        TerritorySystem.RecordCompanyMeeting(profile, companyId, profile.career.day, won);
                        if (won)
                        {
                            var comp = CompanyRegistry.Get(companyId);
                            if (comp != null)
                            {
                                bool worldDone = WorldSystem.MarkCityCleared(profile, comp.CityId);
                                if (worldDone)
                                {
                                    var w = WorldRegistry.WorldOf(comp.CityId);
                                    if (w != null)
                                        GameManager.Instance.CareerFlash =
                                            $"WORLD CLEAR — you've covered all of {w.Name}! The next state is open.";
                                }
                            }
                            if (TerritorySystem.IsCompanyManaged(profile, companyId))
                                FreightSystem.OpenAccountFromWin(profile, wonScenario, profile.career.day);
                        }
                    }
                    else
                    {
                        // Nationwide cold call: warm the relationship, but the phone caps below the
                        // close — no city clear, and no managed account from a call.
                        TerritorySystem.RecordCompanyColdCall(profile, companyId, profile.career.day, won);
                    }
                }
                else if (career && won && wonScenario != null)
                {
                    FreightSystem.OpenAccountFromWin(profile, wonScenario, profile.career.day);
                }

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

            if (doneQuests.Count > 0)
                UiFactory.Label(card.transform, QuestSystem.FlashFor(doneQuests), 16, UiTheme.Positive,
                    TextAnchor.MiddleCenter, FontStyle.Bold);
            if (doneAch.Count > 0)
                UiFactory.Label(card.transform, AchievementSystem.FlashFor(doneAch), 15, UiTheme.Warning,
                    TextAnchor.MiddleCenter, FontStyle.Bold);

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
                UiFactory.Button(buttons.transform, "Continue ▶", () => GameManager.Instance.ReturnToHub(),
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
