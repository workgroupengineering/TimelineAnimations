using TimelineAnimations.Rendering.Models;

namespace TimelineAnimations.Rendering.Services;

public interface ISceneRenderEngine
{
    RenderingEngineKind EngineKind { get; }

    RenderedFrame Render(SceneRenderRequest request, bool includeBackground);
}
