using Avalonia.Controls;
using Avalonia.Platform.Storage;
using TimelineAnimations.App.Controls;
using TimelineAnimations.App.ViewModels;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        HookInteractions();
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private void HookInteractions()
    {
        SceneCanvas.LayerSelectionRequested += HandleLayerSelectionRequested;
        SceneCanvas.LayerTransformRequested += HandleLayerTransformRequested;
        SceneCanvas.PaletteDropRequested += HandlePaletteDropRequested;
        SceneCanvas.TransformInteractionStateChanged += HandleCanvasInteractionStateChanged;

        TimelineEditor.ScrubRequested += HandleTimelineScrubRequested;
        TimelineEditor.TrackSelectionRequested += HandleTrackSelectionRequested;
        TimelineEditor.KeyframeSelectionRequested += HandleKeyframeSelectionRequested;
        TimelineEditor.KeyframeMoveRequested += HandleKeyframeMoveRequested;
        TimelineEditor.KeyframeAddRequested += HandleKeyframeAddRequested;
        TimelineEditor.KeyframeInteractionStateChanged += HandleTimelineInteractionStateChanged;
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
        ViewModel?.AddLayerFromPalette(e.Kind, e.DocumentPosition);
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
            ViewModel.CommitInteractiveChange("Layer transformed");
        }
    }

    private void HandleTimelineScrubRequested(object? sender, TimelineScrubRequestedEventArgs e)
    {
        ViewModel?.Scrub(e.Time);
    }

    private void HandleTrackSelectionRequested(object? sender, TimelineTrackSelectionRequestedEventArgs e)
    {
        ViewModel?.SelectTrack(e.Property);
    }

    private void HandleKeyframeSelectionRequested(object? sender, TimelineKeyframeSelectionRequestedEventArgs e)
    {
        ViewModel?.SelectKeyframe(e.Property, e.KeyframeId);
    }

    private void HandleKeyframeMoveRequested(object? sender, TimelineKeyframeMoveRequestedEventArgs e)
    {
        ViewModel?.MoveKeyframe(e.Property, e.KeyframeId, e.Time);
    }

    private void HandleKeyframeAddRequested(object? sender, TimelineKeyframeAddRequestedEventArgs e)
    {
        ViewModel?.AddKeyframeAt(e.Property, e.Time);
    }

    private void HandleTimelineInteractionStateChanged(object? sender, TimelineInteractionStateChangedEventArgs e)
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
            ViewModel.CommitInteractiveChange("Keyframe moved");
        }
    }

    private async void OpenDocumentClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null || !StorageProvider.CanOpen)
        {
            return;
        }

        var jsonFile = new FilePickerFileType("Timeline Document")
        {
            Patterns = ["*.timeline.json", "*.json"]
        };

        var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open timeline document",
            AllowMultiple = false,
            FileTypeFilter = [jsonFile],
            SuggestedFileType = jsonFile
        });

        var file = result.FirstOrDefault();
        if (file is null)
        {
            return;
        }

        await using var stream = await file.OpenReadAsync();
        var document = await DocumentSerializer.LoadAsync(stream);
        ViewModel.LoadDocument(document, file.Name);
    }

    private async void SaveDocumentClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null || !StorageProvider.CanSave)
        {
            return;
        }

        var jsonFile = new FilePickerFileType("Timeline Document")
        {
            Patterns = ["*.timeline.json"],
            MimeTypes = ["application/json"]
        };

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save timeline document",
            SuggestedFileName = "scene.timeline.json",
            DefaultExtension = "timeline.json",
            FileTypeChoices = [jsonFile],
            SuggestedFileType = jsonFile,
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

        await DocumentSerializer.SaveAsync(stream, ViewModel.Document);
        ViewModel.SetDocumentLabel(file.Name);
    }
}
