namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// Freight transportation modes a Fitzmark BDR sells against. Drives the
    /// vocabulary a rep is expected to use on a discovery call.
    /// </summary>
    public enum FreightMode
    {
        FullTruckload,      // FTL
        LessThanTruckload,  // LTL
        Intermodal,
        Drayage,
        Flatbed,
        Refrigerated,       // "reefer"
        Expedited
    }

    /// <summary>Trailer / equipment types referenced during discovery and quoting.</summary>
    public enum EquipmentType
    {
        DryVan,
        Reefer,
        Flatbed,
        StepDeck,
        Container,
        BoxTruck
    }

    /// <summary>
    /// Coarse personality archetype for a prospect. Controls how forgiving the
    /// prospect is of weak openers and how they react to pressure.
    /// </summary>
    public enum ProspectPersonality
    {
        Friendly,     // patient, gives the rep room
        Busy,         // short on time, rewards brevity
        Skeptical,    // needs proof and credibility
        PriceDriven,  // only the rate matters
        Loyal         // happy with incumbent, hard to move
    }

    /// <summary>
    /// Common objections a shipper raises against switching to a new broker.
    /// Each maps to coaching content in <see cref="ObjectionCatalog"/>.
    /// </summary>
    public enum ObjectionType
    {
        AlreadyHaveBroker,
        RatesTooHigh,
        SendMeAnEmail,
        WeGoDirectToCarriers,
        NoTimeRightNow,
        NotInterested,
        BadPastExperience,
        NeedToCheckWithBoss
    }

    /// <summary>Relative challenge of a training scenario.</summary>
    public enum DifficultyTier
    {
        Easy,
        Medium,
        Hard
    }
}
