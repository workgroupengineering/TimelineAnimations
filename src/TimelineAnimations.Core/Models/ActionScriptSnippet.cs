namespace TimelineAnimations.Core.Models;

public sealed class ActionScriptSnippet
{
    public string Name { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public ActionScriptSnippetCategory Category { get; init; }

    public string Script { get; init; } = string.Empty;
}
