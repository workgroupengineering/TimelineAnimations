namespace TimelineAnimations.Core.Models;

public readonly partial record struct LayerSnapshot(
    double X,
    double Y,
    double Width,
    double Height,
    double ScaleX,
    double ScaleY,
    double SkewX,
    double SkewY,
    double Rotation,
    double Opacity,
    double CornerRadius,
    string Fill,
    string Stroke,
    string Text,
    double FontSize,
    double StrokeThickness,
    bool UseGradient,
    string GradientFrom,
    string GradientTo,
    bool IsClosed,
    bool ShowAsOutline,
    string OutlineColor,
    IReadOnlyList<VectorPointModel> PathPoints,
    AvaloniaControlSettings AvaloniaControl,
    LayerCompositeSettings Compositing,
    LayerTextSettings TextSettings);

public static class LayerSnapshotDefaults
{
    public const double DefaultGradientAngle = 45d;
    public const double DefaultStrokeMiterLimit = 3d;
}

public readonly partial record struct LayerSnapshot
{
    public bool HasFill { get; init; } = true;

    public bool HasStroke { get; init; } = true;

    public LayerGradientKind GradientKind { get; init; } = LayerGradientKind.Linear;

    public double GradientAngle { get; init; } = LayerSnapshotDefaults.DefaultGradientAngle;

    public LayerStrokeCapStyle StrokeCapStyle { get; init; } = LayerStrokeCapStyle.Round;

    public LayerStrokeJoinStyle StrokeJoinStyle { get; init; } = LayerStrokeJoinStyle.Round;

    public double StrokeMiterLimit { get; init; } = LayerSnapshotDefaults.DefaultStrokeMiterLimit;
}
