using System.Text.Json;
using System.Text.Json.Serialization;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class DocumentSerializer
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

    public static async Task SaveAsync(Stream stream, TimelineDocument document, CancellationToken cancellationToken = default)
    {
        await JsonSerializer.SerializeAsync(stream, document, s_options, cancellationToken);
    }

    public static async Task<TimelineDocument> LoadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var document = await JsonSerializer.DeserializeAsync<TimelineDocument>(stream, s_options, cancellationToken);
        return document ?? SampleProjectFactory.Create();
    }

    public static string ToJson(TimelineDocument document)
    {
        return JsonSerializer.Serialize(document, s_options);
    }

    public static TimelineDocument FromJson(string json)
    {
        return JsonSerializer.Deserialize<TimelineDocument>(json, s_options) ?? SampleProjectFactory.Create();
    }

    public static T Clone<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, s_options);
        return JsonSerializer.Deserialize<T>(json, s_options)
            ?? throw new InvalidOperationException("Unable to clone value.");
    }
}
