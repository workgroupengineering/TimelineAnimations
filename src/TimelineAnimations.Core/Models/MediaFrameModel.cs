namespace TimelineAnimations.Core.Models;

public sealed class MediaFrameModel
{
    public double Duration { get; set; } = 1d / 12d;

    public string PackageRelativePath { get; set; } = string.Empty;

    public string ImageBase64 { get; set; } = string.Empty;

    public string Fill { get; set; } = "#223352";

    public string Accent { get; set; } = "#61E6FF";

    public string Label { get; set; } = string.Empty;
}
