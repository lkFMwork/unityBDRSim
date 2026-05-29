using System;
using System.Collections.Generic;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// A won customer that now lives in your book of business. Closing a deal stops
    /// being the end of the loop and becomes the start: an account tenders loads on
    /// its lanes, you price and cover each one for margin, and good (or bad) service
    /// moves its <see cref="health"/> — neglect it or fail its freight and it churns.
    /// Plain serializable fields so it round-trips through <c>JsonUtility</c>.
    /// </summary>
    [Serializable]
    public class FreightAccount
    {
        public string id = "";
        public string company = "New Account";
        public string contact = "";
        public string title = "";
        public string location = "";
        public string industry = "";

        /// <summary>Lanes copied from the prospect at the time you won them.</summary>
        public List<Lane> lanes = new List<Lane>();

        /// <summary>0 = pure rate buyer, 1 = only price matters. Controls quote headroom.</summary>
        public float priceSensitivity = 0.5f;

        /// <summary>Service reputation 0..1. Clean deliveries raise it; failures and
        /// ignored tenders lower it. Below the churn floor, the account leaves.</summary>
        public float health = 0.65f;

        public int wonOnDay = 1;
        public int lastTenderDay = 0;
        public int loadsDelivered = 0;
        public int loadsFailed = 0;
        public float lifetimeMargin = 0f;

        /// <summary>False once the account has churned (lost to a competitor).</summary>
        public bool active = true;
    }
}
