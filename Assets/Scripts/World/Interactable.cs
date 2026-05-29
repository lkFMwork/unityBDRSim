using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// A point of interest in the city the player can walk/drive up to and trigger.
    /// Client sites generate a meeting; the office returns you to the hub. Authored
    /// onto building GameObjects by the setup tool.
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        public enum Kind { Client, Office, Npc, Desk, Exit }

        public Kind kind = Kind.Client;
        public string label = "Client Site";
        public int seed = 0;        // client: meeting seed; npc: mentor index
        public string clientId = ""; // local (Texas) account id, when applicable
        public float range = 4.5f;
    }
}
