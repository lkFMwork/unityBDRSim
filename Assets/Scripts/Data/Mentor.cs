using System.Collections.Generic;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>A senior rep in the break room who dispenses (genuinely useful) coaching.</summary>
    public class Mentor
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Title;
        public readonly string[] Tips;

        public Mentor(string id, string name, string title, string[] tips)
        {
            Id = id;
            Name = name;
            Title = title;
            Tips = tips;
        }
    }

    public static class MentorLibrary
    {
        public static readonly List<Mentor> All = new()
        {
            new Mentor("marcus", "Marcus Vance", "VP of Sales", new[]
            {
                "Activity solves everything. The rep who dials more wins more — protect your call blocks.",
                "A 'no' today is a 'not yet.' Log it, set a follow-up, and move on — don't let it live in your head.",
                "Your first ten seconds set the tone. Sit up and smile before you dial; they can hear it."
            }),
            new Mentor("renee", "Renee Diaz", "Top Account Executive", new[]
            {
                "Diagnose before you prescribe. If you're pitching before you understand their lanes, you've lost.",
                "The best close is a clear next step. Never end a call without a date on the calendar.",
                "Sell the problem you solve, not the features — 'we cover your tough lanes' beats 'we have 40,000 carriers.'"
            }),
            new Mentor("hank", "Hank Owens", "Carrier Ops Veteran", new[]
            {
                "Know the freight. When you can talk reefer temps and detention, gatekeepers stop screening you.",
                "Objections are buying signals in disguise. 'Too expensive' really means 'show me the value.'",
                "Never bad-mouth their current broker. Find the gap they're not covering — and quietly own it."
            }),
        };

        public static int Count => All.Count;

        public static Mentor Get(int index)
        {
            if (All.Count == 0) return null;
            if (index < 0) index = 0;
            if (index >= All.Count) index = All.Count - 1;
            return All[index];
        }
    }
}
