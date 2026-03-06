using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using TimelineAnimations.App.Helpers;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.ViewModels;

public partial class SceneViewModel : ViewModelBase
{
    public SceneViewModel(SceneModel model)
    {
        Model = model;
        previewBrush = BuildPreviewBrush(model);
        RefreshMetadata();
    }

    public SceneModel Model { get; }

    public Guid Id => Model.Id;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string subtitle = string.Empty;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private IBrush previewBrush;

    public void RefreshMetadata()
    {
        Name = Model.Name;
        Subtitle = $"{Model.Layers.Count} layers • {Model.Duration:0.##} s • {Model.FrameRate:0.#} fps • {Model.CanvasWidth:0}×{Model.CanvasHeight:0}";
        PreviewBrush = BuildPreviewBrush(Model);
    }

    private static IBrush BuildPreviewBrush(SceneModel model)
    {
        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops =
            [
                new GradientStop(ColorHelpers.Parse(model.BackgroundFrom, "#09111F"), 0),
                new GradientStop(ColorHelpers.Parse(model.BackgroundTo, "#182748"), 1)
            ]
        };
    }
}
