namespace TimelineAnimations.Core.Models;

public sealed class TimelineDocumentFileSaveResult
{
    public required TimelineDocumentFileFormat Format { get; init; }

    public required string Summary { get; init; }

    public required string SuggestedFileName { get; init; }

    public required IReadOnlyList<AnimationExchangeIssue> Issues { get; init; }
}
