using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;
using TimelineAnimations.Rendering.Models;

namespace TimelineAnimations.Rendering.Services;

public static class SceneRenderRequestBuilder
{
    public static SceneRenderRequest Build(
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
        var preparation = SceneRenderPreparationBuilder.Build(document, layers, time, duration, frameRate);
        return new SceneRenderRequest(
            time,
            canvasWidth,
            canvasHeight,
            transparentStageBackground,
            backgroundFrom,
            backgroundTo,
            document.Animate,
            preparation.LibraryItems,
            preparation.MediaAssets,
            preparation.Samples,
            preparation.ActiveCamera);
    }
}
