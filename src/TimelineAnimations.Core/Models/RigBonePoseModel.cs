namespace TimelineAnimations.Core.Models;

public sealed class RigBonePoseModel
{
    public Guid BoneId { get; set; }

    public double StartX { get; set; }

    public double StartY { get; set; }

    public double Length { get; set; }

    public double Rotation { get; set; }
}
