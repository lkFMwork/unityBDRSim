using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// The title screen. Branding plus a single way in: Continue drops you onto the
    /// Texas overworld — the game's hub, where every former menu action now lives as a
    /// building you drive to. If there's no save yet, you create your BDR instead.
    /// Attach to a GameObject in the MainMenu scene (the setup tool does this).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private Canvas _canvas;

        private void Start() => BuildUi();

        private void BuildUi()
        {
            _canvas = UiFactory.CreateScreenCanvas("MenuCanvas");

            var root = UiFactory.Panel(_canvas.transform, UiTheme.Background, "Root");
            UiFactory.Stretch(root.rectTransform);
            UiFactory.VLayout(root.gameObject, pad: 40, spacing: 14, expandW: false, expandH: false,
                align: TextAnchor.MiddleCenter);
            Tween.FadeIn(root.gameObject.AddComponent<CanvasGroup>(), 0.3f);

            UiFactory.Label(root.transform, "FITZMARK", 64, UiTheme.AccentStrong,
                TextAnchor.MiddleCenter, FontStyle.Bold, "Title");
            UiFactory.Label(root.transform, "BDR Life — Sales RPG", 22, UiTheme.TextPrimary,
                TextAnchor.MiddleCenter, FontStyle.Normal, "Subtitle");

            var gm = GameManager.Instance;
            if (gm.HasProfile)
            {
                var c = gm.Profile;
                UiFactory.Label(root.transform,
                    $"{c.DisplayName} — {ProgressionSystem.RankTitle(c.level)} · Level {c.level}",
                    16, UiTheme.TextMuted, TextAnchor.MiddleCenter, FontStyle.Italic, "Who");

                var cont = UiFactory.Button(root.transform, "▶  Continue",
                    () => GameManager.Instance.GoToTexas(), UiTheme.Positive, Color.white, 24,
                    TextAnchor.MiddleCenter);
                UiFactory.Size(cont.gameObject, prefW: 360f, prefH: 72f);

                var newBdr = UiFactory.Button(root.transform, "New BDR",
                    () => GameManager.Instance.GoToCharacterCreate(), UiTheme.Panel, UiTheme.TextPrimary,
                    18, TextAnchor.MiddleCenter);
                UiFactory.Size(newBdr.gameObject, prefW: 360f, prefH: 50f);
            }
            else
            {
                UiFactory.Label(root.transform,
                    "Create your Business Development Rep, then build their career one call at a time.",
                    16, UiTheme.TextMuted, TextAnchor.MiddleCenter, FontStyle.Italic, "Tagline");

                var create = UiFactory.Button(root.transform, "▶  Create Your BDR",
                    () => GameManager.Instance.GoToCharacterCreate(), UiTheme.Positive, Color.white, 24,
                    TextAnchor.MiddleCenter);
                UiFactory.Size(create.gameObject, prefW: 360f, prefH: 72f);
            }

            var settings = UiFactory.Button(root.transform, "Settings",
                () => new SettingsView(_canvas.transform, null).Open(), UiTheme.PanelDark,
                UiTheme.TextMuted, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(settings.gameObject, prefW: 360f, prefH: 50f);

            var quit = UiFactory.Button(root.transform, "Quit", QuitApp,
                UiTheme.PanelDark, UiTheme.TextMuted, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(quit.gameObject, prefW: 360f, prefH: 50f);
        }

        private static void QuitApp()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
