namespace TimelineAnimations.Core.Models;

public sealed class ActionScriptParseResult
{
    public List<ActionScriptCommand> Commands { get; } = [];

    public List<string> Errors { get; } = [];

    public bool HasErrors => Errors.Count > 0;
}
