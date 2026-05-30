using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// A real animated office worker: instantiates the imported Mixamo character and plays
    /// a single looping humanoid clip (typing, talking, sitting, …) via the Playables API,
    /// so no Animator Controller asset is required. Use <see cref="Spawn"/> to drop one in.
    /// </summary>
    public class OfficeWorker : MonoBehaviour
    {
        private PlayableGraph _graph;
        private string _clipName;

        /// <summary>
        /// Spawn a worker at <paramref name="position"/> facing <paramref name="yaw"/>,
        /// looping the named clip ("typing", "talking", "sitting", "phone", "walking", …).
        /// Returns null if the character model isn't imported yet (caller can fall back).
        /// </summary>
        public static OfficeWorker Spawn(Transform parent, Vector3 position, float yaw, string clipName)
        {
            var prefab = MixamoLibrary.LoadCharacter();
            if (prefab == null) return null;

            var go = Object.Instantiate(prefab, parent);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var worker = go.AddComponent<OfficeWorker>();
            worker.Play(clipName);
            return worker;
        }

        /// <summary>Switch the looping clip (e.g. seat a standing worker).</summary>
        public void Play(string clipName)
        {
            var animator = GetComponent<Animator>();
            if (animator == null) animator = gameObject.AddComponent<Animator>();

            var clip = MixamoLibrary.LoadClip(clipName);
            if (clip == null) return;
            _clipName = clipName;

            if (_graph.IsValid()) _graph.Destroy();
            _graph = PlayableGraph.Create("Worker_" + clipName);
            var output = AnimationPlayableOutput.Create(_graph, "Anim", animator);
            var clipPlayable = AnimationClipPlayable.Create(_graph, clip);
            clipPlayable.SetApplyFootIK(false);
            // Mixamo single takes loop cleanly for idle-style actions.
            output.SetSourcePlayable(clipPlayable);
            _graph.Play();
        }

        public string CurrentClip => _clipName;

        private void OnDestroy()
        {
            if (_graph.IsValid()) _graph.Destroy();
        }
    }
}
