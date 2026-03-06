using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.ViewModels;

public sealed class PaletteItemViewModel
{
    public required string Title { get; init; }

    public required string Subtitle { get; init; }

    public required string Fill { get; init; }

    public required string Accent { get; init; }

    public LayerKind? Kind { get; init; }

    public AvaloniaControlKind? AvaloniaControlKind { get; init; }

    public required string DragData { get; init; }

    public string Badge => AvaloniaControlKind is null ? "+" : "A";
}
