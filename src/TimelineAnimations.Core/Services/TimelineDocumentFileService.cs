using System.Text;
using System.Text.RegularExpressions;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class TimelineDocumentFileService
{
    public static string GetDisplayName(TimelineDocumentFileFormat format)
    {
        return format switch
        {
            TimelineDocumentFileFormat.NativeProject => "Timeline Project",
            TimelineDocumentFileFormat.AvaloniaXaml => AnimationExchangeService.GetDisplayName(AnimationExchangeFormat.AvaloniaXaml),
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
            TimelineDocumentFileFormat.SvgSmil => MapLoadResult(AnimationExchangeService.Import(AnimationExchangeFormat.SvgSmil, content, sourceLabel), format),
            TimelineDocumentFileFormat.HtmlCss => MapLoadResult(AnimationExchangeService.Import(AnimationExchangeFormat.HtmlCss, content, sourceLabel), format),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    public static async Task<TimelineDocumentFileSaveResult> SaveAsync(
        Stream stream,
        TimelineDocument document,
        TimelineDocumentFileFormat format,
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
            case TimelineDocumentFileFormat.SvgSmil:
                return await SaveExchangeAsync(stream, document, format, AnimationExchangeFormat.SvgSmil, cancellationToken);
            case TimelineDocumentFileFormat.HtmlCss:
                return await SaveExchangeAsync(stream, document, format, AnimationExchangeFormat.HtmlCss, cancellationToken);
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }

    public static AnimationExchangeFormat? ToAnimationExchangeFormat(TimelineDocumentFileFormat format)
    {
        return format switch
        {
            TimelineDocumentFileFormat.AvaloniaXaml => AnimationExchangeFormat.AvaloniaXaml,
            TimelineDocumentFileFormat.SvgSmil => AnimationExchangeFormat.SvgSmil,
            TimelineDocumentFileFormat.HtmlCss => AnimationExchangeFormat.HtmlCss,
            _ => null
        };
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
}
