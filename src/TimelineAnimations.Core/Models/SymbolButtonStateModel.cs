namespace TimelineAnimations.Core.Models;

public sealed class SymbolButtonStateModel
{
    public ButtonVisualState State { get; set; } = ButtonVisualState.Up;

    public List<TimelineLayer> Layers { get; set; } = [];
}
