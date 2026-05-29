using System.Collections.Generic;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>A graded, human-readable summary of a finished call.</summary>
    public class CallReport
    {
        public string LetterGrade;
        public float OverallPercent;
        public CallOutcome Outcome;
        public string Headline;
        public string OutcomeNote;
        public readonly Dictionary<ScoreCategory, float> CategoryPercents = new();
        public readonly List<string> Strengths = new();
        public readonly List<string> Improvements = new();
    }

    /// <summary>
    /// Converts a finished <see cref="CallSession"/> into a <see cref="CallReport"/>:
    /// a letter grade, a per-category breakdown, and targeted coaching pulled from
    /// the rep's weakest dimensions.
    /// </summary>
    public static class CallEvaluator
    {
        public static CallReport Evaluate(CallSession session)
        {
            var report = new CallReport
            {
                LetterGrade = session.Score.LetterGrade,
                OverallPercent = session.Score.OverallPercent,
                Outcome = session.Outcome,
                Headline = HeadlineFor(session.Outcome),
                OutcomeNote = NoteFor(session)
            };

            foreach (ScoreCategory cat in System.Enum.GetValues(typeof(ScoreCategory)))
            {
                float pct = session.Score.GetPercent(cat);
                report.CategoryPercents[cat] = pct;
                if (pct >= 0.8f)
                    report.Strengths.Add($"{DisplayName(cat)} — strong.");
            }

            // Coach the two weakest categories that fell short of competent (60%).
            int coached = 0;
            foreach (var cat in session.Score.WeakestFirst())
            {
                if (coached >= 2) break;
                if (session.Score.GetPercent(cat) >= 0.6f) break;
                report.Improvements.Add(CoachingFor(cat));
                coached++;
            }

            if (report.Strengths.Count == 0)
                report.Strengths.Add("Completed the call without losing the prospect early.");
            if (report.Improvements.Count == 0)
                report.Improvements.Add("Solid all around — tighten the close and protect more margin.");

            return report;
        }

        private static string HeadlineFor(CallOutcome outcome) => outcome switch
        {
            CallOutcome.WonCommitment => "Won a first load!",
            CallOutcome.WonTrial => "Earned a trial / next meeting.",
            CallOutcome.NoSaleFollowUp => "No sale today — door left open.",
            CallOutcome.Rejected => "Lost the opportunity.",
            CallOutcome.HungUp => "They hung up.",
            _ => "Call ended."
        };

        private static string NoteFor(CallSession session)
        {
            if (session.Prospect.DealAgreed && session.Lane != null)
            {
                float rate = session.Prospect.AgreedRatePerMile;
                float weekly = session.Lane.WeeklyMargin(rate);
                float target = session.Scenario != null ? session.Scenario.targetWeeklyMargin : 0f;
                string bar = target > 0f
                    ? (weekly >= target ? " (above target)" : " (below target)")
                    : string.Empty;
                return $"Booked {session.Lane.label} at ${rate:0.00}/mi " +
                       $"≈ ${weekly:0}/wk gross margin{bar}.";
            }
            return "No lane booked on this call.";
        }

        private static string DisplayName(ScoreCategory cat) => cat switch
        {
            ScoreCategory.Rapport => "Rapport & opening",
            ScoreCategory.Discovery => "Discovery",
            ScoreCategory.ValueArticulation => "Value articulation",
            ScoreCategory.ObjectionHandling => "Objection handling",
            ScoreCategory.Negotiation => "Negotiation",
            ScoreCategory.Close => "Closing",
            _ => cat.ToString()
        };

        private static string CoachingFor(ScoreCategory cat) => cat switch
        {
            ScoreCategory.Rapport =>
                "Open with a permission-based intro and earn the first 30 seconds before pitching.",
            ScoreCategory.Discovery =>
                "Ask about lanes, volume, and pain before you talk about Fitzmark. Diagnose, then prescribe.",
            ScoreCategory.ValueArticulation =>
                "Tie value to what they told you. Connect their pain to a specific Fitzmark capability.",
            ScoreCategory.ObjectionHandling =>
                "Acknowledge the objection, then reframe — never argue the prospect into a corner.",
            ScoreCategory.Negotiation =>
                "Lead with a rate that protects margin; defend it with value before you discount.",
            ScoreCategory.Close =>
                "Always ask for a concrete next step — a trial load or a booked follow-up.",
            _ => "Review this area before the next call."
        };
    }
}
