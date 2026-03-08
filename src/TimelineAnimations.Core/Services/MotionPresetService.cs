using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class MotionPresetService
{
    public static void EnsureBuiltInPresets(TimelineDocument document)
    {
        if (document.MotionPresets.Count > 0)
        {
            return;
        }

        document.MotionPresets.Add(CreateDriftPreset());
        document.MotionPresets.Add(CreatePunchPreset());
        document.MotionPresets.Add(CreateRevealPreset());
    }

    public static MotionPresetModel CapturePreset(TimelineLayer layer, string name, string description)
    {
        return new MotionPresetModel
        {
            Name = string.IsNullOrWhiteSpace(name) ? $"{layer.Name} Motion" : name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Defaults = DocumentSerializer.Clone(layer.Defaults),
            Tracks = layer.Tracks.Select(CloneTrack).ToList()
        };
    }

    public static bool ApplyPreset(TimelineLayer layer, MotionPresetModel preset)
    {
        if (layer.Kind is LayerKind.Folder or LayerKind.Audio)
        {
            return false;
        }

        layer.Defaults = DocumentSerializer.Clone(preset.Defaults);
        layer.Tracks = preset.Tracks.Select(CloneTrack).ToList();
        return true;
    }

    public static string EnsureUniquePresetName(TimelineDocument document, string? desiredName, Guid? currentId = null)
    {
        var baseName = string.IsNullOrWhiteSpace(desiredName) ? "Motion Preset" : desiredName.Trim();
        var candidate = baseName;
        var suffix = 2;
        while (document.MotionPresets.Any(item =>
                   item.Id != currentId &&
                   string.Equals(item.Name, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{baseName} {suffix++}";
        }

        return candidate;
    }

    private static MotionPresetModel CreateDriftPreset()
    {
        var layer = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Drift", "#24E5C1", string.Empty, 160d, 160d, 0);
        TimelineEditingService.ApplyValue(layer, AnimatedProperty.X, 160d, 0d, false, 6d);
        TimelineEditingService.ApplyValue(layer, AnimatedProperty.Y, 160d, 0d, false, 6d);
        TimelineEditingService.ApplyValue(layer, AnimatedProperty.X, 380d, 3d, false, 6d);
        TimelineEditingService.ApplyValue(layer, AnimatedProperty.Y, 128d, 3d, false, 6d);
        TimelineEditingService.ApplyValue(layer, AnimatedProperty.X, 520d, 6d, false, 6d);
        TimelineEditingService.ApplyValue(layer, AnimatedProperty.Y, 192d, 6d, false, 6d);
        var preset = CapturePreset(layer, "Drift Across", "Gentle lateral drift with a light vertical float.");
        preset.Tracks
            .SelectMany(track => track.Keyframes)
            .ToList()
            .ForEach(keyframe => keyframe.Easing = EasingKind.EaseInOut);
        return preset;
    }

    private static MotionPresetModel CreatePunchPreset()
    {
        var layer = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Punch", "#24E5C1", string.Empty, 240d, 180d, 0);
        TimelineEditingService.ApplyValue(layer, AnimatedProperty.ScaleX, 0.72d, 0d, false, 3d);
        TimelineEditingService.ApplyValue(layer, AnimatedProperty.ScaleY, 0.72d, 0d, false, 3d);
        TimelineEditingService.ApplyValue(layer, AnimatedProperty.ScaleX, 1.12d, 0.28d, false, 3d);
        TimelineEditingService.ApplyValue(layer, AnimatedProperty.ScaleY, 1.12d, 0.28d, false, 3d);
        TimelineEditingService.ApplyValue(layer, AnimatedProperty.ScaleX, 1d, 0.65d, false, 3d);
        TimelineEditingService.ApplyValue(layer, AnimatedProperty.ScaleY, 1d, 0.65d, false, 3d);
        var preset = CapturePreset(layer, "Punch In", "Short scale punch suitable for CTA emphasis.");
        preset.Tracks
            .SelectMany(track => track.Keyframes)
            .ToList()
            .ForEach(keyframe => keyframe.Easing = EasingKind.EaseOut);
        return preset;
    }

    private static MotionPresetModel CreateRevealPreset()
    {
        var layer = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Reveal", "#24E5C1", string.Empty, 220d, 220d, 0);
        TimelineEditingService.ApplyValue(layer, AnimatedProperty.Opacity, 0d, 0d, false, 4d);
        TimelineEditingService.ApplyValue(layer, AnimatedProperty.Y, 260d, 0d, false, 4d);
        TimelineEditingService.ApplyValue(layer, AnimatedProperty.Opacity, 1d, 0.45d, false, 4d);
        TimelineEditingService.ApplyValue(layer, AnimatedProperty.Y, 220d, 0.45d, false, 4d);
        TimelineEditingService.ApplyValue(layer, AnimatedProperty.Opacity, 1d, 4d, false, 4d);
        var preset = CapturePreset(layer, "Lift Reveal", "Opacity and vertical settle reveal.");
        preset.Tracks
            .SelectMany(track => track.Keyframes)
            .ToList()
            .ForEach(keyframe => keyframe.Easing = EasingKind.EaseOut);
        return preset;
    }

    private static LayerTrack CloneTrack(LayerTrack source)
    {
        return new LayerTrack
        {
            Property = source.Property,
            Keyframes = source.Keyframes
                .OrderBy(keyframe => keyframe.Time)
                .Select(keyframe => new KeyframeModel
                {
                    Time = keyframe.Time,
                    Value = keyframe.Value,
                    Easing = keyframe.Easing,
                    ControlPoint1X = keyframe.ControlPoint1X,
                    ControlPoint1Y = keyframe.ControlPoint1Y,
                    ControlPoint2X = keyframe.ControlPoint2X,
                    ControlPoint2Y = keyframe.ControlPoint2Y
                })
                .ToList()
        };
    }
}
