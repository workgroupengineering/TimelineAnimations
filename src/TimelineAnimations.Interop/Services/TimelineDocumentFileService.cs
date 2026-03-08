using System.Text;
using System.Text.RegularExpressions;
using TimelineAnimations.Core.Models;
using TimelineAnimations.FlashFormats;

namespace TimelineAnimations.Core.Services;

public static class TimelineDocumentFileService
{
    public static string GetDisplayName(TimelineDocumentFileFormat format)
    {
        return format switch
        {
            TimelineDocumentFileFormat.NativeProject => "Timeline Project",
            TimelineDocumentFileFormat.AvaloniaXaml => AnimationExchangeService.GetDisplayName(AnimationExchangeFormat.AvaloniaXaml),
            TimelineDocumentFileFormat.FlashXfl => AnimationExchangeService.GetDisplayName(AnimationExchangeFormat.FlashXfl),
            TimelineDocumentFileFormat.SvgSmil => AnimationExchangeService.GetDisplayName(AnimationExchangeFormat.SvgSmil),
            TimelineDocumentFileFormat.HtmlCss => AnimationExchangeService.GetDisplayName(AnimationExchangeFormat.HtmlCss),
            _ => format.ToString()
        };
    }

    public static string GetSuggestedExtension(TimelineDocumentFileFormat format)
    {
        return format switch
        {
            TimelineDocumentFileFormat.NativeProject => "timeline.json",
            TimelineDocumentFileFormat.AvaloniaXaml => AnimationExchangeService.GetSuggestedExtension(AnimationExchangeFormat.AvaloniaXaml),
            TimelineDocumentFileFormat.FlashXfl => AnimationExchangeService.GetSuggestedExtension(AnimationExchangeFormat.FlashXfl),
            TimelineDocumentFileFormat.SvgSmil => AnimationExchangeService.GetSuggestedExtension(AnimationExchangeFormat.SvgSmil),
            TimelineDocumentFileFormat.HtmlCss => AnimationExchangeService.GetSuggestedExtension(AnimationExchangeFormat.HtmlCss),
            _ => "timeline.json"
        };
    }

    public static string GetSuggestedFileName(string name, TimelineDocumentFileFormat format)
    {
        return $"{SanitizeFileName(StripKnownExtensions(name))}.{GetSuggestedExtension(format)}";
    }

    public static TimelineDocumentFileFormat DetectFormat(string? sourceLabel, string? content = null)
    {
        var extension = Path.GetExtension(sourceLabel ?? string.Empty).ToLowerInvariant();
        if (string.Equals(sourceLabel, ".timeline.json", StringComparison.OrdinalIgnoreCase) ||
            (sourceLabel?.EndsWith(".timeline.json", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return TimelineDocumentFileFormat.NativeProject;
        }

        if (extension is ".json")
        {
            return TimelineDocumentFileFormat.NativeProject;
        }

        if (extension is ".axaml" or ".xaml")
        {
            return TimelineDocumentFileFormat.AvaloniaXaml;
        }

        if (extension is ".xfl" or ".fla")
        {
            return TimelineDocumentFileFormat.FlashXfl;
        }

        if (extension is ".svg")
        {
            return TimelineDocumentFileFormat.SvgSmil;
        }

        if (extension is ".html" or ".htm" or ".xhtml")
        {
            return TimelineDocumentFileFormat.HtmlCss;
        }

        var trimmed = content?.TrimStart() ?? string.Empty;
        if (trimmed.StartsWith('{'))
        {
            return TimelineDocumentFileFormat.NativeProject;
        }

        if (trimmed.Contains("https://github.com/avaloniaui", StringComparison.OrdinalIgnoreCase) ||
            Regex.IsMatch(trimmed, @"<(UserControl|Window|Canvas|Grid|StackPanel|Border)\b", RegexOptions.IgnoreCase))
        {
            return TimelineDocumentFileFormat.AvaloniaXaml;
        }

        if (trimmed.Contains("<DOMDocument", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("ns.adobe.com/xfl", StringComparison.OrdinalIgnoreCase))
        {
            return TimelineDocumentFileFormat.FlashXfl;
        }

        if (trimmed.Contains("<svg", StringComparison.OrdinalIgnoreCase))
        {
            return TimelineDocumentFileFormat.SvgSmil;
        }

        if (trimmed.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase))
        {
            return TimelineDocumentFileFormat.HtmlCss;
        }

        return TimelineDocumentFileFormat.NativeProject;
    }

    public static async Task<TimelineDocumentFileLoadResult> LoadAsync(
        Stream stream,
        string sourceLabel,
        TimelineDocumentFileFormat? hintedFormat = null,
        CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        var data = buffer.ToArray();
        var content = Encoding.UTF8.GetString(data).TrimStart('\uFEFF');
        var format = hintedFormat ?? DetectFormat(sourceLabel, content);

        return format switch
        {
            TimelineDocumentFileFormat.NativeProject => new TimelineDocumentFileLoadResult
            {
                Format = format,
                Document = DocumentSerializer.FromJson(content),
                Summary = $"{GetDisplayName(format)} loaded as {Path.GetFileNameWithoutExtension(sourceLabel)}",
                Issues = []
            },
            TimelineDocumentFileFormat.AvaloniaXaml => MapLoadResult(AnimationExchangeService.Import(AnimationExchangeFormat.AvaloniaXaml, content, sourceLabel), format),
            TimelineDocumentFileFormat.FlashXfl => MapLoadResult(FlashXflExchangeService.ImportPackage(data, sourceLabel), format),
            TimelineDocumentFileFormat.SvgSmil => MapLoadResult(AnimationExchangeService.Import(AnimationExchangeFormat.SvgSmil, content, sourceLabel), format),
            TimelineDocumentFileFormat.HtmlCss => MapLoadResult(AnimationExchangeService.Import(AnimationExchangeFormat.HtmlCss, content, sourceLabel), format),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    public static async Task<TimelineDocumentFileSaveResult> SaveAsync(
        Stream stream,
        TimelineDocument document,
        TimelineDocumentFileFormat format,
        string? targetLabel = null,
        CancellationToken cancellationToken = default)
    {
        switch (format)
        {
            case TimelineDocumentFileFormat.NativeProject:
                await DocumentSerializer.SaveAsync(stream, document, cancellationToken);
                return new TimelineDocumentFileSaveResult
                {
                    Format = format,
                    Summary = $"{GetDisplayName(format)} saved from {document.Name}",
                    SuggestedFileName = GetSuggestedFileName(document.Name, format),
                    Issues = []
                };

            case TimelineDocumentFileFormat.AvaloniaXaml:
                return await SaveExchangeAsync(stream, document, format, AnimationExchangeFormat.AvaloniaXaml, cancellationToken);
            case TimelineDocumentFileFormat.FlashXfl:
                return await SaveFlashAsync(stream, document, format, targetLabel, cancellationToken);
            case TimelineDocumentFileFormat.SvgSmil:
                return await SaveExchangeAsync(stream, document, format, AnimationExchangeFormat.SvgSmil, cancellationToken);
            case TimelineDocumentFileFormat.HtmlCss:
                return await SaveExchangeAsync(stream, document, format, AnimationExchangeFormat.HtmlCss, cancellationToken);
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }

    public static Task<TimelineDocumentFileLoadResult> LoadFlashFolderAsync(
        string folderPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var label = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var result = FlashXflExchangeService.ImportPackageDirectory(folderPath, label);
        return Task.FromResult(MapLoadResult(result, TimelineDocumentFileFormat.FlashXfl));
    }

    public static Task<TimelineDocumentFileSaveResult> SaveFlashFolderAsync(
        string parentFolderPath,
        TimelineDocument document,
        string? folderName = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedFolderName = string.IsNullOrWhiteSpace(folderName)
            ? $"{SanitizeFileName(StripKnownExtensions(document.Name))}.xfl"
            : EnsureXflFolderName(folderName);
        var outputPath = Path.Combine(parentFolderPath, normalizedFolderName);
        FlashXflExchangeService.ExportPackageDirectory(outputPath, document);

        return Task.FromResult(new TimelineDocumentFileSaveResult
        {
            Format = TimelineDocumentFileFormat.FlashXfl,
            Summary = $"Adobe Animate XFL folder saved to {normalizedFolderName}",
            SuggestedFileName = normalizedFolderName,
            Issues =
            [
                new AnimationExchangeIssue
                {
                    Severity = AnimationExchangeIssueSeverity.Info,
                    Source = "Flash XFL",
                    Message = $"Saved as an uncompressed XFL folder at {outputPath}."
                }
            ]
        });
    }

    public static async Task<FlashDocumentConversionResult> ConvertFlashArchiveToFolderAsync(
        Stream stream,
        string sourceLabel,
        string parentFolderPath,
        string? folderName = null,
        CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        var data = buffer.ToArray();
        var package = FlashAuthoringPackageService.Read(data, sourceLabel);

        var baseName = string.IsNullOrWhiteSpace(folderName)
            ? StripKnownExtensions(Path.GetFileName(sourceLabel))
            : folderName;
        var outputFolderName = EnsureXflFolderName(baseName);
        var outputPath = Path.Combine(parentFolderPath, outputFolderName);

        FlashAuthoringPackageService.WriteDirectory(outputPath, new FlashAuthoringPackage
        {
            Kind = FlashAuthoringContainerKind.UncompressedXflDirectory,
            SourceLabel = outputPath,
            DocumentRelativePath = package.DocumentRelativePath,
            DocumentData = package.DocumentData,
            Entries = package.Entries
        });

        return new FlashDocumentConversionResult
        {
            Summary = $"Converted Flash archive to XFL folder {outputFolderName}",
            OutputPath = outputPath,
            Issues =
            [
                new AnimationExchangeIssue
                {
                    Severity = AnimationExchangeIssueSeverity.Info,
                    Source = "Flash XFL",
                    Message = $"Converted {sourceLabel} to the uncompressed XFL folder {outputPath}."
                }
            ]
        };
    }

    public static Task<FlashDocumentConversionResult> ConvertFlashFolderToArchiveAsync(
        string folderPath,
        Stream stream,
        string? targetLabel = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var package = FlashAuthoringPackageService.ReadDirectory(folderPath);
        FlashAuthoringPackageService.WriteArchive(stream, new FlashAuthoringPackage
        {
            Kind = FlashAuthoringContainerKind.CompressedFlaArchive,
            SourceLabel = targetLabel ?? folderPath,
            DocumentRelativePath = package.DocumentRelativePath,
            DocumentData = package.DocumentData,
            Entries = package.Entries
        });

        var outputName = string.IsNullOrWhiteSpace(targetLabel)
            ? $"{SanitizeFileName(StripKnownExtensions(Path.GetFileName(folderPath)))}.fla"
            : Path.GetFileName(targetLabel);

        return Task.FromResult(new FlashDocumentConversionResult
        {
            Summary = $"Converted XFL folder to Flash archive {outputName}",
            OutputPath = outputName,
            Issues =
            [
                new AnimationExchangeIssue
                {
                    Severity = AnimationExchangeIssueSeverity.Info,
                    Source = "Flash XFL",
                    Message = $"Converted the XFL folder {folderPath} to the packaged Flash archive {outputName}."
                }
            ]
        });
    }

    public static AnimationExchangeFormat? ToAnimationExchangeFormat(TimelineDocumentFileFormat format)
    {
        return format switch
        {
            TimelineDocumentFileFormat.AvaloniaXaml => AnimationExchangeFormat.AvaloniaXaml,
            TimelineDocumentFileFormat.FlashXfl => AnimationExchangeFormat.FlashXfl,
            TimelineDocumentFileFormat.SvgSmil => AnimationExchangeFormat.SvgSmil,
            TimelineDocumentFileFormat.HtmlCss => AnimationExchangeFormat.HtmlCss,
            _ => null
        };
    }

    private static async Task<TimelineDocumentFileSaveResult> SaveFlashAsync(
        Stream stream,
        TimelineDocument document,
        TimelineDocumentFileFormat format,
        string? targetLabel,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(targetLabel ?? string.Empty);
        if (string.Equals(extension, ".fla", StringComparison.OrdinalIgnoreCase))
        {
            FlashXflExchangeService.ExportPackage(stream, document);
            await stream.FlushAsync(cancellationToken);
            return new TimelineDocumentFileSaveResult
            {
                Format = format,
                Summary = $"{GetDisplayName(format)} packaged from {document.Name}",
                SuggestedFileName = $"{SanitizeFileName(StripKnownExtensions(document.Name))}.fla",
                Issues =
                [
                    new AnimationExchangeIssue
                    {
                        Severity = AnimationExchangeIssueSeverity.Info,
                        Source = "Flash XFL",
                        Message = "Saved as a packaged Flash authoring archive with DOMDocument.xml, LIBRARY symbol XML files, and PublishSettings.xml."
                    }
                ]
            };
        }

        return await SaveExchangeAsync(stream, document, format, AnimationExchangeFormat.FlashXfl, cancellationToken);
    }

    private static TimelineDocumentFileLoadResult MapLoadResult(AnimationExchangeImportResult result, TimelineDocumentFileFormat format)
    {
        return new TimelineDocumentFileLoadResult
        {
            Format = format,
            Document = result.Document,
            Summary = result.Summary,
            Issues = result.Issues
        };
    }

    private static async Task<TimelineDocumentFileSaveResult> SaveExchangeAsync(
        Stream stream,
        TimelineDocument document,
        TimelineDocumentFileFormat format,
        AnimationExchangeFormat exchangeFormat,
        CancellationToken cancellationToken)
    {
        var export = AnimationExchangeService.Export(document, exchangeFormat);
        await using var writer = new StreamWriter(stream, Encoding.UTF8, 1024, leaveOpen: true);
        await writer.WriteAsync(export.Content.AsMemory(), cancellationToken);
        await writer.FlushAsync(cancellationToken);
        return new TimelineDocumentFileSaveResult
        {
            Format = format,
            Summary = export.Summary,
            SuggestedFileName = export.SuggestedFileName,
            Issues = export.Issues
        };
    }

    private static string StripKnownExtensions(string value)
    {
        var normalized = Path.GetFileName(value ?? string.Empty);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "timeline";
        }

        if (normalized.EndsWith(".timeline.json", StringComparison.OrdinalIgnoreCase))
        {
            return normalized[..^(".timeline.json".Length)];
        }

        return Path.GetFileNameWithoutExtension(normalized);
    }

    private static string SanitizeFileName(string value)
    {
        var safe = Regex.Replace(string.IsNullOrWhiteSpace(value) ? "timeline" : value.Trim(), @"[^\w\-]+", "_");
        return string.IsNullOrWhiteSpace(safe) ? "timeline" : safe.ToLowerInvariant();
    }

    private static string EnsureXflFolderName(string value)
    {
        var baseName = SanitizeFileName(StripKnownExtensions(value));
        return baseName.EndsWith(".xfl", StringComparison.OrdinalIgnoreCase)
            ? baseName
            : $"{baseName}.xfl";
    }
}
