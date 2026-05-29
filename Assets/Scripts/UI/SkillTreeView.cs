using System;
using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// A modal overlay for spending skill points on perks. Rebuilds itself after an
    /// unlock so costs/availability stay current; closing invokes a callback (the
    /// menu reloads to refresh the character header).
    /// </summary>
    public class SkillTreeView
    {
        private readonly Transform _canvas;
        private readonly BDRCharacter _character;
        private readonly Action _onClose;
        private GameObject _overlay;

        public SkillTreeView(Transform canvas, BDRCharacter character, Action onClose)
        {
            _canvas = canvas;
            _character = character;
            _onClose = onClose;
        }

        public void Open() => Build();

        private void Build()
        {
            if (_overlay != null) UnityEngine.Object.Destroy(_overlay);

            var overlay = UiFactory.Panel(_canvas, new Color(0f, 0f, 0f, 0.88f), "SkillTreeOverlay");
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.VLayout(overlay.gameObject, pad: 28, spacing: 12, expandH: false,
                align: TextAnchor.UpperCenter);
            _overlay = overlay.gameObject;

            UiFactory.Label(overlay.transform, "SKILL TREE", 30, UiTheme.AccentStrong,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Label(overlay.transform,
                $"Skill points available: {_character.unspentSkillPoints}", 18, UiTheme.Warning,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            var content = UiFactory.MakeScrollView(overlay.transform, new Color(0f, 0f, 0f, 0.15f));
            UiFactory.Size(content.parent.parent.gameObject, flexH: 1f);

            foreach (var perk in PerkLibrary.All)
                BuildPerkRow(content, perk);

            var close = UiFactory.Button(overlay.transform, "Close",
                () => { UnityEngine.Object.Destroy(_overlay); _onClose?.Invoke(); },
                UiTheme.Accent, UiTheme.TextPrimary, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(close.gameObject, prefH: 50f, prefW: 240f);
        }

        private void BuildPerkRow(Transform parent, PerkDefinition perk)
        {
            var row = UiFactory.Panel(parent, UiTheme.Panel, "Perk").gameObject;
            UiFactory.HLayout(row, pad: 10, spacing: 10, expandW: false, expandH: true);
            UiFactory.Size(row, prefH: 78f, flexW: 1f);

            var info = UiFactory.Label(row.transform,
                $"<b>{perk.Name}</b>  <size=12>({perk.Cost} SP)</size>\n<size=13>{perk.Description}</size>",
                15, UiTheme.TextPrimary, TextAnchor.MiddleLeft);
            UiFactory.Size(info.gameObject, flexW: 1f);

            bool owned = PerkSystem.IsUnlocked(_character, perk.Id);
            bool canBuy = PerkSystem.CanUnlock(_character, perk);

            string label = owned ? "Owned" : (canBuy ? "Unlock" : $"{perk.Cost} SP");
            Color bg = owned ? UiTheme.Positive : (canBuy ? UiTheme.Accent : UiTheme.PanelDark);

            var btn = UiFactory.Button(row.transform, label, () =>
            {
                if (PerkSystem.Unlock(_character, perk))
                {
                    GameManager.Instance.SaveProfile();
                    Build(); // refresh costs / availability
                }
            }, bg, UiTheme.TextPrimary, 15, TextAnchor.MiddleCenter);
            UiFactory.Size(btn.gameObject, prefW: 120f);
            btn.interactable = !owned && canBuy;
        }
    }
}
