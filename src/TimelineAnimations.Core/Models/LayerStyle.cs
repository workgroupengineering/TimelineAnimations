namespace TimelineAnimations.Core.Models;

public sealed class LayerStyle
{
    public ShapeDrawingMode DrawingMode { get; set; } = ShapeDrawingMode.Merge;

    public PrimitiveShapeType PrimitiveShape { get; set; }

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

    public double CornerRadiusTopLeft { get; set; } = 28;

    public double CornerRadiusTopRight { get; set; } = 28;

    public double CornerRadiusBottomRight { get; set; } = 28;

    public double CornerRadiusBottomLeft { get; set; } = 28;

    public bool UseGradient { get; set; }

    public LayerGradientKind GradientKind { get; set; } = LayerGradientKind.Linear;

    public double GradientAngle { get; set; } = 45d;

    public double GradientCenterX { get; set; } = 0.5d;

    public double GradientCenterY { get; set; } = 0.5d;

    public double GradientScaleX { get; set; } = 1d;

    public double GradientScaleY { get; set; } = 1d;

    public string GradientFrom { get; set; } = "#4FE1FF";

    public string GradientTo { get; set; } = "#FFFFFF";

    public bool IsClosed { get; set; }

    public double EllipseStartAngle { get; set; }

    public double EllipseSweepAngle { get; set; } = 360d;

    public List<VectorPointModel> PathPoints { get; set; } = [];

    public int PolyStarSides { get; set; } = 5;

    public double PolyStarInnerRadius { get; set; } = 0.46d;

    public bool PolyStarIsStar { get; set; } = true;

    public AvaloniaControlSettings AvaloniaControl { get; set; } = new();
}
