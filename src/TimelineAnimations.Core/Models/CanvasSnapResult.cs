namespace TimelineAnimations.Core.Models;

public readonly record struct CanvasSnapResult(
    double X,
    double Y,
    double Width,
    double Height,
    double? VerticalGuide,
    double? HorizontalGuide);
