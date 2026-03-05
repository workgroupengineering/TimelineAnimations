using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.ViewModels;

public sealed class PaletteItemViewModel
{
    public required string Title { get; init; }

    public required string Subtitle { get; init; }

    public required string Fill { get; init; }

    public required string Accent { get; init; }

    public required LayerKind Kind { get; init; }
}
