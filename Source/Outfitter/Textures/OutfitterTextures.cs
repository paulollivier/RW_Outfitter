using UnityEngine;
using Verse;

namespace Outfitter.Textures
{
    [StaticConstructorOnStartup]
    internal static class OutfitterTextures
    {
        public static readonly Texture2D AddButton = ContentFinder<Texture2D>.Get("add");

        public static readonly Texture2D BgColor =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.2f, 1));

        public static readonly Texture2D DeleteButton = ContentFinder<Texture2D>.Get("delete");

        public static readonly Texture2D Drop = ContentFinder<Texture2D>.Get("UI/Buttons/Drop");

        public static readonly Texture2D FloatRangeSliderTex = ContentFinder<Texture2D>.Get("UI/Widgets/RangeSlider");

        public static readonly Texture2D Info = ContentFinder<Texture2D>.Get("UI/Buttons/InfoButton");

        public static readonly Texture2D ResetButton = ContentFinder<Texture2D>.Get("reset");

        public static readonly Texture2D White = SolidColorMaterials.NewSolidColorTexture(Color.white);
    }
}