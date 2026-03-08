namespace TimelineAnimations.Core.Models;

public sealed class LayerDefaults
{
    public double X { get; set; }

    public double Y { get; set; }

    public double Width { get; set; } = 160;

    public double Height { get; set; } = 120;

    public double ScaleX { get; set; } = 1d;

    public double ScaleY { get; set; } = 1d;

    public double SkewX { get; set; }

    public double SkewY { get; set; }

    public double Rotation { get; set; }

    public double RotationX { get; set; }

    public double RotationY { get; set; }

    public double ZDepth { get; set; }

    public double Opacity { get; set; } = 1;
}
