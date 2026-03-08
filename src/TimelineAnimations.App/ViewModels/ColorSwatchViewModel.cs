using Avalonia.Media;
using TimelineAnimations.App.Helpers;

namespace TimelineAnimations.App.ViewModels;

/// <summary>
/// Represents a reusable color swatch shown in Adobe-style color panels.
/// </summary>
public sealed class ColorSwatchViewModel : ViewModelBase
{
    public ColorSwatchViewModel(string name, string hex)
    {
        Name = name;
        Hex = hex;
    }

    public string Name { get; }

    public string Hex { get; }

    public ISolidColorBrush PreviewBrush => ColorHelpers.Brush(Hex);

    public string Summary => $"{Name} • {Hex}";
}
