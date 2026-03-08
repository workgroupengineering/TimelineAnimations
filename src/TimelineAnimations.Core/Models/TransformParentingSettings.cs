namespace TimelineAnimations.Core.Models;

public sealed class TransformParentingSettings
{
    public Guid? ParentLayerId { get; set; }

    public double BindParentX { get; set; }

    public double BindParentY { get; set; }

    public double BindParentScaleX { get; set; } = 1d;

    public double BindParentScaleY { get; set; } = 1d;

    public double BindParentRotation { get; set; }

    public double BindParentOpacity { get; set; } = 1d;

    public double BindChildX { get; set; }

    public double BindChildY { get; set; }
}
