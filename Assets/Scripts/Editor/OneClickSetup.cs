using UnityEditor;
using UnityEngine;

namespace Fitzmark.BDRSim.Editor
{
    /// <summary>
    /// The single one-click setup for a fresh checkout. Runs everything needed to play:
    ///   1) creates/wires every scene and the Build Settings list (ProjectSetupTool),
    ///   2) imports every sprite pack as crisp tiles (platformer, Kenney map, LPC overworld).
    /// Pinned to the top of Tools → Fitzmark BDR. The granular items remain below for re-runs.
    /// </summary>
    public static class OneClickSetup
    {
        [MenuItem("Tools/Fitzmark BDR/⭐ One-Click Setup (do everything)", false, 0)]
        public static void Run()
        {
            if (!EditorUtility.DisplayDialog("Fitzmark BDR — One-Click Setup",
                    "This will:\n" +
                    "  • create & wire all scenes + Build Settings\n" +
                    "  • import all sprite packs as crisp tiles\n\n" +
                    "Safe to run anytime (it's idempotent). Continue?",
                    "Set everything up", "Cancel"))
                return;

            int sprites = 0;
            try
            {
                EditorUtility.DisplayProgressBar("One-Click Setup", "Creating scenes + Build Settings…", 0.2f);
                ProjectSetupTool.SetUpProject(silent: true);

                EditorUtility.DisplayProgressBar("One-Click Setup", "Importing sprite packs…", 0.65f);
                sprites = PixelSpriteImportTool.ImportAllSprites();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Fitzmark BDR — Ready",
                $"Setup complete:\n\n" +
                $"  • Scenes + Build Settings configured\n" +
                $"  • {sprites} sprite tiles imported\n\n" +
                "Open the MainMenu scene and press Play.", "Let's go");
            Debug.Log($"[Fitzmark BDR] One-Click Setup done — {sprites} sprites imported.");
        }
    }
}
