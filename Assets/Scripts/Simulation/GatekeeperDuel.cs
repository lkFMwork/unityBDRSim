using System;
using Fitzmark.BDRSim.Data;
using UnityEngine;

namespace Fitzmark.BDRSim.Simulation
{
    public enum DuelOutcome { InProgress, Won, Lost }

    /// <summary>
    /// The "Gatekeeper Gauntlet": a turn-based verbal duel. Each turn the gatekeeper
    /// shows a stance (with a readable tell); the player picks a move. The move that
    /// the stance is weak to lands a critical hit on the gatekeeper's Resolve; the
    /// move it resists backfires and costs the player Composure; anything else makes
    /// some progress but takes a little pushback. Drop their Resolve to 0 to get put
    /// through; lose all Composure and they hang up.
    ///
    /// Damage is shaped by the player's attributes (each move keys off a stat;
    /// Resilience raises Composure and softens hits). Deterministic given a seed.
    /// </summary>
    public class GatekeeperDuel
    {
        public int PlayerComposure { get; private set; }
        public int PlayerMaxComposure { get; }
        public int GatekeeperResolve { get; private set; }
        public int GatekeeperMaxResolve { get; }
        public GatekeeperStance CurrentStance { get; private set; }
        public int Turn { get; private set; }
        public DuelOutcome Outcome { get; private set; } = DuelOutcome.InProgress;
        public bool IsOver => Outcome != DuelOutcome.InProgress;

        public event Action<string> Log;
        public event Action StateChanged;
        public event Action<bool> Ended; // true = won

        public GatekeeperMove[] Moves => GatekeeperCombatData.Moves;

        private readonly BDRAttributes _attr;
        private readonly float _damageScale;
        private readonly System.Random _rng;

        public GatekeeperDuel(int maxResolve, BDRAttributes playerAttributes, float gatekeeperDamageScale, int seed)
        {
            _attr = playerAttributes ?? new BDRAttributes();
            _damageScale = gatekeeperDamageScale;
            _rng = new System.Random(seed);

            GatekeeperMaxResolve = Mathf.Max(20, maxResolve);
            GatekeeperResolve = GatekeeperMaxResolve;

            int resilience = _attr.Get(AttributeType.Resilience);
            PlayerMaxComposure = Mathf.Max(40, 100 + (resilience - 5) * 8);
            PlayerComposure = PlayerMaxComposure;
        }

        public void Begin()
        {
            CurrentStance = RollStance();
            Log?.Invoke("The gatekeeper picks up. Read them — and get through.");
            Log?.Invoke(GatekeeperCombatData.StanceTell(CurrentStance));
            StateChanged?.Invoke();
        }

        public void Play(GatekeeperMove move)
        {
            if (IsOver) return;
            Turn++;

            var weakness = GatekeeperCombatData.Weakness(CurrentStance);
            var resist = GatekeeperCombatData.Resist(CurrentStance);
            int statBonus = _attr.Get(GatekeeperCombatData.MoveAttribute(move)) - BDRAttributes.Baseline;

            Log?.Invoke($"You: {GatekeeperCombatData.MoveLine(move)}");

            if (move == weakness)
            {
                int dmg = Mathf.Max(1, 34 + Mathf.RoundToInt(statBonus * 1.5f));
                GatekeeperResolve -= dmg;
                Log?.Invoke($"Critical read — that lands hard.  (-{dmg} resolve)");
            }
            else if (move == resist)
            {
                int self = SelfDamage(26);
                PlayerComposure -= self;
                Log?.Invoke($"Wrong read — they shut you down cold.  (-{self} composure)");
            }
            else
            {
                int dmg = Mathf.Max(1, 12 + Mathf.RoundToInt(statBonus * 1.0f));
                int self = SelfDamage(8);
                GatekeeperResolve -= dmg;
                PlayerComposure -= self;
                Log?.Invoke($"Some progress, but they push back.  (-{dmg} resolve, -{self} composure)");
            }

            if (GatekeeperResolve <= 0)
            {
                GatekeeperResolve = 0;
                Outcome = DuelOutcome.Won;
                Log?.Invoke("\"...alright, hold on. Let me put you through.\"");
                StateChanged?.Invoke();
                Ended?.Invoke(true);
                return;
            }
            if (PlayerComposure <= 0)
            {
                PlayerComposure = 0;
                Outcome = DuelOutcome.Lost;
                Log?.Invoke("\"Like I said — they're not available.\"  *click*");
                StateChanged?.Invoke();
                Ended?.Invoke(false);
                return;
            }

            CurrentStance = RollStance();
            Log?.Invoke(GatekeeperCombatData.StanceTell(CurrentStance));
            StateChanged?.Invoke();
        }

        private int SelfDamage(int baseDamage)
        {
            float resilienceFactor = 1f - (_attr.Get(AttributeType.Resilience) - BDRAttributes.Baseline) * 0.05f;
            return Mathf.Max(1, Mathf.RoundToInt(baseDamage * _damageScale * resilienceFactor));
        }

        private GatekeeperStance RollStance()
        {
            var stances = GatekeeperCombatData.Stances;
            return stances[_rng.Next(stances.Length)];
        }
    }
}
