using TimelineAnimations.Core.Models;
using TimelineAnimations.Rendering.Models;

namespace TimelineAnimations.Rendering.Services;

/// <summary>
/// Provides a backend-specific fallback path for rendering individual layer
/// samples that the active renderer cannot draw natively.
/// </summary>
public interface ILayerRenderFallback
{
    /// <summary>
    /// Renders a single layer sample using the fallback backend.
    /// </summary>
    /// <param name="request">The render request context.</param>
    /// <param name="sample">The sample to render.</param>
    /// <param name="renderAsMask">
    /// Indicates whether the sample should be rendered as a mask surface.
    /// </param>
    /// <returns>The rendered fallback frame.</returns>
    RenderedFrame RenderLayer(SceneRenderRequest request, RenderableLayerSample sample, bool renderAsMask);
}
