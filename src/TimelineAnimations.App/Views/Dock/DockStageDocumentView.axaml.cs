using Avalonia.Controls;
using TimelineAnimations.App.Controls;
using TimelineAnimations.App.ViewModels;
using TimelineAnimations.App.ViewModels.Dock;

namespace TimelineAnimations.App.Views.Dock;

public partial class DockStageDocumentView : UserControl
{
    public DockStageDocumentView()
    {
        InitializeComponent();
        HookInteractions();
    }

    private MainWindowViewModel? ViewModel => (DataContext as DockStageDocumentViewModel)?.Host;

    private void HookInteractions()
    {
        SceneCanvas.LayerSelectionRequested += HandleLayerSelectionRequested;
        SceneCanvas.LayerTransformRequested += HandleLayerTransformRequested;
        SceneCanvas.PaletteDropRequested += HandlePaletteDropRequested;
        SceneCanvas.DrawingRequested += HandleCanvasDrawingRequested;
        SceneCanvas.PathPointMoveRequested += HandleCanvasPathPointMoveRequested;
        SceneCanvas.CanvasResizeRequested += HandleCanvasResizeRequested;
        SceneCanvas.TransformInteractionStateChanged += HandleCanvasInteractionStateChanged;
        SceneCanvas.PrototypeTriggerRequested += HandleCanvasPrototypeTriggerRequested;
    }

    private void HandleLayerSelectionRequested(object? sender, CanvasLayerSelectionRequestedEventArgs e)
    {
        if (e.LayerId is Guid layerId)
        {
            ViewModel?.SelectLayer(layerId);
        }
        else
        {
            ViewModel?.DeselectKeyframe();
        }
    }

    private void HandleLayerTransformRequested(object? sender, CanvasLayerTransformRequestedEventArgs e)
    {
        ViewModel?.UpdateLayerFrame(e.LayerId, e.Bounds);
    }

    private void HandlePaletteDropRequested(object? sender, CanvasPaletteDropRequestedEventArgs e)
    {
        ViewModel?.AddLayerFromPalette(e.Kind, e.DocumentPosition, e.AvaloniaControlKind);
    }

    private void HandleCanvasDrawingRequested(object? sender, CanvasDrawingRequestedEventArgs e)
    {
        ViewModel?.CreateLayerFromCanvas(e.Tool, e.DocumentBounds, e.DocumentPoints);
    }

    private void HandleCanvasPathPointMoveRequested(object? sender, CanvasPathPointMoveRequestedEventArgs e)
    {
        ViewModel?.UpdatePathPoint(e.LayerId, e.PointIndex, e.DocumentPoint);
    }

    private void HandleCanvasResizeRequested(object? sender, CanvasResizeRequestedEventArgs e)
    {
        ViewModel?.SetCanvasSize(e.Width, e.Height);
    }

    private void HandleCanvasInteractionStateChanged(object? sender, CanvasInteractionStateChangedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        if (e.IsActive)
        {
            ViewModel.BeginInteractiveChange();
        }
        else
        {
            ViewModel.CommitInteractiveChange(string.IsNullOrWhiteSpace(e.CompletionStatusMessage) ? "Layer transformed" : e.CompletionStatusMessage);
        }
    }

    private void HandleCanvasPrototypeTriggerRequested(object? sender, CanvasPrototypeTriggerRequestedEventArgs e)
    {
        ViewModel?.HandlePrototypeTrigger(e.LayerId, e.Trigger);
    }

    private void CanvasFitClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ResetViewport();
    }

    private void CanvasActualSizeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ZoomToActualSize();
    }

    public void ResetViewport()
    {
        SceneCanvas.ResetViewport();
    }

    public void ZoomToActualSize()
    {
        SceneCanvas.ZoomToActualSize();
    }
}
