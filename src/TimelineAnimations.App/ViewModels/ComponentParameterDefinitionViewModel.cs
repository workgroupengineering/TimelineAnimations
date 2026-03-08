using CommunityToolkit.Mvvm.ComponentModel;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.ViewModels;

public partial class ComponentParameterDefinitionViewModel : ViewModelBase
{
    public ComponentParameterDefinitionViewModel(ComponentParameterDefinition model)
    {
        Model = model;
        RefreshMetadata();
    }

    public ComponentParameterDefinition Model { get; }

    public Guid Id => Model.Id;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string subtitle = string.Empty;

    [ObservableProperty]
    private string defaultValue = string.Empty;

    public void RefreshMetadata()
    {
        Name = Model.Name;
        DefaultValue = Model.DefaultValue ?? string.Empty;
        Subtitle = string.IsNullOrWhiteSpace(Model.TargetLayerName)
            ? $"{Model.BindingTarget} • {Model.Kind}"
            : $"{Model.BindingTarget} • {Model.Kind} • {Model.TargetLayerName}";
    }
}
