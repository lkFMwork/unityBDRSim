using System;
using System.Collections.Generic;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// Modal overlay listing every quest with live progress (read from the
    /// character) and completion state. Read-only — rewards are granted
    /// automatically by <see cref="QuestSystem.Sync"/>.
    /// </summary>
    public class QuestLogView
    {
        private readonly Transform _canvas;
        private readonly BDRCharacter _character;
        private readonly Action _onClose;
        private GameObject _overlay;

        public QuestLogView(Transform canvas, BDRCharacter character, Action onClose)
        {
            _canvas = canvas;
            _character = character;
            _onClose = onClose;
        }

        public void Open()
        {
            var overlay = UiFactory.Panel(_canvas, new Color(0f, 0f, 0f, 0.9f), "QuestOverlay");
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.VLayout(overlay.gameObject, pad: 24, spacing: 10, expandH: false,
                align: TextAnchor.UpperCenter);
            _overlay = overlay.gameObject;

            UiFactory.Label(overlay.transform, "QUESTS", 28, UiTheme.AccentStrong,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            var content = UiFactory.MakeScrollView(overlay.transform, new Color(0f, 0f, 0f, 0.15f));
            UiFactory.Size(content.parent.parent.gameObject, flexH: 1f);

            foreach (var quest in QuestLibrary.All)
                BuildRow(content, quest);

            var close = UiFactory.Button(overlay.transform, "Close",
                () => { UnityEngine.Object.Destroy(_overlay); _onClose?.Invoke(); },
                UiTheme.Accent, UiTheme.TextPrimary, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(close.gameObject, prefH: 48f, prefW: 240f);
        }

        private void BuildRow(Transform parent, QuestDefinition quest)
        {
            bool done = QuestSystem.IsComplete(_character, quest);

            var row = UiFactory.Panel(parent, UiTheme.Panel, "Quest").gameObject;
            UiFactory.VLayout(row, pad: 12, spacing: 4, expandH: false);
            UiFactory.Size(row, flexW: 1f);

            string status = done ? "   <color=#5FBF7F>✓ COMPLETE</color>" : "";
            UiFactory.Label(row.transform, $"<b>{quest.Title}</b>{status}", 17,
                done ? UiTheme.Positive : UiTheme.TextPrimary, TextAnchor.UpperLeft, FontStyle.Bold);
            UiFactory.Label(row.transform, quest.Description, 13, UiTheme.TextMuted,
                TextAnchor.UpperLeft, FontStyle.Italic);

            foreach (var obj in quest.Objectives)
            {
                int value = Mathf.Min(QuestSystem.ValueOf(_character, obj.Type), obj.Target);
                UiFactory.Label(row.transform, $"• {obj.Describe()}   ({value}/{obj.Target})", 13,
                    UiTheme.TextPrimary, TextAnchor.UpperLeft);
            }

            string reward = RewardText(quest);
            if (!string.IsNullOrEmpty(reward))
                UiFactory.Label(row.transform, "Reward: " + reward, 12, UiTheme.Warning,
                    TextAnchor.UpperLeft, FontStyle.Bold);
        }

        private static string RewardText(QuestDefinition quest)
        {
            var parts = new List<string>();
            if (quest.RewardXp > 0) parts.Add($"{quest.RewardXp} XP");
            if (quest.RewardSkillPoints > 0) parts.Add($"{quest.RewardSkillPoints} SP");
            return string.Join(" + ", parts);
        }
    }
}
