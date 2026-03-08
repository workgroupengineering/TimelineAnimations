using CommunityToolkit.Mvvm.ComponentModel;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.ViewModels;

public partial class FrameSpanViewModel : ViewModelBase
{
    public FrameSpanViewModel(FrameSpanModel model)
    {
        Model = model;
    }

    public FrameSpanModel Model { get; }

    public Guid Id => Model.Id;

    public int StartFrame => Model.StartFrame;

    public int EndFrame => Model.EndFrame;

    public bool IsBlank => Model.IsBlank;

    public int Length => (EndFrame - StartFrame) + 1;

    [ObservableProperty]
    private bool isSelected;
}
