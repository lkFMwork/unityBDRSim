using System;
using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// Modal overlay for multi-channel prospecting on the national book: source leads
    /// and warm them with a cadence of email / LinkedIn / call / video touches, then
    /// book a warm (gatekeeper-free, higher-trust) meeting. Rebuilds after each action.
    /// </summary>
    public class OutreachView
    {
        private readonly Transform _canvas;
        private readonly BDRCharacter _character;
        private readonly Action _onClose;
        private GameObject _overlay;

        public OutreachView(Transform canvas, BDRCharacter character, Action onClose)
        {
            _canvas = canvas;
            _character = character;
            _onClose = onClose;
        }

        public void Open() => Build();

        private void Build()
        {
            if (_character == null) return;
            OutreachSystem.EnsureStarted(_character);
            if (_overlay != null) UnityEngine.Object.Destroy(_overlay);

            var overlay = UiFactory.Panel(_canvas, new Color(0f, 0f, 0f, 0.90f), "OutreachOverlay");
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.VLayout(overlay.gameObject, pad: 24, spacing: 10, expandH: false,
                align: TextAnchor.UpperCenter);
            _overlay = overlay.gameObject;

            UiFactory.Label(overlay.transform, "PROSPECTING — OUTREACH", 28, UiTheme.AccentStrong,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Label(overlay.transform,
                $"Touches left today: {OutreachSystem.ActionsLeft(_character)}    ·    " +
                "vary channels to build a cadence — don't spam one or you'll burn the lead.",
                14, UiTheme.TextMuted, TextAnchor.MiddleCenter, FontStyle.Italic);

            var sourceBtn = UiFactory.Button(overlay.transform, "+ Source a new lead (1 touch)", DoSource,
                UiTheme.AccentStrong, Color.white, 15, TextAnchor.MiddleCenter);
            UiFactory.Size(sourceBtn.gameObject, prefH: 42f, prefW: 360f);

            var content = UiFactory.MakeScrollView(overlay.transform, new Color(0f, 0f, 0f, 0.15f));
            UiFactory.Size(content.parent.parent.gameObject, flexH: 1f);

            var leads = OutreachSystem.Active(_character);
            if (leads.Count == 0)
                UiFactory.Label(content,
                    "No leads in the pipeline.\nSource one above, then work it with varied touches until it's warm.",
                    13, UiTheme.TextMuted, TextAnchor.UpperLeft, FontStyle.Italic);
            else
                foreach (var lead in leads) BuildRow(content, lead);

            var close = UiFactory.Button(overlay.transform, "Close",
                () => { UnityEngine.Object.Destroy(_overlay); _onClose?.Invoke(); },
                UiTheme.Accent, UiTheme.TextPrimary, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(close.gameObject, prefH: 48f, prefW: 240f);
        }

        private void BuildRow(Transform parent, OutreachLead lead)
        {
            var row = UiFactory.Panel(parent, UiTheme.Panel, "Lead").gameObject;
            UiFactory.VLayout(row, pad: 10, spacing: 6, expandH: false);
            UiFactory.Size(row, flexW: 1f);

            bool warm = OutreachSystem.IsWarm(lead);
            UiFactory.Label(row.transform,
                $"<b>{lead.company}</b>  <size=12>{lead.contact} · {lead.title}</size>\n" +
                $"<size=12>Prefers {OutreachSystem.Name(lead.preferredChannel)} · {lead.touches} touch(es)</size>",
                15, UiTheme.TextPrimary, TextAnchor.UpperLeft);

            var meter = UiFactory.MakeMeter(row.transform,
                warm ? "WARM — ready to book" : $"Interest {Mathf.RoundToInt(lead.interest * 100f)}%",
                warm ? UiTheme.Positive : UiTheme.Accent, 18f);
            meter.Set(lead.interest);

            var actions = UiFactory.Panel(row.transform, new Color(0f, 0f, 0f, 0f), "Actions").gameObject;
            UiFactory.HLayout(actions, spacing: 6, expandW: true, expandH: true);
            UiFactory.Size(actions, prefH: 38f);

            if (warm)
            {
                var book = UiFactory.Button(actions.transform, "Book meeting ▶", () => Convert(lead),
                    UiTheme.Positive, Color.white, 15, TextAnchor.MiddleCenter);
                UiFactory.Size(book.gameObject, flexW: 1f);
            }
            else
            {
                TouchButton(actions.transform, lead, OutreachChannel.Email);
                TouchButton(actions.transform, lead, OutreachChannel.LinkedIn);
                TouchButton(actions.transform, lead, OutreachChannel.Call);
                TouchButton(actions.transform, lead, OutreachChannel.Video);
            }
        }

        private void TouchButton(Transform parent, OutreachLead lead, OutreachChannel channel)
        {
            bool fresh = !lead.touchedBefore || lead.lastChannel != channel; // variety is good
            var b = UiFactory.Button(parent, OutreachSystem.Name(channel), () => DoTouch(lead, channel),
                fresh ? UiTheme.Accent : UiTheme.PanelDark, UiTheme.TextPrimary, 13, TextAnchor.MiddleCenter);
            UiFactory.Size(b.gameObject, flexW: 1f);
        }

        private void DoTouch(OutreachLead lead, OutreachChannel channel)
        {
            var res = OutreachSystem.Touch(_character, lead, channel);
            if (res.consumed)
            {
                GameManager.Instance.SaveProfile();
                ToastKind kind = res.status == LeadStatus.Dead ? ToastKind.Danger
                    : res.status == LeadStatus.Warm ? ToastKind.Positive : ToastKind.Info;
                Toasts.Show(res.message, kind);
            }
            else
            {
                Toasts.Show(res.message, ToastKind.Warning);
            }
            Build();
        }

        private void DoSource()
        {
            int seed = unchecked(Environment.TickCount + _character.leads.Count * 31
                + (_character.career != null ? _character.career.day : 0));
            var lead = OutreachSystem.SourceLead(_character, seed);
            if (lead == null) Toasts.Show("No outreach actions left today.", ToastKind.Warning);
            else { GameManager.Instance.SaveProfile(); Toasts.Show($"Sourced {lead.company} — {lead.contact}.", ToastKind.Info); }
            Build();
        }

        private void Convert(OutreachLead lead)
        {
            if (!CareerSystem.HasCallsLeft(_character))
            {
                Toasts.Show("No calls left today — end the day, then book the meeting.", ToastKind.Warning);
                return;
            }
            var scenario = OutreachSystem.BuildWarmScenario(_character, lead);
            CareerSystem.ConsumeCall(_character);
            GameManager.Instance.SaveProfile();
            if (_overlay != null) UnityEngine.Object.Destroy(_overlay);
            GameManager.Instance.StartCareerCall(scenario); // loads the call floor
        }
    }
}
