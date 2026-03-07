using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
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
        var markerLabel = Model.Markers.Count == 0 ? "no markers" : $"{Model.Markers.Count} markers";
        var transitionLabel = Model.OutgoingTransition.Kind == SceneTransitionKind.None
            ? "cut"
            : $"{Model.OutgoingTransition.Kind} {Model.OutgoingTransition.Duration:0.00}s";
        Subtitle = $"{Model.Layers.Count} layers • {Model.Duration:0.##} s • {Model.FrameRate:0.#} fps • {Model.CanvasWidth:0}×{Model.CanvasHeight:0} • {markerLabel} • {transitionLabel}";
        PreviewBrush = BuildPreviewBrush(Model);
    }

    private static IBrush BuildPreviewBrush(SceneModel model)
    {
        return new ImmutableLinearGradientBrush(
            [
                new ImmutableGradientStop(0, ColorHelpers.Parse(model.BackgroundFrom, "#09111F")),
                new ImmutableGradientStop(1, ColorHelpers.Parse(model.BackgroundTo, "#182748"))
            ],
            opacity: 1d,
            startPoint: new RelativePoint(0, 0, RelativeUnit.Relative),
            endPoint: new RelativePoint(1, 1, RelativeUnit.Relative));
    }
}
