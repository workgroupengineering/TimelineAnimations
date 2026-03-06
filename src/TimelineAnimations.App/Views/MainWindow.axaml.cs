using Avalonia.Controls;
using Avalonia.Platform.Storage;
using TimelineAnimations.App.Controls;
using TimelineAnimations.App.Services;
using TimelineAnimations.App.ViewModels;
using TimelineAnimations.Core.Models;
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
        SceneCanvas.DrawingRequested += HandleCanvasDrawingRequested;
        SceneCanvas.PathPointMoveRequested += HandleCanvasPathPointMoveRequested;
        SceneCanvas.TransformInteractionStateChanged += HandleCanvasInteractionStateChanged;
        SceneCanvas.PrototypeTriggerRequested += HandleCanvasPrototypeTriggerRequested;

        TimelineEditor.ScrubRequested += HandleTimelineScrubRequested;
        TimelineEditor.TrackSelectionRequested += HandleTrackSelectionRequested;
        TimelineEditor.KeyframeSelectionRequested += HandleKeyframeSelectionRequested;
        TimelineEditor.KeyframeMoveRequested += HandleKeyframeMoveRequested;
        TimelineEditor.KeyframeAddRequested += HandleKeyframeAddRequested;
        TimelineEditor.KeyframeInteractionStateChanged += HandleTimelineInteractionStateChanged;

        FrameTimeline.FrameRequested += HandleFrameRequested;
        FrameTimeline.LayerSelectionRequested += HandleFrameLayerSelectionRequested;
        FrameTimeline.RangeSelectionRequested += HandleFrameRangeSelectionRequested;
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

    private void HandleCanvasDrawingRequested(object? sender, CanvasDrawingRequestedEventArgs e)
    {
        ViewModel?.CreateLayerFromCanvas(e.Tool, e.DocumentBounds, e.DocumentPoints);
    }

    private void HandleCanvasPathPointMoveRequested(object? sender, CanvasPathPointMoveRequestedEventArgs e)
    {
        ViewModel?.UpdatePathPoint(e.LayerId, e.PointIndex, e.DocumentPoint);
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

    private void HandleTimelineScrubRequested(object? sender, TimelineScrubRequestedEventArgs e)
    {
        ViewModel?.Scrub(e.Time);
    }

    private void HandleTrackSelectionRequested(object? sender, TimelineTrackSelectionRequestedEventArgs e)
    {
        ViewModel?.SelectTrack(e.LayerId, e.Property);
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
            ViewModel.BeginInteractiveChange();
        }
        else
        {
            ViewModel.CommitInteractiveChange("Keyframe moved");
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

        await DocumentSerializer.SaveAsync(stream, ViewModel.CreateExportDocumentSnapshot());
        ViewModel.SetDocumentLabel(file.Name);
    }

    private async void ExportFrameClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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

    private async void ImportAudioClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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
