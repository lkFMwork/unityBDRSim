using Fitzmark.BDRSim.Data;
using UnityEngine;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>
    /// Drives local (Texas) account progression: which clients are reachable (by
    /// level), which are available right now (an in-game time gap must pass between
    /// meeting stages), the current meeting stage, and recording a meeting result —
    /// advancing the relationship toward a close, one harder stage at a time.
    /// </summary>
    public static class TerritorySystem
    {
        public const int MaxStages = 4;
        public const int CooldownDays = 5; // the in-game gap between meeting stages

        public static string StageName(int stage) => stage switch
        {
            1 => "Intro Meeting",
            2 => "Discovery",
            3 => "Proposal",
            4 => "Close",
            _ => "Follow-up"
        };

        public static LocalAccountProgress GetProgress(BDRCharacter c, string clientId)
        {
            c.localAccounts ??= new System.Collections.Generic.List<LocalAccountProgress>();
            foreach (var p in c.localAccounts)
                if (p.clientId == clientId) return p;
            var created = new LocalAccountProgress { clientId = clientId };
            c.localAccounts.Add(created);
            return created;
        }

        public static bool IsUnlocked(BDRCharacter c, LocalClient client) => c.level >= client.RequiredLevel;

        /// <summary>True once the player has beaten the commute platformer to reach this city.</summary>
        public static bool IsCityUnlocked(BDRCharacter c, string clientId) => GetProgress(c, clientId).cityUnlocked;

        /// <summary>Mark a city reachable by fast-travel (after winning the commute level).</summary>
        public static void UnlockCity(BDRCharacter c, string clientId) =>
            GetProgress(c, clientId).cityUnlocked = true;

        public static int CurrentStage(BDRCharacter c, string clientId) => GetProgress(c, clientId).stage;
        public static bool IsClosed(BDRCharacter c, string clientId) => GetProgress(c, clientId).closed;

        public static bool IsAvailable(BDRCharacter c, LocalClient client, int day)
        {
            var p = GetProgress(c, client.Id);
            if (!IsUnlocked(c, client) || p.closed) return false;
            if (p.lastMeetingDay < 0) return true;
            return day - p.lastMeetingDay >= CooldownDays;
        }

        public static int DaysUntilAvailable(BDRCharacter c, string clientId, int day)
        {
            var p = GetProgress(c, clientId);
            if (p.lastMeetingDay < 0) return 0;
            return Mathf.Max(0, CooldownDays - (day - p.lastMeetingDay));
        }

        /// <summary>Platformer + meeting difficulty for the client's current stage.</summary>
        public static int StageDifficulty(BDRCharacter c, LocalClient client)
        {
            var p = GetProgress(c, client.Id);
            return Mathf.Clamp(client.RequiredLevel + p.stage, 1, 10);
        }

        /// <summary>Record a finished meeting: start the cooldown; on a win, advance the
        /// stage (or close the account on the final stage).</summary>
        public static void RecordMeeting(BDRCharacter c, string clientId, int day, bool won)
        {
            var p = GetProgress(c, clientId);
            p.lastMeetingDay = day;
            if (!won) return;
            if (p.stage >= MaxStages) p.closed = true;
            else p.stage++;
        }
    }
}
