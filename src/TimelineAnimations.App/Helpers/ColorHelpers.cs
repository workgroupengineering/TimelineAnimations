using Avalonia.Media;

namespace TimelineAnimations.App.Helpers;

public static class ColorHelpers
{
    public static Color Parse(string? value, string fallback = "#FFFFFF")
    {
        if (!string.IsNullOrWhiteSpace(value) && Color.TryParse(value, out var color))
        {
            return color;
        }

        return Color.TryParse(fallback, out var fallbackColor) ? fallbackColor : Colors.White;
    }

    public static SolidColorBrush Brush(string? value, string fallback = "#FFFFFF")
    {
        return new SolidColorBrush(Parse(value, fallback));
    }

    public static string ToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
