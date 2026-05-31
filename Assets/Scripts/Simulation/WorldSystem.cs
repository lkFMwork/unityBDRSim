using System.Collections.Generic;
using System.Linq;
using Fitzmark.BDRSim.Data;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>
    /// Super Mario World progression over states (worlds) and their cities (level-nodes):
    ///  • The home branch's state is World 1, always open. Remaining worlds follow in fixed
    ///    difficulty order and unlock when the previous world is fully cleared.
    ///  • Within a world, the first city is open; clearing a city opens the next on the path.
    ///  • A city is "cleared" the first time you beat its commute and complete a meeting there.
    /// This is the path gate; <see cref="TerritorySystem"/> still owns meeting stages/cooldowns.
    /// </summary>
    public static class WorldSystem
    {
        /// <summary>The world play-sequence for this character (home state first).</summary>
        public static IReadOnlyList<StateWorld> Sequence(BDRCharacter c) =>
            WorldRegistry.SequenceFor(c != null ? c.homeStateId : "indiana");

        public static bool IsCityCleared(BDRCharacter c, string cityId) =>
            TerritorySystem.GetProgress(c, cityId).cleared;

        /// <summary>A world is cleared once every one of its city-levels is cleared.</summary>
        public static bool IsWorldCleared(BDRCharacter c, StateWorld w) =>
            w != null && w.Cities.All(city => IsCityCleared(c, city.Id));

        /// <summary>
        /// A world is open if it's the home world, or every earlier world in the sequence is
        /// fully cleared (SMW: you reach a new world by finishing the previous one).
        /// </summary>
        public static bool IsWorldOpen(BDRCharacter c, StateWorld w)
        {
            var seq = Sequence(c);
            int idx = IndexOf(seq, w);
            if (idx <= 0) return true; // home world (or unknown) is always open
            for (int i = 0; i < idx; i++)
                if (!IsWorldCleared(c, seq[i])) return false;
            return true;
        }

        /// <summary>
        /// Within an OPEN world, a city is unlocked if it's the first on the path or the
        /// preceding city has been cleared. Cities in a not-yet-open world are all locked.
        /// </summary>
        public static bool IsCityUnlocked(BDRCharacter c, StateWorld w, int cityIndex)
        {
            if (!IsWorldOpen(c, w)) return false;
            if (cityIndex <= 0) return true;
            return IsCityCleared(c, w.Cities[cityIndex - 1].Id);
        }

        public static bool IsCityUnlocked(BDRCharacter c, string cityId)
        {
            var w = WorldRegistry.WorldOf(cityId);
            if (w == null) return false;
            return IsCityUnlocked(c, w, w.Cities.FindIndex(x => x.Id == cityId));
        }

        /// <summary>Mark a city-level cleared (first commute+meeting win) and persist nothing
        /// here — the caller saves. Returns true if this clear newly finished its world.</summary>
        public static bool MarkCityCleared(BDRCharacter c, string cityId)
        {
            var p = TerritorySystem.GetProgress(c, cityId);
            if (p.cleared) return false;
            p.cleared = true;
            var w = WorldRegistry.WorldOf(cityId);
            return w != null && IsWorldCleared(c, w);
        }

        /// <summary>The next world after the given one in this character's sequence, or null.</summary>
        public static StateWorld NextWorld(BDRCharacter c, StateWorld w)
        {
            var seq = Sequence(c);
            int idx = IndexOf(seq, w);
            return idx >= 0 && idx + 1 < seq.Count ? seq[idx + 1] : null;
        }

        /// <summary>The world the player should currently be looking at: the first not-yet-cleared
        /// open world (their live frontier), falling back to the home world.</summary>
        public static StateWorld CurrentWorld(BDRCharacter c)
        {
            var seq = Sequence(c);
            foreach (var w in seq)
                if (IsWorldOpen(c, w) && !IsWorldCleared(c, w)) return w;
            return seq.Count > 0 ? seq[seq.Count - 1] : null; // all cleared → last world
        }

        private static int IndexOf(IReadOnlyList<StateWorld> seq, StateWorld w)
        {
            for (int i = 0; i < seq.Count; i++)
                if (seq[i] == w || (w != null && seq[i].Id == w.Id)) return i;
            return -1;
        }
    }
}
