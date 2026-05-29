using System;
using System.Collections.Generic;
using Fitzmark.BDRSim.Data;
using UnityEngine;

namespace Fitzmark.BDRSim.Simulation
{
    /// <summary>
    /// Procedurally builds a fresh <see cref="ScenarioDefinition"/> (with a
    /// <see cref="ProspectProfile"/> and a lane) for the career loop, scaling
    /// difficulty with the player's progress. Deterministic given a seed, so the
    /// generator is unit-testable and a call can be reproduced.
    ///
    /// The created assets are in-memory ScriptableObjects (never written to disk);
    /// the GameManager holds a reference for the lifetime of the call.
    /// </summary>
    public static class ProspectGenerator
    {
        public static ScenarioDefinition Generate(int difficultyLevel, int seed)
        {
            var rng = new System.Random(seed);
            difficultyLevel = Mathf.Clamp(difficultyLevel, 1, 10);
            float diff = (difficultyLevel - 1) / 9f; // 0..1

            var prospect = ScriptableObject.CreateInstance<ProspectProfile>();
            prospect.contactName = $"{Pick(rng, GeneratorData.FirstNames)} {Pick(rng, GeneratorData.LastNames)}";
            prospect.title = Pick(rng, GeneratorData.Titles);
            prospect.companyName =
                $"{Pick(rng, GeneratorData.CompanyRoots)} {Pick(rng, GeneratorData.CompanySuffixes)}";
            prospect.industry = Pick(rng, GeneratorData.Industries);
            prospect.location = Pick(rng, GeneratorData.Cities);
            prospect.incumbentProvider = Pick(rng, GeneratorData.IncumbentProviders);

            prospect.personality = (ProspectPersonality)rng.Next(0, 5);
            prospect.startingTrust = Clamp01(0.45f - diff * 0.25f + Jitter(rng, 0.05f));
            prospect.startingPatience = Clamp01(0.80f - diff * 0.35f + Jitter(rng, 0.05f));
            prospect.priceSensitivity = Clamp01(0.35f + diff * 0.45f + Jitter(rng, 0.05f));

            prospect.painPoints = new List<string> { Pick(rng, GeneratorData.PainPoints) };
            if (rng.NextDouble() < 0.5) prospect.painPoints.Add(Pick(rng, GeneratorData.PainPoints));

            int objectionCount = 1 + Mathf.RoundToInt(diff * 2f); // 1..3
            prospect.likelyObjections = RandomObjections(rng, objectionCount);
            prospect.lanes = new List<Lane> { GenerateLane(rng) };

            var scenario = ScriptableObject.CreateInstance<ScenarioDefinition>();
            scenario.scenarioId = $"gen-{seed}";
            scenario.title = prospect.companyName;
            scenario.difficulty = diff < 0.34f ? DifficultyTier.Easy
                : (diff < 0.67f ? DifficultyTier.Medium : DifficultyTier.Hard);
            scenario.gatekeeperPresent = rng.NextDouble() < (0.15 + diff * 0.40);
            scenario.prospect = prospect;

            var lane = prospect.lanes[0];
            float fairMargin = (lane.fitzmarkCostPerMile * 1.12f) - lane.fitzmarkCostPerMile;
            scenario.targetWeeklyMargin =
                (float)Math.Round(fairMargin * lane.miles * lane.loadsPerWeek * 0.8f, 0);

            scenario.briefing =
                $"Cold lead: {prospect.contactName}, {prospect.title} at {prospect.companyName} " +
                $"({prospect.location}). They move {lane.mode} freight and mention: " +
                $"\"{prospect.painPoints[0].ToLowerInvariant()}\". Win the lane.";

            return scenario;
        }

        private static Lane GenerateLane(System.Random rng)
        {
            string origin = Pick(rng, GeneratorData.Cities);
            string dest = Pick(rng, GeneratorData.Cities);
            int guard = 8;
            while (dest == origin && guard-- > 0) dest = Pick(rng, GeneratorData.Cities);

            var modes = (FreightMode[])Enum.GetValues(typeof(FreightMode));
            var equipment = (EquipmentType[])Enum.GetValues(typeof(EquipmentType));

            float current = 2.0f + (float)rng.NextDouble() * 1.3f;          // $2.00–$3.30/mi
            float cost = current * (0.80f + (float)rng.NextDouble() * 0.10f); // 80–90% of current

            return new Lane
            {
                label = $"{origin} -> {dest}",
                origin = origin,
                destination = dest,
                miles = 150 + rng.Next(0, 1050),
                mode = modes[rng.Next(modes.Length)],
                equipment = equipment[rng.Next(equipment.Length)],
                loadsPerWeek = 3 + rng.Next(0, 13),
                currentRatePerMile = (float)Math.Round(current, 2),
                fitzmarkCostPerMile = (float)Math.Round(cost, 2)
            };
        }

        private static List<ObjectionType> RandomObjections(System.Random rng, int count)
        {
            var all = (ObjectionType[])Enum.GetValues(typeof(ObjectionType));
            var list = new List<ObjectionType>();
            int guard = 32;
            while (list.Count < count && guard-- > 0)
            {
                var o = all[rng.Next(all.Length)];
                if (!list.Contains(o)) list.Add(o);
            }
            return list;
        }

        private static string Pick(System.Random rng, string[] arr) => arr[rng.Next(arr.Length)];

        private static float Jitter(System.Random rng, float magnitude) =>
            (float)(rng.NextDouble() * 2.0 - 1.0) * magnitude;

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
