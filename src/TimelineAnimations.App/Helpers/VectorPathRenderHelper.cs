using Avalonia;
using Avalonia.Media;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.Helpers;

public static class VectorPathRenderHelper
{
    public static StreamGeometry? BuildGeometry(
        IReadOnlyList<VectorPointModel> points,
        bool isClosed,
        Func<VectorPointModel, Point> anchorSelector,
        Func<VectorPointModel, VectorHandleKind, Point> handleSelector)
    {
        return TimelineAnimations.Rendering.Avalonia.Helpers.VectorPathRenderHelper.BuildGeometry(points, isClosed, anchorSelector, handleSelector);
    }

    public static bool HasVariableWidth(IReadOnlyList<VectorPointModel> points)
    {
        return TimelineAnimations.Rendering.Avalonia.Helpers.VectorPathRenderHelper.HasVariableWidth(points);
    }

    public static void DrawVariableStroke(
        DrawingContext context,
        IReadOnlyList<VectorPointModel> points,
        bool isClosed,
        Func<VectorPointModel, Point> anchorSelector,
        Func<VectorPointModel, VectorHandleKind, Point> handleSelector,
        ISolidColorBrush strokeBrush,
        LayerStrokeCapStyle capStyle,
        double baseThickness)
    {
        TimelineAnimations.Rendering.Avalonia.Helpers.VectorPathRenderHelper.DrawVariableStroke(
            context,
            points,
            isClosed,
            anchorSelector,
            handleSelector,
            strokeBrush,
            capStyle,
            baseThickness);
    }
}
