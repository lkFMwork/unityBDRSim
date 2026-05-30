using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fitzmark.BDRSim.Editor
{
    /// <summary>
    /// One-click configuration for the imported Mixamo character + its animation files.
    /// Sets the base character to a Humanoid rig (creating its avatar), then points every
    /// sibling "Ch33_nonPBR@*.fbx" animation file at that avatar (Humanoid, Copy From Other),
    /// loops the looping idle takes, and reimports. This replaces doing it by hand for all
    /// eleven files. Run: Tools → Fitzmark BDR → Configure Mixamo Animations.
    /// </summary>
    public static class MixamoSetupTool
    {
        private const string Folder = "Assets/Resources/Models/people";
        private const string CharacterFile = "Ch33_nonPBR.fbx";

        [MenuItem("Tools/Fitzmark BDR/Configure Mixamo Animations")]
        private static void Configure()
        {
            string charPath = Path.Combine(Folder, CharacterFile).Replace('\\', '/');
            var charImporter = AssetImporter.GetAtPath(charPath) as ModelImporter;
            if (charImporter == null)
            {
                EditorUtility.DisplayDialog("Fitzmark Mixamo Setup",
                    $"Couldn't find the character at:\n{charPath}\n\nMake sure Ch33_nonPBR.fbx is imported there.",
                    "OK");
                return;
            }

            // 1) Base character: Humanoid, create its own avatar from the model.
            charImporter.animationType = ModelImporterAnimationType.Human;
            charImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            charImporter.SaveAndReimport();

            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(charPath);
            if (avatar == null)
            {
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(charPath))
                    if (obj is Avatar a) { avatar = a; break; }
            }
            if (avatar == null || !avatar.isValid)
            {
                EditorUtility.DisplayDialog("Fitzmark Mixamo Setup",
                    "The character's Humanoid avatar couldn't be created/validated. " +
                    "Open the character's Rig tab and confirm Animation Type = Humanoid, then re-run.",
                    "OK");
                return;
            }

            // 2) Every animation file: Humanoid, copy from the character avatar, loop.
            int configured = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { Folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileName(path);
                if (!file.StartsWith("Ch33_nonPBR@")) continue; // only the @ animation files
                if (!(AssetImporter.GetAtPath(path) is ModelImporter anim)) continue;

                anim.animationType = ModelImporterAnimationType.Human;
                anim.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                anim.sourceAvatar = avatar;

                var clips = anim.defaultClipAnimations;
                if (clips != null && clips.Length > 0)
                {
                    for (int i = 0; i < clips.Length; i++)
                        clips[i].loopTime = ShouldLoop(file);
                    anim.clipAnimations = clips;
                }

                anim.SaveAndReimport();
                configured++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Fitzmark BDR] Mixamo setup complete: avatar created + {configured} animation file(s) configured.");
            EditorUtility.DisplayDialog("Fitzmark Mixamo Setup",
                $"Done!\n\n• Character set to Humanoid (avatar created)\n• {configured} animation files retargeted to it\n\n" +
                "Press Play and enter the Office to see animated workers.",
                "Great");
        }

        // Continuous actions loop; one-shot transitions (Sit To Stand, etc.) do not.
        private static bool ShouldLoop(string file)
        {
            string f = file.ToLowerInvariant();
            if (f.Contains(" to ")) return false;       // "Sit To Stand", "Type To Sit", …
            return true;                                 // Typing, Talking, Sitting, Walking, Phone
        }
    }
}
