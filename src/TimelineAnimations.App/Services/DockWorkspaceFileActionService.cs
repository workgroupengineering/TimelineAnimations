using System.IO;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using TimelineAnimations.App.ViewModels;
using TimelineAnimations.App.Views;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.Services;

internal static class DockWorkspaceFileActionService
{
    public static async Task OpenDocumentAsync(TopLevel? topLevel, MainWindowViewModel viewModel, Action? afterOpen = null)
    {
        var storageProvider = topLevel?.StorageProvider;
        if (storageProvider?.CanOpen != true)
        {
            return;
        }

        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
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
            viewModel.LoadDocument(loaded.Document, file.Name, loaded.Format);
            afterOpen?.Invoke();
            viewModel.ApplyAnimationExchangeResult(loaded.Summary, loaded.Issues);
        }
        catch (Exception exception)
        {
            viewModel.StatusMessage = $"Open failed: {exception.Message}";
        }
    }

    public static async Task SaveDocumentAsync(TopLevel? topLevel, MainWindowViewModel viewModel)
    {
        var storageProvider = topLevel?.StorageProvider;
        if (storageProvider?.CanSave != true)
        {
            return;
        }

        var documentFormat = viewModel.CurrentDocumentFileFormat;
        var fileType = BuildDocumentFileType(documentFormat);

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"Save {TimelineDocumentFileService.GetDisplayName(documentFormat)}",
            SuggestedFileName = TimelineDocumentFileService.GetSuggestedFileName(viewModel.DocumentName, documentFormat),
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

        var result = await TimelineDocumentFileService.SaveAsync(stream, viewModel.CreateExportDocumentSnapshot(), documentFormat, file.Name);
        viewModel.SetDocumentLabel(file.Name, result.Format);
        viewModel.ApplyAnimationExchangeResult($"{result.Summary} → {file.Name}", result.Issues);
    }

    public static async Task ExportCurrentFrameAsync(TopLevel? topLevel, MainWindowViewModel viewModel)
    {
        var storageProvider = topLevel?.StorageProvider;
        if (storageProvider?.CanSave != true)
        {
            return;
        }

        var pngFile = new FilePickerFileType("PNG Image")
        {
            Patterns = ["*.png"],
            MimeTypes = ["image/png"]
        };

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export current frame",
            SuggestedFileName = $"frame_{viewModel.CurrentTime:0.00}s.png",
            DefaultExtension = "png",
            FileTypeChoices = [pngFile],
            SuggestedFileType = pngFile,
            ShowOverwritePrompt = true
        });

        if (file is null)
        {
            return;
        }

        viewModel.StatusMessage = "Exporting current frame...";
        await using var stream = await file.OpenWriteAsync();
        if (stream.CanSeek)
        {
            stream.SetLength(0);
        }

        await FrameExportService.ExportFrameAsync(viewModel.CreateExportDocumentSnapshot(), viewModel.CurrentTime, stream);
        viewModel.StatusMessage = $"Frame exported to {file.Name}";
    }

    public static async Task ExportSequenceAsync(TopLevel? topLevel, MainWindowViewModel viewModel)
    {
        var storageProvider = topLevel?.StorageProvider;
        if (storageProvider?.CanPickFolder != true)
        {
            return;
        }

        var result = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose sequence output folder",
            AllowMultiple = false
        });

        var folder = result.FirstOrDefault();
        var folderPath = folder?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            viewModel.StatusMessage = "Sequence export requires a local folder.";
            return;
        }

        viewModel.StatusMessage = "Rendering PNG sequence...";
        var frameCount = await FrameExportService.ExportSequenceAsync(viewModel.CreateExportDocumentSnapshot(), folderPath);
        viewModel.StatusMessage = $"Rendered {frameCount} frames to {Path.GetFileName(folderPath)}";
    }

    public static async Task ExportProjectSequenceAsync(TopLevel? topLevel, MainWindowViewModel viewModel)
    {
        var storageProvider = topLevel?.StorageProvider;
        if (storageProvider?.CanPickFolder != true)
        {
            return;
        }

        var result = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose project output folder",
            AllowMultiple = false
        });

        var folder = result.FirstOrDefault();
        var folderPath = folder?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            viewModel.StatusMessage = "Project export requires a local folder.";
            return;
        }

        viewModel.StatusMessage = "Rendering all scenes...";
        var sceneCount = await FrameExportService.ExportProjectSequenceAsync(viewModel.CreateExportDocumentSnapshot(), folderPath);
        viewModel.StatusMessage = $"Rendered {sceneCount} scenes to {Path.GetFileName(folderPath)}";
    }

    public static async Task ImportAnimationFormatAsync(TopLevel? topLevel, MainWindowViewModel viewModel)
    {
        var storageProvider = topLevel?.StorageProvider;
        if (storageProvider?.CanOpen != true)
        {
            return;
        }

        var selectedFormat = viewModel.SelectedAnimationExchangeFormat;
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

        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
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
            viewModel.StatusMessage = $"Importing {file.Name}...";
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

            viewModel.LoadDocument(imported.Document, file.Name);
            viewModel.ApplyAnimationExchangeResult(imported.Summary, imported.Issues);
        }
        catch (Exception exception)
        {
            viewModel.StatusMessage = $"Import failed: {exception.Message}";
        }
    }

    public static async Task ExportAnimationFormatAsync(TopLevel? topLevel, MainWindowViewModel viewModel)
    {
        var storageProvider = topLevel?.StorageProvider;
        if (storageProvider?.CanSave != true)
        {
            return;
        }

        var selectedFormat = viewModel.SelectedAnimationExchangeFormat;
        var extension = AnimationExchangeService.GetSuggestedExtension(selectedFormat);
        var export = AnimationExchangeService.Export(viewModel.CreateExportDocumentSnapshot(), selectedFormat);
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

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
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
            viewModel.StatusMessage = $"Exporting {AnimationExchangeService.GetDisplayName(selectedFormat)}...";
            await using var stream = await file.OpenWriteAsync();
            if (stream.CanSeek)
            {
                stream.SetLength(0);
            }

            if (selectedFormat == AnimationExchangeFormat.FlashXfl)
            {
                var saved = await TimelineDocumentFileService.SaveAsync(
                    stream,
                    viewModel.CreateExportDocumentSnapshot(),
                    TimelineDocumentFileFormat.FlashXfl,
                    file.Name);
                viewModel.ApplyAnimationExchangeResult($"{saved.Summary} → {file.Name}", saved.Issues);
            }
            else
            {
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(export.Content);
                await writer.FlushAsync();
                viewModel.ApplyAnimationExchangeResult($"{export.Summary} → {file.Name}", export.Issues);
            }
        }
        catch (Exception exception)
        {
            viewModel.StatusMessage = $"Export failed: {exception.Message}";
        }
    }

    public static async Task ImportAudioAsync(TopLevel? topLevel, MainWindowViewModel viewModel)
    {
        var storageProvider = topLevel?.StorageProvider;
        if (storageProvider?.CanOpen != true)
        {
            return;
        }

        var wavFile = new FilePickerFileType("Wave Audio")
        {
            Patterns = ["*.wav"],
            MimeTypes = ["audio/wav", "audio/x-wav"]
        };

        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
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
        viewModel.AddImportedMediaAsset(asset);
    }

    public static async Task ImportVideoAsync(TopLevel? topLevel, MainWindowViewModel viewModel)
    {
        var storageProvider = topLevel?.StorageProvider;
        if (storageProvider?.CanOpen != true)
        {
            return;
        }

        var imageFiles = new FilePickerFileType("Image Frames")
        {
            Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp"],
            MimeTypes = ["image/png", "image/jpeg", "image/bmp", "image/webp"]
        };

        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
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
        var asset = MediaImportService.ImportVideoSequence(clipName, frames, viewModel.SceneFrameRate);
        viewModel.AddImportedMediaAsset(asset);
    }

    public static void PreviewPublishProfile(MainWindowViewModel viewModel)
    {
        if (viewModel.CreateSelectedPublishProfileSnapshot() is not { } profile)
        {
            return;
        }

        var window = new PreviewPlayerWindow
        {
            DataContext = new PreviewPlayerViewModel(viewModel.CreateExportDocumentSnapshot(), profile)
        };
        window.Show();
        viewModel.StatusMessage = $"Preview opened for {profile.Name}";
    }

    public static async Task ExportPublishProfileAsync(TopLevel? topLevel, MainWindowViewModel viewModel)
    {
        if (viewModel.CreateSelectedPublishProfileSnapshot() is not { } profile)
        {
            return;
        }

        var destinationPath = await SelectPublishDestinationAsync(topLevel, viewModel, profile);
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            return;
        }

        try
        {
            viewModel.StatusMessage = $"Publishing {profile.Name}...";
            var result = await PublishExportService.ExportAsync(
                viewModel.CreateExportDocumentSnapshot(),
                profile,
                destinationPath);
            viewModel.StatusMessage = $"{result.Summary} → {Path.GetFileName(result.PrimaryOutputPath)}";
        }
        catch (Exception exception)
        {
            viewModel.StatusMessage = $"Publish failed: {exception.Message}";
        }
    }

    private static async Task<string?> SelectPublishDestinationAsync(TopLevel? topLevel, MainWindowViewModel viewModel, PublishProfile profile)
    {
        var storageProvider = topLevel?.StorageProvider;
        if (storageProvider is null)
        {
            return null;
        }

        if (PublishExportService.RequiresDirectory(profile.OutputKind))
        {
            if (!storageProvider.CanPickFolder)
            {
                viewModel.StatusMessage = $"Publishing {profile.Name} requires a local folder.";
                return null;
            }

            var folderResult = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = $"Choose output folder for {profile.Name}",
                AllowMultiple = false
            });
            var folderPath = folderResult.FirstOrDefault()?.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                viewModel.StatusMessage = "Publish canceled.";
                return null;
            }

            return folderPath;
        }

        if (!storageProvider.CanSave)
        {
            viewModel.StatusMessage = $"Publishing {profile.Name} requires a local file destination.";
            return null;
        }

        var extension = PublishExportService.GetSuggestedExtension(profile.OutputKind);
        var fileType = CreatePublishFileType(profile.OutputKind, extension);
        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
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
            viewModel.StatusMessage = "Publish canceled.";
            return null;
        }

        if (!string.IsNullOrWhiteSpace(extension) &&
            string.IsNullOrWhiteSpace(Path.GetExtension(filePath)))
        {
            filePath = $"{filePath}.{extension}";
        }

        return filePath;
    }

    private static FilePickerFileType CreatePublishFileType(PublishOutputKind outputKind, string extension)
    {
        var pattern = string.IsNullOrWhiteSpace(extension) ? "*.*" : $"*.{extension}";
        return outputKind switch
        {
            PublishOutputKind.PngSequence => new FilePickerFileType("PNG Sequence Folder") { Patterns = [pattern] },
            PublishOutputKind.SpriteSheet => new FilePickerFileType("Sprite Sheet") { Patterns = [pattern], MimeTypes = ["application/json"] },
            PublishOutputKind.Gif => new FilePickerFileType("GIF Animation") { Patterns = [pattern], MimeTypes = ["image/gif"] },
            PublishOutputKind.Mp4 => new FilePickerFileType("MP4 Video") { Patterns = [pattern], MimeTypes = ["video/mp4"] },
            PublishOutputKind.JsonSceneGraph => new FilePickerFileType("JSON Scene Graph") { Patterns = [pattern], MimeTypes = ["application/json"] },
            PublishOutputKind.Package => new FilePickerFileType("Animation Package") { Patterns = [pattern], MimeTypes = ["application/octet-stream", "application/zip"] },
            _ => new FilePickerFileType("Published Output") { Patterns = [pattern] }
        };
    }

    private static string GetSuggestedPublishFileName(PublishProfile profile, string extension)
    {
        var safeName = string.IsNullOrWhiteSpace(profile.Name) ? "publish" : profile.Name.Replace(' ', '_');
        return string.IsNullOrWhiteSpace(extension)
            ? safeName
            : $"{safeName}.{extension}";
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
}
