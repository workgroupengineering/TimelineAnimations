using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Rendering.Models;

public class SceneRenderRequest
{
    public SceneRenderRequest(
        double time,
        double canvasWidth,
        double canvasHeight,
        bool transparentStageBackground,
        string backgroundFrom,
        string backgroundTo,
        AnimateDocumentSettings animate,
        IReadOnlyDictionary<Guid, LibraryItem> libraryItems,
        IReadOnlyDictionary<Guid, MediaAsset> mediaAssets,
        IReadOnlyList<RenderableLayerSample> samples,
        LayerSnapshot? activeCamera)
    {
        Time = time;
        CanvasWidth = canvasWidth;
        CanvasHeight = canvasHeight;
        TransparentStageBackground = transparentStageBackground;
        BackgroundFrom = backgroundFrom;
        BackgroundTo = backgroundTo;
        Animate = animate;
        LibraryItems = libraryItems;
        MediaAssets = mediaAssets;
        Samples = samples;
        ActiveCamera = activeCamera;
        GuideSamples = CollectGuideSamples(samples);
    }

    public double Time { get; }

    public double CanvasWidth { get; }

    public double CanvasHeight { get; }

    public bool TransparentStageBackground { get; }

    public string BackgroundFrom { get; }

    public string BackgroundTo { get; }

    public AnimateDocumentSettings Animate { get; }

    public IReadOnlyDictionary<Guid, LibraryItem> LibraryItems { get; }

    public IReadOnlyDictionary<Guid, MediaAsset> MediaAssets { get; }

    public IReadOnlyList<RenderableLayerSample> Samples { get; }

    public IReadOnlyList<RenderableLayerSample> GuideSamples { get; }

    public LayerSnapshot? ActiveCamera { get; }

    private static IReadOnlyList<RenderableLayerSample> CollectGuideSamples(IReadOnlyList<RenderableLayerSample> samples)
    {
        List<RenderableLayerSample>? guides = null;
        for (var index = 0; index < samples.Count; index++)
        {
            if (samples[index].Role != LayerCompositeRole.Guide)
            {
                continue;
            }

            guides ??= new List<RenderableLayerSample>();
            guides.Add(samples[index]);
        }

        return guides ?? [];
    }
}
