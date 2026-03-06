namespace TimelineAnimations.Core.Models;

public sealed class FrameLabelModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int Frame { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Script { get; set; } = string.Empty;
}
