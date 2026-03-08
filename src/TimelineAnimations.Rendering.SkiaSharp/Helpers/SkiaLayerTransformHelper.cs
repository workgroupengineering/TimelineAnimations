using SkiaSharp;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Rendering.SkiaSharp.Helpers;

public static class SkiaLayerTransformHelper
{
    private const double Tolerance = 0.0001d;

    public static bool HasTransform(LayerSnapshot snapshot)
    {
        var scaleX = Math.Abs(snapshot.ScaleX) < Tolerance ? 0d : snapshot.ScaleX;
        var scaleY = Math.Abs(snapshot.ScaleY) < Tolerance ? 0d : snapshot.ScaleY;
        return Math.Abs(scaleX - 1d) > Tolerance ||
               Math.Abs(scaleY - 1d) > Tolerance ||
               Math.Abs(snapshot.SkewX) > Tolerance ||
               Math.Abs(snapshot.SkewY) > Tolerance ||
               Math.Abs(snapshot.Rotation) > 0.01d;
    }

    public static void ApplyTransform(SKCanvas canvas, SKRect rect, LayerSnapshot snapshot, LibraryItem? sourceLibraryItem)
    {
        if (!HasTransform(snapshot))
        {
            return;
        }

        var origin = ResolveOrigin(rect, sourceLibraryItem);
        canvas.Translate(origin.X, origin.Y);
        if (Math.Abs(snapshot.Rotation) > 0.01d)
        {
            canvas.RotateDegrees((float)snapshot.Rotation);
        }

        if (Math.Abs(snapshot.SkewX) > Tolerance || Math.Abs(snapshot.SkewY) > Tolerance)
        {
            canvas.Skew(
                (float)Math.Tan(snapshot.SkewX * Math.PI / 180d),
                (float)Math.Tan(snapshot.SkewY * Math.PI / 180d));
        }

        var scaleX = Math.Abs(snapshot.ScaleX) < Tolerance ? 0d : snapshot.ScaleX;
        var scaleY = Math.Abs(snapshot.ScaleY) < Tolerance ? 0d : snapshot.ScaleY;
        if (Math.Abs(scaleX - 1d) > Tolerance || Math.Abs(scaleY - 1d) > Tolerance)
        {
            canvas.Scale((float)scaleX, (float)scaleY);
        }

        canvas.Translate(-origin.X, -origin.Y);
    }

    public static SKPoint ResolveOrigin(SKRect rect, LibraryItem? sourceLibraryItem)
    {
        var registrationX = sourceLibraryItem?.RegistrationPointX ?? 0.5d;
        var registrationY = sourceLibraryItem?.RegistrationPointY ?? 0.5d;
        return new SKPoint(
            (float)(rect.Left + (rect.Width * registrationX)),
            (float)(rect.Top + (rect.Height * registrationY)));
    }
}
