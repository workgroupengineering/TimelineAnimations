using CommunityToolkit.Mvvm.ComponentModel;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.ViewModels;

public partial class VisualStateViewModel : ViewModelBase
{
    public VisualStateViewModel(VisualStateModel model)
    {
        Model = model;
        RefreshMetadata();
    }

    public VisualStateModel Model { get; }

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
        var template = Model.Template;
        Subtitle = $"{template.Kind} • {template.Defaults.Width:0}×{template.Defaults.Height:0} • {template.Style.Fill}";
    }
}
