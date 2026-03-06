namespace TimelineAnimations.Core.Models;

public sealed class AnimationExchangeIssue
{
    public required AnimationExchangeIssueSeverity Severity { get; init; }

    public required string Source { get; init; }

    public required string Message { get; init; }
}
