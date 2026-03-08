using Avalonia;
using Avalonia.Media.Imaging;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Rendering.Models;
using TimelineAnimations.Rendering.Services;

namespace TimelineAnimations.App.Services;

public sealed class CompositeSceneState : SceneRenderRequest
{
    public CompositeSceneState(
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
        : base(
            time,
            canvasWidth,
            canvasHeight,
            transparentStageBackground,
            backgroundFrom,
            backgroundTo,
            animate,
            libraryItems,
            mediaAssets,
            samples,
            activeCamera)
    {
    }
}

public static class CompositeFrameRenderer
{
    public static CompositeSceneState BuildSceneState(
        TimelineDocument document,
        IReadOnlyList<TimelineLayer> layers,
        double time,
        double duration,
        double frameRate,
        double canvasWidth,
        double canvasHeight,
        bool transparentStageBackground,
        string backgroundFrom,
        string backgroundTo)
    {
        var request = SceneRenderRequestBuilder.Build(
            document,
            layers,
            time,
            duration,
            frameRate,
            canvasWidth,
            canvasHeight,
            transparentStageBackground,
            backgroundFrom,
            backgroundTo);
        return new CompositeSceneState(
            request.Time,
            request.CanvasWidth,
            request.CanvasHeight,
            request.TransparentStageBackground,
            request.BackgroundFrom,
            request.BackgroundTo,
            request.Animate,
            request.LibraryItems,
            request.MediaAssets,
            request.Samples,
            request.ActiveCamera);
    }

    public static WriteableBitmap RenderBitmap(CompositeSceneState state, bool includeBackground)
    {
        return RenderingEngineHostService.RenderBitmap(state, RenderingEngineKind.Avalonia, includeBackground);
    }

    public static LayerSnapshot? ResolveActiveCamera(IReadOnlyList<RenderableLayerSample> samples)
    {
        return SceneRenderTransformService.ResolveActiveCamera(samples);
    }

    public static LayerSnapshot TransformSnapshot(
        LayerSnapshot snapshot,
        LayerSnapshot? activeCamera,
        AnimateDocumentSettings? animate,
        double canvasWidth,
        double canvasHeight,
        double? parallaxDepth = null)
    {
        return SceneRenderTransformService.TransformSnapshot(snapshot, activeCamera, animate, canvasWidth, canvasHeight, parallaxDepth);
    }

    public static LayerSnapshot TransformSnapshot(
        LayerSnapshot snapshot,
        LayerSnapshot? activeCamera,
        double canvasWidth,
        double canvasHeight,
        double? parallaxDepth = null)
    {
        return SceneRenderTransformService.TransformSnapshot(snapshot, activeCamera, canvasWidth, canvasHeight, parallaxDepth);
    }

    public static Point TransformPoint(
        Point worldPoint,
        LayerSnapshot? activeCamera,
        double canvasWidth,
        double canvasHeight,
        double parallaxDepth = 1d)
    {
        var transformed = SceneRenderTransformService.TransformPoint(
            new RenderPoint(worldPoint.X, worldPoint.Y),
            activeCamera,
            canvasWidth,
            canvasHeight,
            parallaxDepth);
        return new Point(transformed.X, transformed.Y);
    }

    public static Point InverseTransformPoint(
        Point viewportPoint,
        LayerSnapshot? activeCamera,
        double canvasWidth,
        double canvasHeight,
        double parallaxDepth = 1d)
    {
        var transformed = SceneRenderTransformService.InverseTransformPoint(
            new RenderPoint(viewportPoint.X, viewportPoint.Y),
            activeCamera,
            canvasWidth,
            canvasHeight,
            parallaxDepth);
        return new Point(transformed.X, transformed.Y);
    }
}
