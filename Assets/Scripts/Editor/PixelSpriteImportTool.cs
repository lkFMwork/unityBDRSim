using UnityEditor;
using UnityEngine;

namespace Fitzmark.BDRSim.Editor
{
    /// <summary>
    /// Reimports Kenney sprite packs as crisp 2D Sprites. Standalone PNGs default to textureType
    /// Default (Texture2D), so Resources.Load&lt;Sprite&gt; returns null (the game falls back to
    /// placeholder squares) and pixels look blurry. This sets each tile to Sprite (Single), point
    /// filter, no compression, FullRect mesh. Covers the platformer pack and the overworld map pack.
    /// Run from Tools → Fitzmark BDR.
    /// </summary>
    public static class PixelSpriteImportTool
    {
        private const string PlatformerFolder = "Assets/Resources/Models/kenney_pixel-platformer";
        private const string MapFolder = "Assets/Resources/Models/kenney_map-pack/PNG";

        [MenuItem("Tools/Fitzmark BDR/Import Pixel Platformer Sprites")]
        private static void ImportPlatformer() => ImportFolder(PlatformerFolder, 18f, "platformer");

        [MenuItem("Tools/Fitzmark BDR/Import Overworld Map Tiles")]
        private static void ImportMap() => ImportFolder(MapFolder, 64f, "overworld map");

        [MenuItem("Tools/Fitzmark BDR/Import ALL Kenney Sprites")]
        private static void ImportAll()
        {
            ImportFolder(PlatformerFolder, 18f, "platformer", silent: true);
            ImportFolder(MapFolder, 64f, "overworld map", silent: true);
            EditorUtility.DisplayDialog("Fitzmark Sprites",
                "Imported the platformer and overworld map packs as crisp Sprites.\n\nPress Play.", "Great");
        }

        private static void ImportFolder(string folder, float ppu, string label, bool silent = false)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!silent)
                    EditorUtility.DisplayDialog("Fitzmark Sprites",
                        $"Couldn't find the pack at:\n{folder}\n\nImport it there first.", "OK");
                return;
            }

            int done = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;

                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = ppu;
                ti.filterMode = FilterMode.Point;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.wrapMode = TextureWrapMode.Clamp;

                var settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                ti.SetTextureSettings(settings);
                ti.SaveAndReimport();
                done++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Fitzmark BDR] Imported {done} {label} textures as crisp Sprites.");
            if (!silent)
                EditorUtility.DisplayDialog("Fitzmark Sprites",
                    $"Done — {done} {label} tiles set to Sprite (Point filter).\n\nPress Play.", "Great");
        }
    }
}
