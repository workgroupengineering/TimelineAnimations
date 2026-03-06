namespace TimelineAnimations.Core.Models;

public sealed class LayerStyle
{
    public string Fill { get; set; } = "#4FE1FF";

    public string Stroke { get; set; } = "#FFFFFF";

    public double StrokeThickness { get; set; } = 1.6d;

    public string Text { get; set; } = string.Empty;

    public double FontSize { get; set; } = 42;

    public double CornerRadius { get; set; } = 28;

    public bool UseGradient { get; set; }

    public string GradientFrom { get; set; } = "#4FE1FF";

    public string GradientTo { get; set; } = "#FFFFFF";

    public bool IsClosed { get; set; }

    public List<VectorPointModel> PathPoints { get; set; } = [];
}
