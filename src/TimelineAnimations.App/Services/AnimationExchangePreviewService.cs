using TimelineAnimations.App.Models;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.Services;

public static class AnimationExchangePreviewService
{
    public static AnimationExchangePreviewResult BuildPreview(TimelineDocument document, AnimationExchangeFormat format)
    {
        var snapshot = DocumentSerializer.Clone(document);
        var export = AnimationExchangeService.Export(snapshot, format);
        var imported = AnimationExchangeService.Import(format, export.Content, export.SuggestedFileName);

        return new AnimationExchangePreviewResult
        {
            Format = format,
            Code = export.Content,
            SuggestedFileName = export.SuggestedFileName,
            Summary = $"{export.Summary} • preview snapshot ready",
            VisualSummary = format switch
            {
                AnimationExchangeFormat.AvaloniaXaml => "Runtime XAML preview uses Avalonia's runtime loader and falls back to the imported scene snapshot when needed.",
                AnimationExchangeFormat.FlashXfl => "Visual preview renders a round-trip scene snapshot reconstructed from Flash XFL timelines, packaged symbols, linkage metadata, classic text settings, and explicit motion tracks.",
                AnimationExchangeFormat.SvgSmil => "Visual preview renders a safe round-trip snapshot of the exported SVG / SMIL document.",
                AnimationExchangeFormat.HtmlCss => "Visual preview renders a safe round-trip snapshot of the exported HTML / CSS document.",
                _ => "Visual preview ready."
            },
            Issues = MergeIssues(export.Issues, imported.Issues),
            PreviewDocument = imported.Document
        };
    }

    private static IReadOnlyList<AnimationExchangeIssue> MergeIssues(
        IReadOnlyList<AnimationExchangeIssue> exportIssues,
        IReadOnlyList<AnimationExchangeIssue> importIssues)
    {
        var merged = new List<AnimationExchangeIssue>(exportIssues.Count + importIssues.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        AppendIssues(exportIssues, merged, seen);
        AppendIssues(importIssues, merged, seen);
        return merged;
    }

    private static void AppendIssues(
        IReadOnlyList<AnimationExchangeIssue> issues,
        List<AnimationExchangeIssue> target,
        HashSet<string> seen)
    {
        foreach (var issue in issues)
        {
            var key = $"{issue.Severity}|{issue.Source}|{issue.Message}";
            if (!seen.Add(key))
            {
                continue;
            }

            target.Add(issue);
        }
    }
}
