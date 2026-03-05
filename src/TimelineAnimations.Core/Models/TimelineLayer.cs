namespace TimelineAnimations.Core.Models;

public sealed class TimelineLayer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "Layer";

    public LayerKind Kind { get; set; }

    public bool IsVisible { get; set; } = true;

    public int ZIndex { get; set; }

    public LayerDefaults Defaults { get; set; } = new();

    public LayerStyle Style { get; set; } = new();

    public List<LayerTrack> Tracks { get; set; } = [];
}
