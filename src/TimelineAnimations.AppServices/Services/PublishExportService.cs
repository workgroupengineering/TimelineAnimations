using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Media.Imaging;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.Services;

public sealed class PublishExportResult
{
    public string Summary { get; set; } = string.Empty;

    public string PrimaryOutputPath { get; set; } = string.Empty;

    public int FrameCount { get; set; }

    public List<string> SupplementalPaths { get; set; } = [];
}

public static class PublishExportService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public static IReadOnlyList<PublishValidationIssue> Validate(TimelineDocument document, PublishProfile profile)
    {
        var issues = PublishValidationService.Validate(document, profile).ToList();
        if (profile.OutputKind is PublishOutputKind.Gif or PublishOutputKind.Mp4 && !IsFfmpegAvailable())
        {
            issues.Add(new PublishValidationIssue
            {
                Severity = "Error",
                Source = "Tooling",
                Message = "ffmpeg is required for GIF and MP4 publish profiles."
            });
        }

        return issues;
    }

    public static bool RequiresDirectory(PublishOutputKind outputKind)
    {
        return outputKind == PublishOutputKind.PngSequence;
    }

    public static string GetSuggestedExtension(PublishOutputKind outputKind)
    {
        return outputKind switch
        {
            PublishOutputKind.PngSequence => string.Empty,
            PublishOutputKind.SpriteSheet => "png",
            PublishOutputKind.Gif => "gif",
            PublishOutputKind.Mp4 => "mp4",
            PublishOutputKind.JsonSceneGraph => "json",
            PublishOutputKind.Package => "zip",
            _ => "dat"
        };
    }

    public static async Task<PublishExportResult> ExportAsync(
        TimelineDocument document,
        PublishProfile profile,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        var snapshot = DocumentSerializer.Clone(document);
        PublishProfileService.EnsureProfiles(snapshot);
        var issues = Validate(snapshot, profile);
        if (issues.Any(issue => string.Equals(issue.Severity, "Error", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, issues.Select(issue => $"{issue.Severity}: {issue.Message}")));
        }

        return profile.OutputKind switch
        {
            PublishOutputKind.PngSequence => await ExportPngSequenceAsync(snapshot, profile, destinationPath, issues, cancellationToken),
            PublishOutputKind.SpriteSheet => await ExportSpriteSheetAsync(snapshot, profile, destinationPath, issues, cancellationToken),
            PublishOutputKind.Gif => await ExportGifAsync(snapshot, profile, destinationPath, issues, cancellationToken),
            PublishOutputKind.Mp4 => await ExportMp4Async(snapshot, profile, destinationPath, issues, cancellationToken),
            PublishOutputKind.JsonSceneGraph => await ExportSceneGraphAsync(snapshot, profile, destinationPath, issues, cancellationToken),
            PublishOutputKind.Package => await ExportPackageAsync(snapshot, profile, destinationPath, issues, cancellationToken),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static bool IsFfmpegAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            });
            process?.WaitForExit(2000);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<PublishExportResult> ExportPngSequenceAsync(
        TimelineDocument document,
        PublishProfile profile,
        string destinationPath,
        IReadOnlyList<PublishValidationIssue> issues,
        CancellationToken cancellationToken)
    {
        var frameCount = await FrameExportService.ExportSequenceAsync(
            document,
            destinationPath,
            profile.FrameRate,
            profile.PlayAllScenes,
            profile.Width,
            profile.Height,
            profile.TransparentBackground,
            useWorkArea: false,
            cancellationToken);

        var result = new PublishExportResult
        {
            PrimaryOutputPath = destinationPath,
            FrameCount = frameCount,
            Summary = $"Rendered {frameCount} PNG frames"
        };
        await WriteSupplementalFilesAsync(document, profile, destinationPath, GetSupplementalStem(document, profile), result, issues, cancellationToken);
        return result;
    }

    private static async Task<PublishExportResult> ExportSpriteSheetAsync(
        TimelineDocument document,
        PublishProfile profile,
        string destinationPath,
        IReadOnlyList<PublishValidationIssue> issues,
        CancellationToken cancellationToken)
    {
        var frameCount = FrameExportService.GetFrameCount(document, profile.FrameRate, profile.PlayAllScenes);
        var columns = Math.Max(1, profile.SpriteSheetColumns);
        var rows = (int)Math.Ceiling(frameCount / (double)columns);
        using var sheet = new RenderTargetBitmap(
            new PixelSize(columns * Math.Max(1, profile.Width), rows * Math.Max(1, profile.Height)),
            new Vector(96, 96));
        var metadataFrames = new List<object>(frameCount);
        using (var context = sheet.CreateDrawingContext(profile.TransparentBackground))
        {
            for (var frame = 0; frame < frameCount; frame++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var time = Math.Min(frame / Math.Max(1d, profile.FrameRate), FrameExportService.GetPlaybackDuration(document, profile.PlayAllScenes));
                using var bitmap = FrameExportService.RenderFrameBitmap(
                    document,
                    time,
                    profile.Width,
                    profile.Height,
                    profile.FrameRate,
                    profile.PlayAllScenes,
                    profile.TransparentBackground);
                var column = frame % columns;
                var row = frame / columns;
                var targetRect = new Rect(column * profile.Width, row * profile.Height, profile.Width, profile.Height);
                context.DrawImage(bitmap, targetRect);
                metadataFrames.Add(new
                {
                    index = frame,
                    time,
                    x = (int)targetRect.X,
                    y = (int)targetRect.Y,
                    width = profile.Width,
                    height = profile.Height
                });
            }
        }

        await using (var stream = File.Create(destinationPath))
        {
            sheet.Save(stream);
            await stream.FlushAsync(cancellationToken);
        }

        var metadataPath = Path.ChangeExtension(destinationPath, ".json");
        await File.WriteAllTextAsync(
            metadataPath,
            JsonSerializer.Serialize(new
            {
                profile = profile.Name,
                columns,
                rows,
                frameCount,
                frames = metadataFrames
            }, s_jsonOptions),
            cancellationToken);

        var result = new PublishExportResult
        {
            PrimaryOutputPath = destinationPath,
            FrameCount = frameCount,
            Summary = $"Exported sprite sheet with {frameCount} frames",
            SupplementalPaths = [metadataPath]
        };
        await WriteSupplementalFilesAsync(document, profile, Path.GetDirectoryName(destinationPath) ?? string.Empty, Path.GetFileNameWithoutExtension(destinationPath), result, issues, cancellationToken);
        return result;
    }

    private static async Task<PublishExportResult> ExportGifAsync(
        TimelineDocument document,
        PublishProfile profile,
        string destinationPath,
        IReadOnlyList<PublishValidationIssue> issues,
        CancellationToken cancellationToken)
    {
        return await ExportWithFfmpegAsync(document, profile, destinationPath, issues, cancellationToken, isGif: true);
    }

    private static async Task<PublishExportResult> ExportMp4Async(
        TimelineDocument document,
        PublishProfile profile,
        string destinationPath,
        IReadOnlyList<PublishValidationIssue> issues,
        CancellationToken cancellationToken)
    {
        return await ExportWithFfmpegAsync(document, profile, destinationPath, issues, cancellationToken, isGif: false);
    }

    private static async Task<PublishExportResult> ExportSceneGraphAsync(
        TimelineDocument document,
        PublishProfile profile,
        string destinationPath,
        IReadOnlyList<PublishValidationIssue> issues,
        CancellationToken cancellationToken)
    {
        var sceneGraphJson = JsonSerializer.Serialize(BuildSceneGraph(document, profile, issues), s_jsonOptions);
        await File.WriteAllTextAsync(destinationPath, sceneGraphJson, cancellationToken);
        return new PublishExportResult
        {
            PrimaryOutputPath = destinationPath,
            Summary = "Scene graph exported"
        };
    }

    private static async Task<PublishExportResult> ExportPackageAsync(
        TimelineDocument document,
        PublishProfile profile,
        string destinationPath,
        IReadOnlyList<PublishValidationIssue> issues,
        CancellationToken cancellationToken)
    {
        var packageRoot = Path.Combine(Path.GetTempPath(), $"timeline_publish_{Guid.NewGuid():N}");
        Directory.CreateDirectory(packageRoot);
        try
        {
            var sequenceFolder = Path.Combine(packageRoot, "frames");
            var sequenceResult = await ExportPngSequenceAsync(document, profile, sequenceFolder, issues, cancellationToken);
            var sceneGraphPath = Path.Combine(packageRoot, "scene-graph.json");
            await File.WriteAllTextAsync(sceneGraphPath, JsonSerializer.Serialize(BuildSceneGraph(document, profile, issues), s_jsonOptions), cancellationToken);
            var profilePath = Path.Combine(packageRoot, "publish-profile.json");
            await File.WriteAllTextAsync(profilePath, JsonSerializer.Serialize(profile, s_jsonOptions), cancellationToken);
            if (profile.IncludeValidationReport)
            {
                await WriteValidationReportAsync(Path.Combine(packageRoot, "validation-report.txt"), issues, cancellationToken);
            }

            var manifestPath = Path.Combine(packageRoot, "package-manifest.json");
            await File.WriteAllTextAsync(
                manifestPath,
                JsonSerializer.Serialize(new
                {
                    document = document.Name,
                    profile = profile.Name,
                    output = "frames",
                    frameCount = sequenceResult.FrameCount,
                    sceneGraph = "scene-graph.json"
                }, s_jsonOptions),
                cancellationToken);

            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            ZipFile.CreateFromDirectory(packageRoot, destinationPath);
            return new PublishExportResult
            {
                PrimaryOutputPath = destinationPath,
                FrameCount = sequenceResult.FrameCount,
                Summary = $"Packaged {sequenceResult.FrameCount} frames into archive"
            };
        }
        finally
        {
            if (Directory.Exists(packageRoot))
            {
                Directory.Delete(packageRoot, recursive: true);
            }
        }
    }

    private static async Task<PublishExportResult> ExportWithFfmpegAsync(
        TimelineDocument document,
        PublishProfile profile,
        string destinationPath,
        IReadOnlyList<PublishValidationIssue> issues,
        CancellationToken cancellationToken,
        bool isGif)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"timeline_ffmpeg_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        try
        {
            var framesFolder = Path.Combine(tempRoot, "frames");
            var frameCount = await FrameExportService.ExportSequenceAsync(
                document,
                framesFolder,
                profile.FrameRate,
                profile.PlayAllScenes,
                profile.Width,
                profile.Height,
                profile.TransparentBackground,
                useWorkArea: false,
                cancellationToken);

            if (isGif)
            {
                var palettePath = Path.Combine(tempRoot, "palette.png");
                await RunFfmpegAsync(
                    ["-y", "-framerate", $"{profile.FrameRate:0.###}", "-i", Path.Combine(framesFolder, "frame_%04d.png"), "-vf", "palettegen=max_colors=256", palettePath],
                    cancellationToken);
                await RunFfmpegAsync(
                    ["-y", "-framerate", $"{profile.FrameRate:0.###}", "-i", Path.Combine(framesFolder, "frame_%04d.png"), "-i", palettePath, "-lavfi", "paletteuse=dither=bayer:bayer_scale=5", destinationPath],
                    cancellationToken);
            }
            else
            {
                await RunFfmpegAsync(
                    ["-y", "-framerate", $"{profile.FrameRate:0.###}", "-i", Path.Combine(framesFolder, "frame_%04d.png"), "-c:v", "libx264", "-pix_fmt", "yuv420p", "-movflags", "+faststart", destinationPath],
                    cancellationToken);
            }

            var result = new PublishExportResult
            {
                PrimaryOutputPath = destinationPath,
                FrameCount = frameCount,
                Summary = isGif
                    ? $"Exported animated GIF with {frameCount} frames"
                    : $"Exported MP4 with {frameCount} frames"
            };
            await WriteSupplementalFilesAsync(document, profile, Path.GetDirectoryName(destinationPath) ?? string.Empty, Path.GetFileNameWithoutExtension(destinationPath), result, issues, cancellationToken);
            return result;
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static async Task RunFfmpegAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var stderr = await stderrTask;
        var stdout = await stdoutTask;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"ffmpeg publish failed.{Environment.NewLine}{stderr}{Environment.NewLine}{stdout}");
        }
    }

    private static async Task WriteSupplementalFilesAsync(
        TimelineDocument document,
        PublishProfile profile,
        string folderPath,
        string fileStem,
        PublishExportResult result,
        IReadOnlyList<PublishValidationIssue> issues,
        CancellationToken cancellationToken)
    {
        if (profile.IncludeSceneGraph)
        {
            var sceneGraphPath = Path.Combine(folderPath, $"{fileStem}.scenegraph.json");
            await File.WriteAllTextAsync(sceneGraphPath, JsonSerializer.Serialize(BuildSceneGraph(document, profile, issues), s_jsonOptions), cancellationToken);
            result.SupplementalPaths.Add(sceneGraphPath);
        }

        if (profile.IncludeValidationReport)
        {
            var validationPath = Path.Combine(folderPath, $"{fileStem}.validation.txt");
            await WriteValidationReportAsync(validationPath, issues, cancellationToken);
            result.SupplementalPaths.Add(validationPath);
        }
    }

    private static string GetSupplementalStem(TimelineDocument document, PublishProfile profile)
    {
        var seed = string.IsNullOrWhiteSpace(profile.Name) ? document.Name : profile.Name;
        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(seed.Length);
        foreach (var character in seed)
        {
            builder.Append(invalidChars.Contains(character) ? '_' : character);
        }

        var stem = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(stem) ? "publish" : stem;
    }

    private static async Task WriteValidationReportAsync(string path, IReadOnlyList<PublishValidationIssue> issues, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        if (issues.Count == 0)
        {
            builder.AppendLine("No publish validation issues.");
        }
        else
        {
            foreach (var issue in issues)
            {
                builder.AppendLine($"{issue.Severity}: {issue.Source} - {issue.Message}");
            }
        }

        await File.WriteAllTextAsync(path, builder.ToString(), cancellationToken);
    }

    private static object BuildSceneGraph(TimelineDocument document, PublishProfile profile, IReadOnlyList<PublishValidationIssue> issues)
    {
        SceneEditingService.EnsureScenes(document);
        return new
        {
            document = new
            {
                document.Name,
                canvasWidth = document.CanvasWidth,
                canvasHeight = document.CanvasHeight,
                document.TransparentStageBackground,
                document.BackgroundFrom,
                document.BackgroundTo
            },
            profile = new
            {
                profile.Name,
                profile.OutputKind,
                profile.Width,
                profile.Height,
                profile.FrameRate,
                profile.PlayAllScenes
            },
            scenes = document.Scenes.Select(scene => new
            {
                scene.Id,
                scene.Name,
                scene.Duration,
                scene.FrameRate,
                frameLabels = scene.FrameLabels.Select(label => new { label.Frame, label.Name, label.Script }),
                layers = scene.Layers.OrderBy(layer => layer.ZIndex).Select(layer => new
                {
                    layer.Id,
                    layer.Name,
                    layer.Kind,
                    layer.ZIndex,
                    layer.IsVisible,
                    layer.SourceLibraryItemId,
                    layer.InstanceName,
                    layer.Media.SourceMediaAssetId,
                    behaviors = layer.Behaviors.Select(behavior => new
                    {
                        behavior.Name,
                        behavior.Trigger,
                        behavior.TriggerArgument,
                        behavior.Action,
                        behavior.TargetSceneId,
                        behavior.TargetFrameLabel,
                        behavior.TargetLayerId,
                        behavior.VariableName,
                        behavior.VariableValue,
                        behavior.Script
                    })
                })
            }),
            library = document.LibraryItems.Select(item => new
            {
                item.Id,
                item.Name,
                item.SymbolKind,
                item.IsComponent,
                item.ComponentCategory,
                item.LinkageId,
                item.BaseClassName,
                item.ExportForRuntimeSharing,
                item.ImportForRuntimeSharing,
                item.ExportInFirstFrame,
                item.SharedLibraryPath,
                item.UpdateAutomatically,
                item.UseScale9Grid,
                item.Scale9Left,
                item.Scale9Top,
                item.Scale9Right,
                item.Scale9Bottom
            }),
            media = document.MediaAssets.Select(asset => new
            {
                asset.Id,
                asset.Name,
                asset.Kind,
                asset.Duration
            }),
            validation = issues
        };
    }
}
