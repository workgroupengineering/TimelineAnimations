namespace TimelineAnimations.FlashFormats;

public sealed class FlashAuthoringPackage
{
    public required FlashAuthoringContainerKind Kind { get; init; }

    public required string SourceLabel { get; init; }

    public required string DocumentRelativePath { get; init; }

    public required byte[] DocumentData { get; init; }

    public required IReadOnlyList<FlashAuthoringPackageEntry> Entries { get; init; }
}
