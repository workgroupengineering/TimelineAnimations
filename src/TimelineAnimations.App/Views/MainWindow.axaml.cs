using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using TimelineAnimations.App.Controls;
using TimelineAnimations.App.Services;
using TimelineAnimations.App.ViewModels;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.Views;

public partial class MainWindow : Window
{
    private bool _isTimelineResizeDragging;
    private Point _timelineResizeOrigin;
    private double _timelineResizeHeightOrigin;

    public MainWindow()
    {
        InitializeComponent();
        HookInteractions();
        Closing += HandleClosing;
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private void HandleClosing(object? sender, WindowClosingEventArgs e)
    {
        ViewModel?.DockWorkspace.SaveLayout();
    }

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

    private async void ImportAnimationFormatClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null || !StorageProvider.CanOpen)
        {
            return;
        }

        var selectedFormat = ViewModel.SelectedAnimationExchangeFormat;
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

    private void ImportAnimationFormatMenuClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem { CommandParameter: string formatKey } && ViewModel is not null)
        {
            ViewModel.SelectAnimationExchangeFormatCommand.Execute(formatKey);
        }

        ImportAnimationFormatClick(sender, e);
    }

    private async void ExportAnimationFormatClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null || !StorageProvider.CanSave)
        {
            return;
        }

        var selectedFormat = ViewModel.SelectedAnimationExchangeFormat;
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

    private void ExportAnimationFormatMenuClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem { CommandParameter: string formatKey } && ViewModel is not null)
        {
            ViewModel.SelectAnimationExchangeFormatCommand.Execute(formatKey);
        }

        ExportAnimationFormatClick(sender, e);
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
