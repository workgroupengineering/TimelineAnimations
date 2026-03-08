namespace TimelineAnimations.Core.Models;

public sealed class MotionPresetModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "Motion Preset";

    public string Description { get; set; } = string.Empty;

    public LayerDefaults Defaults { get; set; } = new();

    public List<LayerTrack> Tracks { get; set; } = [];
}
