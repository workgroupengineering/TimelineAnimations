namespace TimelineAnimations.Core.Models;

public sealed class WarpPinModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public double BindX { get; set; }

    public double BindY { get; set; }

    public double X { get; set; }

    public double Y { get; set; }

    public double Radius { get; set; } = 0.36d;

    public double Strength { get; set; } = 1d;
}
