namespace TimelineAnimations.Core.Models;

public sealed class MediaAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "New Media";

    public MediaAssetKind Kind { get; set; }

    public string SourceLabel { get; set; } = string.Empty;

    public string PackageRelativePath { get; set; } = string.Empty;

    public double Duration { get; set; } = 1d;

    public double FrameRate { get; set; } = 12d;

    public int Width { get; set; }

    public int Height { get; set; }

    public int SampleRate { get; set; }

    public int ChannelCount { get; set; } = 1;

    public string PreviewFill { get; set; } = "#61E6FF";

    public string PreviewAccent { get; set; } = "#FFD166";

    public string AudioBase64 { get; set; } = string.Empty;

    public List<double> WaveformSamples { get; set; } = [];

    public List<MediaFrameModel> Frames { get; set; } = [];
}
