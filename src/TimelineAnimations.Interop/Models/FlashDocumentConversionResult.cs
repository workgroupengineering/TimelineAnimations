namespace TimelineAnimations.Core.Models;

public sealed class FlashDocumentConversionResult
{
    public required string Summary { get; init; }

    public required string OutputPath { get; init; }

    public required IReadOnlyList<AnimationExchangeIssue> Issues { get; init; }
}
