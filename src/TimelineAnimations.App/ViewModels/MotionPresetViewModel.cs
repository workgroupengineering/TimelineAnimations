using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.ViewModels;

public sealed class MotionPresetViewModel : ViewModelBase
{
    public MotionPresetViewModel(MotionPresetModel model)
    {
        Model = model;
    }

    public MotionPresetModel Model { get; }

    public Guid Id => Model.Id;

    public string Name => Model.Name;

    public string Description => string.IsNullOrWhiteSpace(Model.Description)
        ? $"{Model.Tracks.Count} motion tracks"
        : Model.Description;

    public override string ToString() => Name;
}
