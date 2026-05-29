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

    /// <summary>
    /// Lifecycle of a freight load on the BDR's desk, once an account is won and
    /// starts tendering freight. The player moves a load Offered → (quote) →
    /// AwaitingCarrier → (cover) → InTransit → Delivered, or it's Lost (priced too
    /// high) or FellThrough (a covered load that failed service).
    /// </summary>
    public enum LoadStatus
    {
        Offered,         // tendered by the shipper — needs your quote
        AwaitingCarrier, // you won the freight — needs a carrier to cover it
        InTransit,       // covered and moving — resolves on the delivery day
        Delivered,       // delivered clean — margin & commission realized
        Lost,            // you quoted over their ceiling — went to a competitor
        FellThrough      // covered, but the carrier failed — claim + reputation hit
    }
}
