using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Rendering.Models;

public sealed class SceneRenderPreparation
{
    public SceneRenderPreparation(
        IReadOnlyDictionary<Guid, LibraryItem> libraryItems,
        IReadOnlyDictionary<Guid, MediaAsset> mediaAssets,
        IReadOnlyList<RenderableLayerSample> samples,
        LayerSnapshot? activeCamera)
    {
        LibraryItems = libraryItems;
        MediaAssets = mediaAssets;
        Samples = samples;
        ActiveCamera = activeCamera;
    }

    public IReadOnlyDictionary<Guid, LibraryItem> LibraryItems { get; }

    public IReadOnlyDictionary<Guid, MediaAsset> MediaAssets { get; }

    public IReadOnlyList<RenderableLayerSample> Samples { get; }

    public LayerSnapshot? ActiveCamera { get; }
}
