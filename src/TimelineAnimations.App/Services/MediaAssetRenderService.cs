using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.Services;

public static class MediaAssetRenderService
{
    private static readonly Dictionary<string, Bitmap> s_bitmapCache = [];

    public static MediaFrameModel? GetFrame(MediaAsset asset, double mediaTime)
    {
        if (asset.Kind != MediaAssetKind.Video || asset.Frames.Count == 0)
        {
            return null;
        }

        var frameIndex = MediaTimelineService.ResolveFrameIndex(asset, Math.Max(0d, mediaTime));
        return frameIndex < 0 || frameIndex >= asset.Frames.Count
            ? null
            : asset.Frames[frameIndex];
    }

    public static Bitmap? GetFrameBitmap(MediaAsset asset, double mediaTime)
    {
        var frame = GetFrame(asset, mediaTime);
        if (frame is null || string.IsNullOrWhiteSpace(frame.ImageBase64))
        {
            return null;
        }

        var frameIndex = MediaTimelineService.ResolveFrameIndex(asset, Math.Max(0d, mediaTime));
        var cacheKey = $"{asset.Id:N}:{frameIndex}";
        if (s_bitmapCache.TryGetValue(cacheKey, out var bitmap))
        {
            return bitmap;
        }

        var bytes = Convert.FromBase64String(frame.ImageBase64);
        bitmap = new Bitmap(new MemoryStream(bytes, writable: false));
        s_bitmapCache[cacheKey] = bitmap;
        return bitmap;
    }

    public static void DrawVideoFrame(
        DrawingContext context,
        Rect rect,
        LayerSnapshot snapshot,
        MediaAsset asset,
        double mediaTime,
        double opacity,
        Color fallbackFill,
        Color fallbackAccent,
        bool renderAsMask)
    {
        var frame = GetFrame(asset, mediaTime);
        if (frame is null)
        {
            DrawSyntheticVideoFrame(context, rect, snapshot, asset.Name, asset.PreviewFill, asset.PreviewAccent, opacity, renderAsMask);
            return;
        }

        if (renderAsMask)
        {
            DrawSyntheticVideoFrame(context, rect, snapshot, string.Empty, "#FFFFFF", "#FFFFFF", opacity, renderAsMask: true);
            return;
        }

        var bitmap = GetFrameBitmap(asset, mediaTime);
        if (bitmap is null)
        {
            DrawSyntheticVideoFrame(context, rect, snapshot, frame.Label, frame.Fill, frame.Accent, opacity, renderAsMask: false);
            return;
        }

        using (context.PushOpacity(TimelineMath.Clamp(opacity, 0d, 1d)))
        {
            context.DrawImage(bitmap, rect);
        }

        var borderPen = new Pen(new SolidColorBrush(ApplyAlpha(fallbackAccent, 220)), Math.Max(1d, snapshot.StrokeThickness));
        context.DrawRectangle(null, borderPen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
        if (!string.IsNullOrWhiteSpace(frame.Label))
        {
            var labelRect = new Rect(rect.X + 14, rect.Y + 12, Math.Min(rect.Width - 28, 108), 24);
            context.DrawRectangle(new SolidColorBrush(Color.FromArgb(148, 8, 16, 26)), null, labelRect, 12, 12);
            var text = new FormattedText(
                frame.Label,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default),
                Math.Max(11d, snapshot.FontSize * 0.28d),
                new SolidColorBrush(ApplyAlpha(fallbackAccent, 240)));
            context.DrawText(text, new Point(labelRect.X + 10, labelRect.Y + 4));
        }
    }

    public static void DrawSyntheticVideoFrame(
        DrawingContext context,
        Rect rect,
        LayerSnapshot snapshot,
        string label,
        string fill,
        string accent,
        double opacity,
        bool renderAsMask)
    {
        var fillColor = ApplyAlpha(Color.Parse(string.IsNullOrWhiteSpace(fill) ? "#223352" : fill), (byte)(Math.Clamp(opacity, 0d, 1d) * 255d));
        var accentColor = ApplyAlpha(Color.Parse(string.IsNullOrWhiteSpace(accent) ? "#61E6FF" : accent), (byte)(Math.Clamp(opacity, 0d, 1d) * 255d));
        var fillBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops =
            [
                new GradientStop(fillColor, 0),
                new GradientStop(Blend(fillColor, accentColor, 0.24d), 1)
            ]
        };
        var borderPen = new Pen(new SolidColorBrush(accentColor), Math.Max(1d, snapshot.StrokeThickness));
        context.DrawRectangle(fillBrush, borderPen, rect, snapshot.CornerRadius, snapshot.CornerRadius);

        var stripBrush = new SolidColorBrush(ApplyAlpha(Blend(fillColor, accentColor, 0.34d), renderAsMask ? (byte)255 : (byte)168));
        context.DrawRectangle(stripBrush, null, new Rect(rect.X, rect.Bottom - Math.Min(36, rect.Height * 0.18d), rect.Width, Math.Min(36, rect.Height * 0.18d)), snapshot.CornerRadius, snapshot.CornerRadius);

        if (renderAsMask || string.IsNullOrWhiteSpace(label))
        {
            return;
        }

        var text = new FormattedText(
            label,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default),
            Math.Max(12d, snapshot.FontSize * 0.34d),
            new SolidColorBrush(Colors.White));
        context.DrawText(text, new Point(rect.X + 18, rect.Y + Math.Max(18d, (rect.Height - text.Height) / 2d)));
    }

    private static Color Blend(Color baseColor, Color tint, double amount)
    {
        var normalized = Math.Clamp(amount, 0d, 1d);
        return Color.FromArgb(
            baseColor.A,
            (byte)(baseColor.R + ((tint.R - baseColor.R) * normalized)),
            (byte)(baseColor.G + ((tint.G - baseColor.G) * normalized)),
            (byte)(baseColor.B + ((tint.B - baseColor.B) * normalized)));
    }

    private static Color ApplyAlpha(Color color, byte alpha)
    {
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}
