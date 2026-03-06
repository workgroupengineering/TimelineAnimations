namespace TimelineAnimations.Core.Models;

public readonly record struct LayerSnapshot(
    double X,
    double Y,
    double Width,
    double Height,
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
    IReadOnlyList<VectorPointModel> PathPoints,
    LayerCompositeSettings Compositing);
