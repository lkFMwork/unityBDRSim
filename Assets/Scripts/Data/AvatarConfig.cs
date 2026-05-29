using System;
using UnityEngine;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// Palette-and-index description of a BDR's look. Stores only indices/scalars
    /// (no textures or meshes) so it serializes trivially and is rendered by the
    /// avatar builder against <see cref="AvatarPalette"/>. Swap the builder for
    /// real rigged models later without changing this data.
    /// </summary>
    [Serializable]
    public class AvatarConfig
    {
        public int skinTone;     // index into AvatarPalette.SkinTones
        public int outfitColor;  // index into AvatarPalette.Outfits
        public int accentColor;  // index into AvatarPalette.Accents (tie / lanyard)
        public int hairColor;    // index into AvatarPalette.HairColors
        public int build = 1;    // 0 = slim, 1 = average, 2 = broad
        [Range(0.9f, 1.12f)] public float height = 1.0f;

        public AvatarConfig Clone() => new AvatarConfig
        {
            skinTone = skinTone,
            outfitColor = outfitColor,
            accentColor = accentColor,
            hairColor = hairColor,
            build = build,
            height = height
        };

        public static string BuildName(int build) => build switch
        {
            0 => "Slim",
            2 => "Broad",
            _ => "Average"
        };
    }

    /// <summary>Fixed color palettes the avatar builder draws from. No assets required.</summary>
    public static class AvatarPalette
    {
        public static readonly Color[] SkinTones =
        {
            new Color(0.96f, 0.84f, 0.72f),
            new Color(0.89f, 0.72f, 0.57f),
            new Color(0.76f, 0.57f, 0.42f),
            new Color(0.56f, 0.40f, 0.29f),
            new Color(0.36f, 0.25f, 0.18f),
        };

        public static readonly Color[] Outfits =
        {
            new Color(0.16f, 0.22f, 0.34f), // navy
            new Color(0.20f, 0.20f, 0.22f), // charcoal
            new Color(0.45f, 0.18f, 0.20f), // maroon
            new Color(0.16f, 0.33f, 0.28f), // forest
            new Color(0.62f, 0.62f, 0.66f), // grey
            new Color(0.30f, 0.26f, 0.42f), // plum
        };

        public static readonly Color[] Accents =
        {
            new Color(0.22f, 0.55f, 0.95f), // blue
            new Color(0.92f, 0.66f, 0.20f), // amber
            new Color(0.85f, 0.30f, 0.30f), // red
            new Color(0.28f, 0.70f, 0.45f), // green
            new Color(0.85f, 0.85f, 0.90f), // white
            new Color(0.55f, 0.35f, 0.75f), // purple
        };

        public static readonly Color[] HairColors =
        {
            new Color(0.10f, 0.08f, 0.07f), // black
            new Color(0.30f, 0.20f, 0.12f), // brown
            new Color(0.62f, 0.45f, 0.22f), // dark blond
            new Color(0.80f, 0.72f, 0.45f), // blond
            new Color(0.55f, 0.20f, 0.12f), // auburn
            new Color(0.72f, 0.72f, 0.74f), // grey
        };

        private static Color At(Color[] arr, int i) => arr[((i % arr.Length) + arr.Length) % arr.Length];

        public static Color Skin(int i) => At(SkinTones, i);
        public static Color Outfit(int i) => At(Outfits, i);
        public static Color Accent(int i) => At(Accents, i);
        public static Color Hair(int i) => At(HairColors, i);

        public static int SkinCount => SkinTones.Length;
        public static int OutfitCount => Outfits.Length;
        public static int AccentCount => Accents.Length;
        public static int HairCount => HairColors.Length;
    }
}
