using System;
using UnityEngine;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>The five core attributes that define a BDR's playstyle.</summary>
    public enum AttributeType
    {
        Charisma,         // warmth on the opener; raises starting trust
        Negotiation,      // hold margin; prospects accept higher rates
        ProductKnowledge, // articulate value and handle objections
        Resilience,       // resist burnout; patience drains slower
        Prospecting       // sharper discovery; better questions
    }

    /// <summary>
    /// A BDR's attribute spread. Values run 1..10, with 5 as the neutral
    /// baseline that yields no bonus or penalty. Serializable so it round-trips
    /// through JSON saves and the Unity inspector.
    /// </summary>
    [Serializable]
    public class BDRAttributes
    {
        public const int Min = 1;
        public const int Max = 12;        // point-buy caps at 10; Sales Style mods can push to 12
        public const int Baseline = 5;

        [Range(Min, Max)] public int charisma = Baseline;
        [Range(Min, Max)] public int negotiation = Baseline;
        [Range(Min, Max)] public int productKnowledge = Baseline;
        [Range(Min, Max)] public int resilience = Baseline;
        [Range(Min, Max)] public int prospecting = Baseline;

        public int Total => charisma + negotiation + productKnowledge + resilience + prospecting;

        public int Get(AttributeType type) => type switch
        {
            AttributeType.Charisma => charisma,
            AttributeType.Negotiation => negotiation,
            AttributeType.ProductKnowledge => productKnowledge,
            AttributeType.Resilience => resilience,
            AttributeType.Prospecting => prospecting,
            _ => Baseline
        };

        public void Set(AttributeType type, int value)
        {
            value = Mathf.Clamp(value, Min, Max);
            switch (type)
            {
                case AttributeType.Charisma: charisma = value; break;
                case AttributeType.Negotiation: negotiation = value; break;
                case AttributeType.ProductKnowledge: productKnowledge = value; break;
                case AttributeType.Resilience: resilience = value; break;
                case AttributeType.Prospecting: prospecting = value; break;
            }
        }

        /// <summary>Adjust an attribute by delta, clamped. Returns the actual change applied.</summary>
        public int Adjust(AttributeType type, int delta)
        {
            int before = Get(type);
            Set(type, before + delta);
            return Get(type) - before;
        }

        public BDRAttributes Clone() => new BDRAttributes
        {
            charisma = charisma,
            negotiation = negotiation,
            productKnowledge = productKnowledge,
            resilience = resilience,
            prospecting = prospecting
        };

        public static string DisplayName(AttributeType type) => type switch
        {
            AttributeType.Charisma => "Charisma",
            AttributeType.Negotiation => "Negotiation",
            AttributeType.ProductKnowledge => "Product Knowledge",
            AttributeType.Resilience => "Resilience",
            AttributeType.Prospecting => "Prospecting",
            _ => type.ToString()
        };

        public static readonly AttributeType[] All =
        {
            AttributeType.Charisma,
            AttributeType.Negotiation,
            AttributeType.ProductKnowledge,
            AttributeType.Resilience,
            AttributeType.Prospecting
        };
    }
}
