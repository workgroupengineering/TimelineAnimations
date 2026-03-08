namespace TimelineAnimations.Core.Models;

public sealed class VectorPointModel
{
    public double X { get; set; }

    public double Y { get; set; }

    public double? InHandleX { get; set; }

    public double? InHandleY { get; set; }

    public double? OutHandleX { get; set; }

    public double? OutHandleY { get; set; }

    public VectorHandleMode HandleMode { get; set; } = VectorHandleMode.Corner;

    public double StrokeWidthScale { get; set; } = 1d;
}
