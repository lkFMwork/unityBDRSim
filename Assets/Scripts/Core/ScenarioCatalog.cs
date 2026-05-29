using System.Collections.Generic;
using Fitzmark.BDRSim.Data;
using UnityEngine;

namespace Fitzmark.BDRSim.Core
{
    /// <summary>
    /// Discovers the available training scenarios at runtime by loading every
    /// <see cref="ScenarioDefinition"/> under <c>Assets/Resources/Scenarios</c>.
    /// Using a Resources folder keeps scenario content decoupled from scenes —
    /// drop in a new asset and it appears in the menu, no wiring required.
    /// </summary>
    public static class ScenarioCatalog
    {
        public const string ResourcesPath = "Scenarios";

        private static List<ScenarioDefinition> _cache;

        public static IReadOnlyList<ScenarioDefinition> All
        {
            get
            {
                if (_cache != null) return _cache;

                var loaded = Resources.LoadAll<ScenarioDefinition>(ResourcesPath);
                _cache = new List<ScenarioDefinition>(loaded);
                _cache.Sort((a, b) =>
                {
                    int byDifficulty = a.difficulty.CompareTo(b.difficulty);
                    return byDifficulty != 0
                        ? byDifficulty
                        : string.CompareOrdinal(a.title, b.title);
                });
                return _cache;
            }
        }

        public static ScenarioDefinition First => All.Count > 0 ? All[0] : null;

        /// <summary>Forget the cached list (e.g. after authoring new scenarios in the editor).</summary>
        public static void Invalidate() => _cache = null;
    }
}
