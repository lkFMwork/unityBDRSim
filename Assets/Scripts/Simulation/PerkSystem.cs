using Fitzmark.BDRSim.Data;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>Unlocking and aggregating skill-tree perks for a character.</summary>
    public static class PerkSystem
    {
        public static PerkEffects Aggregate(BDRCharacter character)
        {
            var e = new PerkEffects();
            if (character?.unlockedPerks == null) return e;

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
            return e;
        }

        public static bool IsUnlocked(BDRCharacter character, string perkId) =>
            character?.unlockedPerks != null && character.unlockedPerks.Contains(perkId);

        public static bool CanUnlock(BDRCharacter character, PerkDefinition perk) =>
            character != null && perk != null
            && !IsUnlocked(character, perk.Id)
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
