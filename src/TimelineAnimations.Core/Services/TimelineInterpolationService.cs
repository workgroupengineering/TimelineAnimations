using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class TimelineInterpolationService
{
    public static LayerSnapshot SampleLayer(TimelineLayer layer, double time)
    {
        var shapeGeometry = SamplePathGeometry(layer, time);
        return new LayerSnapshot(
            X: SampleProperty(layer, AnimatedProperty.X, time),
            Y: SampleProperty(layer, AnimatedProperty.Y, time),
            Width: SampleProperty(layer, AnimatedProperty.Width, time),
            Height: SampleProperty(layer, AnimatedProperty.Height, time),
            ScaleX: SampleProperty(layer, AnimatedProperty.ScaleX, time),
            ScaleY: SampleProperty(layer, AnimatedProperty.ScaleY, time),
            SkewX: SampleProperty(layer, AnimatedProperty.SkewX, time),
            SkewY: SampleProperty(layer, AnimatedProperty.SkewY, time),
            Rotation: SampleProperty(layer, AnimatedProperty.Rotation, time),
            Opacity: SampleProperty(layer, AnimatedProperty.Opacity, time),
            CornerRadius: layer.Style.CornerRadius,
            Fill: layer.Style.Fill,
            Stroke: layer.Style.Stroke,
            Text: layer.Style.Text,
            FontSize: layer.Style.FontSize,
            StrokeThickness: layer.Style.StrokeThickness,
            UseGradient: layer.Style.UseGradient,
            GradientFrom: layer.Style.GradientFrom,
            GradientTo: layer.Style.GradientTo,
            IsClosed: shapeGeometry.IsClosed,
            ShowAsOutline: layer.ShowAsOutline,
            OutlineColor: layer.OutlineColor,
            PathPoints: shapeGeometry.PathPoints,
            AvaloniaControl: layer.Style.AvaloniaControl.Clone(),
            Compositing: layer.Compositing.Clone(),
            TextSettings: layer.Style.TextSettings.Clone())
        {
            HasFill = layer.Style.HasFill,
            HasStroke = layer.Style.HasStroke,
            GradientKind = layer.Style.GradientKind,
            GradientAngle = layer.Style.GradientAngle,
            StrokeCapStyle = layer.Style.StrokeCapStyle,
            StrokeJoinStyle = layer.Style.StrokeJoinStyle,
            StrokeMiterLimit = layer.Style.StrokeMiterLimit
        };
    }

    public static (bool IsClosed, IReadOnlyList<VectorPointModel> PathPoints) SamplePathGeometry(TimelineLayer layer, double time)
    {
        if (layer.Kind != LayerKind.Path)
        {
            return (layer.Style.IsClosed, VectorPathService.ClonePoints(layer.Style.PathPoints));
        }

        var ordered = layer.ShapeKeyframes
            .OrderBy(item => item.Time)
            .ToList();
        if (ordered.Count == 0)
        {
            return (layer.Style.IsClosed, VectorPathService.ClonePoints(layer.Style.PathPoints));
        }

        ShapeKeyframeModel? previous = null;
        ShapeKeyframeModel? next = null;
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

        if (previous is null)
        {
            var first = next!;
            if (Math.Abs(first.Time) < 0.0001d || Math.Abs(first.Time - time) < 0.0001d)
            {
                return (first.IsClosed, VectorPathService.ClonePoints(first.PathPoints));
            }

            var defaultPoints = VectorPathService.ClonePoints(layer.Style.PathPoints);
            if (CanInterpolate(defaultPoints, first.PathPoints))
            {
                var progress = first.Time < 0.0001d ? 1d : time / first.Time;
                var eased = TimelineEasingService.Apply(first, progress);
                return (ResolveClosedState(layer.Style.IsClosed, first.IsClosed, eased), InterpolatePathPoints(defaultPoints, first.PathPoints, eased));
            }

            return (layer.Style.IsClosed, defaultPoints);
        }

        if (next is null || ReferenceEquals(previous, next) || Math.Abs(next.Time - previous.Time) < 0.0001d)
        {
            return (previous.IsClosed, VectorPathService.ClonePoints(previous.PathPoints));
        }

        if (!CanInterpolate(previous.PathPoints, next.PathPoints))
        {
            return (previous.IsClosed, VectorPathService.ClonePoints(previous.PathPoints));
        }

        var morphProgress = (time - previous.Time) / (next.Time - previous.Time);
        var easedProgress = TimelineEasingService.Apply(next, morphProgress);
        return (ResolveClosedState(previous.IsClosed, next.IsClosed, easedProgress), InterpolatePathPoints(previous.PathPoints, next.PathPoints, easedProgress));
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
        var easedProgress = TimelineEasingService.Apply(next, progress);
        return previous.Value + ((next.Value - previous.Value) * easedProgress);
    }

    public static double GetDefaultValue(TimelineLayer layer, AnimatedProperty property)
    {
        return property switch
        {
            AnimatedProperty.X => layer.Defaults.X,
            AnimatedProperty.Y => layer.Defaults.Y,
            AnimatedProperty.Width => layer.Defaults.Width,
            AnimatedProperty.Height => layer.Defaults.Height,
            AnimatedProperty.ScaleX => layer.Defaults.ScaleX,
            AnimatedProperty.ScaleY => layer.Defaults.ScaleY,
            AnimatedProperty.SkewX => layer.Defaults.SkewX,
            AnimatedProperty.SkewY => layer.Defaults.SkewY,
            AnimatedProperty.Rotation => layer.Defaults.Rotation,
            AnimatedProperty.Opacity => layer.Defaults.Opacity,
            _ => throw new ArgumentOutOfRangeException(nameof(property), property, null)
        };
    }

    private static bool CanInterpolate(IReadOnlyList<VectorPointModel> previous, IReadOnlyList<VectorPointModel> next)
    {
        return previous.Count > 0 && previous.Count == next.Count;
    }

    private static List<VectorPointModel> InterpolatePathPoints(IReadOnlyList<VectorPointModel> previous, IReadOnlyList<VectorPointModel> next, double progress)
    {
        var eased = TimelineMath.Clamp(progress, 0, 1);
        var points = new List<VectorPointModel>(previous.Count);
        for (var index = 0; index < previous.Count; index++)
        {
            points.Add(new VectorPointModel
            {
                X = previous[index].X + ((next[index].X - previous[index].X) * eased),
                Y = previous[index].Y + ((next[index].Y - previous[index].Y) * eased)
            });
        }

        return points;
    }

    private static bool ResolveClosedState(bool previous, bool next, double progress)
    {
        if (previous == next)
        {
            return previous;
        }

        return progress > 0.5d ? next : previous;
    }
}
