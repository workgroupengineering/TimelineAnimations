namespace TimelineAnimations.Core.Models;

public sealed class FrameSpanModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int StartFrame { get; set; }

    public int EndFrame { get; set; }

    public bool IsBlank { get; set; }
}
