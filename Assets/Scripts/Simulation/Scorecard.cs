using System;
using System.Collections.Generic;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>
    /// Accumulates points per <see cref="ScoreCategory"/> over the course of a
    /// call and converts them to percentages and a letter grade. Pure C# so it
    /// can be exercised by edit-mode unit tests without entering play mode.
    /// </summary>
    public class Scorecard
    {
        /// <summary>Maximum points attainable in any single category.</summary>
        public const float MaxPerCategory = 20f;

        private readonly Dictionary<ScoreCategory, float> _points = new();

        public Scorecard()
        {
            foreach (ScoreCategory cat in Enum.GetValues(typeof(ScoreCategory)))
                _points[cat] = 0f;
        }

        /// <summary>Add (or subtract) points in a category, clamped to [0, MaxPerCategory].</summary>
        public void Add(ScoreCategory category, float amount)
        {
            float next = _points[category] + amount;
            if (next < 0f) next = 0f;
            if (next > MaxPerCategory) next = MaxPerCategory;
            _points[category] = next;
        }

        public float GetPoints(ScoreCategory category) => _points[category];

        /// <summary>0..1 fraction of the category maximum earned.</summary>
        public float GetPercent(ScoreCategory category) => _points[category] / MaxPerCategory;

        public float TotalPoints
        {
            get
            {
                float sum = 0f;
                foreach (var kvp in _points) sum += kvp.Value;
                return sum;
            }
        }

        public float MaxTotalPoints => MaxPerCategory * _points.Count;

        /// <summary>Overall score as a 0..1 fraction.</summary>
        public float OverallPercent => MaxTotalPoints > 0f ? TotalPoints / MaxTotalPoints : 0f;

        /// <summary>Letter grade derived from <see cref="OverallPercent"/>.</summary>
        public string LetterGrade => GradeFor(OverallPercent);

        public static string GradeFor(float percent)
        {
            if (percent >= 0.90f) return "A";
            if (percent >= 0.80f) return "B";
            if (percent >= 0.70f) return "C";
            if (percent >= 0.60f) return "D";
            return "F";
        }

        /// <summary>Returns categories ordered weakest-first (lowest percent earned).</summary>
        public IEnumerable<ScoreCategory> WeakestFirst()
        {
            var list = new List<ScoreCategory>(_points.Keys);
            list.Sort((a, b) => GetPercent(a).CompareTo(GetPercent(b)));
            return list;
        }
    }
}
