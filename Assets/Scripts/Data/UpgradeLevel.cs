using System;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>A purchased business upgrade and how many levels of it you own.</summary>
    [Serializable]
    public class UpgradeLevel
    {
        public string id = "";
        public int level = 0;
    }
}
