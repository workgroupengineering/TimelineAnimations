using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class TimelineInterpolationService
{
    public static LayerSnapshot SampleLayer(TimelineLayer layer, double time)
    {
        return new LayerSnapshot(
            X: SampleProperty(layer, AnimatedProperty.X, time),
            Y: SampleProperty(layer, AnimatedProperty.Y, time),
            Width: SampleProperty(layer, AnimatedProperty.Width, time),
            Height: SampleProperty(layer, AnimatedProperty.Height, time),
            Rotation: SampleProperty(layer, AnimatedProperty.Rotation, time),
            Opacity: SampleProperty(layer, AnimatedProperty.Opacity, time),
            CornerRadius: layer.Style.CornerRadius,
            Fill: layer.Style.Fill,
            Stroke: layer.Style.Stroke,
            Text: layer.Style.Text,
            FontSize: layer.Style.FontSize);
    }

    public static double SampleProperty(TimelineLayer layer, AnimatedProperty property, double time)
    {
        var track = layer.Tracks.FirstOrDefault(item => item.Property == property);
        var fallback = GetDefaultValue(layer, property);

        if (track is null || track.Keyframes.Count == 0)
        {
            return fallback;
        }

        var ordered = track.Keyframes.OrderBy(item => item.Time).ToList();
        KeyframeModel? previous = null;
        KeyframeModel? next = null;

        foreach (var keyframe in ordered)
        {
            if (keyframe.Time <= time)
            {
                previous = keyframe;
            }

            if (keyframe.Time >= time)
            {
                next = keyframe;
                break;
            }
        }

        if (previous is null && next is null)
        {
            return fallback;
        }

        if (previous is null)
        {
            return Math.Abs(next!.Time - time) < 0.0001d ? next.Value : fallback;
        }

        if (next is null || ReferenceEquals(previous, next) || Math.Abs(next.Time - previous.Time) < 0.0001d)
        {
            return previous.Value;
        }

        var progress = (time - previous.Time) / (next.Time - previous.Time);
        return previous.Value + ((next.Value - previous.Value) * progress);
    }

    public static double GetDefaultValue(TimelineLayer layer, AnimatedProperty property)
    {
        return property switch
        {
            AnimatedProperty.X => layer.Defaults.X,
            AnimatedProperty.Y => layer.Defaults.Y,
            AnimatedProperty.Width => layer.Defaults.Width,
            AnimatedProperty.Height => layer.Defaults.Height,
            AnimatedProperty.Rotation => layer.Defaults.Rotation,
            AnimatedProperty.Opacity => layer.Defaults.Opacity,
            _ => throw new ArgumentOutOfRangeException(nameof(property), property, null)
        };
    }
}
