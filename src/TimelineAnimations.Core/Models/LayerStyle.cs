namespace TimelineAnimations.Core.Models;

public sealed class LayerStyle
{
    public bool HasFill { get; set; } = true;

    public string Fill { get; set; } = "#4FE1FF";

    public bool HasStroke { get; set; } = true;

    public string Stroke { get; set; } = "#FFFFFF";

    public double StrokeThickness { get; set; } = 1.6d;

    public LayerStrokeCapStyle StrokeCapStyle { get; set; } = LayerStrokeCapStyle.Round;

    public LayerStrokeJoinStyle StrokeJoinStyle { get; set; } = LayerStrokeJoinStyle.Round;

    public double StrokeMiterLimit { get; set; } = 3d;

    public string Text { get; set; } = string.Empty;

    public double FontSize { get; set; } = 42;

    public LayerTextSettings TextSettings { get; set; } = new();

    public double CornerRadius { get; set; } = 28;

    public bool UseGradient { get; set; }

    public LayerGradientKind GradientKind { get; set; } = LayerGradientKind.Linear;

    public double GradientAngle { get; set; } = 45d;

    public string GradientFrom { get; set; } = "#4FE1FF";

    public string GradientTo { get; set; } = "#FFFFFF";

    public bool IsClosed { get; set; }

    public List<VectorPointModel> PathPoints { get; set; } = [];

    public AvaloniaControlSettings AvaloniaControl { get; set; } = new();
}
