using System.Collections.Generic;
using UnityEngine;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Per-city visual identity for the drivable city scene. Each Texas territory maps to
    /// a theme that picks which Kenney building kit to populate blocks with, a ground/road
    /// palette, and a density, so Houston reads industrial, Dallas as downtown skyscrapers,
    /// Austin as suburban, etc. — one procedural city builder, re-skinned per place.
    /// </summary>
    public enum CityStyle { Downtown, Commercial, Industrial, Suburban }

    public struct CityTheme
    {
        public string DisplayName;
        public CityStyle Style;
        public string[] Buildings;   // resolved Kenney FBX resource paths
        public Color Ground;
        public Color Grass;
        public int Blocks;           // grid radius in blocks (NxN = (2r+1)^2)
        public bool Trees;
    }

    public static class CityThemes
    {
        private const string Roads = "Models/kenney_city-kit-roads/Models/FBX format/";
        private const string Comm = "Models/kenney_city-kit-commercial_2.1/Models/FBX format/";
        private const string Ind = "Models/kenney_city-kit-industrial_1.0/Models/FBX format/";
        private const string Sub = "Models/kenney_city-kit-suburban_20/Models/FBX format/";

        // Road pieces (shared across themes), resolved as resource paths.
        public const string RoadStraight = Roads + "road-straight";
        public const string RoadCrossroad = Roads + "road-crossroad";
        public const string RoadBend = Roads + "road-bend";
        public const string RoadTile = Roads + "tile-low";
        public const string CarModel = "Models/kenney_car-kit/Models/FBX format/sedan";
        public const string DeliveryModel = "Models/kenney_car-kit/Models/FBX format/delivery";

        private static readonly string[] Commercial =
        {
            Comm + "building-a", Comm + "building-b", Comm + "building-c",
            Comm + "building-d", Comm + "building-e", Comm + "building-f",
        };
        private static readonly string[] Skyscrapers =
        {
            Comm + "building-skyscraper-a", Comm + "building-skyscraper-b",
            Comm + "building-skyscraper-c", Comm + "building-d", Comm + "building-g",
        };
        private static readonly string[] Industrial =
        {
            Ind + "building-a", Ind + "building-b", Ind + "building-c",
            Ind + "building-d", Ind + "building-e", Ind + "building-f",
        };
        private static readonly string[] Suburban =
        {
            Sub + "building-type-a", Sub + "building-type-b", Sub + "building-type-c",
            Sub + "building-type-d", Sub + "building-type-e", Sub + "building-type-f",
        };

        // City id -> theme. Marquee metros get a distinct look; anything unmapped falls back
        // to Commercial, so new cities are always safe.
        private static readonly Dictionary<string, CityStyle> ByClient = new()
        {
            // Texas
            { "dallas", CityStyle.Downtown },
            { "houston", CityStyle.Industrial },
            { "san_antonio", CityStyle.Commercial },
            { "austin", CityStyle.Suburban },
            { "fort_worth", CityStyle.Industrial },
            { "el_paso", CityStyle.Suburban },
            { "arlington", CityStyle.Suburban },
            // Headliner metros across the other states
            { "indianapolis", CityStyle.Downtown },
            { "nashville", CityStyle.Downtown },
            { "memphis", CityStyle.Industrial },
            { "atlanta", CityStyle.Downtown },
            { "birmingham", CityStyle.Industrial },
            { "mobile", CityStyle.Industrial },
            { "savannah", CityStyle.Industrial },
            { "st_louis", CityStyle.Downtown },
            { "kansas_city", CityStyle.Commercial },
            { "phoenix", CityStyle.Suburban },
            { "tucson", CityStyle.Suburban },
            { "buffalo", CityStyle.Industrial },
            { "new_york_city", CityStyle.Downtown },
        };

        public static CityTheme For(string clientId, string displayName)
        {
            var style = ByClient.TryGetValue(clientId ?? "", out var s) ? s : CityStyle.Commercial;
            return Build(style, displayName);
        }

        private static CityTheme Build(CityStyle style, string name) => style switch
        {
            CityStyle.Downtown => new CityTheme
            {
                DisplayName = name, Style = style, Buildings = Skyscrapers,
                Ground = new Color(0.22f, 0.22f, 0.25f), Grass = new Color(0.26f, 0.34f, 0.24f),
                Blocks = 3, Trees = false,
            },
            CityStyle.Industrial => new CityTheme
            {
                DisplayName = name, Style = style, Buildings = Industrial,
                Ground = new Color(0.30f, 0.29f, 0.26f), Grass = new Color(0.34f, 0.33f, 0.22f),
                Blocks = 3, Trees = false,
            },
            CityStyle.Suburban => new CityTheme
            {
                DisplayName = name, Style = style, Buildings = Suburban,
                Ground = new Color(0.34f, 0.40f, 0.30f), Grass = new Color(0.30f, 0.44f, 0.26f),
                Blocks = 2, Trees = true,
            },
            _ => new CityTheme
            {
                DisplayName = name, Style = style, Buildings = Commercial,
                Ground = new Color(0.28f, 0.29f, 0.32f), Grass = new Color(0.28f, 0.38f, 0.26f),
                Blocks = 3, Trees = true,
            },
        };
    }
}
