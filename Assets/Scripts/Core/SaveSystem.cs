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
        private const string FileName = "fitzmark_bdr_save.json";

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool Exists => File.Exists(FilePath);

        public static void Save(BDRCharacter character)
        {
            if (character == null) return;
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(character, true));
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
                }
                return character;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Fitzmark BDR] Failed to load profile: {e.Message}");
                return null;
            }
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
