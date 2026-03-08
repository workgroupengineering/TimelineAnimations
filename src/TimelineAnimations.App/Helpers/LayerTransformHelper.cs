using Avalonia;
using Avalonia.Media;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.Helpers;

public static class LayerTransformHelper
{
    public static bool TryCreateMatrix(
        Rect rect,
        LayerSnapshot snapshot,
        LibraryItem? sourceLibraryItem,
        out Matrix matrix)
    {
        return TimelineAnimations.Rendering.Avalonia.Helpers.LayerTransformHelper.TryCreateMatrix(rect, snapshot, sourceLibraryItem, out matrix);
    }

    public static Point ResolveOrigin(Rect rect, LibraryItem? sourceLibraryItem)
    {
        return TimelineAnimations.Rendering.Avalonia.Helpers.LayerTransformHelper.ResolveOrigin(rect, sourceLibraryItem);
    }

    public static Point TransformPoint(
        Point point,
        Rect rect,
        LayerSnapshot snapshot,
        LibraryItem? sourceLibraryItem)
    {
        return TimelineAnimations.Rendering.Avalonia.Helpers.LayerTransformHelper.TransformPoint(point, rect, snapshot, sourceLibraryItem);
    }

    public static Point InverseTransformPoint(
        Point point,
        Rect rect,
        LayerSnapshot snapshot,
        LibraryItem? sourceLibraryItem)
    {
        return TimelineAnimations.Rendering.Avalonia.Helpers.LayerTransformHelper.InverseTransformPoint(point, rect, snapshot, sourceLibraryItem);
    }

    public static IReadOnlyList<Point> GetTransformedCorners(
        Rect rect,
        LayerSnapshot snapshot,
        LibraryItem? sourceLibraryItem)
    {
        return TimelineAnimations.Rendering.Avalonia.Helpers.LayerTransformHelper.GetTransformedCorners(rect, snapshot, sourceLibraryItem);
    }
}
