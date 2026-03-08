using TimelineAnimations.Rendering.Models;

namespace TimelineAnimations.App.Models;

public sealed class RendererPreferencesState
{
    public RenderingEngineKind SelectedEngine { get; set; } = RenderingEngineKind.Avalonia;
}
