using System;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using UnityEngine;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>Modal overlay showing the office deal-count leaderboard.</summary>
    public class LeaderboardView
    {
        private readonly Transform _canvas;
        private readonly BDRCharacter _character;
        private readonly Action _onClose;
        private GameObject _overlay;

        public LeaderboardView(Transform canvas, BDRCharacter character, Action onClose)
        {
            _canvas = canvas;
            _character = character;
            _onClose = onClose;
        }

        public void Open()
        {
            var overlay = UiFactory.Panel(_canvas, new Color(0f, 0f, 0f, 0.9f), "LeaderboardOverlay");
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.VLayout(overlay.gameObject, pad: 24, spacing: 10, expandH: false,
                align: TextAnchor.UpperCenter);
            _overlay = overlay.gameObject;

            UiFactory.Label(overlay.transform, "OFFICE LEADERBOARD", 28, UiTheme.AccentStrong,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Label(overlay.transform, "Deals closed", 14, UiTheme.TextMuted,
                TextAnchor.MiddleCenter, FontStyle.Italic);

            var content = UiFactory.MakeScrollView(overlay.transform, new Color(0f, 0f, 0f, 0.15f));
            UiFactory.Size(content.parent.parent.gameObject, flexH: 1f);

            var standings = LeaderboardSystem.Standings(_character);
            for (int i = 0; i < standings.Count; i++)
                BuildRow(content, i + 1, standings[i]);

            var close = UiFactory.Button(overlay.transform, "Close",
                () => { UnityEngine.Object.Destroy(_overlay); _onClose?.Invoke(); },
                UiTheme.Accent, UiTheme.TextPrimary, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(close.gameObject, prefH: 48f, prefW: 240f);
        }

        private void BuildRow(Transform parent, int rank, LeaderboardEntry entry)
        {
            var row = UiFactory.Panel(parent, entry.IsPlayer ? UiTheme.Accent : UiTheme.Panel, "Row").gameObject;
            UiFactory.HLayout(row, pad: 10, spacing: 10, expandW: false, expandH: true);
            UiFactory.Size(row, prefH: 40f, flexW: 1f);

            var rankLabel = UiFactory.Label(row.transform, $"#{rank}", 16, UiTheme.TextPrimary,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Size(rankLabel.gameObject, prefW: 44f);

            var nameLabel = UiFactory.Label(row.transform, entry.Name, 16, UiTheme.TextPrimary,
                TextAnchor.MiddleLeft, entry.IsPlayer ? FontStyle.Bold : FontStyle.Normal);
            UiFactory.Size(nameLabel.gameObject, flexW: 1f);

            var dealsLabel = UiFactory.Label(row.transform, entry.Deals.ToString(), 16, UiTheme.TextPrimary,
                TextAnchor.MiddleRight, FontStyle.Bold);
            UiFactory.Size(dealsLabel.gameObject, prefW: 60f);
        }
    }
}
