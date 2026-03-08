using Avalonia;
using Avalonia.Media;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.Helpers;

public static class LayerStyleRenderHelper
{
    public static IBrush CreateFillBrush(LayerSnapshot snapshot, Color solidFill, Color gradientFrom, Color gradientTo, double opacity)
    {
        return TimelineAnimations.Rendering.Avalonia.Helpers.LayerStyleRenderHelper.CreateFillBrush(snapshot, solidFill, gradientFrom, gradientTo, opacity);
    }

    public static IPen CreateStrokePen(LayerSnapshot snapshot, ISolidColorBrush strokeBrush, double thickness)
    {
        return TimelineAnimations.Rendering.Avalonia.Helpers.LayerStyleRenderHelper.CreateStrokePen(snapshot, strokeBrush, thickness);
    }

    public static RoundedRect CreateRoundedRect(LayerSnapshot snapshot, Rect rect)
    {
        return TimelineAnimations.Rendering.Avalonia.Helpers.LayerStyleRenderHelper.CreateRoundedRect(snapshot, rect);
    }

    public static double GetMaxCornerRadius(LayerSnapshot snapshot)
    {
        return TimelineAnimations.Rendering.Avalonia.Helpers.LayerStyleRenderHelper.GetMaxCornerRadius(snapshot);
    }

    public static Geometry? CreateEllipseGeometry(Rect rect, LayerSnapshot snapshot, bool includeCenterForFill)
    {
        return TimelineAnimations.Rendering.Avalonia.Helpers.LayerStyleRenderHelper.CreateEllipseGeometry(rect, snapshot, includeCenterForFill);
    }
}
