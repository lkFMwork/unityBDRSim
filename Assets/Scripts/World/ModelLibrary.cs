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

        /// <summary>True if a real model exists for this key (mapped or direct path).</summary>
        public static bool Has(string key)
        {
            if (ModelCatalog.TryResolve(key, out var e)) return Resources.Load<GameObject>(e.Path) != null;
            return Resources.Load<GameObject>(Root + key) != null;
        }

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
            Color? placeholderColor = null, bool placeholderLabel = true, Vector3? fitSize = null)
        {
            GameObject prefab = null;
            float kitScale = scale;
            if (ModelCatalog.TryResolve(key, out var entry))
            {
                prefab = Resources.Load<GameObject>(entry.Path);
                kitScale = entry.Scale;
            }
            prefab ??= Resources.Load<GameObject>(Root + key);

            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, parent);
                go.transform.localScale = Vector3.one * kitScale;
                if (fitSize.HasValue) FitToSize(go, fitSize.Value);
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

        private static bool TryWorldBounds(GameObject go, out Bounds b)
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
