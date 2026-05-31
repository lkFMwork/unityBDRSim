using UnityEngine;
using Fitzmark.BDRSim.UI;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Loads 3D models from <c>Resources/Models</c> at runtime, resolving friendly keys
    /// through <see cref="ModelCatalog"/> and auto-scaling each model to a sensible
    /// real-world size. Because our scenes are built in code, this is how real art enters
    /// the game: a <c>Spawn("desk", ...)</c> call finds the imported Kenney desk, scales it
    /// to the requested height, and drops it on the floor. If a key isn't mapped or the file
    /// is missing, you get a labeled placeholder box so the scene still lays out.
    /// </summary>
    public static class ModelLibrary
    {
        public const string Root = "Models/";

        // Resource path for a key: catalog mapping, a bare key (gets Models/ prefixed),
        // or a key that already includes the Models/ prefix (used as-is). This tolerance
        // matters because city paths are authored with the full Models/... path.
        private static string ResolvePath(string key, out float kitScale)
        {
            kitScale = 1f;
            if (ModelCatalog.TryResolve(key, out var e)) { kitScale = e.Scale; return e.Path; }
            return key.StartsWith(Root) ? key : Root + key;
        }

        /// <summary>True if a real model exists for this key (mapped or direct path).</summary>
        public static bool Has(string key) => Resources.Load<GameObject>(ResolvePath(key, out _)) != null;

        /// <summary>
        /// Spawn a model for <paramref name="key"/> under <paramref name="parent"/> at
        /// <paramref name="localPos"/>. <paramref name="fitSize"/>, when given, scales the
        /// model so its bounding box fits those metres (x,y,z; components ≤0 are ignored)
        /// and rests on the floor — this guarantees correct size regardless of the source
        /// FBX's native scale. Without it, the catalog's kit scale is used. Falls back to a
        /// labeled placeholder box when the model is absent.
        /// </summary>
        public static GameObject Spawn(string key, Transform parent, Vector3 localPos,
            float yaw = 0f, float scale = 1f, Vector3? placeholderSize = null,
            Color? placeholderColor = null, bool placeholderLabel = true, Vector3? fitSize = null,
            float fitFootprint = 0f, float fitHeight = 0f, float fitMaxHeight = 0f, bool ground = true)
        {
            string path = ResolvePath(key, out float kitScale);
            GameObject prefab = Resources.Load<GameObject>(path);
            if (scale != 1f && !ModelCatalog.TryResolve(key, out _)) kitScale = scale;

            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, parent);
                go.transform.localScale = Vector3.one * kitScale;
                if (fitHeight > 0f) FitByAxis(go, fitHeight, axisY: true);
                else if (fitFootprint > 0f) FitByAxis(go, fitFootprint, axisY: false, maxOther: fitMaxHeight);
                else if (fitSize.HasValue) FitToSize(go, fitSize.Value);
                EnsureTextured(go, path);
                if (ground) GroundOn(go, parent, localPos);
                else { go.transform.localPosition = localPos; SitBaseAtLocalZero(go); }
            }
            else
            {
                go = BuildPlaceholder(key, parent, placeholderSize ?? Vector3.one,
                    placeholderColor ?? new Color(0.50f, 0.55f, 0.65f), placeholderLabel);
                go.transform.localPosition = localPos;
            }
            go.name = key;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return go;
        }

        /// <summary>
        /// Scale uniformly so one dimension matches <paramref name="target"/> metres: the
        /// taller of the height (axisY) or the larger horizontal footprint. Uniform scaling
        /// keeps the model's natural proportions — the right fit for buildings and trees,
        /// where forcing a box (FitToSize) squashes tall shapes into tiny ones.
        /// <paramref name="maxOther"/> caps the perpendicular extent (e.g. footprint-fit a
        /// building but don't let it tower past a height cap).
        /// </summary>
        private static void FitByAxis(GameObject go, float target, bool axisY, float maxOther = 0f)
        {
            if (!TryWorldBounds(go, out var b) || b.size == Vector3.zero) return;
            float current = axisY ? b.size.y : Mathf.Max(b.size.x, b.size.z);
            if (current <= 0.0001f) return;
            float f = target / current;

            if (maxOther > 0f)
            {
                float other = (axisY ? Mathf.Max(b.size.x, b.size.z) : b.size.y) * f;
                if (other > maxOther) f *= maxOther / other; // clamp the perpendicular extent
            }
            go.transform.localScale *= f;
        }

        /// <summary>
        /// Kenney kits texture everything from one shared colormap atlas next to the FBX
        /// (FBX format/Textures/colormap.png). On import a model's material can come in flat
        /// (no usable base map) and render like a solid primitive. To guarantee colors, build
        /// one shared URP/Lit material per kit colormap and force it onto every renderer of a
        /// spawned model — the car already looks right, this makes the buildings match.
        /// </summary>
        private static void EnsureTextured(GameObject go, string modelResourcePath)
        {
            var mat = ColormapMaterial(modelResourcePath);
            if (mat == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                // One material per submesh, all the shared colormap material.
                int n = r.sharedMaterials.Length;
                if (n <= 1) { r.sharedMaterial = mat; continue; }
                var arr = new Material[n];
                for (int i = 0; i < n; i++) arr[i] = mat;
                r.sharedMaterials = arr;
            }
        }

        private static readonly System.Collections.Generic.Dictionary<string, Material> _colormapMats = new();

        // A shared URP/Lit material sampling "<kit>/Models/FBX format/Textures/colormap".
        private static Material ColormapMaterial(string modelResourcePath)
        {
            int fbxIdx = modelResourcePath.IndexOf("/FBX format/", System.StringComparison.Ordinal);
            if (fbxIdx < 0) return null;
            string texPath = modelResourcePath.Substring(0, fbxIdx) + "/FBX format/Textures/colormap";
            if (_colormapMats.TryGetValue(texPath, out var cached)) return cached;

            var tex = Resources.Load<Texture2D>(texPath);
            Material mat = null;
            if (tex != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader) { name = "Kenney_colormap", enableInstancing = true };
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
            }
            _colormapMats[texPath] = mat; // cache even null, so we only probe once
            return mat;
        }

        /// <summary>Scale a spawned model so its bounds fit within <paramref name="size"/> metres.</summary>
        private static void FitToSize(GameObject go, Vector3 size)
        {
            if (!TryWorldBounds(go, out var b) || b.size == Vector3.zero) return;
            float sx = size.x > 0f ? size.x / b.size.x : float.MaxValue;
            float sy = size.y > 0f ? size.y / b.size.y : float.MaxValue;
            float sz = size.z > 0f ? size.z / b.size.z : float.MaxValue;
            float f = Mathf.Min(sx, Mathf.Min(sy, sz));
            if (f > 0f && f != float.MaxValue) go.transform.localScale *= f;
        }

        /// <summary>Sit the model's base on the floor at <paramref name="localPos"/> (y is the floor).</summary>
        private static void GroundOn(GameObject go, Transform parent, Vector3 localPos)
        {
            go.transform.localPosition = localPos;
            if (!TryWorldBounds(go, out var b)) return;
            float baseY = parent != null ? parent.position.y + localPos.y : localPos.y;
            float lift = baseY - b.min.y;
            go.transform.position += new Vector3(0f, lift, 0f);
        }

        /// <summary>
        /// World-space bounds computed from each mesh's *local* bounds transformed by its
        /// renderer matrix. Unlike Renderer.bounds, mesh bounds are valid immediately after
        /// Instantiate (no wait for a render/transform sync), so auto-fit is deterministic —
        /// fixing the bug where a model occasionally measured zero, skipped its fit, and
        /// stayed at giant native size.
        /// </summary>
        public static bool TryWorldBounds(GameObject go, out Bounds b)
        {
            b = default;
            bool has = false;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                Accumulate(ref b, ref has, mf.sharedMesh.bounds, mf.transform.localToWorldMatrix);
            }
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (smr.sharedMesh == null) continue;
                Accumulate(ref b, ref has, smr.sharedMesh.bounds, smr.transform.localToWorldMatrix);
            }
            return has;
        }

        private static void Accumulate(ref Bounds b, ref bool has, Bounds local, Matrix4x4 m)
        {
            Vector3 c = local.center, e = local.extents;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = c + new Vector3(
                    (i & 1) == 0 ? -e.x : e.x,
                    (i & 2) == 0 ? -e.y : e.y,
                    (i & 4) == 0 ? -e.z : e.z);
                Vector3 w = m.MultiplyPoint3x4(corner);
                if (!has) { b = new Bounds(w, Vector3.zero); has = true; }
                else b.Encapsulate(w);
            }
        }

        /// <summary>Sit the model's base at its parent's local origin (feet at y=0 locally),
        /// for a character body parented to a controller root — no world re-grounding.</summary>
        private static void SitBaseAtLocalZero(GameObject go)
        {
            if (!TryWorldBounds(go, out var b)) return;
            float localBottom = go.transform.parent != null
                ? go.transform.parent.InverseTransformPoint(b.min).y
                : b.min.y;
            go.transform.localPosition -= new Vector3(0f, localBottom, 0f);
        }

        private static GameObject BuildPlaceholder(string key, Transform parent, Vector3 size, Color color, bool label)
        {
            var go = new GameObject(key);
            go.transform.SetParent(parent, false);

            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Placeholder";
            box.transform.SetParent(go.transform, false);
            box.transform.localScale = size;
            box.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            var col = box.GetComponent<Collider>();
            if (col != null) Object.Destroy(col); // don't trap the player on stand-ins
            var r = box.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = MaterialLibrary.Get(color);

            if (!label) return go;

            var lblGo = new GameObject("Label", typeof(MeshRenderer), typeof(TextMesh));
            lblGo.transform.SetParent(go.transform, false);
            lblGo.transform.localPosition = new Vector3(0f, size.y + 0.55f, 0f);
            var tm = lblGo.GetComponent<TextMesh>();
            tm.text = key;
            tm.anchor = TextAnchor.LowerCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 48;
            tm.characterSize = 0.16f;
            tm.color = Color.white;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                tm.font = font;
                lblGo.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
            lblGo.AddComponent<Billboard>();
            return go;
        }
    }

    /// <summary>Keeps a transform facing the camera (for floating placeholder labels).</summary>
    public class Billboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
