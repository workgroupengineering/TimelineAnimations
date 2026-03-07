using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class AudioMixService
{
    public static AudioMeterSnapshot BuildMeterSnapshot(TimelineLayer layer, MediaAsset? asset, double sceneTime)
    {
        if (layer.Kind != LayerKind.Audio || asset is null || layer.IsMuted)
        {
            return AudioMeterSnapshot.Silent;
        }

        var mediaTime = MediaTimelineService.ResolveMediaTime(layer, asset, sceneTime);
        if (mediaTime is null)
        {
            return AudioMeterSnapshot.Silent;
        }

        var effectiveGain = GetEffectiveGain(layer, asset, sceneTime);
        if (effectiveGain <= 0.0001d || asset.WaveformSamples.Count == 0)
        {
            return AudioMeterSnapshot.Silent with { EffectiveGain = effectiveGain };
        }

        var sampleIndex = Math.Clamp(
            (int)Math.Round((mediaTime.Value / Math.Max(0.05d, asset.Duration)) * Math.Max(0, asset.WaveformSamples.Count - 1)),
            0,
            Math.Max(0, asset.WaveformSamples.Count - 1));
        var start = Math.Max(0, sampleIndex - 4);
        var end = Math.Min(asset.WaveformSamples.Count - 1, sampleIndex + 4);
        var peak = 0d;
        var sumSquares = 0d;
        var count = 0;

        for (var index = start; index <= end; index++)
        {
            var amplitude = Math.Clamp(asset.WaveformSamples[index], 0d, 1d) * effectiveGain;
            peak = Math.Max(peak, amplitude);
            sumSquares += amplitude * amplitude;
            count++;
        }

        var rms = count == 0 ? 0d : Math.Sqrt(sumSquares / count);
        var (leftGain, rightGain) = GetStereoGains(layer);
        return new AudioMeterSnapshot(
            TimelineMath.Clamp(peak, 0d, 1d),
            TimelineMath.Clamp(rms, 0d, 1d),
            leftGain,
            rightGain,
            effectiveGain,
            peak > 0.0001d);
    }

    public static double GetEffectiveGain(TimelineLayer layer, MediaAsset? asset, double sceneTime)
    {
        var clipDuration = MediaTimelineService.GetClipDuration(layer, asset);
        var clipRelativeTime = sceneTime - layer.Media.StartTime;
        if (clipRelativeTime < 0d || clipRelativeTime > clipDuration)
        {
            return 0d;
        }

        var volumeGain = TimelineMath.Clamp(layer.Media.Volume, 0d, 1d);
        var decibelGain = Math.Pow(10d, layer.Media.GainDb / 20d);
        var fadeMultiplier = 1d;

        if (layer.Media.FadeInDuration > 0.001d)
        {
            fadeMultiplier = Math.Min(fadeMultiplier, TimelineMath.Clamp(clipRelativeTime / layer.Media.FadeInDuration, 0d, 1d));
        }

        if (layer.Media.FadeOutDuration > 0.001d)
        {
            var fadeOutStart = Math.Max(0d, clipDuration - layer.Media.FadeOutDuration);
            if (clipRelativeTime >= fadeOutStart)
            {
                fadeMultiplier = Math.Min(fadeMultiplier, TimelineMath.Clamp((clipDuration - clipRelativeTime) / layer.Media.FadeOutDuration, 0d, 1d));
            }
        }

        return TimelineMath.Clamp(volumeGain * decibelGain * fadeMultiplier, 0d, 4d);
    }

    public static (double LeftGain, double RightGain) GetStereoGains(TimelineLayer layer)
    {
        var pan = TimelineMath.Clamp(layer.Media.Pan, -1d, 1d);
        var left = Math.Sqrt((1d - pan) / 2d);
        var right = Math.Sqrt((1d + pan) / 2d);
        return (left, right);
    }
}
