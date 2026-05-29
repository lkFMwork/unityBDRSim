using System;
using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// Modal overlay to spend commission (cash) on business upgrades — CRM dials, lead
    /// quality, an account manager. Mirrors the skill-tree view: rebuilds after each
    /// purchase, closing invokes a callback.
    /// </summary>
    public class UpgradesView
    {
        private readonly Transform _canvas;
        private readonly BDRCharacter _character;
        private readonly Action _onClose;
        private GameObject _overlay;

        public UpgradesView(Transform canvas, BDRCharacter character, Action onClose)
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

            var overlay = UiFactory.Panel(_canvas, new Color(0f, 0f, 0f, 0.90f), "UpgradesOverlay");
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.VLayout(overlay.gameObject, pad: 24, spacing: 10, expandH: false,
                align: TextAnchor.UpperCenter);
            _overlay = overlay.gameObject;

            UiFactory.Label(overlay.transform, "BUSINESS UPGRADES", 28, UiTheme.AccentStrong,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Label(overlay.transform, $"Cash available: ${_character.cash:N0}", 18,
                UiTheme.Positive, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Label(overlay.transform,
                $"Weekly bills: ${EconomySystem.WeeklyBills(_character):N0} — keep the book earning to cover them.",
                14, UiTheme.TextMuted, TextAnchor.MiddleCenter, FontStyle.Italic);

            var content = UiFactory.MakeScrollView(overlay.transform, new Color(0f, 0f, 0f, 0.15f));
            UiFactory.Size(content.parent.parent.gameObject, flexH: 1f);

            foreach (var def in UpgradeCatalog.All)
                BuildRow(content, def);

            var close = UiFactory.Button(overlay.transform, "Close",
                () => { UnityEngine.Object.Destroy(_overlay); _onClose?.Invoke(); },
                UiTheme.Accent, UiTheme.TextPrimary, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(close.gameObject, prefH: 48f, prefW: 240f);
        }

        private void BuildRow(Transform parent, UpgradeDef def)
        {
            var row = UiFactory.Panel(parent, UiTheme.Panel, "Upgrade").gameObject;
            UiFactory.HLayout(row, pad: 10, spacing: 10, expandW: false, expandH: true);
            UiFactory.Size(row, prefH: 96f, flexW: 1f);

            int level = EconomySystem.LevelOf(_character, def.id);
            int cost = EconomySystem.CostToUpgrade(_character, def.id);
            bool maxed = cost < 0;
            bool canBuy = EconomySystem.CanAfford(_character, def.id);

            var info = UiFactory.Label(row.transform,
                $"<b>{def.name}</b>  <size=11>(Lv {level}/{def.maxLevel})</size>\n<size=13>{def.description}</size>",
                15, UiTheme.TextPrimary, TextAnchor.MiddleLeft);
            UiFactory.Size(info.gameObject, flexW: 1f);

            string label = maxed ? "MAX" : (canBuy ? $"Buy  ${cost:N0}" : $"${cost:N0}");
            Color bg = maxed ? UiTheme.Positive : (canBuy ? UiTheme.Accent : UiTheme.PanelDark);

            var btn = UiFactory.Button(row.transform, label, () =>
            {
                if (EconomySystem.Buy(_character, def.id))
                {
                    GameManager.Instance.SaveProfile();
                    Toasts.Show($"Bought {def.name} (Lv {EconomySystem.LevelOf(_character, def.id)}).",
                        ToastKind.Positive);
                    Build();
                }
            }, bg, UiTheme.TextPrimary, 15, TextAnchor.MiddleCenter);
            UiFactory.Size(btn.gameObject, prefW: 150f);
            btn.interactable = !maxed && canBuy;
        }
    }
}
