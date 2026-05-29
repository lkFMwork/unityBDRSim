using System;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// Modal settings overlay: master volume, mute, graphics quality tier, and a
    /// reduced-motion accessibility toggle. Reads/writes <see cref="SettingsService"/>
    /// and rebuilds to reflect changes. Reachable from the menu and the cockpit.
    /// </summary>
    public class SettingsView
    {
        private readonly Transform _canvas;
        private readonly Action _onClose;
        private GameObject _overlay;

        public SettingsView(Transform canvas, Action onClose)
        {
            _canvas = canvas;
            _onClose = onClose;
        }

        public void Open() => Build();

        private void Build()
        {
            if (_overlay != null) UnityEngine.Object.Destroy(_overlay);

            var overlay = UiFactory.Panel(_canvas, new Color(0f, 0f, 0f, 0.92f), "SettingsOverlay");
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.VLayout(overlay.gameObject, pad: 28, spacing: 12, expandH: false,
                align: TextAnchor.UpperCenter);
            _overlay = overlay.gameObject;

            UiFactory.Label(overlay.transform, "SETTINGS", 28, UiTheme.AccentStrong,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            BuildVolumeRow(overlay.transform);
            Row(overlay.transform, "Sound",
                SettingsService.Muted ? "Muted" : "On", SettingsService.ToggleMute);
            Row(overlay.transform, "Graphics quality",
                SettingsService.QualityName, SettingsService.CycleQuality);
            Row(overlay.transform, "Reduced motion (accessibility)",
                SettingsService.ReducedMotion ? "On" : "Off", SettingsService.ToggleReducedMotion);

            var close = UiFactory.Button(overlay.transform, "Close",
                () => { UnityEngine.Object.Destroy(_overlay); _onClose?.Invoke(); },
                UiTheme.Accent, UiTheme.TextPrimary, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(close.gameObject, prefH: 48f, prefW: 240f);
        }

        private void BuildVolumeRow(Transform parent)
        {
            var row = UiFactory.Panel(parent, UiTheme.Panel, "Volume").gameObject;
            UiFactory.HLayout(row, pad: 12, spacing: 10, expandW: false, expandH: true);
            UiFactory.Size(row, prefH: 56f, flexW: 1f);

            var label = UiFactory.Label(row.transform, "Master volume", 16, UiTheme.TextPrimary,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Size(label.gameObject, flexW: 1f);

            var minus = UiFactory.Button(row.transform, "–",
                () => { SettingsService.SetVolume(SettingsService.MasterVolume - 0.1f); Build(); },
                UiTheme.Accent, UiTheme.TextPrimary, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(minus.gameObject, prefW: 48f);

            var value = UiFactory.Label(row.transform, $"{Mathf.RoundToInt(SettingsService.MasterVolume * 100f)}%",
                16, UiTheme.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Size(value.gameObject, prefW: 70f);

            var plus = UiFactory.Button(row.transform, "+",
                () => { SettingsService.SetVolume(SettingsService.MasterVolume + 0.1f); Build(); },
                UiTheme.Accent, UiTheme.TextPrimary, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(plus.gameObject, prefW: 48f);
        }

        private void Row(Transform parent, string label, string valueText, Action onToggle)
        {
            var row = UiFactory.Panel(parent, UiTheme.Panel, "Row").gameObject;
            UiFactory.HLayout(row, pad: 12, spacing: 10, expandW: false, expandH: true);
            UiFactory.Size(row, prefH: 56f, flexW: 1f);

            var l = UiFactory.Label(row.transform, label, 16, UiTheme.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Size(l.gameObject, flexW: 1f);

            var btn = UiFactory.Button(row.transform, valueText, () => { onToggle(); Build(); },
                UiTheme.Accent, UiTheme.TextPrimary, 15, TextAnchor.MiddleCenter);
            UiFactory.Size(btn.gameObject, prefW: 160f);
        }
    }
}
