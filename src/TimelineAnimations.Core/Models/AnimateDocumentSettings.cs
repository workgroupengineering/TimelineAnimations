namespace TimelineAnimations.Core.Models;

public sealed class AnimateDocumentSettings
{
    public AnimateDocumentType Type { get; set; } = AnimateDocumentType.Html5Canvas;

    public AnimateDocumentUnits Units { get; set; } = AnimateDocumentUnits.Pixels;

    public bool IsResponsive { get; set; } = true;

    public bool UseExternalScriptFile { get; set; } = true;

    public bool UseWebFonts { get; set; } = true;

    public bool UseAdvancedLayers { get; set; } = true;

    public string TemplateName { get; set; } = "HTML5 Canvas";

    public bool Enable3DTransforms { get; set; } = true;

    public double PerspectiveAngle { get; set; } = 55d;

    public double VanishingPointX { get; set; } = 0.5d;

    public double VanishingPointY { get; set; } = 0.5d;
}
