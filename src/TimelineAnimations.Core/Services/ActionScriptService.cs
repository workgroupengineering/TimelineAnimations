using System.Text;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class ActionScriptService
{
    public static ActionScriptParseResult Parse(string? script)
    {
        var result = new ActionScriptParseResult();
        if (string.IsNullOrWhiteSpace(script))
        {
            return result;
        }

        var lines = script.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var rawLine = lines[index];
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var tokens = Tokenize(line);
            if (tokens.Count == 0)
            {
                continue;
            }

            var lineNumber = index + 1;
            var command = CreateCommand(tokens, lineNumber, line, result.Errors);
            if (command is not null)
            {
                result.Commands.Add(command);
            }
        }

        return result;
    }

    public static string GetSummary(string? script)
    {
        var parse = Parse(script);
        if (parse.Commands.Count == 0 && parse.Errors.Count == 0)
        {
            return "No scripted actions";
        }

        if (parse.Errors.Count > 0)
        {
            return $"{parse.Commands.Count} commands • {parse.Errors.Count} errors";
        }

        return parse.Commands.Count == 1
            ? "1 command ready"
            : $"{parse.Commands.Count} commands ready";
    }

    private static ActionScriptCommand? CreateCommand(
        IReadOnlyList<string> tokens,
        int lineNumber,
        string rawLine,
        ICollection<string> errors)
    {
        var keyword = tokens[0].Trim().ToLowerInvariant();
        return keyword switch
        {
            "play" => CreateFixedCommand(ActionScriptCommandKind.Play, tokens, lineNumber, rawLine, 0, errors),
            "stop" => CreateFixedCommand(ActionScriptCommandKind.Stop, tokens, lineNumber, rawLine, 0, errors),
            "goto_scene" => CreateVariableCommand(ActionScriptCommandKind.GoToScene, tokens, lineNumber, rawLine, 1, errors),
            "goto_label" => CreateVariableCommand(ActionScriptCommandKind.GoToLabel, tokens, lineNumber, rawLine, 1, errors),
            "goto_frame" => CreateFixedCommand(ActionScriptCommandKind.GoToFrame, tokens, lineNumber, rawLine, 1, errors),
            "show" => CreateVariableCommand(ActionScriptCommandKind.ShowLayer, tokens, lineNumber, rawLine, 1, errors),
            "hide" => CreateVariableCommand(ActionScriptCommandKind.HideLayer, tokens, lineNumber, rawLine, 1, errors),
            "set_var" => CreateVariableCommand(ActionScriptCommandKind.SetVariable, tokens, lineNumber, rawLine, 2, errors),
            "set_text" => CreateVariableCommand(ActionScriptCommandKind.SetText, tokens, lineNumber, rawLine, 2, errors),
            "set_button" => CreateFixedCommand(ActionScriptCommandKind.SetButtonState, tokens, lineNumber, rawLine, 2, errors),
            "set_state" => CreateFixedCommand(ActionScriptCommandKind.SetVisualState, tokens, lineNumber, rawLine, 3, errors),
            "trace" => CreateVariableCommand(ActionScriptCommandKind.Trace, tokens, lineNumber, rawLine, 1, errors),
            _ => AddUnknownCommandError(tokens[0], lineNumber, errors)
        };
    }

    private static ActionScriptCommand? CreateFixedCommand(
        ActionScriptCommandKind kind,
        IReadOnlyList<string> tokens,
        int lineNumber,
        string rawLine,
        int expectedArguments,
        ICollection<string> errors)
    {
        if (tokens.Count - 1 != expectedArguments)
        {
            errors.Add($"Line {lineNumber}: expected {expectedArguments} argument(s) for '{tokens[0]}'.");
            return null;
        }

        return new ActionScriptCommand
        {
            Kind = kind,
            LineNumber = lineNumber,
            RawText = rawLine,
            Arguments = [.. tokens.Skip(1)]
        };
    }

    private static ActionScriptCommand? CreateVariableCommand(
        ActionScriptCommandKind kind,
        IReadOnlyList<string> tokens,
        int lineNumber,
        string rawLine,
        int minimumArguments,
        ICollection<string> errors)
    {
        if (tokens.Count - 1 < minimumArguments)
        {
            errors.Add($"Line {lineNumber}: expected at least {minimumArguments} argument(s) for '{tokens[0]}'.");
            return null;
        }

        return new ActionScriptCommand
        {
            Kind = kind,
            LineNumber = lineNumber,
            RawText = rawLine,
            Arguments = [.. tokens.Skip(1)]
        };
    }

    private static ActionScriptCommand? AddUnknownCommandError(string keyword, int lineNumber, ICollection<string> errors)
    {
        errors.Add($"Line {lineNumber}: unknown action command '{keyword}'.");
        return null;
    }

    private static List<string> Tokenize(string line)
    {
        var tokens = new List<string>();
        var builder = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (!inQuotes && char.IsWhiteSpace(character))
            {
                FlushToken(builder, tokens);
                continue;
            }

            builder.Append(character);
        }

        FlushToken(builder, tokens);
        return tokens;
    }

    private static void FlushToken(StringBuilder builder, ICollection<string> tokens)
    {
        if (builder.Length == 0)
        {
            return;
        }

        tokens.Add(builder.ToString());
        builder.Clear();
    }
}
