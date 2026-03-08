namespace TimelineAnimations.Core.Models;

public sealed class RigBoneModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "Bone";

    public Guid? ParentBoneId { get; set; }

    public double BindStartX { get; set; }

    public double BindStartY { get; set; }

    public double StartX { get; set; }

    public double StartY { get; set; }

    public double Length { get; set; } = 0.32d;

    public double Rotation { get; set; }

    public double Envelope { get; set; } = 0.28d;
}
