using System.Text.Json;
using TimelineAnimations.App.Models;

namespace TimelineAnimations.App.Services;

public static class RendererPreferencesPersistenceService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private static string SettingsFolder =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TimelineAnimationsStudio");

    private static string SettingsPath => Path.Combine(SettingsFolder, "renderer-preferences.json");

    public static RendererPreferencesState Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new RendererPreferencesState();
            }

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<RendererPreferencesState>(json, s_jsonOptions) ?? new RendererPreferencesState();
        }
        catch
        {
            return new RendererPreferencesState();
        }
    }

    public static void Save(RendererPreferencesState state)
    {
        Directory.CreateDirectory(SettingsFolder);
        var json = JsonSerializer.Serialize(state, s_jsonOptions);
        File.WriteAllText(SettingsPath, json);
    }
}
