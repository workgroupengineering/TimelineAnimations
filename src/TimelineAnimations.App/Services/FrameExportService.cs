using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using TimelineAnimations.App.Helpers;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.Services;

public static class FrameExportService
{
    public static async Task ExportFrameAsync(TimelineDocument document, double time, Stream stream, CancellationToken cancellationToken = default)
    {
        using var bitmap = RenderFrame(document, time);
        bitmap.Save(stream);
        await stream.FlushAsync(cancellationToken);
    }

    public static async Task<int> ExportSequenceAsync(
        TimelineDocument document,
        string folderPath,
        double framesPerSecond = 30,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(folderPath);

        var frameCount = Math.Max(1, (int)Math.Ceiling(document.Duration * framesPerSecond) + 1);
        for (var frame = 0; frame < frameCount; frame++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var time = Math.Min(frame / framesPerSecond, document.Duration);
            var filePath = Path.Combine(folderPath, $"frame_{frame:0000}.png");

            await using var stream = File.Create(filePath);
            using var bitmap = RenderFrame(document, time);
            bitmap.Save(stream);
            await stream.FlushAsync(cancellationToken);
        }

        return frameCount;
    }

    private static RenderTargetBitmap RenderFrame(TimelineDocument document, double time)
    {
        var pixelSize = new PixelSize(
            Math.Max(1, (int)Math.Ceiling(document.CanvasWidth)),
            Math.Max(1, (int)Math.Ceiling(document.CanvasHeight)));

        var bitmap = new RenderTargetBitmap(pixelSize, new Vector(96, 96));
        using var context = bitmap.CreateDrawingContext(true);

        var stageRect = new Rect(0, 0, document.CanvasWidth, document.CanvasHeight);
        DrawBackground(context, stageRect, document);

        foreach (var layer in document.Layers.OrderBy(item => item.ZIndex))
        {
            if (!layer.IsVisible)
            {
                continue;
            }

            DrawLayer(context, layer, time);
        }

        return bitmap;
    }

    private static void DrawBackground(DrawingContext context, Rect stageRect, TimelineDocument document)
    {
        var stageBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0.15, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0.9, 1, RelativeUnit.Relative),
            GradientStops =
            [
                new GradientStop(ColorHelpers.Parse(document.BackgroundFrom, "#09111F"), 0),
                new GradientStop(ColorHelpers.Parse(document.BackgroundTo, "#182748"), 1)
            ]
        };

        context.DrawRectangle(stageBrush, null, stageRect);
        context.DrawEllipse(new SolidColorBrush(Color.FromArgb(30, 36, 229, 193)), null, new Point(stageRect.Width * 0.22, stageRect.Height * 0.2), 150, 120);
        context.DrawEllipse(new SolidColorBrush(Color.FromArgb(24, 255, 138, 76)), null, new Point(stageRect.Width * 0.84, stageRect.Height * 0.76), 170, 130);
    }

    private static void DrawLayer(DrawingContext context, TimelineLayer layer, double time)
    {
        var snapshot = TimelineInterpolationService.SampleLayer(layer, time);
        var rect = new Rect(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height);
        var fillColor = ApplyAlpha(ColorHelpers.Parse(snapshot.Fill), (byte)(Math.Clamp(snapshot.Opacity, 0, 1) * 255));
        var glowBrush = new SolidColorBrush(ApplyAlpha(fillColor, 32));
        var fillBrush = new SolidColorBrush(fillColor);
        var strokeBrush = new SolidColorBrush(ApplyAlpha(ColorHelpers.Parse(snapshot.Stroke), (byte)(Math.Clamp(snapshot.Opacity, 0, 1) * 220)));

        context.DrawRectangle(glowBrush, null, rect.Inflate(10), snapshot.CornerRadius + 10, snapshot.CornerRadius + 10);

        switch (layer.Kind)
        {
            case LayerKind.Rectangle:
                using (PushRotation(context, rect, snapshot.Rotation))
                {
                    context.DrawRectangle(fillBrush, new Pen(strokeBrush, 1.4), rect, snapshot.CornerRadius, snapshot.CornerRadius);
                }

                break;
            case LayerKind.Ellipse:
                using (PushRotation(context, rect, snapshot.Rotation))
                {
                    context.DrawEllipse(fillBrush, new Pen(strokeBrush, 1.4), rect.Center, rect.Width / 2, rect.Height / 2);
                }

                break;
            case LayerKind.Text:
                context.DrawRectangle(new SolidColorBrush(ApplyAlpha(Color.Parse("#102038"), (byte)(Math.Clamp(snapshot.Opacity, 0, 1) * 180))), new Pen(new SolidColorBrush(Color.Parse("#385886")), 1), rect, 24, 24);
                DrawText(context, rect, snapshot.Text, snapshot.Fill, snapshot.FontSize);
                break;
        }
    }

    private static void DrawText(DrawingContext context, Rect rect, string text, string fill, double fontSize)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default),
            Math.Max(14, fontSize),
            ColorHelpers.Brush(fill, "#FFFFFF"));

        var point = new Point(rect.X + 22, rect.Y + Math.Max(16, (rect.Height - formattedText.Height) / 2));
        context.DrawText(formattedText, point);
    }

    private static Color ApplyAlpha(Color color, byte alpha)
    {
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private static IDisposable PushRotation(DrawingContext context, Rect rect, double angle)
    {
        if (Math.Abs(angle) < 0.01d)
        {
            return EmptyDisposable.Instance;
        }

        var radians = angle * Math.PI / 180d;
        var center = rect.Center;
        var matrix =
            Matrix.CreateTranslation(-center.X, -center.Y) *
            Matrix.CreateRotation(radians) *
            Matrix.CreateTranslation(center.X, center.Y);

        return context.PushTransform(matrix);
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
