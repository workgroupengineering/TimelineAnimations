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

    public string TimeLabel => $"{Time:0.00}s";

    public string ValueLabel => $"{Value:0.##}";

    public void RefreshFromModel()
    {
        Time = Model.Time;
        Value = Model.Value;
        OnPropertyChanged(nameof(TimeLabel));
        OnPropertyChanged(nameof(ValueLabel));
    }
}
