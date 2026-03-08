namespace TimelineAnimations.Core.Models;

public sealed class RigPoseModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "Pose";

    public double Time { get; set; }

    public List<RigBonePoseModel> Bones { get; set; } = [];
}
