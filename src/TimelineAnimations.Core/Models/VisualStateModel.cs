namespace TimelineAnimations.Core.Models;

public sealed class VisualStateModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "State";

    public TimelineLayer Template { get; set; } = new();
}
