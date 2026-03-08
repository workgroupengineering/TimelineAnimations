namespace TimelineAnimations.FlashFormats;

public sealed class FlashAuthoringPackageEntry
{
    public required string RelativePath { get; init; }

    public required byte[] Data { get; init; }

    public required FlashAuthoringPackageEntryKind Kind { get; init; }
}
