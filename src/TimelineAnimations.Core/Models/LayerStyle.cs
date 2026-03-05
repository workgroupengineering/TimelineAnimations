namespace TimelineAnimations.Core.Models;

public sealed class LayerStyle
{
    public string Fill { get; set; } = "#4FE1FF";

    public string Stroke { get; set; } = "#FFFFFF";

    public string Text { get; set; } = string.Empty;

    public double FontSize { get; set; } = 42;

    public double CornerRadius { get; set; } = 28;
}
