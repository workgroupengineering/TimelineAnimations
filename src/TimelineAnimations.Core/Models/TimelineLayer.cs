namespace TimelineAnimations.Core.Models;

public sealed class TimelineLayer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "Layer";

    public LayerKind Kind { get; set; }

    public bool IsVisible { get; set; } = true;

    public bool IsLocked { get; set; }

    public Guid? SourceLibraryItemId { get; set; }

    public SymbolPlaybackMode SymbolPlaybackMode { get; set; } = SymbolPlaybackMode.SceneTime;

    public double SymbolPlaybackOffset { get; set; }

    public int SymbolLockedFrame { get; set; }

    public ButtonVisualState SymbolButtonState { get; set; } = ButtonVisualState.Up;

    public int ZIndex { get; set; }

    public LayerDefaults Defaults { get; set; } = new();

    public LayerStyle Style { get; set; } = new();

    public LayerCompositeSettings Compositing { get; set; } = new();

    public LayerMediaSettings Media { get; set; } = new();

    public List<InteractionBehaviorModel> Behaviors { get; set; } = [];

    public List<FrameSpanModel> FrameSpans { get; set; } = [];

    public List<ShapeKeyframeModel> ShapeKeyframes { get; set; } = [];

    public List<LayerTrack> Tracks { get; set; } = [];
}
