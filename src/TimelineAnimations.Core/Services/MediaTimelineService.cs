using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class MediaTimelineService
{
    public static bool IsMediaLayer(TimelineLayer layer)
    {
        return (layer.Kind is LayerKind.Audio or LayerKind.Video) && layer.Media.SourceMediaAssetId is not null;
    }

    public static bool IsStageRenderable(TimelineLayer layer)
    {
        return layer.Kind is not (LayerKind.Audio or LayerKind.Folder);
    }

    public static double GetClipDuration(TimelineLayer layer, MediaAsset? asset)
    {
        var assetDuration = Math.Max(0.05d, asset?.Duration ?? 1d);
        return Math.Max(0.05d, layer.Media.ClipDuration > 0d ? layer.Media.ClipDuration : assetDuration);
    }

    public static double GetPlaybackDuration(TimelineLayer layer, MediaAsset? asset)
    {
        var clipDuration = GetClipDuration(layer, asset);
        if (layer.Media.PlaybackMode is MediaPlaybackMode.Stream or MediaPlaybackMode.Stop || asset is null)
        {
            return layer.Media.PlaybackMode == MediaPlaybackMode.Stop ? 0d : clipDuration;
        }

        if (IsRepeatingForever(layer))
        {
            return clipDuration;
        }

        var repeatedDuration = Math.Max(0.05d, (Math.Max(0.05d, asset.Duration) * Math.Max(1, layer.Media.RepeatCount)) - layer.Media.ClipOffset);
        return Math.Max(clipDuration, repeatedDuration);
    }

    public static double GetClipEndTime(TimelineLayer layer, MediaAsset? asset)
    {
        return layer.Media.StartTime + GetPlaybackDuration(layer, asset);
    }

    public static double? ResolveMediaTime(TimelineLayer layer, MediaAsset asset, double sceneTime)
    {
        var assetDuration = Math.Max(0.05d, asset.Duration);
        var clipDuration = GetClipDuration(layer, asset);
        var playbackDuration = GetPlaybackDuration(layer, asset);
        var clipStart = layer.Media.StartTime;

        if (sceneTime < clipStart)
        {
            return null;
        }

        if (layer.Media.PlaybackMode == MediaPlaybackMode.Stop)
        {
            return null;
        }

        var clipTime = sceneTime - clipStart;
        if (layer.Media.PlaybackMode == MediaPlaybackMode.Stream && clipTime > clipDuration)
        {
            return null;
        }

        if (layer.Media.PlaybackMode is MediaPlaybackMode.Start or MediaPlaybackMode.Event && !IsRepeatingForever(layer) && clipTime > playbackDuration)
        {
            return null;
        }

        var mediaTime = layer.Media.ClipOffset + clipTime;
        if (IsRepeatingForever(layer))
        {
            return Mod(mediaTime, assetDuration);
        }

        var repeatedSpan = assetDuration * Math.Max(1, layer.Media.RepeatCount);
        if (layer.Media.PlaybackMode is MediaPlaybackMode.Start or MediaPlaybackMode.Event)
        {
            if (mediaTime >= repeatedSpan)
            {
                return null;
            }

            return TimelineMath.Clamp(Mod(mediaTime, assetDuration), 0d, assetDuration);
        }

        return mediaTime > assetDuration ? null : TimelineMath.Clamp(mediaTime, 0d, assetDuration);
    }

    public static int GetClipStartFrame(TimelineLayer layer, double frameRate, int totalFrames)
    {
        return FrameTimelineService.TimeToFrame(Math.Max(0d, layer.Media.StartTime), frameRate, totalFrames);
    }

    public static int GetClipEndFrame(TimelineLayer layer, MediaAsset? asset, double frameRate, int totalFrames)
    {
        var clipDuration = GetClipDuration(layer, asset);
        var endTime = Math.Max(0d, layer.Media.StartTime + clipDuration);
        return FrameTimelineService.TimeToFrame(endTime, frameRate, totalFrames);
    }

    public static int ResolveFrameIndex(MediaAsset asset, double mediaTime)
    {
        if (asset.Frames.Count == 0)
        {
            return -1;
        }

        if (asset.Duration <= 0.05d || asset.FrameRate <= 0d)
        {
            return Math.Clamp((int)Math.Floor(mediaTime * Math.Max(1d, asset.FrameRate)), 0, asset.Frames.Count - 1);
        }

        var accumulated = 0d;
        for (var index = 0; index < asset.Frames.Count; index++)
        {
            accumulated += Math.Max(0.001d, asset.Frames[index].Duration);
            if (mediaTime <= accumulated || index == asset.Frames.Count - 1)
            {
                return index;
            }
        }

        return asset.Frames.Count - 1;
    }

    public static bool IsAudible(TimelineLayer layer)
    {
        return layer.Kind == LayerKind.Audio && layer.Media.SourceMediaAssetId is not null && !layer.IsMuted;
    }

    public static bool IsRepeatingForever(TimelineLayer layer)
    {
        return layer.Media.Loop || layer.Media.RepeatForever;
    }

    private static double Mod(double value, double divisor)
    {
        var remainder = value % divisor;
        return remainder < 0d ? remainder + divisor : remainder;
    }
}
