using System;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using UnityEngine;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>Modal overlay listing every achievement and whether it's been earned.</summary>
    public class AchievementsView
    {
        private readonly Transform _canvas;
        private readonly BDRCharacter _character;
        private readonly Action _onClose;
        private GameObject _overlay;

        public AchievementsView(Transform canvas, BDRCharacter character, Action onClose)
        {
            _canvas = canvas;
            _character = character;
            _onClose = onClose;
        }

        public void Open()
        {
            int earned = _character.unlockedAchievements != null ? _character.unlockedAchievements.Count : 0;

            var overlay = UiFactory.Panel(_canvas, new Color(0f, 0f, 0f, 0.9f), "AchievementsOverlay");
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.VLayout(overlay.gameObject, pad: 24, spacing: 10, expandH: false,
                align: TextAnchor.UpperCenter);
            _overlay = overlay.gameObject;

            UiFactory.Label(overlay.transform, "ACHIEVEMENTS", 28, UiTheme.AccentStrong,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Label(overlay.transform, $"{earned} / {AchievementLibrary.All.Count} earned", 15,
                UiTheme.TextMuted, TextAnchor.MiddleCenter, FontStyle.Bold);

            var content = UiFactory.MakeScrollView(overlay.transform, new Color(0f, 0f, 0f, 0.15f));
            UiFactory.Size(content.parent.parent.gameObject, flexH: 1f);

            foreach (var a in AchievementLibrary.All)
                BuildRow(content, a);

            var close = UiFactory.Button(overlay.transform, "Close",
                () => { UnityEngine.Object.Destroy(_overlay); _onClose?.Invoke(); },
                UiTheme.Accent, UiTheme.TextPrimary, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(close.gameObject, prefH: 48f, prefW: 240f);
        }

        private void BuildRow(Transform parent, Achievement a)
        {
            bool earned = AchievementSystem.IsUnlocked(_character, a.Id);

            var row = UiFactory.Panel(parent, UiTheme.Panel, "Ach").gameObject;
            UiFactory.VLayout(row, pad: 10, spacing: 2, expandH: false);
            UiFactory.Size(row, flexW: 1f);

            string tag = earned ? "   <color=#EAA833>★ EARNED</color>" : "   <color=#6A7080>locked</color>";
            UiFactory.Label(row.transform, $"<b>{a.Name}</b>{tag}", 16,
                earned ? UiTheme.TextPrimary : UiTheme.TextMuted, TextAnchor.UpperLeft, FontStyle.Bold);
            UiFactory.Label(row.transform, a.Description, 13, UiTheme.TextMuted,
                TextAnchor.UpperLeft, FontStyle.Italic);
        }
    }
}
