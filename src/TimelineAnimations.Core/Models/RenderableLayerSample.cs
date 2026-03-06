namespace TimelineAnimations.Core.Models;

public sealed class RenderableLayerSample
{
    public Guid SourceLayerId { get; set; }

    public Guid? SourceLibraryItemId { get; set; }

    public Guid? SourceMediaAssetId { get; set; }

    public LayerKind Kind { get; set; }

    public int ZIndex { get; set; }

    public LayerSnapshot Snapshot { get; set; }

    public double? MediaTime { get; set; }

    public LayerCompositeRole Role => Snapshot.Compositing.Role;

    public LayerBlendMode BlendMode => Snapshot.Compositing.BlendMode;

    public int MaskLayerCount => Math.Max(1, Snapshot.Compositing.MaskLayerCount);

    public double ParallaxDepth => Math.Max(0.2d, Snapshot.Compositing.ParallaxDepth);
}
