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
        StageZoomOverlay.FitRequested += HandleStageZoomFitRequested;
        StageZoomOverlay.ActualSizeRequested += HandleStageZoomActualSizeRequested;
        SceneCanvas.LayerSelectionRequested += HandleLayerSelectionRequested;
        SceneCanvas.LayerTransformRequested += HandleLayerTransformRequested;
        SceneCanvas.PaletteDropRequested += HandlePaletteDropRequested;
        SceneCanvas.DrawingRequested += HandleCanvasDrawingRequested;
        SceneCanvas.LayerStyleSampleRequested += HandleCanvasLayerStyleSampleRequested;
        SceneCanvas.LayerStyleApplyRequested += HandleCanvasLayerStyleApplyRequested;
        SceneCanvas.PathPointMoveRequested += HandleCanvasPathPointMoveRequested;
        SceneCanvas.PathPointSelectionRequested += HandleCanvasPathPointSelectionRequested;
        SceneCanvas.PathHandleMoveRequested += HandleCanvasPathHandleMoveRequested;
        SceneCanvas.PathPointWidthScaleRequested += HandleCanvasPathPointWidthScaleRequested;
        SceneCanvas.LassoSelectionRequested += HandleCanvasLassoSelectionRequested;
        SceneCanvas.WarpPinMoveRequested += HandleCanvasWarpPinMoveRequested;
        SceneCanvas.RigBoneMoveRequested += HandleCanvasRigBoneMoveRequested;
        SceneCanvas.LayerEraseRequested += HandleCanvasLayerEraseRequested;
        SceneCanvas.CanvasResizeRequested += HandleCanvasResizeRequested;
        SceneCanvas.TransformInteractionStateChanged += HandleCanvasInteractionStateChanged;
        SceneCanvas.PrototypeTriggerRequested += HandleCanvasPrototypeTriggerRequested;
    }

    private void HandleStageZoomFitRequested(object? sender, EventArgs e)
    {
        ResetViewport();
    }

    private void HandleStageZoomActualSizeRequested(object? sender, EventArgs e)
    {
        ZoomToActualSize();
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

    private void HandleCanvasLayerStyleSampleRequested(object? sender, CanvasLayerStyleSampleRequestedEventArgs e)
    {
        ViewModel?.SampleLayerStyle(e.LayerId);
    }

    private void HandleCanvasLayerStyleApplyRequested(object? sender, CanvasLayerStyleApplyRequestedEventArgs e)
    {
        ViewModel?.ApplyStageStyle(e.LayerId, e.ApplicationKind);
    }

    private void HandleCanvasPathPointMoveRequested(object? sender, CanvasPathPointMoveRequestedEventArgs e)
    {
        ViewModel?.UpdatePathPoint(e.LayerId, e.PointIndex, e.DocumentPoint);
    }

    private void HandleCanvasPathPointSelectionRequested(object? sender, CanvasPathPointSelectionRequestedEventArgs e)
    {
        ViewModel?.SelectPathPoint(e.LayerId, e.PointIndex);
    }

    private void HandleCanvasPathHandleMoveRequested(object? sender, CanvasPathHandleMoveRequestedEventArgs e)
    {
        ViewModel?.UpdatePathHandle(e.LayerId, e.PointIndex, e.HandleKind, e.DocumentPoint);
    }

    private void HandleCanvasPathPointWidthScaleRequested(object? sender, CanvasPathPointWidthScaleRequestedEventArgs e)
    {
        ViewModel?.UpdatePathPointWidthScale(e.LayerId, e.PointIndex, e.Scale);
    }

    private void HandleCanvasLassoSelectionRequested(object? sender, CanvasLassoSelectionRequestedEventArgs e)
    {
        ViewModel?.ApplyLassoSelection(e.LayerIds);
    }

    private void HandleCanvasWarpPinMoveRequested(object? sender, CanvasWarpPinMoveRequestedEventArgs e)
    {
        ViewModel?.UpdateWarpPin(e.LayerId, e.PinId, e.NormalizedPoint);
    }

    private void HandleCanvasRigBoneMoveRequested(object? sender, CanvasRigBoneMoveRequestedEventArgs e)
    {
        ViewModel?.UpdateRigBoneHandle(e.LayerId, e.BoneId, e.HandleKind, e.NormalizedPoint);
    }

    private void HandleCanvasLayerEraseRequested(object? sender, CanvasLayerEraseRequestedEventArgs e)
    {
        ViewModel?.ApplyCanvasErase(e.LayerId, e.DocumentPoint, e.Radius);
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
