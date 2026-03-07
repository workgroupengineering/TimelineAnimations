using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class PowerEditingService
{
    public static bool ApplyValueAcrossFrames(
        TimelineLayer layer,
        AnimatedProperty property,
        double value,
        int startFrame,
        int endFrame,
        double frameRate,
        double duration)
    {
        if (layer.Kind == LayerKind.Audio || layer.Kind == LayerKind.Folder)
        {
            return false;
        }

        var normalizedStart = Math.Min(startFrame, endFrame);
        var normalizedEnd = Math.Max(startFrame, endFrame);
        var updated = false;
        for (var frame = normalizedStart; frame <= normalizedEnd; frame++)
        {
            var time = FrameTimelineService.FrameToTime(frame, frameRate);
            TimelineEditingService.SetKeyframe(layer, property, time, value, duration);
            updated = true;
        }

        return updated;
    }

    public static bool ApplyBoundsAcrossFrames(
        TimelineLayer layer,
        double x,
        double y,
        double width,
        double height,
        int startFrame,
        int endFrame,
        double frameRate,
        double duration)
    {
        var updated = ApplyValueAcrossFrames(layer, AnimatedProperty.X, x, startFrame, endFrame, frameRate, duration);
        updated |= ApplyValueAcrossFrames(layer, AnimatedProperty.Y, y, startFrame, endFrame, frameRate, duration);
        updated |= ApplyValueAcrossFrames(layer, AnimatedProperty.Width, Math.Max(24d, width), startFrame, endFrame, frameRate, duration);
        updated |= ApplyValueAcrossFrames(layer, AnimatedProperty.Height, Math.Max(24d, height), startFrame, endFrame, frameRate, duration);
        return updated;
    }
}
