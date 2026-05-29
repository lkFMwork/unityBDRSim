using System.Collections.Generic;
using Fitzmark.BDRSim.Data;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>
    /// Resolves which active abilities a character brings into a call: the one
    /// granted by their Sales Style plus any unlocked from ability trees,
    /// de-duplicated.
    /// </summary>
    public static class AbilitySystem
    {
        public static List<AbilityDefinition> AvailableAbilities(BDRCharacter character)
        {
            var result = new List<AbilityDefinition>();
            if (character == null) return result;
            var seen = new HashSet<string>();

            void Add(string id)
            {
                if (string.IsNullOrEmpty(id) || !seen.Add(id)) return;
                var ability = AbilityLibrary.Get(id);
                if (ability != null) result.Add(ability);
            }

            var style = SalesStyleLibrary.Get(character.styleId);
            if (style != null) Add(style.GrantedAbilityId);

            if (character.unlockedPerks != null)
            {
                foreach (var perkId in character.unlockedPerks)
                {
                    var perk = PerkLibrary.Get(perkId);
                    if (perk != null) Add(perk.GrantsAbilityId);
                }
            }
            return result;
        }
    }
}
