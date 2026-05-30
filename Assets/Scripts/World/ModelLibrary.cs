using UnityEngine;
using Fitzmark.BDRSim.UI;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Loads 3D models from <c>Resources/Models</c> at runtime, with a graceful labeled
    /// placeholder when a model isn't present yet. Because our scenes are built in code,
    /// this is how real art enters the game: drop an FBX/prefab into
    /// <c>Assets/Resources/Models/&lt;key&gt;</c> and every <c>Spawn("&lt;key&gt;")</c> call
    /// lights up with it — no code change. Until then you get a tagged stand-in box, so
    /// the office is walkable and laid-out today and upgrades model-by-model.
    /// </summary>
    public static class ModelLibrary
    {
        public const string Root = "Models/";

        /// <summary>True if a real model exists for this key (vs. a placeholder).</summary>
        public static bool Has(string key) => Resources.Load<GameObject>(Root + key) != null;

        /// <summary>
        /// Instantiate <c>Resources/Models/&lt;key&gt;</c> under <paramref name="parent"/>,
        /// or a labeled placeholder box of <paramref name="placeholderSize"/> if it's missing.
        /// Returns the spawned root so callers can add colliders/Interactables/Animators.
        /// </summary>
        public static GameObject Spawn(string key, Transform parent, Vector3 localPos,
            float yaw = 0f, float scale = 1f, Vector3? placeholderSize = null,
            Color? placeholderColor = null, bool placeholderLabel = true)
        {
            var prefab = Resources.Load<GameObject>(Root + key);
            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, parent);
                go.transform.localScale = Vector3.one * scale;
            }
            else
            {
                go = BuildPlaceholder(key, parent, placeholderSize ?? Vector3.one,
                    placeholderColor ?? new Color(0.50f, 0.55f, 0.65f), placeholderLabel);
            }
            go.name = key;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return go;
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

            // A floating label so each stand-in reads clearly in screenshots.
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
