using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.ViewModels;

public sealed class RigPoseViewModel(RigPoseModel model)
{
    public RigPoseModel Model { get; } = model;

    public Guid Id => Model.Id;

    public string Name => Model.Name;

    public double Time => Model.Time;

    public string Summary => $"{Model.Name} • {Model.Time:0.00}s";

    public override string ToString()
    {
        return Summary;
    }
}
