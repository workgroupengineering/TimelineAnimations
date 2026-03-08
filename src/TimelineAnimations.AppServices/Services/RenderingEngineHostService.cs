using Avalonia.Media.Imaging;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Rendering.Avalonia.Services;
using TimelineAnimations.Rendering.Models;
using TimelineAnimations.Rendering.Services;
using TimelineAnimations.Rendering.SkiaSharp.Services;

namespace TimelineAnimations.App.Services;

public static class RenderingEngineHostService
{
    private static readonly AvaloniaSceneRenderEngine s_avaloniaEngine = new();
    private static readonly SkiaSceneRenderEngine s_skiaEngine = new(s_avaloniaEngine);
    private static RenderingEngineKind s_selectedEngine = RendererPreferencesPersistenceService.Load().SelectedEngine;

    public static IReadOnlyList<RenderingEngineKind> AvailableEngines { get; } =
    [
        RenderingEngineKind.Avalonia,
        RenderingEngineKind.SkiaSharp
    ];

    public static RenderingEngineKind SelectedEngine
    {
        get => s_selectedEngine;
        set => s_selectedEngine = value;
    }

    public static ISceneRenderEngine Resolve(RenderingEngineKind engineKind)
    {
        return engineKind switch
        {
            RenderingEngineKind.SkiaSharp => s_skiaEngine,
            _ => s_avaloniaEngine
        };
    }

    public static SceneRenderRequest BuildRequest(
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
        return SceneRenderRequestBuilder.Build(
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
    }

    public static RenderedFrame RenderFrame(SceneRenderRequest request, RenderingEngineKind engineKind, bool includeBackground)
    {
        return Resolve(engineKind).Render(request, includeBackground);
    }

    public static RenderedFrame RenderFrame(SceneRenderRequest request, bool includeBackground)
    {
        return RenderFrame(request, SelectedEngine, includeBackground);
    }

    public static WriteableBitmap RenderBitmap(SceneRenderRequest request, RenderingEngineKind engineKind, bool includeBackground)
    {
        var frame = RenderFrame(request, engineKind, includeBackground);
        return RenderedFrameBitmapFactory.Create(frame);
    }

    public static WriteableBitmap RenderBitmap(SceneRenderRequest request, bool includeBackground)
    {
        return RenderBitmap(request, SelectedEngine, includeBackground);
    }
}
