namespace TimelineAnimations.Core.Models;

public sealed class LayerCompositeSettings
{
    public LayerCompositeRole Role { get; set; } = LayerCompositeRole.Normal;

    public LayerBlendMode BlendMode { get; set; } = LayerBlendMode.Normal;

    public int MaskLayerCount { get; set; } = 1;

    public double BlurRadius { get; set; }

    public double GlowSize { get; set; } = 18d;

    public double GlowOpacity { get; set; }

    public string GlowColor { get; set; } = "#FFFFFF";

    public double ShadowBlur { get; set; }

    public double ShadowOpacity { get; set; }

    public double ShadowOffsetX { get; set; }

    public double ShadowOffsetY { get; set; }

    public string ShadowColor { get; set; } = "#09111F";

    public double TintStrength { get; set; }

    public string TintColor { get; set; } = "#FFFFFF";

    public double Brightness { get; set; }

    public double Saturation { get; set; } = 1d;

    public double ParallaxDepth { get; set; } = 1d;

    public FlashColorEffectMode FlashColorEffectMode { get; set; } = FlashColorEffectMode.None;

    public double FlashAlphaPercent { get; set; } = 100d;

    public string FlashTintColor { get; set; } = "#FFFFFF";

    public double FlashTintPercent { get; set; }

    public double FlashBrightnessPercent { get; set; }

    public double FlashRedPercent { get; set; } = 100d;

    public double FlashGreenPercent { get; set; } = 100d;

    public double FlashBluePercent { get; set; } = 100d;

    public double FlashAdvancedAlphaPercent { get; set; } = 100d;

    public double FlashRedOffset { get; set; }

    public double FlashGreenOffset { get; set; }

    public double FlashBlueOffset { get; set; }

    public double FlashAlphaOffset { get; set; }

    public LayerCompositeSettings Clone()
    {
        return new LayerCompositeSettings
        {
            Role = Role,
            BlendMode = BlendMode,
            MaskLayerCount = MaskLayerCount,
            BlurRadius = BlurRadius,
            GlowSize = GlowSize,
            GlowOpacity = GlowOpacity,
            GlowColor = GlowColor,
            ShadowBlur = ShadowBlur,
            ShadowOpacity = ShadowOpacity,
            ShadowOffsetX = ShadowOffsetX,
            ShadowOffsetY = ShadowOffsetY,
            ShadowColor = ShadowColor,
            TintStrength = TintStrength,
            TintColor = TintColor,
            Brightness = Brightness,
            Saturation = Saturation,
            ParallaxDepth = ParallaxDepth,
            FlashColorEffectMode = FlashColorEffectMode,
            FlashAlphaPercent = FlashAlphaPercent,
            FlashTintColor = FlashTintColor,
            FlashTintPercent = FlashTintPercent,
            FlashBrightnessPercent = FlashBrightnessPercent,
            FlashRedPercent = FlashRedPercent,
            FlashGreenPercent = FlashGreenPercent,
            FlashBluePercent = FlashBluePercent,
            FlashAdvancedAlphaPercent = FlashAdvancedAlphaPercent,
            FlashRedOffset = FlashRedOffset,
            FlashGreenOffset = FlashGreenOffset,
            FlashBlueOffset = FlashBlueOffset,
            FlashAlphaOffset = FlashAlphaOffset
        };
    }
}
