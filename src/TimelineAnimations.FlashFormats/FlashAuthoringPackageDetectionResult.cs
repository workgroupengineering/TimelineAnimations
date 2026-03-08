namespace TimelineAnimations.FlashFormats;

public sealed class FlashAuthoringPackageDetectionResult
{
    public required FlashAuthoringContainerKind Kind { get; init; }

    public required bool IsSupported { get; init; }

    public required string Message { get; init; }
}
