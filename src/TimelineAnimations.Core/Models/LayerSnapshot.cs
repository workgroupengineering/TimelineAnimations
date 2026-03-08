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
    double RotationX,
    double RotationY,
    double ZDepth,
    double Opacity,
    double CornerRadius,
    double CornerRadiusTopLeft,
    double CornerRadiusTopRight,
    double CornerRadiusBottomRight,
    double CornerRadiusBottomLeft,
    string Fill,
    string Stroke,
    string Text,
    double FontSize,
    double StrokeThickness,
    bool UseGradient,
    string GradientFrom,
    string GradientTo,
    ShapeDrawingMode DrawingMode,
    PrimitiveShapeType PrimitiveShape,
    bool IsClosed,
    double EllipseStartAngle,
    double EllipseSweepAngle,
    bool ShowAsOutline,
    string OutlineColor,
    IReadOnlyList<VectorPointModel> PathPoints,
    int PolyStarSides,
    double PolyStarInnerRadius,
    bool PolyStarIsStar,
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
    public LayerSnapshot(
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
        double CornerRadiusTopLeft,
        double CornerRadiusTopRight,
        double CornerRadiusBottomRight,
        double CornerRadiusBottomLeft,
        string Fill,
        string Stroke,
        string Text,
        double FontSize,
        double StrokeThickness,
        bool UseGradient,
        string GradientFrom,
        string GradientTo,
        ShapeDrawingMode DrawingMode,
        PrimitiveShapeType PrimitiveShape,
        bool IsClosed,
        double EllipseStartAngle,
        double EllipseSweepAngle,
        bool ShowAsOutline,
        string OutlineColor,
        IReadOnlyList<VectorPointModel> PathPoints,
        int PolyStarSides,
        double PolyStarInnerRadius,
        bool PolyStarIsStar,
        AvaloniaControlSettings AvaloniaControl,
        LayerCompositeSettings Compositing,
        LayerTextSettings TextSettings)
        : this(
            X,
            Y,
            Width,
            Height,
            ScaleX,
            ScaleY,
            SkewX,
            SkewY,
            Rotation,
            0d,
            0d,
            0d,
            Opacity,
            CornerRadius,
            CornerRadiusTopLeft,
            CornerRadiusTopRight,
            CornerRadiusBottomRight,
            CornerRadiusBottomLeft,
            Fill,
            Stroke,
            Text,
            FontSize,
            StrokeThickness,
            UseGradient,
            GradientFrom,
            GradientTo,
            DrawingMode,
            PrimitiveShape,
            IsClosed,
            EllipseStartAngle,
            EllipseSweepAngle,
            ShowAsOutline,
            OutlineColor,
            PathPoints,
            PolyStarSides,
            PolyStarInnerRadius,
            PolyStarIsStar,
            AvaloniaControl,
            Compositing,
            TextSettings)
    {
    }

    public bool HasFill { get; init; } = true;

    public bool HasStroke { get; init; } = true;

    public LayerGradientKind GradientKind { get; init; } = LayerGradientKind.Linear;

    public double GradientAngle { get; init; } = LayerSnapshotDefaults.DefaultGradientAngle;

    public double GradientCenterX { get; init; } = 0.5d;

    public double GradientCenterY { get; init; } = 0.5d;

    public double GradientScaleX { get; init; } = 1d;

    public double GradientScaleY { get; init; } = 1d;

    public LayerStrokeCapStyle StrokeCapStyle { get; init; } = LayerStrokeCapStyle.Round;

    public LayerStrokeJoinStyle StrokeJoinStyle { get; init; } = LayerStrokeJoinStyle.Round;

    public double StrokeMiterLimit { get; init; } = LayerSnapshotDefaults.DefaultStrokeMiterLimit;
}
