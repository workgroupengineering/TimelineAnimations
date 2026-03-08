namespace TimelineAnimations.Core.Models;

public sealed class LayerCompositeSettings
{
    public LayerCompositeRole Role { get; set; } = LayerCompositeRole.Normal;

    public LayerBlendMode BlendMode { get; set; } = LayerBlendMode.Normal;

    public int MaskLayerCount { get; set; } = 1;

    public double BlurRadius { get; set; }

    public LayerFilterQuality BlurQuality { get; set; } = LayerFilterQuality.Low;

    public double GlowSize { get; set; } = 18d;

    public double GlowOpacity { get; set; }

    public string GlowColor { get; set; } = "#FFFFFF";

    public LayerFilterQuality GlowQuality { get; set; } = LayerFilterQuality.Low;

    public bool GlowKnockout { get; set; }

    public double ShadowBlur { get; set; }

    public double ShadowOpacity { get; set; }

    public double ShadowOffsetX { get; set; }

    public double ShadowOffsetY { get; set; }

    public string ShadowColor { get; set; } = "#09111F";

    public LayerFilterQuality ShadowQuality { get; set; } = LayerFilterQuality.Low;

    public bool ShadowKnockout { get; set; }

    public bool ShadowHideObject { get; set; }

    public double TintStrength { get; set; }

    public string TintColor { get; set; } = "#FFFFFF";

    public double Brightness { get; set; }

    public double Contrast { get; set; }

    public double HueShift { get; set; }

    public double Saturation { get; set; } = 1d;

    public LayerBevelType BevelType { get; set; } = LayerBevelType.Inner;

    public double BevelSize { get; set; }

    public double BevelStrength { get; set; }

    public double BevelAngle { get; set; } = 45d;

    public double BevelDistance { get; set; } = 4d;

    public string BevelHighlightColor { get; set; } = "#FFFFFF";

    public string BevelShadowColor { get; set; } = "#09111F";

    public LayerFilterQuality BevelQuality { get; set; } = LayerFilterQuality.Low;

    public bool BevelKnockout { get; set; }

    public double GradientGlowSize { get; set; }

    public double GradientGlowStrength { get; set; }

    public double GradientGlowAngle { get; set; } = 45d;

    public double GradientGlowDistance { get; set; }

    public string GradientGlowFrom { get; set; } = "#61E6FF";

    public string GradientGlowTo { get; set; } = "#09111F";

    public LayerFilterQuality GradientGlowQuality { get; set; } = LayerFilterQuality.Low;

    public bool GradientGlowKnockout { get; set; }

    public LayerBevelType GradientBevelType { get; set; } = LayerBevelType.Inner;

    public double GradientBevelSize { get; set; }

    public double GradientBevelStrength { get; set; }

    public double GradientBevelAngle { get; set; } = 45d;

    public double GradientBevelDistance { get; set; } = 4d;

    public string GradientBevelFrom { get; set; } = "#FFFFFF";

    public string GradientBevelTo { get; set; } = "#09111F";

    public LayerFilterQuality GradientBevelQuality { get; set; } = LayerFilterQuality.Low;

    public bool GradientBevelKnockout { get; set; }

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
            BlurQuality = BlurQuality,
            GlowSize = GlowSize,
            GlowOpacity = GlowOpacity,
            GlowColor = GlowColor,
            GlowQuality = GlowQuality,
            GlowKnockout = GlowKnockout,
            ShadowBlur = ShadowBlur,
            ShadowOpacity = ShadowOpacity,
            ShadowOffsetX = ShadowOffsetX,
            ShadowOffsetY = ShadowOffsetY,
            ShadowColor = ShadowColor,
            ShadowQuality = ShadowQuality,
            ShadowKnockout = ShadowKnockout,
            ShadowHideObject = ShadowHideObject,
            TintStrength = TintStrength,
            TintColor = TintColor,
            Brightness = Brightness,
            Contrast = Contrast,
            HueShift = HueShift,
            Saturation = Saturation,
            BevelType = BevelType,
            BevelSize = BevelSize,
            BevelStrength = BevelStrength,
            BevelAngle = BevelAngle,
            BevelDistance = BevelDistance,
            BevelHighlightColor = BevelHighlightColor,
            BevelShadowColor = BevelShadowColor,
            BevelQuality = BevelQuality,
            BevelKnockout = BevelKnockout,
            GradientGlowSize = GradientGlowSize,
            GradientGlowStrength = GradientGlowStrength,
            GradientGlowAngle = GradientGlowAngle,
            GradientGlowDistance = GradientGlowDistance,
            GradientGlowFrom = GradientGlowFrom,
            GradientGlowTo = GradientGlowTo,
            GradientGlowQuality = GradientGlowQuality,
            GradientGlowKnockout = GradientGlowKnockout,
            GradientBevelType = GradientBevelType,
            GradientBevelSize = GradientBevelSize,
            GradientBevelStrength = GradientBevelStrength,
            GradientBevelAngle = GradientBevelAngle,
            GradientBevelDistance = GradientBevelDistance,
            GradientBevelFrom = GradientBevelFrom,
            GradientBevelTo = GradientBevelTo,
            GradientBevelQuality = GradientBevelQuality,
            GradientBevelKnockout = GradientBevelKnockout,
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
