using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// A simple opponent brain: close the distance, attack when in range (on a
    /// cooldown), block some of the player's attacks, and occasionally back off.
    /// Aggression/blocking scale with difficulty. Produces a <see cref="FighterIntent"/>
    /// each frame for the gatekeeper fighter.
    /// </summary>
    public class FighterAI
    {
        private readonly float _attackInterval;
        private readonly float _blockChance;
        private readonly float _reach;
        private readonly System.Random _rng = new System.Random();
        private float _cooldown;

        public FighterAI(int difficulty)
        {
            _attackInterval = Mathf.Clamp(0.95f - difficulty * 0.05f, 0.40f, 1.0f);
            _blockChance = Mathf.Clamp(0.15f + difficulty * 0.05f, 0f, 0.6f);
            _reach = 1.7f;
        }

        public FighterIntent Tick(Fighter self, Fighter opponent, float dt)
        {
            var intent = new FighterIntent();
            if (self == null || opponent == null || self.IsKO || opponent.IsKO) return intent;

            _cooldown -= dt;
            float dx = opponent.transform.position.x - self.transform.position.x;
            float dist = Mathf.Abs(dx);
            float dir = dx >= 0f ? 1f : -1f;

            if (dist > _reach * 2.2f)
            {
                // Far: approach, but sometimes lob a projectile to pressure.
                if (_cooldown <= 0f && _rng.NextDouble() < 0.25)
                {
                    intent.attack = AttackType.Projectile;
                    _cooldown = _attackInterval * 1.4f;
                }
                else intent.move = dir;
            }
            else if (dist > _reach)
            {
                intent.move = dir; // approach into range
            }
            else if (opponent.IsAttacking && _rng.NextDouble() < _blockChance)
            {
                intent.block = true;
            }
            else if (_cooldown <= 0f)
            {
                double r = _rng.NextDouble();
                intent.attack = r < 0.45 ? AttackType.Light
                              : r < 0.75 ? AttackType.Heavy
                              : r < 0.90 ? AttackType.Launcher
                              : AttackType.Special;
                _cooldown = _attackInterval;
            }
            else if (_rng.NextDouble() < 0.2)
            {
                intent.move = -dir * 0.5f; // brief spacing
            }
            return intent;
        }
    }
}
