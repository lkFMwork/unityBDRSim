using System.Collections.Generic;
using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// The freight desk — your book of business once deals are closed. Won accounts
    /// tender loads; you set a sell rate (stay under the shipper's ceiling to win the
    /// freight), then cover each won load with a carrier (cheaper = more margin but
    /// more risk it falls through). Advancing the day delivers in-transit loads (cash
    /// commission), tenders new freight, and churns neglected accounts. This is the
    /// recurring money loop that makes closing a deal the <i>start</i>, not the end.
    /// Attach to a GameObject in the FreightDesk scene (the setup tool does this).
    /// </summary>
    public class FreightDeskController : MonoBehaviour
    {
        private BDRCharacter _c;
        private Canvas _canvas;
        private readonly Dictionary<string, float> _desired = new Dictionary<string, float>();
        private string _coveringLoadId;
        private string _flash;

        private void Start()
        {
            _c = GameManager.Instance.Profile;
            if (_c == null) { BuildNoProfile(); return; }
            CareerSystem.EnsureStarted(_c);
            FreightSystem.EnsureStarted(_c);
            Rebuild();
        }

        // ---- shell ----------------------------------------------------------

        private void Rebuild()
        {
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(false);
                Destroy(_canvas.gameObject);
            }

            _canvas = UiFactory.CreateScreenCanvas("FreightDeskCanvas");
            var root = UiFactory.Panel(_canvas.transform, UiTheme.Background, "Root");
            UiFactory.Stretch(root.rectTransform);
            UiFactory.VLayout(root.gameObject, pad: 14, spacing: 10, expandH: false);

            BuildHeader(root.transform);

            if (!string.IsNullOrEmpty(_flash))
                UiFactory.Label(root.transform, _flash, 15, UiTheme.Positive,
                    TextAnchor.MiddleLeft, FontStyle.Bold, "Flash");

            var body = UiFactory.Panel(root.transform, UiTheme.Background, "Body").gameObject;
            UiFactory.HLayout(body, spacing: 12, expandW: true, expandH: true);
            UiFactory.Size(body, flexH: 1f);

            BuildAccountsColumn(body.transform);
            BuildBoardColumn(body.transform);

            BuildFooter(root.transform);
        }

        private void BuildHeader(Transform parent)
        {
            var bar = UiFactory.Panel(parent, UiTheme.PanelDark, "Header").gameObject;
            UiFactory.HLayout(bar, pad: 12, spacing: 12, expandH: true);
            UiFactory.Size(bar, prefH: 64f, flexH: 0f);

            var title = UiFactory.Label(bar.transform, "FREIGHT DESK", 24, UiTheme.AccentStrong,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Size(title.gameObject, prefW: 210f);

            int week = CareerSystem.Week(_c.career.day);
            int active = FreightSystem.ActiveAccounts(_c).Count;
            var stats = UiFactory.Label(bar.transform,
                $"<b>Cash ${_c.cash:N0}</b>    ·    Lifetime margin ${_c.lifetimeMargin:N0}    ·    " +
                $"Book: {active} account(s)    ·    Day {_c.career.day} (Week {week})",
                16, UiTheme.TextPrimary, TextAnchor.MiddleLeft);
            UiFactory.Size(stats.gameObject, flexW: 1f);
        }

        private void BuildFooter(Transform parent)
        {
            var footer = UiFactory.Panel(parent, UiTheme.Background, "Footer").gameObject;
            UiFactory.HLayout(footer, spacing: 10, expandW: true, expandH: true);
            UiFactory.Size(footer, prefH: 52f, flexH: 0f);

            var advance = UiFactory.Button(footer.transform, "Advance Day ▶", AdvanceDay,
                UiTheme.Positive, Color.white, 16, TextAnchor.MiddleCenter);
            UiFactory.Size(advance.gameObject, flexW: 1f);

            var back = UiFactory.Button(footer.transform, "Back",
                () => GameManager.Instance.ReturnToHub(), UiTheme.PanelDark, UiTheme.TextMuted,
                15, TextAnchor.MiddleCenter);
            UiFactory.Size(back.gameObject, prefW: 140f);
        }

        // ---- accounts column ------------------------------------------------

        private void BuildAccountsColumn(Transform parent)
        {
            var panel = UiFactory.Panel(parent, UiTheme.Panel, "Accounts").gameObject;
            UiFactory.VLayout(panel, pad: 12, spacing: 8, expandH: false);
            UiFactory.Size(panel, prefW: 320f, flexW: 0f, flexH: 1f);

            UiFactory.Label(panel.transform, "YOUR BOOK", 16, UiTheme.AccentStrong,
                TextAnchor.MiddleLeft, FontStyle.Bold);

            var content = UiFactory.MakeScrollView(panel.transform, new Color(0f, 0f, 0f, 0.12f));
            UiFactory.Size(content.parent.parent.gameObject, flexH: 1f);

            if (_c.accounts == null || _c.accounts.Count == 0)
            {
                UiFactory.Label(content,
                    "No accounts yet.\n\nClose a deal on a call or in Texas to open your first account — " +
                    "then they'll start tendering freight here on the board.",
                    13, UiTheme.TextMuted, TextAnchor.UpperLeft, FontStyle.Italic);
                return;
            }

            foreach (var a in _c.accounts)
                if (a != null) BuildAccountCard(content, a);
        }

        private void BuildAccountCard(Transform parent, FreightAccount a)
        {
            var card = UiFactory.Panel(parent,
                a.active ? UiTheme.PanelDark : new Color(0.10f, 0.10f, 0.12f), "Acct").gameObject;
            UiFactory.VLayout(card, pad: 10, spacing: 4, expandH: false);
            UiFactory.Size(card, flexW: 1f);

            string head = a.active
                ? $"<b>{a.company}</b>"
                : $"<b>{a.company}</b>  <color=#D94C4C>CHURNED</color>";
            UiFactory.Label(card.transform, head, 15, UiTheme.TextPrimary, TextAnchor.UpperLeft);
            UiFactory.Label(card.transform, $"{a.location} · {a.industry}", 12, UiTheme.TextMuted,
                TextAnchor.UpperLeft);

            var meter = UiFactory.MakeMeter(card.transform, $"Service {Mathf.RoundToInt(a.health * 100f)}%",
                HealthColor(a.health), 18f);
            meter.Set(a.health);

            UiFactory.Label(card.transform,
                $"Lifetime margin ${a.lifetimeMargin:N0} · {a.loadsDelivered} delivered · {a.loadsFailed} failed",
                12, UiTheme.TextMuted, TextAnchor.UpperLeft);
        }

        // ---- load board -----------------------------------------------------

        private void BuildBoardColumn(Transform parent)
        {
            var panel = UiFactory.Panel(parent, UiTheme.Panel, "Board").gameObject;
            UiFactory.VLayout(panel, pad: 12, spacing: 8, expandH: false);
            UiFactory.Size(panel, flexW: 1f, flexH: 1f);

            UiFactory.Label(panel.transform, "LOAD BOARD", 16, UiTheme.AccentStrong,
                TextAnchor.MiddleLeft, FontStyle.Bold);

            var content = UiFactory.MakeScrollView(panel.transform, new Color(0f, 0f, 0f, 0.12f));
            UiFactory.Size(content.parent.parent.gameObject, flexH: 1f);

            var tenders = FreightSystem.LoadsByStatus(_c, LoadStatus.Offered);
            var needCarrier = FreightSystem.LoadsByStatus(_c, LoadStatus.AwaitingCarrier);
            var inTransit = FreightSystem.LoadsByStatus(_c, LoadStatus.InTransit);
            var recent = FreightSystem.Recent(_c);

            Section(content, $"TENDERS — quote these ({tenders.Count})");
            if (tenders.Count == 0) Empty(content, "No open tenders. Advance the day for new freight.");
            else foreach (var l in tenders) BuildTenderRow(content, l);

            Section(content, $"COVER — assign a carrier ({needCarrier.Count})");
            if (needCarrier.Count == 0) Empty(content, "Nothing waiting on a carrier.");
            else foreach (var l in needCarrier) BuildCoverRow(content, l);

            Section(content, $"IN TRANSIT ({inTransit.Count})");
            if (inTransit.Count == 0) Empty(content, "No loads moving right now.");
            else foreach (var l in inTransit) BuildTransitRow(content, l);

            if (recent.Count > 0)
            {
                Section(content, "RECENT");
                foreach (var l in recent) BuildRecentRow(content, l);
            }
        }

        private void BuildTenderRow(Transform parent, FreightLoad l)
        {
            var row = UiFactory.Panel(parent, UiTheme.PanelDark, "Tender").gameObject;
            UiFactory.VLayout(row, pad: 10, spacing: 6, expandH: false);
            UiFactory.Size(row, flexW: 1f);

            UiFactory.Label(row.transform,
                $"<b>{l.accountCompany}</b>   {l.LaneLabel} · {l.miles} mi · {l.equipment} · {l.commodity}",
                14, UiTheme.TextPrimary, TextAnchor.UpperLeft);
            UiFactory.Label(row.transform,
                $"market ${l.marketRatePerMile:0.00}/mi · they'll pay up to ~${l.shipperMaxPerMile:0.00}/mi · expires day {l.expiresDay}",
                12, UiTheme.TextMuted, TextAnchor.UpperLeft);

            float rate = DesiredRate(l);
            float estMargin = (rate - l.marketRatePerMile * 0.88f) * l.miles;

            var ctl = UiFactory.Panel(row.transform, new Color(0f, 0f, 0f, 0f), "Ctl").gameObject;
            UiFactory.HLayout(ctl, spacing: 8, expandW: false, expandH: true);
            UiFactory.Size(ctl, prefH: 40f);

            var minus = UiFactory.Button(ctl.transform, "–", () => AdjustRate(l, -0.05f),
                UiTheme.Accent, UiTheme.TextPrimary, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(minus.gameObject, prefW: 42f);

            var rateLbl = UiFactory.Label(ctl.transform, $"${rate:0.00}/mi", 16, UiTheme.TextPrimary,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Size(rateLbl.gameObject, prefW: 104f);

            var plus = UiFactory.Button(ctl.transform, "+", () => AdjustRate(l, 0.05f),
                UiTheme.Accent, UiTheme.TextPrimary, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(plus.gameObject, prefW: 42f);

            var info = UiFactory.Label(ctl.transform,
                $"sell ${rate * l.miles:N0} · est. margin ${estMargin:N0}", 12, UiTheme.TextMuted,
                TextAnchor.MiddleLeft);
            UiFactory.Size(info.gameObject, flexW: 1f);

            var quote = UiFactory.Button(ctl.transform, "Quote ▶", () => DoQuote(l),
                UiTheme.Positive, Color.white, 14, TextAnchor.MiddleCenter);
            UiFactory.Size(quote.gameObject, prefW: 104f);
        }

        private void BuildCoverRow(Transform parent, FreightLoad l)
        {
            var row = UiFactory.Panel(parent, UiTheme.PanelDark, "Cover").gameObject;
            UiFactory.VLayout(row, pad: 10, spacing: 6, expandH: false);
            UiFactory.Size(row, flexW: 1f);

            UiFactory.Label(row.transform,
                $"<b>{l.accountCompany}</b>   {l.LaneLabel} · {l.miles} mi · sold ${l.quotedRatePerMile:0.00}/mi (${l.ShipperTotal:N0})",
                14, UiTheme.TextPrimary, TextAnchor.UpperLeft);

            if (_coveringLoadId != l.id)
            {
                var find = UiFactory.Button(row.transform, "Find carriers ▶",
                    () => { _coveringLoadId = l.id; Rebuild(); }, UiTheme.Accent, UiTheme.TextPrimary,
                    14, TextAnchor.MiddleCenter);
                UiFactory.Size(find.gameObject, prefW: 170f, prefH: 36f);
                return;
            }

            foreach (var carrier in FreightMarket.CarrierShortlist(l))
            {
                var opt = carrier; // capture for the closure
                float margin = (l.quotedRatePerMile - opt.ratePerMile) * l.miles;
                var btn = UiFactory.Button(row.transform,
                    $"{opt.name}   ·   ${opt.ratePerMile:0.00}/mi   ·   {Mathf.RoundToInt(opt.reliability * 100f)}% on-time   ·   margin ${margin:N0}",
                    () => DoCover(l, opt),
                    margin >= 0f ? UiTheme.Panel : UiTheme.Danger, UiTheme.TextPrimary, 13,
                    TextAnchor.MiddleLeft);
                UiFactory.Size(btn.gameObject, flexW: 1f, prefH: 34f);
            }

            var cancel = UiFactory.Button(row.transform, "Cancel",
                () => { _coveringLoadId = null; Rebuild(); }, UiTheme.PanelDark, UiTheme.TextMuted,
                12, TextAnchor.MiddleCenter);
            UiFactory.Size(cancel.gameObject, prefW: 100f, prefH: 28f);
        }

        private void BuildTransitRow(Transform parent, FreightLoad l)
        {
            var row = UiFactory.Panel(parent, UiTheme.PanelDark, "Transit").gameObject;
            UiFactory.VLayout(row, pad: 10, spacing: 2, expandH: false);
            UiFactory.Size(row, flexW: 1f);

            int eta = Mathf.Max(0, l.deliveryDay - _c.career.day);
            UiFactory.Label(row.transform, $"<b>{l.accountCompany}</b>   {l.LaneLabel}", 14,
                UiTheme.TextPrimary, TextAnchor.UpperLeft);
            UiFactory.Label(row.transform,
                $"{l.carrierName} · ETA day {l.deliveryDay} (in {eta}d) · margin pending ${l.TotalMargin:N0}",
                12, UiTheme.TextMuted, TextAnchor.UpperLeft);
        }

        private void BuildRecentRow(Transform parent, FreightLoad l)
        {
            Color col = l.status == LoadStatus.Delivered ? UiTheme.Positive
                : l.status == LoadStatus.FellThrough ? UiTheme.Danger : UiTheme.TextMuted;
            UiFactory.Label(parent, $"{l.accountCompany} · {l.LaneLabel} — {l.note}", 12, col,
                TextAnchor.UpperLeft);
        }

        // ---- actions --------------------------------------------------------

        private float DesiredRate(FreightLoad l)
        {
            if (!_desired.TryGetValue(l.id, out var r))
            {
                r = Mathf.Round(l.shipperMaxPerMile / 0.05f) * 0.05f; // start at the ceiling
                _desired[l.id] = r;
            }
            return r;
        }

        private void AdjustRate(FreightLoad l, float delta)
        {
            float r = DesiredRate(l) + delta;
            float lo = l.marketRatePerMile * 0.85f;
            float hi = l.shipperMaxPerMile * 1.30f;
            _desired[l.id] = Mathf.Clamp(Mathf.Round(r / 0.05f) * 0.05f, lo, hi);
            Rebuild();
        }

        private void DoQuote(FreightLoad l)
        {
            var res = FreightSystem.Quote(_c, l, DesiredRate(l));
            _flash = res.message;
            Toasts.Show(res.message, res.won ? ToastKind.Positive : ToastKind.Warning);
            GameManager.Instance.SaveProfile();
            Rebuild();
        }

        private void DoCover(FreightLoad l, CarrierOption opt)
        {
            if (FreightSystem.Cover(_c, l, opt, _c.career.day))
            {
                _flash = $"Booked {opt.name} on {l.accountCompany} — margin locked at " +
                         $"${(l.quotedRatePerMile - opt.ratePerMile) * l.miles:N0}.";
                Toasts.Show(_flash, ToastKind.Positive);
            }
            _coveringLoadId = null;
            GameManager.Instance.SaveProfile();
            Rebuild();
        }

        private void AdvanceDay()
        {
            var result = GameManager.Instance.EndBusinessDay(out var freight, out var economy);
            string ws = result.WeekEnded
                ? (result.QuotaMet
                    ? $"Week cleared {result.DealsWon}/{result.Goal} (+{result.RewardSkillPoints} SP, +{result.RewardXp} XP)."
                    : $"Week missed {result.DealsWon}/{result.Goal}.")
                : "";
            string fs = freight.Summary();
            string es = economy.Summary();
            string flash = ws;
            if (!string.IsNullOrEmpty(fs)) flash = string.IsNullOrEmpty(flash) ? fs : flash + "   " + fs;
            if (!string.IsNullOrEmpty(es)) flash = string.IsNullOrEmpty(flash) ? es : flash + "   " + es;
            _flash = string.IsNullOrEmpty(flash) ? $"Day {_c.career.day} — quiet board today." : flash;

            if (!string.IsNullOrEmpty(fs))
                Toasts.Show(fs, freight.Failed > 0 ? ToastKind.Warning : ToastKind.Positive);
            if (!string.IsNullOrEmpty(es))
                Toasts.Show(es, economy.Poached > 0 ? ToastKind.Danger : ToastKind.Info);
            if (result.WeekEnded)
                Toasts.Show(ws, result.QuotaMet ? ToastKind.Positive : ToastKind.Warning);

            _coveringLoadId = null;
            Rebuild();
        }

        // ---- helpers --------------------------------------------------------

        private static Color HealthColor(float h) =>
            h >= 0.6f ? UiTheme.Positive : h >= 0.35f ? UiTheme.Warning : UiTheme.Danger;

        private void Section(Transform parent, string text) =>
            UiFactory.Label(parent, text, 13, UiTheme.Warning, TextAnchor.MiddleLeft, FontStyle.Bold);

        private void Empty(Transform parent, string text) =>
            UiFactory.Label(parent, text, 12, UiTheme.TextMuted, TextAnchor.UpperLeft, FontStyle.Italic);

        private void BuildNoProfile()
        {
            _canvas = UiFactory.CreateScreenCanvas("FreightDeskCanvas");
            var root = UiFactory.Panel(_canvas.transform, UiTheme.Background, "Root");
            UiFactory.Stretch(root.rectTransform);
            UiFactory.VLayout(root.gameObject, pad: 40, spacing: 16, expandH: true,
                align: TextAnchor.MiddleCenter);

            UiFactory.Label(root.transform,
                "No BDR yet.\nCreate a rep and close a deal to open your freight desk.",
                18, UiTheme.Warning, TextAnchor.MiddleCenter, FontStyle.Bold);

            var back = UiFactory.Button(root.transform, "Back to Menu",
                () => GameManager.Instance.ReturnToMenu(), UiTheme.Accent, UiTheme.TextPrimary,
                16, TextAnchor.MiddleCenter);
            UiFactory.Size(back.gameObject, prefW: 220f, prefH: 52f);
        }
    }
}
