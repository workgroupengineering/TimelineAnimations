using Avalonia;
using Avalonia.Media;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.Helpers;

public static class LayerTransformHelper
{
    private const double Tolerance = 0.0001d;

    public static bool TryCreateMatrix(
        Rect rect,
        LayerSnapshot snapshot,
        LibraryItem? sourceLibraryItem,
        out Matrix matrix)
    {
        var scaleX = Math.Abs(snapshot.ScaleX) < Tolerance ? 0d : snapshot.ScaleX;
        var scaleY = Math.Abs(snapshot.ScaleY) < Tolerance ? 0d : snapshot.ScaleY;
        var hasScale = Math.Abs(scaleX - 1d) > Tolerance || Math.Abs(scaleY - 1d) > Tolerance;
        var hasSkew = Math.Abs(snapshot.SkewX) > Tolerance || Math.Abs(snapshot.SkewY) > Tolerance;
        var hasRotation = Math.Abs(snapshot.Rotation) > 0.01d;
        if (!hasScale && !hasSkew && !hasRotation)
        {
            matrix = default;
            return false;
        }

        var origin = ResolveOrigin(rect, sourceLibraryItem);
        var scaleMatrix = new Matrix(scaleX, 0d, 0d, scaleY, 0d, 0d);
        var skewMatrix = new Matrix(
            1d,
            Math.Tan(snapshot.SkewY * Math.PI / 180d),
            Math.Tan(snapshot.SkewX * Math.PI / 180d),
            1d,
            0d,
            0d);
        matrix =
            Matrix.CreateTranslation(-origin.X, -origin.Y) *
            scaleMatrix *
            skewMatrix *
            Matrix.CreateRotation(snapshot.Rotation * Math.PI / 180d) *
            Matrix.CreateTranslation(origin.X, origin.Y);
        return true;
    }

    public static Point ResolveOrigin(Rect rect, LibraryItem? sourceLibraryItem)
    {
        var registrationX = sourceLibraryItem?.RegistrationPointX ?? 0.5d;
        var registrationY = sourceLibraryItem?.RegistrationPointY ?? 0.5d;
        return new Point(
            rect.X + (rect.Width * registrationX),
            rect.Y + (rect.Height * registrationY));
    }

    public static Point TransformPoint(
        Point point,
        Rect rect,
        LayerSnapshot snapshot,
        LibraryItem? sourceLibraryItem)
    {
        if (!TryCreateMatrix(rect, snapshot, sourceLibraryItem, out var matrix))
        {
            return point;
        }

        return matrix.Transform(point);
    }

    public static Point InverseTransformPoint(
        Point point,
        Rect rect,
        LayerSnapshot snapshot,
        LibraryItem? sourceLibraryItem)
    {
        if (!TryCreateMatrix(rect, snapshot, sourceLibraryItem, out var matrix) ||
            !matrix.TryInvert(out var inverse))
        {
            return point;
        }

        return inverse.Transform(point);
    }

    public static IReadOnlyList<Point> GetTransformedCorners(
        Rect rect,
        LayerSnapshot snapshot,
        LibraryItem? sourceLibraryItem)
    {
        var corners = new[]
        {
            rect.TopLeft,
            rect.TopRight,
            rect.BottomLeft,
            rect.BottomRight
        };

        if (!TryCreateMatrix(rect, snapshot, sourceLibraryItem, out var matrix))
        {
            return corners;
        }

        for (var index = 0; index < corners.Length; index++)
        {
            corners[index] = matrix.Transform(corners[index]);
        }

        return corners;
    }
}
