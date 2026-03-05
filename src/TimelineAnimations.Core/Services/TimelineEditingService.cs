using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class TimelineEditingService
{
    private const double TimeTolerance = 0.0001d;

    public static TimelineLayer CreateLayer(
        LayerKind kind,
        string name,
        string fill,
        string text,
        double x,
        double y,
        int zIndex)
    {
        var defaults = kind switch
        {
            LayerKind.Rectangle => new LayerDefaults { X = x, Y = y, Width = 240, Height = 152, Opacity = 1 },
            LayerKind.Ellipse => new LayerDefaults { X = x, Y = y, Width = 184, Height = 184, Opacity = 0.9 },
            LayerKind.Text => new LayerDefaults { X = x, Y = y, Width = 360, Height = 92, Opacity = 1 },
            _ => new LayerDefaults { X = x, Y = y }
        };

        var style = new LayerStyle
        {
            Fill = fill,
            Stroke = "#FFFFFF",
            Text = kind == LayerKind.Text ? text : string.Empty,
            FontSize = kind == LayerKind.Text ? 48 : 42,
            CornerRadius = kind == LayerKind.Rectangle ? 28 : 999
        };

        return new TimelineLayer
        {
            Name = name,
            Kind = kind,
            ZIndex = zIndex,
            Defaults = defaults,
            Style = style,
            Tracks = CreateDefaultTracks(defaults)
        };
    }

    public static TimelineLayer DuplicateLayer(TimelineLayer source, int newZIndex)
    {
        var clone = DocumentSerializer.Clone(source);
        clone.Id = Guid.NewGuid();
        clone.Name = $"{source.Name} Copy";
        clone.ZIndex = newZIndex;

        foreach (var track in clone.Tracks)
        {
            foreach (var keyframe in track.Keyframes)
            {
                keyframe.Id = Guid.NewGuid();
            }
        }

        return clone;
    }

    public static void AddLayer(TimelineDocument document, TimelineLayer layer)
    {
        document.Layers.Add(layer);
        NormalizeZOrder(document);
    }

    public static bool RemoveLayer(TimelineDocument document, Guid layerId)
    {
        var removed = document.Layers.RemoveAll(layer => layer.Id == layerId) > 0;
        if (removed)
        {
            NormalizeZOrder(document);
        }

        return removed;
    }

    public static LayerTrack GetOrCreateTrack(TimelineLayer layer, AnimatedProperty property)
    {
        var track = layer.Tracks.FirstOrDefault(item => item.Property == property);
        if (track is not null)
        {
            return track;
        }

        track = new LayerTrack { Property = property };
        layer.Tracks.Add(track);
        layer.Tracks.Sort(static (left, right) => left.Property.CompareTo(right.Property));
        return track;
    }

    public static KeyframeModel SetKeyframe(TimelineLayer layer, AnimatedProperty property, double time, double value, double duration)
    {
        var clampedTime = TimelineMath.Clamp(time, 0, duration);
        var track = GetOrCreateTrack(layer, property);
        var existing = track.Keyframes.FirstOrDefault(item => Math.Abs(item.Time - clampedTime) < TimeTolerance);

        if (existing is not null)
        {
            existing.Value = value;
            SortTrack(track);
            return existing;
        }

        var keyframe = new KeyframeModel
        {
            Time = clampedTime,
            Value = value
        };

        track.Keyframes.Add(keyframe);
        SortTrack(track);
        return keyframe;
    }

    public static bool RemoveKeyframe(TimelineLayer layer, AnimatedProperty property, Guid keyframeId)
    {
        var track = layer.Tracks.FirstOrDefault(item => item.Property == property);
        return track is not null && track.Keyframes.RemoveAll(item => item.Id == keyframeId) > 0;
    }

    public static bool MoveKeyframe(TimelineLayer layer, AnimatedProperty property, Guid keyframeId, double time, double duration)
    {
        var track = layer.Tracks.FirstOrDefault(item => item.Property == property);
        var keyframe = track?.Keyframes.FirstOrDefault(item => item.Id == keyframeId);

        if (track is null || keyframe is null)
        {
            return false;
        }

        keyframe.Time = TimelineMath.Clamp(time, 0, duration);

        var duplicates = track.Keyframes
            .Where(item => item.Id != keyframeId && Math.Abs(item.Time - keyframe.Time) < TimeTolerance)
            .ToList();

        foreach (var duplicate in duplicates)
        {
            track.Keyframes.Remove(duplicate);
        }

        SortTrack(track);
        return true;
    }

    public static void ApplyValue(TimelineLayer layer, AnimatedProperty property, double value, double time, bool createKeyframe, double duration)
    {
        if (createKeyframe)
        {
            SetKeyframe(layer, property, time, value, duration);
            return;
        }

        UpdateDefaultValue(layer, property, value);
    }

    public static void UpdateDefaultValue(TimelineLayer layer, AnimatedProperty property, double value)
    {
        switch (property)
        {
            case AnimatedProperty.X:
                layer.Defaults.X = value;
                break;
            case AnimatedProperty.Y:
                layer.Defaults.Y = value;
                break;
            case AnimatedProperty.Width:
                layer.Defaults.Width = Math.Max(24, value);
                break;
            case AnimatedProperty.Height:
                layer.Defaults.Height = Math.Max(24, value);
                break;
            case AnimatedProperty.Rotation:
                layer.Defaults.Rotation = value;
                break;
            case AnimatedProperty.Opacity:
                layer.Defaults.Opacity = TimelineMath.Clamp(value, 0, 1);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(property), property, null);
        }
    }

    public static void NormalizeZOrder(TimelineDocument document)
    {
        for (var index = 0; index < document.Layers.Count; index++)
        {
            document.Layers[index].ZIndex = index;
        }
    }

    public static void BringForward(TimelineDocument document, Guid layerId)
    {
        var ordered = document.Layers.OrderBy(layer => layer.ZIndex).ToList();
        var index = ordered.FindIndex(layer => layer.Id == layerId);
        if (index < 0 || index == ordered.Count - 1)
        {
            return;
        }

        (ordered[index], ordered[index + 1]) = (ordered[index + 1], ordered[index]);
        document.Layers = ordered;
        NormalizeZOrder(document);
    }

    public static void SendBackward(TimelineDocument document, Guid layerId)
    {
        var ordered = document.Layers.OrderBy(layer => layer.ZIndex).ToList();
        var index = ordered.FindIndex(layer => layer.Id == layerId);
        if (index <= 0)
        {
            return;
        }

        (ordered[index], ordered[index - 1]) = (ordered[index - 1], ordered[index]);
        document.Layers = ordered;
        NormalizeZOrder(document);
    }

    private static List<LayerTrack> CreateDefaultTracks(LayerDefaults defaults)
    {
        return
        [
            new LayerTrack
            {
                Property = AnimatedProperty.X,
                Keyframes =
                [
                    new KeyframeModel { Time = 0, Value = defaults.X }
                ]
            },
            new LayerTrack
            {
                Property = AnimatedProperty.Y,
                Keyframes =
                [
                    new KeyframeModel { Time = 0, Value = defaults.Y }
                ]
            },
            new LayerTrack
            {
                Property = AnimatedProperty.Width,
                Keyframes =
                [
                    new KeyframeModel { Time = 0, Value = defaults.Width }
                ]
            },
            new LayerTrack
            {
                Property = AnimatedProperty.Height,
                Keyframes =
                [
                    new KeyframeModel { Time = 0, Value = defaults.Height }
                ]
            },
            new LayerTrack
            {
                Property = AnimatedProperty.Rotation,
                Keyframes =
                [
                    new KeyframeModel { Time = 0, Value = defaults.Rotation }
                ]
            },
            new LayerTrack
            {
                Property = AnimatedProperty.Opacity,
                Keyframes =
                [
                    new KeyframeModel { Time = 0, Value = defaults.Opacity }
                ]
            }
        ];
    }

    private static void SortTrack(LayerTrack track)
    {
        track.Keyframes.Sort(static (left, right) => left.Time.CompareTo(right.Time));
    }
}
