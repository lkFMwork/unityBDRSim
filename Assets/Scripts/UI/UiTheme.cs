using UnityEngine;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>Central color palette so the menu and call screens stay consistent.</summary>
    public static class UiTheme
    {
        public static readonly Color Background = new Color(0.07f, 0.09f, 0.14f);
        public static readonly Color Panel = new Color(0.13f, 0.16f, 0.24f);
        public static readonly Color PanelDark = new Color(0.10f, 0.12f, 0.18f);
        public static readonly Color Accent = new Color(0.16f, 0.45f, 0.85f);
        public static readonly Color AccentStrong = new Color(0.22f, 0.55f, 0.95f);
        public static readonly Color Positive = new Color(0.28f, 0.70f, 0.45f);
        public static readonly Color Warning = new Color(0.92f, 0.66f, 0.20f);
        public static readonly Color Danger = new Color(0.85f, 0.30f, 0.30f);

        public static readonly Color TextPrimary = new Color(0.93f, 0.95f, 0.98f);
        public static readonly Color TextMuted = new Color(0.64f, 0.70f, 0.80f);

        // Hex strings for rich-text <color> tags in the transcript.
        public const string HexProspect = "#FFD27F"; // prospect speaks (amber)
        public const string HexRep = "#9FD0FF";       // the rep speaks (light blue)
        public const string HexNarrator = "#8C95A8";  // narration (muted)

        public static string RichRep(string s) => $"<color={HexRep}><b>You:</b> {s}</color>";
        public static string RichProspect(string s) => $"<color={HexProspect}><b>Prospect:</b> {s}</color>";
        public static string RichNarrator(string s) => $"<color={HexNarrator}><i>{s}</i></color>";
    }
}
