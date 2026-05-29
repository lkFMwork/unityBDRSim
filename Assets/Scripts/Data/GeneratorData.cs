namespace Fitzmark.BDRSim.Data
{
    /// <summary>Word banks the procedural prospect generator draws from.</summary>
    public static class GeneratorData
    {
        public static readonly string[] FirstNames =
        {
            "Pat", "Dana", "Alex", "Sam", "Jordan", "Morgan", "Casey", "Riley", "Taylor", "Jamie",
            "Chris", "Drew", "Quinn", "Avery", "Reese", "Skyler", "Cameron", "Devon", "Harper", "Logan"
        };

        public static readonly string[] LastNames =
        {
            "Morgan", "Cole", "Rivera", "Nguyen", "Patel", "Hayes", "Brooks", "Reed", "Foster", "Diaz",
            "Walsh", "Bauer", "Klein", "Owens", "Vance", "Sutton", "Marsh", "Boyd", "Frey", "Lang"
        };

        public static readonly string[] CompanyRoots =
        {
            "Hoosier", "Crossroads", "Meridian", "Summit", "Ironwood", "Lakeside", "Cardinal", "Vertex",
            "Allied", "Keystone", "Northwind", "Brightline", "Granite", "Harbor", "Pioneer", "Redwood"
        };

        public static readonly string[] CompanySuffixes =
        {
            "Manufacturing", "Foods", "Industrial", "Distribution", "Logistics Group", "Components",
            "Materials", "Supply Co.", "Products", "Mills"
        };

        public static readonly string[] Industries =
        {
            "Industrial / Manufacturing", "Food & Beverage", "Consumer Goods", "Building Materials",
            "Automotive", "Chemicals", "Retail Distribution", "Agriculture"
        };

        public static readonly string[] Titles =
        {
            "Logistics Manager", "Shipping Coordinator", "Director of Logistics",
            "VP of Supply Chain", "Operations Manager", "Transportation Manager"
        };

        // origin/destination cities for generated lanes
        public static readonly string[] Cities =
        {
            "Indianapolis, IN", "Chicago, IL", "Detroit, MI", "Columbus, OH", "Atlanta, GA",
            "Dallas, TX", "Memphis, TN", "Charlotte, NC", "Kansas City, MO", "Nashville, TN",
            "Louisville, KY", "St. Louis, MO", "Cincinnati, OH", "Milwaukee, WI", "Pittsburgh, PA"
        };

        public static readonly string[] PainPoints =
        {
            "Carriers no-show during peak season",
            "Spotty communication once freight is picked up",
            "Reefer capacity falls apart every summer",
            "Got burned on a claim last year",
            "Rates spike whenever the market tightens",
            "Direct carriers reject overflow lanes",
            "No real-time visibility on loads in transit",
            "Detention and late fees keep piling up"
        };

        public static readonly string[] IncumbentProviders =
        {
            "a regional broker", "two core carriers", "an in-house fleet",
            "a national 3PL", "a patchwork of small carriers", "their shipper-of-choice program"
        };
    }
}
