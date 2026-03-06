namespace TimelineAnimations.Core.Models;

public sealed class LayerMediaSettings
{
    public Guid? SourceMediaAssetId { get; set; }

    public MediaPlaybackMode PlaybackMode { get; set; } = MediaPlaybackMode.Stream;

    public double StartTime { get; set; }

    public double ClipOffset { get; set; }

    public double ClipDuration { get; set; }

    public bool Loop { get; set; }

    public double Volume { get; set; } = 1d;

    public LayerMediaSettings Clone()
    {
        return new LayerMediaSettings
        {
            SourceMediaAssetId = SourceMediaAssetId,
            PlaybackMode = PlaybackMode,
            StartTime = StartTime,
            ClipOffset = ClipOffset,
            ClipDuration = ClipDuration,
            Loop = Loop,
            Volume = Volume
        };
    }
}
