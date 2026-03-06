using CommunityToolkit.Mvvm.ComponentModel;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.ViewModels;

public partial class KeyframeViewModel : ViewModelBase
{
    public KeyframeViewModel(KeyframeModel model)
    {
        Model = model;
        RefreshFromModel();
    }

    public KeyframeModel Model { get; }

    public Guid Id => Model.Id;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private double time;

    [ObservableProperty]
    private double value;

    [ObservableProperty]
    private EasingKind easing;

    [ObservableProperty]
    private double controlPoint1X;

    [ObservableProperty]
    private double controlPoint1Y;

    [ObservableProperty]
    private double controlPoint2X;

    [ObservableProperty]
    private double controlPoint2Y;

    public string TimeLabel => $"{Time:0.00}s";

    public string ValueLabel => $"{Value:0.##}";

    public bool IsCustomEasing => Easing == EasingKind.Custom;

    public string EasingLabel => Easing == EasingKind.Custom ? "Custom Curve" : Easing.ToString();

    public void RefreshFromModel()
    {
        Time = Model.Time;
        Value = Model.Value;
        Easing = Model.Easing;
        ControlPoint1X = Model.ControlPoint1X;
        ControlPoint1Y = Model.ControlPoint1Y;
        ControlPoint2X = Model.ControlPoint2X;
        ControlPoint2Y = Model.ControlPoint2Y;
        OnPropertyChanged(nameof(TimeLabel));
        OnPropertyChanged(nameof(ValueLabel));
        OnPropertyChanged(nameof(EasingLabel));
        OnPropertyChanged(nameof(IsCustomEasing));
    }
}
