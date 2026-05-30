using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Fitzmark.BDRSim.Editor
{
    /// <summary>
    /// Dumps the full structure of a selected model to a text file so it can be shared:
    /// transform hierarchy + components, skinned meshes and their bones, animation clips,
    /// materials/shaders, import settings, and overall size. This is how the assistant
    /// "sees" a rig it can't render — select a model (FBX/prefab) in the Project window
    /// (or a GameObject in the scene), run the menu item, then paste the contents of
    /// Assets/model-report.txt back into the chat.
    /// </summary>
    public static class ModelReportTool
    {
        [MenuItem("Tools/Fitzmark BDR/Report Selected Model")]
        private static void Report()
        {
            var obj = Selection.activeObject;
            string path = obj != null ? AssetDatabase.GetAssetPath(obj) : null;
            GameObject root = obj as GameObject;
            if (root == null && !string.IsNullOrEmpty(path))
                root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) root = Selection.activeGameObject;

            if (root == null)
            {
                EditorUtility.DisplayDialog("Fitzmark Model Report",
                    "Select a model (FBX/prefab) in the Project window, or a GameObject in the scene, then run again.",
                    "OK");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== FITZMARK MODEL REPORT ===");
            sb.AppendLine("name: " + root.name);
            if (!string.IsNullOrEmpty(path)) sb.AppendLine("asset: " + path);

            if (!string.IsNullOrEmpty(path) &&
                AssetImporter.GetAtPath(path) is ModelImporter importer)
            {
                sb.AppendLine($"import: scaleFactor={importer.globalScale}, rig={importer.animationType}, " +
                              $"materials={importer.materialImportMode}, optimizeMesh={importer.optimizeMeshPolygons}");
            }

            var rends = root.GetComponentsInChildren<Renderer>(true);
            if (rends.Length > 0)
            {
                var b = rends[0].bounds;
                foreach (var r in rends) b.Encapsulate(r.bounds);
                sb.AppendLine($"world bounds size: {b.size}  (~{b.size.y:0.00} tall, {b.size.x:0.00} wide as imported)");
            }

            if (!string.IsNullOrEmpty(path))
            {
                var clips = new List<string>();
                foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (a is AnimationClip clip && !clip.name.StartsWith("__preview"))
                        clips.Add($"  - {clip.name}  ({clip.length:0.00}s, {(clip.isLooping ? "loop" : "once")})");
                sb.AppendLine($"animation clips: {clips.Count}");
                foreach (var c in clips) sb.AppendLine(c);
            }

            var skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            sb.AppendLine($"skinned meshes: {skins.Length}");
            foreach (var s in skins)
            {
                int tris = s.sharedMesh != null ? s.sharedMesh.triangles.Length / 3 : 0;
                sb.AppendLine($"  mesh '{(s.sharedMesh != null ? s.sharedMesh.name : "?")}': {tris} tris, {s.bones.Length} bones");
                int n = 0;
                foreach (var bone in s.bones)
                {
                    if (bone == null) continue;
                    sb.AppendLine("    bone: " + bone.name);
                    if (++n >= 70) { sb.AppendLine("    ...(more bones truncated)"); break; }
                }
            }

            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length > 0)
            {
                int tris = 0;
                foreach (var f in filters) if (f.sharedMesh != null) tris += f.sharedMesh.triangles.Length / 3;
                sb.AppendLine($"static meshes: {filters.Length}  ({tris} tris total)");
            }

            var mats = new HashSet<string>();
            foreach (var r in rends)
                foreach (var m in r.sharedMaterials)
                    if (m != null) mats.Add($"{m.name}  (shader: {(m.shader != null ? m.shader.name : "?")})");
            sb.AppendLine($"materials: {mats.Count}");
            foreach (var m in mats) sb.AppendLine("  - " + m);

            sb.AppendLine("hierarchy:");
            DumpHierarchy(root.transform, 1, sb);

            const string outPath = "Assets/model-report.txt";
            File.WriteAllText(outPath, sb.ToString());
            AssetDatabase.Refresh();
            Debug.Log("[Fitzmark BDR] Wrote " + outPath + "\n\n" + sb);
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(outPath));
        }

        private static void DumpHierarchy(Transform t, int depth, StringBuilder sb)
        {
            if (depth > 7) return;
            sb.Append(new string(' ', depth * 2)).Append(t.name);
            var comps = t.GetComponents<Component>();
            bool any = false;
            foreach (var c in comps)
            {
                if (c == null || c is Transform) continue;
                sb.Append(any ? ", " : "  [").Append(c.GetType().Name);
                any = true;
            }
            if (any) sb.Append("]");
            sb.AppendLine();
            foreach (Transform child in t) DumpHierarchy(child, depth + 1, sb);
        }
    }
}
