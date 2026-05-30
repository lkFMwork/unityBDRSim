using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fitzmark.BDRSim.Editor
{
    /// <summary>
    /// Fixes the "solid white character" problem: Mixamo's FBX-for-Unity embeds its
    /// textures inside the .fbx, so URP has no base map to draw and renders the model
    /// white. This extracts the embedded textures (and materials) into asset files next
    /// to the character and reimports, so the suit/skin/hair textures appear.
    /// Run: Tools → Fitzmark BDR → Fix Character Textures.
    /// </summary>
    public static class TextureExtractTool
    {
        private const string PeopleFolder = "Assets/Resources/Models/people";
        private const string CharacterFbx = PeopleFolder + "/Ch33_nonPBR.fbx";

        [MenuItem("Tools/Fitzmark BDR/Fix Character Textures")]
        private static void Fix()
        {
            if (!(AssetImporter.GetAtPath(CharacterFbx) is ModelImporter importer))
            {
                EditorUtility.DisplayDialog("Fitzmark Texture Fix",
                    $"Couldn't find the character at:\n{CharacterFbx}", "OK");
                return;
            }

            string texFolder = PeopleFolder + "/Textures";
            if (!AssetDatabase.IsValidFolder(texFolder))
                AssetDatabase.CreateFolder(PeopleFolder, "Textures");
            string matFolder = PeopleFolder + "/Materials";
            if (!AssetDatabase.IsValidFolder(matFolder))
                AssetDatabase.CreateFolder(PeopleFolder, "Materials");

            // 1) Pull the embedded textures out into real assets (remaps the materials to them).
            importer.ExtractTextures(texFolder);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(CharacterFbx, ImportAssetOptions.ForceUpdate);

            // 2) Extract the materials so they become editable assets referencing those textures.
            int mats = 0;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(CharacterFbx))
            {
                if (!(obj is Material m)) continue;
                string dest = AssetDatabase.GenerateUniqueAssetPath($"{matFolder}/{m.name}.mat");
                if (string.IsNullOrEmpty(AssetDatabase.ExtractAsset(m, dest))) mats++;
            }
            AssetDatabase.WriteImportSettingsIfDirty(CharacterFbx);
            AssetDatabase.ImportAsset(CharacterFbx, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            int texCount = Directory.Exists(texFolder)
                ? Directory.GetFiles(texFolder).Count(f =>
                    f.EndsWith(".png") || f.EndsWith(".jpg") || f.EndsWith(".jpeg") ||
                    f.EndsWith(".tga") || f.EndsWith(".psd"))
                : 0;

            Debug.Log($"[Fitzmark BDR] Texture fix: extracted {texCount} texture(s), {mats} material(s).");
            EditorUtility.DisplayDialog("Fitzmark Texture Fix",
                texCount > 0
                    ? $"Extracted {texCount} textures + {mats} materials.\n\n" +
                      "Press Play — the character should now show its suit/skin/hair."
                    : "No embedded textures were found in the FBX.\n\n" +
                      "Re-download the character from Mixamo as 'FBX for Unity' (textures embedded), " +
                      "re-import, and run this again.",
                "OK");
        }
    }
}
