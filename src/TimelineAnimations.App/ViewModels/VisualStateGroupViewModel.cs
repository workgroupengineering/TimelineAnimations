using CommunityToolkit.Mvvm.ComponentModel;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.ViewModels;

public partial class VisualStateGroupViewModel : ViewModelBase
{
    public VisualStateGroupViewModel(VisualStateGroupModel model)
    {
        Model = model;
        RefreshMetadata();
    }

    public VisualStateGroupModel Model { get; }

    public Guid Id => Model.Id;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string subtitle = string.Empty;

    [ObservableProperty]
    private bool isSelected;

    public void RefreshMetadata()
    {
        Name = Model.Name;
        Subtitle = string.IsNullOrWhiteSpace(Model.ActiveStateName)
            ? $"{Model.States.Count} states"
            : $"{Model.States.Count} states • active {Model.ActiveStateName}";
    }
}
