using System.Collections.Generic;
using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Loads the imported Mixamo character and its animation clips by friendly name.
    /// The clips live in sibling "@"-suffixed FBX files (e.g. Ch33_nonPBR@Typing.fbx);
    /// each holds a single humanoid take. Configure those files once via
    /// Tools → Fitzmark BDR → Configure Mixamo Animations so they retarget onto the
    /// character avatar, then play them at runtime through <see cref="OfficeWorker"/>.
    /// </summary>
    public static class MixamoLibrary
    {
        private const string CharacterPath = "Models/people/Ch33_nonPBR";
        private const string ClipPrefix = "Models/people/Ch33_nonPBR@";

        // Friendly name -> the "@"-file holding that take.
        private static readonly Dictionary<string, string> ClipFiles = new()
        {
            { "typing", ClipPrefix + "Typing" },
            { "talking", ClipPrefix + "Talking" },
            { "phone", ClipPrefix + "Talking On Phone" },
            { "talkingSeated", ClipPrefix + "TalkingWhileSitting" },
            { "sitting", ClipPrefix + "Sitting" },
            { "walking", ClipPrefix + "Walking" },
            { "sitToStand", ClipPrefix + "Sit To Stand" },
            { "standToSit", ClipPrefix + "Stand To Sit" },
            { "sitToType", ClipPrefix + "Sit To Type" },
            { "typeToSit", ClipPrefix + "Type To Sit" },
        };

        private static readonly Dictionary<string, AnimationClip> _cache = new();

        public static bool Available => Resources.Load<GameObject>(CharacterPath) != null;

        public static GameObject LoadCharacter() => Resources.Load<GameObject>(CharacterPath);

        /// <summary>Load a clip by friendly name (e.g. "typing"), or null if missing.</summary>
        public static AnimationClip LoadClip(string friendlyName)
        {
            if (_cache.TryGetValue(friendlyName, out var cached)) return cached;
            if (!ClipFiles.TryGetValue(friendlyName, out var path)) return null;

            AnimationClip best = null;
            foreach (var obj in Resources.LoadAll<AnimationClip>(path))
            {
                if (obj == null || obj.name.StartsWith("__preview")) continue;
                best = obj; // one real take per file
                break;
            }
            _cache[friendlyName] = best;
            return best;
        }
    }
}
