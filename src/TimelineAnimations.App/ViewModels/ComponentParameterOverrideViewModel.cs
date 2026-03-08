using CommunityToolkit.Mvvm.ComponentModel;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.ViewModels;

public partial class ComponentParameterOverrideViewModel : ViewModelBase
{
    public ComponentParameterOverrideViewModel(ComponentParameterDefinition definition, ComponentParameterOverride model)
    {
        Definition = definition;
        Model = model;
        RefreshMetadata();
    }

    public ComponentParameterDefinition Definition { get; }

    public ComponentParameterOverride Model { get; }

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string subtitle = string.Empty;

    [ObservableProperty]
    private string value = string.Empty;

    public void RefreshMetadata()
    {
        Name = Model.Name;
        Value = Model.Value ?? string.Empty;
        Subtitle = string.IsNullOrWhiteSpace(Definition.TargetLayerName)
            ? $"{Definition.BindingTarget} • {Definition.Kind}"
            : $"{Definition.BindingTarget} • {Definition.Kind} • {Definition.TargetLayerName}";
    }
}
