using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// Builds the main menu at runtime: a branded header and a scenario picker
    /// driven by <see cref="ScenarioCatalog"/>. Selecting a scenario hands off to
    /// <see cref="GameManager"/>, which loads the call floor.
    /// Attach this to a GameObject in the MainMenu scene (the setup tool does this).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private void Start()
        {
            ScenarioCatalog.Invalidate(); // pick up any newly authored scenarios
            BuildUi();
        }

        private void BuildUi()
        {
            var canvas = UiFactory.CreateScreenCanvas("MenuCanvas");

            var root = UiFactory.Panel(canvas.transform, UiTheme.Background, "Root");
            UiFactory.Stretch(root.rectTransform);
            UiFactory.VLayout(root.gameObject, pad: 32, spacing: 18, expandH: false,
                align: TextAnchor.UpperCenter);

            // Header
            UiFactory.Label(root.transform, "FITZMARK", 56, UiTheme.AccentStrong,
                TextAnchor.MiddleCenter, FontStyle.Bold, "Title");
            UiFactory.Label(root.transform, "BDR Call Simulator", 26, UiTheme.TextPrimary,
                TextAnchor.MiddleCenter, FontStyle.Normal, "Subtitle");
            UiFactory.Label(root.transform,
                "Business Development rep training — prospect, pitch, handle objections, " +
                "negotiate a rate, and close the load.", 16, UiTheme.TextMuted,
                TextAnchor.MiddleCenter, FontStyle.Italic, "Tagline");

            UiFactory.Label(root.transform, "Choose a scenario", 20, UiTheme.TextPrimary,
                TextAnchor.MiddleLeft, FontStyle.Bold, "PickHeader");

            // Scenario list
            var listGo = UiFactory.Panel(root.transform, UiTheme.Background, "List").gameObject;
            UiFactory.VLayout(listGo, pad: 0, spacing: 10);
            UiFactory.Size(listGo, flexH: 1f);

            var scenarios = ScenarioCatalog.All;
            if (scenarios.Count == 0)
            {
                UiFactory.Label(listGo.transform,
                    "No scenarios found.\n\nIn the Unity Editor, run\n" +
                    "Tools → Fitzmark BDR → Setup Project (One-Click)\n" +
                    "to generate the sample Fitzmark scenarios and scenes.",
                    18, UiTheme.Warning, TextAnchor.MiddleCenter, FontStyle.Bold, "Empty");
            }
            else
            {
                foreach (var scenario in scenarios)
                    BuildScenarioRow(listGo.transform, scenario);
            }

            // Footer
            var footer = UiFactory.Panel(root.transform, UiTheme.Background, "Footer").gameObject;
            UiFactory.HLayout(footer, spacing: 12, expandW: true);
            UiFactory.Size(footer, prefH: 48f);

            var quit = UiFactory.Button(footer.transform, "Quit", QuitApp,
                UiTheme.PanelDark, UiTheme.TextMuted, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(quit.gameObject, prefW: 160f);
        }

        private void BuildScenarioRow(Transform parent, ScenarioDefinition scenario)
        {
            var rowBtn = UiFactory.Button(parent, string.Empty,
                () => GameManager.Instance.StartScenario(scenario),
                UiTheme.Panel, UiTheme.TextPrimary);
            UiFactory.Size(rowBtn.gameObject, prefH: 84f, flexW: 1f);

            // Replace the empty default label with a richer two-line layout.
            var content = rowBtn.transform.GetChild(0).gameObject; // the "Text" child
            var txt = content.GetComponent<Text>();
            if (txt != null)
            {
                string company = scenario.prospect != null ? scenario.prospect.companyName : "Unknown";
                string contact = scenario.prospect != null ? scenario.prospect.DisplayHeadline : "";
                txt.text =
                    $"<b>{scenario.title}</b>   <color={DifficultyHex(scenario.difficulty)}>" +
                    $"[{scenario.difficulty}]</color>\n<size=14>{contact}</size>";
                txt.alignment = TextAnchor.MiddleLeft;
            }
        }

        private static string DifficultyHex(DifficultyTier tier) => tier switch
        {
            DifficultyTier.Easy => "#5FBF7F",
            DifficultyTier.Medium => "#EAA833",
            DifficultyTier.Hard => "#D94C4C",
            _ => "#FFFFFF"
        };

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
