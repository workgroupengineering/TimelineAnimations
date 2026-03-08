using System.Text.Json;
using System.Text.Json.Serialization;
using TimelineAnimations.App.Models;

namespace TimelineAnimations.App.Services;

public static class WorkspaceLayoutPersistenceService
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public static string DefaultPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TimelineAnimationsStudio",
            "workspace-layout.json");

    public static WorkspaceLayoutState Load()
    {
        return Load(DefaultPath);
    }

    public static WorkspaceLayoutState Load(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return new WorkspaceLayoutState();
            }

            var json = File.ReadAllText(path);
            var state = JsonSerializer.Deserialize<WorkspaceLayoutState>(json, s_options) ?? new WorkspaceLayoutState();
            Normalize(state);
            return state;
        }
        catch
        {
            return new WorkspaceLayoutState();
        }
    }

    public static bool Save(WorkspaceLayoutState state)
    {
        return Save(DefaultPath, state);
    }

    public static bool Save(string path, WorkspaceLayoutState state)
    {
        try
        {
            Normalize(state);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(state, s_options);
            File.WriteAllText(path, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void Normalize(WorkspaceLayoutState state)
    {
        state.LeftDockWidth = Math.Max(220, state.LeftDockWidth);
        state.RightDockWidth = Math.Max(260, state.RightDockWidth);
        state.TimelineDockHeight = Math.Max(240, state.TimelineDockHeight);
        state.LeftRestoreMode = NormalizeRestoreMode(state.LeftRestoreMode);
        state.RightRestoreMode = NormalizeRestoreMode(state.RightRestoreMode);
        state.TimelineRestoreMode = NormalizeRestoreMode(state.TimelineRestoreMode);
    }

    private static WorkspacePanelMode NormalizeRestoreMode(WorkspacePanelMode mode)
    {
        return mode == WorkspacePanelMode.Hidden ? WorkspacePanelMode.Docked : mode;
    }
}
