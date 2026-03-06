namespace TimelineAnimations.Core.Models;

public sealed class ActionScriptCommand
{
    public int LineNumber { get; init; }

    public ActionScriptCommandKind Kind { get; init; }

    public IReadOnlyList<string> Arguments { get; init; } = [];

    public string RawText { get; init; } = string.Empty;
}
