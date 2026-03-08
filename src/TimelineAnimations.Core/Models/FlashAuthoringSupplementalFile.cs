namespace TimelineAnimations.Core.Models;

public sealed class FlashAuthoringSupplementalFile
{
    public string RelativePath { get; set; } = string.Empty;

    public byte[] Data { get; set; } = [];
}
