namespace TimelineAnimations.Core.Models;

public sealed class SceneMarkerModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int Frame { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public string Color { get; set; } = "#57C9FF";

    public SceneMarkerKind Kind { get; set; } = SceneMarkerKind.Cue;
}
