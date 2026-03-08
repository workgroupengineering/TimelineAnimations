using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using TimelineAnimations.App.Controls;
using TimelineAnimations.App.Models;
using TimelineAnimations.App.Services;
using TimelineAnimations.App.ViewModels;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowNativeMenuFactory _nativeMenuFactory = new();
    private bool _isTimelineResizeDragging;
    private NativeMenu? _attachedNativeMenu;
    private MainWindowViewModel? _attachedNativeMenuViewModel;
    private Point _timelineResizeOrigin;
    private double _timelineResizeHeightOrigin;

    public MainWindow()
    {
        InitializeComponent();
        HookInteractions();
        Opened += HandleOpened;
        Closing += HandleClosing;
        DataContextChanged += HandleDataContextChanged;
        PropertyChanged += HandleWindowPropertyChanged;
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private void HandleClosing(object? sender, WindowClosingEventArgs e)
    {
        ViewModel?.DockWorkspace.SaveLayout();
    }

    private void HandleOpened(object? sender, EventArgs e)
    {
        AttachNativeMenuIfPossible();
        SyncNativeMenuExportedState();
    }

    private void HandleDataContextChanged(object? sender, EventArgs e)
    {
        AttachNativeMenuIfPossible();
        SyncNativeMenuExportedState();
    }

    private void HandleWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == NativeMenu.IsNativeMenuExportedProperty &&
            e.NewValue is bool isExported)
        {
            HandleNativeMenuExportedChanged(isExported);
        }
    }

    private void HandleNativeMenuExportedChanged(bool isExported)
    {
        ViewModel?.SetNativeMenuExported(isExported);
    }

    private void SyncNativeMenuExportedState()
    {
        ViewModel?.SetNativeMenuExported(NativeMenu.GetIsNativeMenuExported(this));
    }

    private void AttachNativeMenuIfPossible()
    {
        if (ViewModel is null)
        {
            return;
        }

        if (ReferenceEquals(ViewModel, _attachedNativeMenuViewModel) && _attachedNativeMenu is not null)
        {
            return;
        }

        _attachedNativeMenu = _nativeMenuFactory.Create(
            ViewModel,
            new MainWindowNativeMenuActions(
                OpenDocumentAsync,
                SaveDocumentAsync,
                OpenFlashXflFolderAsync,
                SaveFlashXflFolderAsync,
                ConvertFlashFlaToXflFolderAsync,
                ConvertFlashXflFolderToFlaAsync,
                ImportAnimationFormatAsync,
                ExportAnimationFormatAsync,
                ImportAudioAsync,
                ImportVideoAsync,
                PreviewPublishProfile,
                ExportPublishProfileAsync,
                ExportFrameAsync,
                RenderSequenceAsync,
                RenderProjectAsync,
                () => SceneCanvas.ResetViewport(),
                () => SceneCanvas.ZoomToActualSize(),
                () => StorageProvider.CanOpen,
                () => StorageProvider.CanSave,
                () => StorageProvider.CanPickFolder));
        _attachedNativeMenuViewModel = ViewModel;
        NativeMenu.SetMenu(this, _attachedNativeMenu);
    }

    private void HookInteractions()
    {
        ClassicStageZoomOverlay.FitRequested += HandleClassicStageZoomFitRequested;
        ClassicStageZoomOverlay.ActualSizeRequested += HandleClassicStageZoomActualSizeRequested;
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

        TimelineEditorHeader.ScrubRequested += HandleTimelineScrubRequested;
        TimelineEditorHeader.KeyframeInteractionStateChanged += HandleTimelineInteractionStateChanged;
        TimelineEditorBody.ScrubRequested += HandleTimelineScrubRequested;
        TimelineEditorBody.LayerSelectionRequested += HandleTimelineLayerSelectionRequested;
        TimelineEditorBody.TrackSelectionRequested += HandleTrackSelectionRequested;
        TimelineEditorBody.HierarchyToggleRequested += HandleTimelineHierarchyToggleRequested;
        TimelineEditorBody.KeyframeSelectionRequested += HandleKeyframeSelectionRequested;
        TimelineEditorBody.KeyframeMoveRequested += HandleKeyframeMoveRequested;
        TimelineEditorBody.KeyframeAddRequested += HandleKeyframeAddRequested;
        TimelineEditorBody.KeyframeInteractionStateChanged += HandleTimelineInteractionStateChanged;

        FrameTimelineHeader.FrameRequested += HandleFrameRequested;
        FrameTimelineHeader.RulerInteractionRequested += HandleFrameRulerInteractionRequested;
        FrameTimelineHeader.InteractionStateChanged += HandleFrameInteractionStateChanged;
        FrameTimelineBody.FrameRequested += HandleFrameRequested;
        FrameTimelineBody.LayerSelectionRequested += HandleFrameLayerSelectionRequested;
        FrameTimelineBody.RangeSelectionRequested += HandleFrameRangeSelectionRequested;
        FrameTimelineBody.HierarchyToggleRequested += HandleFrameHierarchyToggleRequested;
        FrameTimelineBody.InteractionStateChanged += HandleFrameInteractionStateChanged;
    }

    private void HandleClassicStageZoomFitRequested(object? sender, EventArgs e)
    {
        SceneCanvas.ResetViewport();
    }

    private void HandleClassicStageZoomActualSizeRequested(object? sender, EventArgs e)
    {
        SceneCanvas.ZoomToActualSize();
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
            ViewModel.BeginInteractiveChange(InteractiveChangeKind.KeyframeDrag);
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

    private void HandleTimelineScrubRequested(object? sender, TimelineScrubRequestedEventArgs e)
    {
        ViewModel?.Scrub(e.Time);
    }

    private void HandleTrackSelectionRequested(object? sender, TimelineTrackSelectionRequestedEventArgs e)
    {
        ViewModel?.SelectTrack(e.LayerId, e.Property);
    }

    private void HandleTimelineLayerSelectionRequested(object? sender, TimelineLayerSelectionRequestedEventArgs e)
    {
        ViewModel?.SelectLayer(e.LayerId);
    }

    private void HandleTimelineHierarchyToggleRequested(object? sender, TimelineHierarchyToggleRequestedEventArgs e)
    {
        ViewModel?.ToggleTimelineHierarchy(e.LayerId);
    }

    private void HandleKeyframeSelectionRequested(object? sender, TimelineKeyframeSelectionRequestedEventArgs e)
    {
        ViewModel?.SelectKeyframe(e.LayerId, e.Property, e.KeyframeId);
    }

    private void HandleKeyframeMoveRequested(object? sender, TimelineKeyframeMoveRequestedEventArgs e)
    {
        ViewModel?.MoveKeyframe(e.LayerId, e.Property, e.KeyframeId, e.Time);
    }

    private void HandleKeyframeAddRequested(object? sender, TimelineKeyframeAddRequestedEventArgs e)
    {
        ViewModel?.AddKeyframeAt(e.LayerId, e.Property, e.Time);
    }

    private void HandleTimelineInteractionStateChanged(object? sender, TimelineInteractionStateChangedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        if (e.IsActive)
        {
            var kind = e.InteractionKind == TimelineEditorInteractionKind.Scrub
                ? InteractiveChangeKind.TimelineNavigation
                : InteractiveChangeKind.KeyframeDrag;
            ViewModel.BeginInteractiveChange(kind);
        }
        else
        {
            var statusMessage = e.InteractionKind == TimelineEditorInteractionKind.Scrub
                ? "Timeline scrubbed"
                : "Keyframe moved";
            ViewModel.CommitInteractiveChange(statusMessage);
        }
    }

    private void HandleFrameRequested(object? sender, FrameTimelineFrameRequestedEventArgs e)
    {
        ViewModel?.SeekFrame(e.Frame);
    }

    private void HandleFrameLayerSelectionRequested(object? sender, FrameTimelineLayerSelectionRequestedEventArgs e)
    {
        ViewModel?.SelectLayer(e.LayerId);
    }

    private void HandleFrameRangeSelectionRequested(object? sender, FrameTimelineRangeSelectionRequestedEventArgs e)
    {
        ViewModel?.SelectFrameRange(e.LayerId, e.StartFrame, e.EndFrame);
    }

    private void HandleFrameHierarchyToggleRequested(object? sender, FrameTimelineHierarchyToggleRequestedEventArgs e)
    {
        ViewModel?.ToggleFrameHierarchy(e.LayerId);
    }

    private void HandleFrameRulerInteractionRequested(object? sender, FrameTimelineRulerInteractionRequestedEventArgs e)
    {
        ViewModel?.ApplyFrameRulerInteraction(e.InteractionKind, e.Frame);
    }

    private void HandleFrameInteractionStateChanged(object? sender, FrameTimelineInteractionStateChangedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        if (e.IsActive)
        {
            var kind = e.InteractionKind == FrameTimelineInteractionKind.PlayheadScrub
                ? InteractiveChangeKind.TimelineNavigation
                : InteractiveChangeKind.FrameTimelineDrag;
            ViewModel.BeginInteractiveChange(kind);
        }
        else
        {
            var statusMessage = e.InteractionKind == FrameTimelineInteractionKind.PlayheadScrub
                ? "Timeline scrubbed"
                : "Frame range updated";
            ViewModel.CommitInteractiveChange(statusMessage);
        }
    }

    private void CanvasFitClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SceneCanvas.ResetViewport();
    }

    private void CanvasActualSizeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SceneCanvas.ZoomToActualSize();
    }

    private void TimelineResizeGripPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _isTimelineResizeDragging = true;
        _timelineResizeOrigin = e.GetPosition(this);
        _timelineResizeHeightOrigin = ViewModel.TimelineDockHeight.Value;
        e.Pointer.Capture(sender as IInputElement);
        e.Handled = true;
    }

    private void TimelineResizeGripPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isTimelineResizeDragging || ViewModel is null)
        {
            return;
        }

        var deltaY = e.GetPosition(this).Y - _timelineResizeOrigin.Y;
        ViewModel.SetTimelineDockHeightPixels(_timelineResizeHeightOrigin - deltaY);
        e.Handled = true;
    }

    private void TimelineResizeGripPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isTimelineResizeDragging)
        {
            return;
        }

        _isTimelineResizeDragging = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void TimelineResizeGripPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _isTimelineResizeDragging = false;
    }

    private async void OpenDocumentClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await OpenDocumentAsync();
    }

    private async Task OpenDocumentAsync()
    {
        if (ViewModel is null || !StorageProvider.CanOpen)
        {
            return;
        }

        var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open timeline document or animation",
            AllowMultiple = false,
            FileTypeFilter = BuildSupportedDocumentFileTypes(),
            SuggestedFileType = BuildNativeProjectFileType()
        });

        var file = result.FirstOrDefault();
        if (file is null)
        {
            return;
        }

        try
        {
            await using var stream = await file.OpenReadAsync();
            var loaded = await TimelineDocumentFileService.LoadAsync(stream, file.Name);
            ViewModel.LoadDocument(loaded.Document, file.Name, loaded.Format);
            ViewModel.ApplyAnimationExchangeResult(loaded.Summary, loaded.Issues);
            SceneCanvas.ResetViewport();
        }
        catch (Exception exception)
        {
            ViewModel.StatusMessage = $"Open failed: {exception.Message}";
        }
    }

    private async void SaveDocumentClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await SaveDocumentAsync();
    }

    private async Task SaveDocumentAsync()
    {
        if (ViewModel is null || !StorageProvider.CanSave)
        {
            return;
        }

        var documentFormat = ViewModel.CurrentDocumentFileFormat;
        var fileType = BuildDocumentFileType(documentFormat);

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"Save {TimelineDocumentFileService.GetDisplayName(documentFormat)}",
            SuggestedFileName = TimelineDocumentFileService.GetSuggestedFileName(ViewModel.DocumentName, documentFormat),
            DefaultExtension = TimelineDocumentFileService.GetSuggestedExtension(documentFormat),
            FileTypeChoices = [fileType],
            SuggestedFileType = fileType,
            ShowOverwritePrompt = true
        });

        if (file is null)
        {
            return;
        }

        await using var stream = await file.OpenWriteAsync();
        if (stream.CanSeek)
        {
            stream.SetLength(0);
        }

        var result = await TimelineDocumentFileService.SaveAsync(stream, ViewModel.CreateExportDocumentSnapshot(), documentFormat, file.Name);
        ViewModel.SetDocumentLabel(file.Name, result.Format);
        ViewModel.ApplyAnimationExchangeResult($"{result.Summary} → {file.Name}", result.Issues);
    }

    private async void OpenFlashXflFolderClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await OpenFlashXflFolderAsync();
    }

    private async Task OpenFlashXflFolderAsync()
    {
        if (ViewModel is null || !StorageProvider.CanPickFolder)
        {
            return;
        }

        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open Flash XFL folder",
            AllowMultiple = false
        });

        var folder = result.FirstOrDefault();
        var folderPath = folder?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            ViewModel.StatusMessage = "Opening Flash XFL folders requires a local folder.";
            return;
        }

        try
        {
            var loaded = await TimelineDocumentFileService.LoadFlashFolderAsync(folderPath);
            ViewModel.LoadDocument(loaded.Document, Path.GetFileName(folderPath), loaded.Format);
            ViewModel.ApplyAnimationExchangeResult($"{loaded.Summary} → {Path.GetFileName(folderPath)}", loaded.Issues);
            SceneCanvas.ResetViewport();
        }
        catch (Exception exception)
        {
            ViewModel.StatusMessage = $"Open XFL folder failed: {exception.Message}";
        }
    }

    private async void SaveFlashXflFolderClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await SaveFlashXflFolderAsync();
    }

    private async Task SaveFlashXflFolderAsync()
    {
        if (ViewModel is null || !StorageProvider.CanPickFolder)
        {
            return;
        }

        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose parent folder for Flash XFL export",
            AllowMultiple = false
        });

        var folder = result.FirstOrDefault();
        var folderPath = folder?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            ViewModel.StatusMessage = "Saving Flash XFL folders requires a local parent folder.";
            return;
        }

        try
        {
            var saved = await TimelineDocumentFileService.SaveFlashFolderAsync(folderPath, ViewModel.CreateExportDocumentSnapshot());
            ViewModel.ApplyAnimationExchangeResult(saved.Summary, saved.Issues);
        }
        catch (Exception exception)
        {
            ViewModel.StatusMessage = $"Save XFL folder failed: {exception.Message}";
        }
    }

    private async void ConvertFlashFlaToXflFolderClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ConvertFlashFlaToXflFolderAsync();
    }

    private async Task ConvertFlashFlaToXflFolderAsync()
    {
        if (ViewModel is null || !StorageProvider.CanOpen || !StorageProvider.CanPickFolder)
        {
            return;
        }

        var flashType = new FilePickerFileType("Flash Authoring Archive")
        {
            Patterns = ["*.fla"],
            MimeTypes = ["application/octet-stream", "application/zip"]
        };

        var sourceResult = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose Flash FLA archive",
            AllowMultiple = false,
            FileTypeFilter = [flashType],
            SuggestedFileType = flashType
        });

        var sourceFile = sourceResult.FirstOrDefault();
        if (sourceFile is null)
        {
            return;
        }

        var folderResult = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose output parent folder for XFL conversion",
            AllowMultiple = false
        });

        var parentFolder = folderResult.FirstOrDefault();
        var parentFolderPath = parentFolder?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(parentFolderPath))
        {
            ViewModel.StatusMessage = "Converting to XFL requires a local output folder.";
            return;
        }

        try
        {
            await using var stream = await sourceFile.OpenReadAsync();
            var conversion = await TimelineDocumentFileService.ConvertFlashArchiveToFolderAsync(stream, sourceFile.Name, parentFolderPath);
            ViewModel.ApplyAnimationExchangeResult(conversion.Summary, conversion.Issues);
        }
        catch (Exception exception)
        {
            ViewModel.StatusMessage = $"Convert FLA to XFL failed: {exception.Message}";
        }
    }

    private async void ConvertFlashXflFolderToFlaClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ConvertFlashXflFolderToFlaAsync();
    }

    private async Task ConvertFlashXflFolderToFlaAsync()
    {
        if (ViewModel is null || !StorageProvider.CanPickFolder || !StorageProvider.CanSave)
        {
            return;
        }

        var folderResult = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose Flash XFL folder",
            AllowMultiple = false
        });

        var folder = folderResult.FirstOrDefault();
        var folderPath = folder?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            ViewModel.StatusMessage = "Converting XFL to FLA requires a local source folder.";
            return;
        }

        var flashFileType = new FilePickerFileType("Flash Authoring Archive")
        {
            Patterns = ["*.fla"],
            MimeTypes = ["application/octet-stream", "application/zip"]
        };

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Flash FLA archive",
            SuggestedFileName = $"{Path.GetFileNameWithoutExtension(folderPath)}.fla",
            DefaultExtension = "fla",
            FileTypeChoices = [flashFileType],
            SuggestedFileType = flashFileType,
            ShowOverwritePrompt = true
        });

        if (file is null)
        {
            return;
        }

        try
        {
            await using var stream = await file.OpenWriteAsync();
            if (stream.CanSeek)
            {
                stream.SetLength(0);
            }

            var conversion = await TimelineDocumentFileService.ConvertFlashFolderToArchiveAsync(folderPath, stream, file.Name);
            ViewModel.ApplyAnimationExchangeResult($"{conversion.Summary} → {file.Name}", conversion.Issues);
        }
        catch (Exception exception)
        {
            ViewModel.StatusMessage = $"Convert XFL to FLA failed: {exception.Message}";
        }
    }

    private async void ExportFrameClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ExportFrameAsync();
    }

    private async Task ExportFrameAsync()
    {
        if (ViewModel is null || !StorageProvider.CanSave)
        {
            return;
        }

        var pngFile = new FilePickerFileType("PNG Image")
        {
            Patterns = ["*.png"],
            MimeTypes = ["image/png"]
        };

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export current frame",
            SuggestedFileName = $"frame_{ViewModel.CurrentTime:0.00}s.png",
            DefaultExtension = "png",
            FileTypeChoices = [pngFile],
            SuggestedFileType = pngFile,
            ShowOverwritePrompt = true
        });

        if (file is null)
        {
            return;
        }

        ViewModel.StatusMessage = "Exporting current frame...";
        await using var stream = await file.OpenWriteAsync();
        if (stream.CanSeek)
        {
            stream.SetLength(0);
        }

        await FrameExportService.ExportFrameAsync(ViewModel.CreateExportDocumentSnapshot(), ViewModel.CurrentTime, stream);
        ViewModel.StatusMessage = $"Frame exported to {file.Name}";
    }

    private async void RenderSequenceClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await RenderSequenceAsync();
    }

    private async Task RenderSequenceAsync()
    {
        if (ViewModel is null || !StorageProvider.CanPickFolder)
        {
            return;
        }

        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose sequence output folder",
            AllowMultiple = false
        });

        var folder = result.FirstOrDefault();
        var folderPath = folder?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            ViewModel.StatusMessage = "Sequence export requires a local folder.";
            return;
        }

        ViewModel.StatusMessage = "Rendering PNG sequence...";
        var frameCount = await FrameExportService.ExportSequenceAsync(ViewModel.CreateExportDocumentSnapshot(), folderPath);
        ViewModel.StatusMessage = $"Rendered {frameCount} frames to {Path.GetFileName(folderPath)}";
    }

    private async void RenderProjectClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await RenderProjectAsync();
    }

    private async Task RenderProjectAsync()
    {
        if (ViewModel is null || !StorageProvider.CanPickFolder)
        {
            return;
        }

        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose project output folder",
            AllowMultiple = false
        });

        var folder = result.FirstOrDefault();
        var folderPath = folder?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            ViewModel.StatusMessage = "Project export requires a local folder.";
            return;
        }

        ViewModel.StatusMessage = "Rendering all scenes...";
        var sceneCount = await FrameExportService.ExportProjectSequenceAsync(ViewModel.CreateExportDocumentSnapshot(), folderPath);
        ViewModel.StatusMessage = $"Rendered {sceneCount} scenes to {Path.GetFileName(folderPath)}";
    }

    private async void ImportAnimationFormatClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ImportAnimationFormatAsync();
    }

    private Task ImportAnimationFormatAsync(AnimationExchangeFormat formatOverride)
    {
        return ImportAnimationFormatAsync((AnimationExchangeFormat?)formatOverride);
    }

    private async Task ImportAnimationFormatAsync(AnimationExchangeFormat? formatOverride = null)
    {
        if (ViewModel is null || !StorageProvider.CanOpen)
        {
            return;
        }

        if (formatOverride is AnimationExchangeFormat overrideFormat)
        {
            ViewModel.SelectAnimationExchangeFormatCommand.Execute(overrideFormat.ToString());
        }

        var selectedFormat = formatOverride ?? ViewModel.SelectedAnimationExchangeFormat;
        var fileType = selectedFormat switch
        {
            AnimationExchangeFormat.AvaloniaXaml => new FilePickerFileType("Avalonia XAML Animation")
            {
                Patterns = ["*.axaml", "*.xaml"],
                MimeTypes = ["application/xml", "text/xml"]
            },
            AnimationExchangeFormat.FlashXfl => new FilePickerFileType("Flash XFL Animation")
            {
                Patterns = ["*.xfl", "*.fla"],
                MimeTypes = ["application/xml", "text/xml", "application/octet-stream", "application/zip"]
            },
            AnimationExchangeFormat.SvgSmil => new FilePickerFileType("SVG Animation")
            {
                Patterns = ["*.svg"],
                MimeTypes = ["image/svg+xml"]
            },
            AnimationExchangeFormat.HtmlCss => new FilePickerFileType("HTML Animation")
            {
                Patterns = ["*.html", "*.htm", "*.xhtml"],
                MimeTypes = ["text/html", "application/xhtml+xml"]
            },
            _ => new FilePickerFileType("Animation File") { Patterns = ["*.*"] }
        };

        var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = $"Import {AnimationExchangeService.GetDisplayName(selectedFormat)}",
            AllowMultiple = false,
            FileTypeFilter = [fileType],
            SuggestedFileType = fileType
        });

        var file = result.FirstOrDefault();
        if (file is null)
        {
            return;
        }

        try
        {
            ViewModel.StatusMessage = $"Importing {file.Name}...";
            await using var stream = await file.OpenReadAsync();
            AnimationExchangeImportResult imported;
            if (selectedFormat == AnimationExchangeFormat.FlashXfl)
            {
                var loaded = await TimelineDocumentFileService.LoadAsync(stream, file.Name, TimelineDocumentFileFormat.FlashXfl);
                imported = new AnimationExchangeImportResult
                {
                    Format = AnimationExchangeFormat.FlashXfl,
                    Document = loaded.Document,
                    Summary = loaded.Summary,
                    Issues = loaded.Issues
                };
            }
            else
            {
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();
                imported = AnimationExchangeService.Import(selectedFormat, content, file.Name);
            }

            ViewModel.LoadDocument(imported.Document, file.Name);
            SceneCanvas.ResetViewport();
            ViewModel.ApplyAnimationExchangeResult(imported.Summary, imported.Issues);
        }
        catch (Exception exception)
        {
            ViewModel.StatusMessage = $"Import failed: {exception.Message}";
        }
    }

    private async void ImportAnimationFormatMenuClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem { CommandParameter: string formatKey } && ViewModel is not null)
        {
            ViewModel.SelectAnimationExchangeFormatCommand.Execute(formatKey);
        }

        await ImportAnimationFormatAsync();
    }

    private async void ExportAnimationFormatClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ExportAnimationFormatAsync();
    }

    private Task ExportAnimationFormatAsync(AnimationExchangeFormat formatOverride)
    {
        return ExportAnimationFormatAsync((AnimationExchangeFormat?)formatOverride);
    }

    private async Task ExportAnimationFormatAsync(AnimationExchangeFormat? formatOverride = null)
    {
        if (ViewModel is null || !StorageProvider.CanSave)
        {
            return;
        }

        if (formatOverride is AnimationExchangeFormat overrideFormat)
        {
            ViewModel.SelectAnimationExchangeFormatCommand.Execute(overrideFormat.ToString());
        }

        var selectedFormat = formatOverride ?? ViewModel.SelectedAnimationExchangeFormat;
        var extension = AnimationExchangeService.GetSuggestedExtension(selectedFormat);
        var export = AnimationExchangeService.Export(ViewModel.CreateExportDocumentSnapshot(), selectedFormat);
        var fileType = selectedFormat switch
        {
            AnimationExchangeFormat.AvaloniaXaml => new FilePickerFileType("Avalonia XAML Animation")
            {
                Patterns = ["*.axaml", "*.xaml"],
                MimeTypes = ["application/xml", "text/xml"]
            },
            AnimationExchangeFormat.FlashXfl => new FilePickerFileType("Flash XFL Animation")
            {
                Patterns = ["*.xfl", "*.fla"],
                MimeTypes = ["application/xml", "text/xml", "application/octet-stream", "application/zip"]
            },
            AnimationExchangeFormat.SvgSmil => new FilePickerFileType("SVG Animation")
            {
                Patterns = ["*.svg"],
                MimeTypes = ["image/svg+xml"]
            },
            AnimationExchangeFormat.HtmlCss => new FilePickerFileType("HTML Animation")
            {
                Patterns = ["*.html", "*.htm", "*.xhtml"],
                MimeTypes = ["text/html", "application/xhtml+xml"]
            },
            _ => new FilePickerFileType("Animation File") { Patterns = ["*.*"] }
        };

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"Export {AnimationExchangeService.GetDisplayName(selectedFormat)}",
            SuggestedFileName = export.SuggestedFileName,
            DefaultExtension = extension,
            FileTypeChoices = [fileType],
            SuggestedFileType = fileType,
            ShowOverwritePrompt = true
        });

        if (file is null)
        {
            return;
        }

        try
        {
            ViewModel.StatusMessage = $"Exporting {AnimationExchangeService.GetDisplayName(selectedFormat)}...";
            await using var stream = await file.OpenWriteAsync();
            if (stream.CanSeek)
            {
                stream.SetLength(0);
            }

            if (selectedFormat == AnimationExchangeFormat.FlashXfl)
            {
                var saved = await TimelineDocumentFileService.SaveAsync(
                    stream,
                    ViewModel.CreateExportDocumentSnapshot(),
                    TimelineDocumentFileFormat.FlashXfl,
                    file.Name);
                ViewModel.ApplyAnimationExchangeResult($"{saved.Summary} → {file.Name}", saved.Issues);
            }
            else
            {
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(export.Content);
                await writer.FlushAsync();
                ViewModel.ApplyAnimationExchangeResult($"{export.Summary} → {file.Name}", export.Issues);
            }
        }
        catch (Exception exception)
        {
            ViewModel.StatusMessage = $"Export failed: {exception.Message}";
        }
    }

    private async void ExportAnimationFormatMenuClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem { CommandParameter: string formatKey } && ViewModel is not null)
        {
            ViewModel.SelectAnimationExchangeFormatCommand.Execute(formatKey);
        }

        await ExportAnimationFormatAsync();
    }

    private async void ImportAudioClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ImportAudioAsync();
    }

    private async Task ImportAudioAsync()
    {
        if (ViewModel is null || !StorageProvider.CanOpen)
        {
            return;
        }

        var wavFile = new FilePickerFileType("Wave Audio")
        {
            Patterns = ["*.wav"],
            MimeTypes = ["audio/wav", "audio/x-wav"]
        };

        var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import audio clip",
            AllowMultiple = false,
            FileTypeFilter = [wavFile],
            SuggestedFileType = wavFile
        });

        var file = result.FirstOrDefault();
        if (file is null)
        {
            return;
        }

        await using var stream = await file.OpenReadAsync();
        var asset = await MediaImportService.ImportAudioAsync(file.Name, stream);
        ViewModel.AddImportedMediaAsset(asset);
    }

    private async void ImportVideoClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ImportVideoAsync();
    }

    private async Task ImportVideoAsync()
    {
        if (ViewModel is null || !StorageProvider.CanOpen)
        {
            return;
        }

        var imageFiles = new FilePickerFileType("Image Frames")
        {
            Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp"],
            MimeTypes = ["image/png", "image/jpeg", "image/bmp", "image/webp"]
        };

        var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import video sequence",
            AllowMultiple = true,
            FileTypeFilter = [imageFiles],
            SuggestedFileType = imageFiles
        });

        var files = result
            .OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (files.Count == 0)
        {
            return;
        }

        var frames = new List<ImportedMediaFrame>(files.Count);
        foreach (var file in files)
        {
            await using var stream = await file.OpenReadAsync();
            await using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            frames.Add(new ImportedMediaFrame(file.Name, buffer.ToArray()));
        }

        var clipName = files.Count == 1
            ? files[0].Name
            : $"{Path.GetFileNameWithoutExtension(files[0].Name)} Sequence";
        var asset = MediaImportService.ImportVideoSequence(clipName, frames, ViewModel.SceneFrameRate);
        ViewModel.AddImportedMediaAsset(asset);
    }

    private void PreviewPublishProfileClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PreviewPublishProfile();
    }

    private void PreviewPublishProfile()
    {
        if (ViewModel?.CreateSelectedPublishProfileSnapshot() is not { } profile)
        {
            return;
        }

        var window = new PreviewPlayerWindow
        {
            DataContext = new PreviewPlayerViewModel(ViewModel.CreateExportDocumentSnapshot(), profile)
        };
        window.Show();
        ViewModel.StatusMessage = $"Preview opened for {profile.Name}";
    }

    private async void ExportPublishProfileClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ExportPublishProfileAsync();
    }

    private async Task ExportPublishProfileAsync()
    {
        if (ViewModel?.CreateSelectedPublishProfileSnapshot() is not { } profile)
        {
            return;
        }

        var destinationPath = await SelectPublishDestinationAsync(profile);
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            return;
        }

        try
        {
            ViewModel.StatusMessage = $"Publishing {profile.Name}...";
            var result = await PublishExportService.ExportAsync(
                ViewModel.CreateExportDocumentSnapshot(),
                profile,
                destinationPath);
            ViewModel.StatusMessage = $"{result.Summary} → {Path.GetFileName(result.PrimaryOutputPath)}";
        }
        catch (Exception exception)
        {
            ViewModel.StatusMessage = $"Publish failed: {exception.Message}";
        }
    }

    private async Task<string?> SelectPublishDestinationAsync(PublishProfile profile)
    {
        if (PublishExportService.RequiresDirectory(profile.OutputKind))
        {
            if (!StorageProvider.CanPickFolder)
            {
                if (ViewModel is not null)
                {
                    ViewModel.StatusMessage = $"Publishing {profile.Name} requires a local folder.";
                }
                return null;
            }

            var folderResult = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = $"Choose output folder for {profile.Name}",
                AllowMultiple = false
            });
            var folderPath = folderResult.FirstOrDefault()?.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                if (ViewModel is not null)
                {
                    ViewModel.StatusMessage = "Publish canceled.";
                }
                return null;
            }

            return folderPath;
        }

        if (!StorageProvider.CanSave)
        {
            if (ViewModel is not null)
            {
                ViewModel.StatusMessage = $"Publishing {profile.Name} requires a local file destination.";
            }
            return null;
        }

        var extension = PublishExportService.GetSuggestedExtension(profile.OutputKind);
        var fileType = CreatePublishFileType(profile.OutputKind, extension);
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"Publish {profile.Name}",
            SuggestedFileName = GetSuggestedPublishFileName(profile, extension),
            DefaultExtension = extension,
            FileTypeChoices = [fileType],
            SuggestedFileType = fileType,
            ShowOverwritePrompt = true
        });

        var filePath = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            if (ViewModel is not null)
            {
                ViewModel.StatusMessage = "Publish canceled.";
            }
            return null;
        }

        if (!string.IsNullOrWhiteSpace(extension) &&
            string.IsNullOrWhiteSpace(Path.GetExtension(filePath)))
        {
            filePath = $"{filePath}.{extension}";
        }

        return filePath;
    }

    private FilePickerFileType CreatePublishFileType(PublishOutputKind outputKind, string extension)
    {
        return new FilePickerFileType(GetPublishFileTypeLabel(outputKind))
        {
            Patterns = [string.IsNullOrWhiteSpace(extension) ? "*" : $"*.{extension}"]
        };
    }

    private string GetSuggestedPublishFileName(PublishProfile profile, string extension)
    {
        var documentName = ViewModel?.DocumentName ?? "timeline";
        var profileName = string.IsNullOrWhiteSpace(profile.Name) ? "publish" : profile.Name;
        var seed = $"{documentName}_{profileName}";
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(seed.Select(character => invalidChars.Contains(character) ? '_' : character).ToArray()).Trim('_', ' ');
        var fileName = string.IsNullOrWhiteSpace(sanitized) ? "timeline_publish" : sanitized;
        return string.IsNullOrWhiteSpace(extension) ? fileName : $"{fileName}.{extension}";
    }

    private static IReadOnlyList<FilePickerFileType> BuildSupportedDocumentFileTypes()
    {
        return
        [
            new FilePickerFileType("Timeline Documents And Animations")
            {
                Patterns = ["*.timeline.json", "*.json", "*.axaml", "*.xaml", "*.xfl", "*.fla", "*.svg", "*.html", "*.htm", "*.xhtml"]
            },
            BuildNativeProjectFileType(),
            BuildDocumentFileType(TimelineDocumentFileFormat.AvaloniaXaml),
            BuildDocumentFileType(TimelineDocumentFileFormat.FlashXfl),
            BuildDocumentFileType(TimelineDocumentFileFormat.SvgSmil),
            BuildDocumentFileType(TimelineDocumentFileFormat.HtmlCss)
        ];
    }

    private static FilePickerFileType BuildNativeProjectFileType()
    {
        return new FilePickerFileType("Timeline Project")
        {
            Patterns = ["*.timeline.json", "*.json"],
            MimeTypes = ["application/json"]
        };
    }

    private static FilePickerFileType BuildDocumentFileType(TimelineDocumentFileFormat format)
    {
        return format switch
        {
            TimelineDocumentFileFormat.NativeProject => BuildNativeProjectFileType(),
            TimelineDocumentFileFormat.AvaloniaXaml => new FilePickerFileType("Avalonia XAML Animation")
            {
                Patterns = ["*.axaml", "*.xaml"],
                MimeTypes = ["application/xml", "text/xml"]
            },
            TimelineDocumentFileFormat.FlashXfl => new FilePickerFileType("Flash XFL Animation")
            {
                Patterns = ["*.xfl", "*.fla"],
                MimeTypes = ["application/xml", "text/xml", "application/octet-stream", "application/zip"]
            },
            TimelineDocumentFileFormat.SvgSmil => new FilePickerFileType("SVG Animation")
            {
                Patterns = ["*.svg"],
                MimeTypes = ["image/svg+xml"]
            },
            TimelineDocumentFileFormat.HtmlCss => new FilePickerFileType("HTML Animation")
            {
                Patterns = ["*.html", "*.htm", "*.xhtml"],
                MimeTypes = ["text/html", "application/xhtml+xml"]
            },
            _ => new FilePickerFileType("Timeline File") { Patterns = ["*.*"] }
        };
    }

    private static string GetPublishFileTypeLabel(PublishOutputKind outputKind)
    {
        return outputKind switch
        {
            PublishOutputKind.SpriteSheet => "Sprite Sheet",
            PublishOutputKind.Gif => "Animated GIF",
            PublishOutputKind.Mp4 => "MP4 Video",
            PublishOutputKind.JsonSceneGraph => "JSON Scene Graph",
            PublishOutputKind.Package => "Publish Package",
            _ => "Publish Output"
        };
    }
}
