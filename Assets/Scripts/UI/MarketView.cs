using System;
using System.Collections.Generic;
using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// Read-only freight market board: shows today's $/mile on your accounts' lanes
    /// (plus a few national reference lanes) versus their typical rate, so you can see
    /// where the market is hot and quote accordingly. Rates move with the seasonal
    /// swing in <see cref="FreightMarket"/>.
    /// </summary>
    public class MarketView
    {
        private readonly Transform _canvas;
        private readonly BDRCharacter _character;
        private readonly Action _onClose;
        private GameObject _overlay;

        public MarketView(Transform canvas, BDRCharacter character, Action onClose)
        {
            _canvas = canvas;
            _character = character;
            _onClose = onClose;
        }

        public void Open() => Build();

        private void Build()
        {
            if (_character == null) return;
            if (_overlay != null) UnityEngine.Object.Destroy(_overlay);

            var overlay = UiFactory.Panel(_canvas, new Color(0f, 0f, 0f, 0.92f), "MarketOverlay");
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.VLayout(overlay.gameObject, pad: 24, spacing: 10, expandH: false,
                align: TextAnchor.UpperCenter);
            _overlay = overlay.gameObject;

            int day = _character.career != null ? _character.career.day : 1;

            UiFactory.Label(overlay.transform, "FREIGHT MARKET", 28, UiTheme.AccentStrong,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Label(overlay.transform,
                $"Day {day} — rates move with season & demand. Quote into a hot market for fatter margin.",
                14, UiTheme.TextMuted, TextAnchor.MiddleCenter, FontStyle.Italic);

            var content = UiFactory.MakeScrollView(overlay.transform, new Color(0f, 0f, 0f, 0.15f));
            UiFactory.Size(content.parent.parent.gameObject, flexH: 1f);

            foreach (var lane in LanesToShow())
                BuildRow(content, lane, day);

            var close = UiFactory.Button(overlay.transform, "Close",
                () => { UnityEngine.Object.Destroy(_overlay); _onClose?.Invoke(); },
                UiTheme.Accent, UiTheme.TextPrimary, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(close.gameObject, prefH: 48f, prefW: 240f);
        }

        private List<Lane> LanesToShow()
        {
            var lanes = new List<Lane>();
            if (_character.accounts != null)
                foreach (var a in _character.accounts)
                    if (a != null && a.active && a.lanes != null)
                        foreach (var l in a.lanes)
                            if (l != null) lanes.Add(l);

            if (lanes.Count < 4) lanes.AddRange(Reference);
            return lanes;
        }

        private void BuildRow(Transform parent, Lane lane, int day)
        {
            float market = FreightMarket.MarketRatePerMile(lane, day);
            float baseline = Mathf.Max(0.5f, lane.currentRatePerMile);
            float deltaPct = (market / baseline - 1f) * 100f;

            bool hot = deltaPct > 2f;
            bool soft = deltaPct < -2f;
            Color col = hot ? UiTheme.Positive : (soft ? UiTheme.Warning : UiTheme.TextMuted);
            string tag = hot ? "HOT" : (soft ? "SOFT" : "STEADY");

            var row = UiFactory.Panel(parent, UiTheme.PanelDark, "Lane").gameObject;
            UiFactory.HLayout(row, pad: 10, spacing: 10, expandW: false, expandH: true);
            UiFactory.Size(row, prefH: 56f, flexW: 1f);

            var info = UiFactory.Label(row.transform,
                $"<b>{lane.equipment}</b>  <size=12>{lane.label}</size>",
                14, UiTheme.TextPrimary, TextAnchor.MiddleLeft);
            UiFactory.Size(info.gameObject, flexW: 1f);

            var rate = UiFactory.Label(row.transform,
                $"<b>${market:0.00}/mi</b>  <size=12>{(deltaPct >= 0 ? "+" : "")}{deltaPct:0.0}%  {tag}</size>",
                14, col, TextAnchor.MiddleRight, FontStyle.Bold);
            UiFactory.Size(rate.gameObject, prefW: 220f);
        }

        private static readonly Lane[] Reference =
        {
            new Lane { label = "Los Angeles, CA → Dallas, TX", miles = 1440, equipment = EquipmentType.DryVan, currentRatePerMile = 2.15f },
            new Lane { label = "Chicago, IL → Atlanta, GA", miles = 720, equipment = EquipmentType.Reefer, currentRatePerMile = 2.85f },
            new Lane { label = "Seattle, WA → Denver, CO", miles = 1320, equipment = EquipmentType.Flatbed, currentRatePerMile = 2.60f },
            new Lane { label = "Miami, FL → Charlotte, NC", miles = 730, equipment = EquipmentType.DryVan, currentRatePerMile = 2.40f }
        };
    }
}
