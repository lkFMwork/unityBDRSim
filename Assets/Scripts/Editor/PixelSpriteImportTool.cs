using UnityEditor;
using UnityEngine;

namespace Fitzmark.BDRSim.Editor
{
    /// <summary>
    /// Reimports the Kenney Pixel Platformer PNGs as crisp 2D Sprites. Standalone PNGs default
    /// to textureType Default (Texture2D) with bilinear filtering, so Resources.Load&lt;Sprite&gt;
    /// returns null (the game falls back to placeholder squares) and the pixels look blurry.
    /// This sets every tile in the pack to Sprite (Single), Point filter, no compression, and
    /// the correct pixels-per-unit — so the 2D platformer shows the real pixel art.
    /// Run: Tools → Fitzmark BDR → Import Pixel Platformer Sprites.
    /// </summary>
    public static class PixelSpriteImportTool
    {
        private const string PackFolder = "Assets/Resources/Models/kenney_pixel-platformer";

        [MenuItem("Tools/Fitzmark BDR/Import Pixel Platformer Sprites")]
        private static void Import()
        {
            if (!AssetDatabase.IsValidFolder(PackFolder))
            {
                EditorUtility.DisplayDialog("Fitzmark Pixel Sprites",
                    $"Couldn't find the pack at:\n{PackFolder}\n\nImport the Kenney Pixel Platformer pack there first.",
                    "OK");
                return;
            }

            int done = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { PackFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;

                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 18f;          // Kenney tiles are 18px (24px chars scale to fit in code)
                ti.filterMode = FilterMode.Point;       // crisp pixels
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
                done++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Fitzmark BDR] Imported {done} pixel-platformer textures as crisp Sprites.");
            EditorUtility.DisplayDialog("Fitzmark Pixel Sprites",
                $"Done — {done} tiles set to Sprite (Point filter).\n\n" +
                "Press Play and travel to a city: the platformer now shows the real Kenney pixel art.",
                "Great");
        }
    }
}
