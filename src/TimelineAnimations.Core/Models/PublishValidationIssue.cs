namespace TimelineAnimations.Core.Models;

public sealed class PublishValidationIssue
{
    public string Severity { get; set; } = "Warning";

    public string Message { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;
}
