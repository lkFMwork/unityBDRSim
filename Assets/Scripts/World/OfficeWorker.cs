using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// A real animated character built on the imported Mixamo model. Two modes:
    ///  • a single looping clip (typing, talking, phone, sitting) for stationary workers, via
    ///    <see cref="Spawn"/>;
    ///  • locomotion (idle ↔ walk) for the player and wandering NPCs, via <see cref="Attach"/>
    ///    + <see cref="SetWalk"/>, driven by the mover's own controller.
    /// Animation runs through the Playables API, so no Animator Controller asset is needed.
    /// </summary>
    public class OfficeWorker : MonoBehaviour
    {
        private Animator _animator;
        private PlayableGraph _graph;
        private string _clipName;

        // locomotion state
        private bool _locomotion;
        private bool _walking;
        private string _idleClip = "idle";

        /// <summary>Stationary worker looping one clip. Null if the model isn't imported.</summary>
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

        /// <summary>
        /// Attach the Mixamo character as a child body of <paramref name="parent"/> (at local
        /// origin) in locomotion mode, for a player/NPC whose controller moves the root and
        /// calls <see cref="SetWalk"/>. Returns null if the model isn't imported.
        /// </summary>
        public static OfficeWorker Attach(Transform parent, string idleClip = "idle")
        {
            var prefab = MixamoLibrary.LoadCharacter();
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            var worker = go.AddComponent<OfficeWorker>();
            worker._locomotion = true;
            worker._idleClip = idleClip;
            worker.Play(idleClip);
            return worker;
        }

        /// <summary>Locomotion: blend to walk above a small speed, else idle.</summary>
        public void SetWalk(float speed01)
        {
            if (!_locomotion) return;
            bool wantWalk = speed01 > 0.15f;
            if (wantWalk == _walking && _graph.IsValid()) return; // no per-frame rebuilds
            _walking = wantWalk;
            Play(wantWalk ? "walking" : _idleClip);
        }

        /// <summary>Play a single looping clip by friendly name.</summary>
        public void Play(string clipName)
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
                if (_animator == null) _animator = gameObject.AddComponent<Animator>();
                _animator.applyRootMotion = false; // we move the transform ourselves
            }

            var clip = MixamoLibrary.LoadClip(clipName) ?? MixamoLibrary.LoadClip("talking");
            if (clip == null) return;
            _clipName = clipName;

            if (_graph.IsValid()) _graph.Destroy();
            _graph = PlayableGraph.Create("Worker_" + clipName);
            var output = AnimationPlayableOutput.Create(_graph, "Anim", _animator);
            var clipPlayable = AnimationClipPlayable.Create(_graph, clip);
            clipPlayable.SetApplyFootIK(false);
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
