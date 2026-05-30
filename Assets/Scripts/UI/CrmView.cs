using System;
using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// The CRM home base: one dashboard over the whole operation. Surfaces today's
    /// capacity (calls + outreach touches + quota), a smart "next actions" list that
    /// reads the live state of every pillar (tenders to quote, loads to cover, warm
    /// leads to book, accounts at risk), the pipeline at a glance, and quick-launch
    /// into each activity. Read-only aggregation over the existing systems.
    /// </summary>
    public class CrmView
    {
        private readonly Transform _canvas;
        private readonly BDRCharacter _character;
        private readonly Action _onClose;
        private GameObject _overlay;

        public CrmView(Transform canvas, BDRCharacter character, Action onClose)
        {
            _canvas = canvas;
            _character = character;
            _onClose = onClose;
        }

        public void Open() => Build();

        private void Build()
        {
            var c = _character;
            if (c == null) return;
            OutreachSystem.EnsureStarted(c);
            FreightSystem.EnsureStarted(c);
            if (_overlay != null) UnityEngine.Object.Destroy(_overlay);

            var overlay = UiFactory.Panel(_canvas, new Color(0f, 0f, 0f, 0.92f), "CrmOverlay");
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.VLayout(overlay.gameObject, pad: 24, spacing: 8, expandH: false,
                align: TextAnchor.UpperCenter);
            _overlay = overlay.gameObject;

            UiFactory.Label(overlay.transform, "CRM — DASHBOARD", 28, UiTheme.AccentStrong,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Label(overlay.transform,
                $"{c.DisplayName} · {ProgressionSystem.RankTitle(c.level)} · Lv {c.level}    ·    " +
                $"Cash ${c.cash:N0}    ·    Day {c.career.day} (Week {CareerSystem.Week(c.career.day)})",
                16, UiTheme.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);

            int callsLeft = c.career.callsRemainingToday;
            int touchesLeft = OutreachSystem.ActionsLeft(c);
            UiFactory.Label(overlay.transform,
                $"Today — Calls {callsLeft}/{CareerSystem.CallsPerDay(c)}   ·   Outreach touches {touchesLeft}   ·   " +
                $"Week quota {c.career.weekDealsWon}/{c.career.weekDealsGoal}",
                14, UiTheme.TextMuted, TextAnchor.MiddleCenter, FontStyle.Italic);

            var content = UiFactory.MakeScrollView(overlay.transform, new Color(0f, 0f, 0f, 0.15f));
            UiFactory.Size(content.parent.parent.gameObject, flexH: 1f);

            BuildNextActions(content, c);
            BuildPipeline(content, c);

            BuildQuickLaunch(overlay.transform, c);

            var close = UiFactory.Button(overlay.transform, "Close",
                () => { UnityEngine.Object.Destroy(_overlay); _onClose?.Invoke(); },
                UiTheme.Accent, UiTheme.TextPrimary, 16, TextAnchor.MiddleCenter);
            UiFactory.Size(close.gameObject, prefH: 44f, prefW: 220f);
        }

        private void BuildNextActions(Transform parent, BDRCharacter c)
        {
            UiFactory.Label(parent, "NEXT ACTIONS", 14, UiTheme.Warning, TextAnchor.MiddleLeft, FontStyle.Bold);

            int tenders = FreightSystem.LoadsByStatus(c, LoadStatus.Offered).Count;
            int awaiting = FreightSystem.LoadsByStatus(c, LoadStatus.AwaitingCarrier).Count;
            int warm = OutreachSystem.Active(c).FindAll(OutreachSystem.IsWarm).Count;
            int atRisk = FreightSystem.ActiveAccounts(c).FindAll(a => a.health < 0.35f).Count;
            int callsLeft = c.career.callsRemainingToday;

            bool any = false;
            if (warm > 0) { ActionRow(parent,$"Book {warm} warm meeting(s) — they're ready to close.", UiTheme.Positive, OpenOutreach); any = true; }
            if (tenders > 0) { ActionRow(parent,$"Quote {tenders} open tender(s) on the freight desk.", UiTheme.Accent, GoFreight); any = true; }
            if (awaiting > 0) { ActionRow(parent,$"Cover {awaiting} won load(s) with a carrier.", UiTheme.Accent, GoFreight); any = true; }
            if (atRisk > 0) { ActionRow(parent,$"{atRisk} account(s) at risk — service them before a rival does.", UiTheme.Danger, GoFreight); any = true; }
            if (callsLeft > 0) { ActionRow(parent,$"Take a cold call ({callsLeft} left today).", UiTheme.Accent, TakeCall); any = true; }
            if (touchesAndIdle(c, warm)) { ActionRow(parent,"Work the pipeline — source and warm new leads.", UiTheme.Accent, OpenOutreach); any = true; }

            if (!any)
                UiFactory.Label(parent, "All caught up for today. End the day to advance the market and your book.",
                    13, UiTheme.TextMuted, TextAnchor.UpperLeft, FontStyle.Italic);
        }

        private static bool touchesAndIdle(BDRCharacter c, int warm) =>
            OutreachSystem.ActionsLeft(c) > 0 && OutreachSystem.Active(c).Count == 0;

        private void BuildPipeline(Transform parent, BDRCharacter c)
        {
            UiFactory.Label(parent, "PIPELINE AT A GLANCE", 14, UiTheme.Warning, TextAnchor.MiddleLeft, FontStyle.Bold);

            int leads = OutreachSystem.Active(c).Count;
            int warm = OutreachSystem.Active(c).FindAll(OutreachSystem.IsWarm).Count;
            var accounts = FreightSystem.ActiveAccounts(c);
            int tenders = FreightSystem.LoadsByStatus(c, LoadStatus.Offered).Count;
            int awaiting = FreightSystem.LoadsByStatus(c, LoadStatus.AwaitingCarrier).Count;
            int inTransit = FreightSystem.LoadsByStatus(c, LoadStatus.InTransit).Count;

            Stat(parent, "Outreach", $"{leads} active lead(s) · {warm} warm");
            Stat(parent, "Freight book", $"{accounts.Count} account(s) · lifetime margin ${c.lifetimeMargin:N0}");
            Stat(parent, "Loads", $"{tenders} to quote · {awaiting} to cover · {inTransit} in transit");
        }

        private void Stat(Transform parent, string label, string value)
        {
            var row = UiFactory.Panel(parent, UiTheme.PanelDark, "Stat").gameObject;
            UiFactory.HLayout(row, pad: 10, spacing: 10, expandW: false, expandH: true);
            UiFactory.Size(row, prefH: 40f, flexW: 1f);
            var l = UiFactory.Label(row.transform, label, 14, UiTheme.TextMuted, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Size(l.gameObject, prefW: 130f);
            var v = UiFactory.Label(row.transform, value, 14, UiTheme.TextPrimary, TextAnchor.MiddleLeft);
            UiFactory.Size(v.gameObject, flexW: 1f);
        }

        private void ActionRow(Transform parent, string text, Color accent, Action onClick)
        {
            var btn = UiFactory.Button(parent, "▶  " + text, onClick, accent, Color.white, 14, TextAnchor.MiddleLeft);
            UiFactory.Size(btn.gameObject, prefH: 42f, flexW: 1f);
        }

        private void BuildQuickLaunch(Transform parent, BDRCharacter c)
        {
            var row = UiFactory.Panel(parent, new Color(0f, 0f, 0f, 0f), "QuickLaunch").gameObject;
            UiFactory.HLayout(row, spacing: 8, expandW: true, expandH: true);
            UiFactory.Size(row, prefH: 50f);

            Launch(row.transform, "Call", TakeCall);
            Launch(row.transform, "Outreach", OpenOutreach);
            Launch(row.transform, "Freight", GoFreight);
            Launch(row.transform, "Market", OpenMarket);
            Launch(row.transform, "Texas", GoTexas);
            Launch(row.transform, "Upgrades", OpenUpgrades);
        }

        private void Launch(Transform parent, string label, Action onClick)
        {
            var b = UiFactory.Button(parent, label, onClick, UiTheme.Panel, UiTheme.TextPrimary, 14, TextAnchor.MiddleCenter);
            UiFactory.Size(b.gameObject, flexW: 1f);
        }

        // ---- actions --------------------------------------------------------

        private void Dismiss() { if (_overlay != null) UnityEngine.Object.Destroy(_overlay); }

        private void TakeCall()
        {
            if (!CareerSystem.HasCallsLeft(_character))
            {
                Toasts.Show("No calls left today — end the day first.", ToastKind.Warning);
                return;
            }
            Dismiss();
            GameManager.Instance.TakeColdCall();
        }

        private void OpenOutreach() => new OutreachView(_canvas, _character, null).Open();
        private void OpenUpgrades() => new UpgradesView(_canvas, _character, null).Open();
        private void OpenMarket() => new MarketView(_canvas, _character, null).Open();
        private void GoFreight() { Dismiss(); GameManager.Instance.GoToFreightDesk(); }
        private void GoTexas() { Dismiss(); GameManager.Instance.GoToTexas(); }
    }
}
