namespace TimelineAnimations.Core.Models;

public sealed class KeyframeModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public double Time { get; set; }

    public double Value { get; set; }
}
