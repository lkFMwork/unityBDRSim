using System.IO;
using Fitzmark.BDRSim.Data;
using UnityEngine;

namespace Fitzmark.BDRSim.Core
{
    /// <summary>
    /// JSON persistence for the player's <see cref="BDRCharacter"/>, stored under
    /// <see cref="Application.persistentDataPath"/>. Failures are swallowed and
    /// reported (a corrupt save should never hard-crash the game).
    /// </summary>
    public static class SaveSystem
    {
        /// <summary>Current save schema version. Bump when the save shape changes.</summary>
        public const int CurrentSaveVersion = 1;

        private const string FileName = "fitzmark_bdr_save.json";

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
        private static string TempPath => FilePath + ".tmp";

        public static bool Exists => File.Exists(FilePath);

        public static void Save(BDRCharacter character)
        {
            if (character == null) return;
            character.saveVersion = CurrentSaveVersion;
            try
            {
                // Atomic write: serialize to a temp file, then swap it in, so an
                // interrupted save can never leave a half-written (corrupt) profile.
                File.WriteAllText(TempPath, JsonUtility.ToJson(character, true));
                if (File.Exists(FilePath)) File.Replace(TempPath, FilePath, null);
                else File.Move(TempPath, FilePath);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Fitzmark BDR] Failed to save profile: {e.Message}");
            }
        }

        public static BDRCharacter Load()
        {
            if (!Exists) return null;
            try
            {
                var character = JsonUtility.FromJson<BDRCharacter>(File.ReadAllText(FilePath));
                // Guard against partially-initialized saves.
                if (character != null)
                {
                    character.avatar ??= new AvatarConfig();
                    character.attributes ??= new BDRAttributes();
                    character.career ??= new CareerState();
                    character.unlockedPerks ??= new System.Collections.Generic.List<string>();
                    character.completedQuests ??= new System.Collections.Generic.List<string>();
                    character.unlockedAchievements ??= new System.Collections.Generic.List<string>();
                    character.rivalDeals ??= new System.Collections.Generic.List<int>();
                    character.localAccounts ??= new System.Collections.Generic.List<LocalAccountProgress>();
                    character.accounts ??= new System.Collections.Generic.List<FreightAccount>();
                    character.loads ??= new System.Collections.Generic.List<FreightLoad>();
                    character.upgrades ??= new System.Collections.Generic.List<UpgradeLevel>();
                    character.leads ??= new System.Collections.Generic.List<OutreachLead>();
                    character.acknowledgedRank ??= "";
                    Migrate(character);
                }
                return character;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Fitzmark BDR] Failed to load profile: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Brings an older save forward. The null-guards above already backfill fields
        /// added over time; per-version data fixes are keyed on <c>saveVersion</c> here.
        /// </summary>
        private static void Migrate(BDRCharacter c)
        {
            // (No data reshaping needed yet — guards cover added fields.)
            c.saveVersion = CurrentSaveVersion;
        }

        public static void Delete()
        {
            try
            {
                if (Exists) File.Delete(FilePath);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Fitzmark BDR] Failed to delete profile: {e.Message}");
            }
        }
    }
}
