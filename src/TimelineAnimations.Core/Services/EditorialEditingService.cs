using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class EditorialEditingService
{
    public static TimelineLayer? RazorSplitLayer(
        TimelineLayer layer,
        MediaAsset? mediaAsset,
        double splitTime,
        int newZIndex)
    {
        if (!MediaTimelineService.IsMediaLayer(layer))
        {
            return null;
        }

        var clipStart = Math.Max(0d, layer.Media.StartTime);
        var clipDuration = MediaTimelineService.GetClipDuration(layer, mediaAsset);
        var relativeTime = splitTime - clipStart;
        if (relativeTime <= 0.02d || relativeTime >= clipDuration - 0.02d)
        {
            return null;
        }

        var right = TimelineEditingService.DuplicateLayer(layer, newZIndex);
        right.Name = $"{layer.Name} B";
        right.Media.StartTime = splitTime;
        right.Media.ClipOffset = layer.Media.ClipOffset + relativeTime;
        right.Media.ClipDuration = Math.Max(0.05d, clipDuration - relativeTime);
        right.ParentLayerId = layer.ParentLayerId;

        layer.Media.ClipDuration = Math.Max(0.05d, relativeTime);
        return right;
    }

    public static bool Slip(TimelineLayer layer, MediaAsset? mediaAsset, double deltaSeconds)
    {
        if (!MediaTimelineService.IsMediaLayer(layer))
        {
            return false;
        }

        var clipDuration = MediaTimelineService.GetClipDuration(layer, mediaAsset);
        var assetDuration = Math.Max(clipDuration, mediaAsset?.Duration ?? clipDuration);
        var maxOffset = Math.Max(0d, assetDuration - clipDuration);
        var nextOffset = TimelineMath.Clamp(layer.Media.ClipOffset + deltaSeconds, 0d, maxOffset);
        if (Math.Abs(nextOffset - layer.Media.ClipOffset) < 0.0001d)
        {
            return false;
        }

        layer.Media.ClipOffset = nextOffset;
        return true;
    }

    public static bool Slide(IList<TimelineLayer> layers, Guid layerId, double deltaSeconds)
    {
        var ordered = layers
            .Where(MediaTimelineService.IsMediaLayer)
            .OrderBy(item => item.Media.StartTime)
            .ToList();
        var index = ordered.FindIndex(item => item.Id == layerId);
        if (index < 0)
        {
            return false;
        }

        var layer = ordered[index];
        var clipDuration = Math.Max(0.05d, layer.Media.ClipDuration);
        var previousEnd = index > 0
            ? ordered[index - 1].Media.StartTime + Math.Max(0.05d, ordered[index - 1].Media.ClipDuration)
            : 0d;
        var nextStart = index < ordered.Count - 1
            ? ordered[index + 1].Media.StartTime
            : double.PositiveInfinity;
        var nextStartMin = index < ordered.Count - 1
            ? previousEnd + clipDuration
            : double.PositiveInfinity;
        var minStart = previousEnd;
        var maxStart = double.IsPositiveInfinity(nextStart) ? Math.Max(minStart, layer.Media.StartTime + 60d) : Math.Max(minStart, nextStart - clipDuration);
        var targetStart = TimelineMath.Clamp(layer.Media.StartTime + deltaSeconds, minStart, maxStart);
        if (Math.Abs(targetStart - layer.Media.StartTime) < 0.0001d)
        {
            return false;
        }

        var appliedDelta = targetStart - layer.Media.StartTime;
        layer.Media.StartTime = targetStart;

        if (index > 0)
        {
            ordered[index - 1].Media.ClipDuration = Math.Max(0.05d, ordered[index - 1].Media.ClipDuration + appliedDelta);
        }

        if (index < ordered.Count - 1)
        {
            ordered[index + 1].Media.StartTime = Math.Max(nextStartMin, ordered[index + 1].Media.StartTime + appliedDelta);
        }

        return true;
    }

    public static bool Ripple(IList<TimelineLayer> layers, Guid layerId, double deltaSeconds)
    {
        var ordered = layers
            .Where(MediaTimelineService.IsMediaLayer)
            .OrderBy(item => item.Media.StartTime)
            .ToList();
        var index = ordered.FindIndex(item => item.Id == layerId);
        if (index < 0)
        {
            return false;
        }

        var layer = ordered[index];
        var nextDuration = Math.Max(0.05d, layer.Media.ClipDuration + deltaSeconds);
        var appliedDelta = nextDuration - layer.Media.ClipDuration;
        if (Math.Abs(appliedDelta) < 0.0001d)
        {
            return false;
        }

        layer.Media.ClipDuration = nextDuration;
        for (var candidateIndex = index + 1; candidateIndex < ordered.Count; candidateIndex++)
        {
            ordered[candidateIndex].Media.StartTime = Math.Max(0d, ordered[candidateIndex].Media.StartTime + appliedDelta);
        }

        return true;
    }
}

