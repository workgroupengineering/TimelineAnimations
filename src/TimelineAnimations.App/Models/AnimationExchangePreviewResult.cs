using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.Models;

public sealed class AnimationExchangePreviewResult
{
    public required AnimationExchangeFormat Format { get; init; }

    public required string Code { get; init; }

    public required string SuggestedFileName { get; init; }

    public required string Summary { get; init; }

    public required string VisualSummary { get; init; }

    public required IReadOnlyList<AnimationExchangeIssue> Issues { get; init; }

    public required TimelineDocument PreviewDocument { get; init; }
}
