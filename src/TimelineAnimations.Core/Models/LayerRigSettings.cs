namespace TimelineAnimations.Core.Models;

public sealed class LayerRigSettings
{
    public bool IsEnabled { get; set; }

    public List<RigBoneModel> Bones { get; set; } = [];

    public List<RigPoseModel> Poses { get; set; } = [];
}
