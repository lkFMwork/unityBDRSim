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
            float fitFootprint = 0f, float fitHeight = 0f)
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
                else if (fitFootprint > 0f) FitByAxis(go, fitFootprint, axisY: false);
                else if (fitSize.HasValue) FitToSize(go, fitSize.Value);
                EnsureTextured(go, path);
                GroundOn(go, parent, localPos);
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
        /// </summary>
        private static void FitByAxis(GameObject go, float target, bool axisY)
        {
            if (!TryWorldBounds(go, out var b) || b.size == Vector3.zero) return;
            float current = axisY ? b.size.y : Mathf.Max(b.size.x, b.size.z);
            if (current <= 0.0001f) return;
            go.transform.localScale *= target / current;
        }

        /// <summary>
        /// Kenney kits texture everything from one shared colormap atlas next to the FBX
        /// (FBX format/Textures/colormap.png). On import a model's material can lose that
        /// link and render flat/white; if a renderer's material has no base texture, assign
        /// the kit's colormap so buildings/props show their colors like the car does.
        /// </summary>
        private static void EnsureTextured(GameObject go, string modelResourcePath)
        {
            var tex = LoadColormap(modelResourcePath);
            if (tex == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") == null)
                        m.SetTexture("_BaseMap", tex);
                    if (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") == null)
                        m.SetTexture("_MainTex", tex);
                }
            }
        }

        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> _colormaps = new();

        // The colormap sits at "<kit>/Models/FBX format/Textures/colormap" relative to the model.
        private static Texture2D LoadColormap(string modelResourcePath)
        {
            int fbxIdx = modelResourcePath.IndexOf("/FBX format/", System.StringComparison.Ordinal);
            if (fbxIdx < 0) return null;
            string baseDir = modelResourcePath.Substring(0, fbxIdx) + "/FBX format/Textures/colormap";
            if (_colormaps.TryGetValue(baseDir, out var cached)) return cached;
            var tex = Resources.Load<Texture2D>(baseDir);
            _colormaps[baseDir] = tex;
            return tex;
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

        public static bool TryWorldBounds(GameObject go, out Bounds b)
        {
            b = default;
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return false;
            b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return true;
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
