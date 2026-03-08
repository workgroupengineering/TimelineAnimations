namespace TimelineAnimations.Core.Models;

public sealed class TimelineDocumentFileLoadResult
{
    public required TimelineDocumentFileFormat Format { get; init; }

    public required TimelineDocument Document { get; init; }

    public required string Summary { get; init; }

    public required IReadOnlyList<AnimationExchangeIssue> Issues { get; init; }
}
