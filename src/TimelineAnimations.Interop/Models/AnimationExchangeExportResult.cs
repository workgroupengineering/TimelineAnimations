namespace TimelineAnimations.Core.Models;

public sealed class AnimationExchangeExportResult
{
    public required AnimationExchangeFormat Format { get; init; }

    public required string Content { get; init; }

    public required string SuggestedFileName { get; init; }

    public required string Summary { get; init; }

    public required IReadOnlyList<AnimationExchangeIssue> Issues { get; init; }
}
