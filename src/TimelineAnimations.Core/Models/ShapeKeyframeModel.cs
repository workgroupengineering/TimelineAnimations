namespace TimelineAnimations.Core.Models;

public sealed class ShapeKeyframeModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public double Time { get; set; }

    public EasingKind Easing { get; set; } = EasingKind.Linear;

    public double ControlPoint1X { get; set; } = 0.25d;

    public double ControlPoint1Y { get; set; } = 0.1d;

    public double ControlPoint2X { get; set; } = 0.25d;

    public double ControlPoint2Y { get; set; } = 1d;

    public bool IsClosed { get; set; }

    public List<VectorPointModel> PathPoints { get; set; } = [];
}
