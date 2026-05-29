using System.Collections.Generic;
using UnityEngine;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// A shipper contact the BDR is calling: who they are, what they ship, and
    /// how they behave on the phone. This is pure content authored as an asset so
    /// trainers can add prospects without touching code.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Prospect_",
        menuName = "Fitzmark BDR/Prospect Profile",
        order = 10)]
    public class ProspectProfile : ScriptableObject
    {
        [Header("Identity")]
        public string contactName = "Pat Morgan";
        public string title = "Logistics Manager";
        public string companyName = "Acme Manufacturing";
        public string industry = "Industrial / Manufacturing";
        public string location = "Indianapolis, IN";

        [Header("Disposition")]
        public ProspectPersonality personality = ProspectPersonality.Busy;

        [Tooltip("How much patience the prospect starts with (0 = about to hang up, 1 = all day).")]
        [Range(0f, 1f)] public float startingPatience = 0.7f;

        [Tooltip("How much the prospect trusts the rep at hello (0 = guarded, 1 = warm).")]
        [Range(0f, 1f)] public float startingTrust = 0.35f;

        [Tooltip("How fixated the prospect is on price (0 = value buyer, 1 = only the rate matters).")]
        [Range(0f, 1f)] public float priceSensitivity = 0.5f;

        [Header("Account")]
        [Tooltip("Current broker / provider, if any. Empty means they ship direct or in-house.")]
        public string incumbentProvider = "a regional broker";

        [Tooltip("Pain points the rep can uncover and sell against.")]
        public List<string> painPoints = new()
        {
            "Carriers no-show during produce season",
            "No visibility once a load is picked up"
        };

        [Tooltip("Objections this prospect is likely to raise, in rough priority order.")]
        public List<ObjectionType> likelyObjections = new()
        {
            ObjectionType.AlreadyHaveBroker,
            ObjectionType.SendMeAnEmail
        };

        [Header("Freight Network")]
        public List<Lane> lanes = new();

        /// <summary>The lane the rep is primarily trying to win on this call.</summary>
        public Lane PrimaryLane => (lanes != null && lanes.Count > 0) ? lanes[0] : null;

        public string DisplayHeadline => $"{contactName}, {title} @ {companyName}";
    }
}
