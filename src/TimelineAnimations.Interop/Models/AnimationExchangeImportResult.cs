namespace TimelineAnimations.Core.Models;

public sealed class AnimationExchangeImportResult
{
    public required AnimationExchangeFormat Format { get; init; }

    public required TimelineDocument Document { get; init; }

    public required string Summary { get; init; }

    public required IReadOnlyList<AnimationExchangeIssue> Issues { get; init; }
}
