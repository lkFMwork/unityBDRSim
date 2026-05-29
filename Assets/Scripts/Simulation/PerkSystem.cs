using Fitzmark.BDRSim.Data;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>
    /// Unlocking ability-tree nodes and aggregating their effects together with the
    /// character's Sales Style trait.
    /// </summary>
    public static class PerkSystem
    {
        public static PerkEffects Aggregate(BDRCharacter character)
        {
            var e = new PerkEffects();
            if (character == null) return e;

            // Sales Style trait is a passive that stacks with perks.
            var style = SalesStyleLibrary.Get(character.styleId);
            if (style != null)
            {
                e.TrustBonus += style.TrustBonus;
                e.PatienceDrainReduction += style.PatienceDrainReduction;
                e.NegotiationSkill += style.NegotiationSkill;
                e.ScoreBonusAll += style.ScoreBonusAll;
                e.XpMultiplier += style.XpMultiplierBonus;
                e.ExtraCallsPerDay += style.ExtraCallsPerDay;
            }

            if (character.unlockedPerks != null)
            {
                foreach (var id in character.unlockedPerks)
                {
                    var p = PerkLibrary.Get(id);
                    if (p == null) continue;
                    e.TrustBonus += p.TrustBonus;
                    e.PatienceDrainReduction += p.PatienceDrainReduction;
                    e.NegotiationSkill += p.NegotiationSkill;
                    e.ScoreBonusAll += p.ScoreBonusAll;
                    e.XpMultiplier += p.XpMultiplierBonus;
                    e.ExtraCallsPerDay += p.ExtraCallsPerDay;
                }
            }
            return e;
        }

        public static bool IsUnlocked(BDRCharacter character, string perkId) =>
            character?.unlockedPerks != null && character.unlockedPerks.Contains(perkId);

        public static bool PrerequisiteMet(BDRCharacter character, PerkDefinition perk) =>
            perk != null && (string.IsNullOrEmpty(perk.RequiresPerkId)
                             || IsUnlocked(character, perk.RequiresPerkId));

        public static bool CanUnlock(BDRCharacter character, PerkDefinition perk) =>
            character != null && perk != null
            && !IsUnlocked(character, perk.Id)
            && PrerequisiteMet(character, perk)
            && character.unspentSkillPoints >= perk.Cost;

        public static bool Unlock(BDRCharacter character, PerkDefinition perk)
        {
            if (!CanUnlock(character, perk)) return false;
            character.unspentSkillPoints -= perk.Cost;
            character.unlockedPerks.Add(perk.Id);
            return true;
        }
    }
}
