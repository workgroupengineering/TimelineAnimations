using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using TimelineAnimations.App.Helpers;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.Services;

public sealed class CompositeSceneState
{
    public CompositeSceneState(
        double canvasWidth,
        double canvasHeight,
        string backgroundFrom,
        string backgroundTo,
        IReadOnlyDictionary<Guid, MediaAsset> mediaAssets,
        IReadOnlyList<RenderableLayerSample> samples,
        LayerSnapshot? activeCamera)
    {
        CanvasWidth = canvasWidth;
        CanvasHeight = canvasHeight;
        BackgroundFrom = backgroundFrom;
        BackgroundTo = backgroundTo;
        MediaAssets = mediaAssets;
        Samples = samples;
        ActiveCamera = activeCamera;
        GuideSamples =
        [
            .. samples.Where(sample => sample.Role == LayerCompositeRole.Guide)
        ];
    }

    public double CanvasWidth { get; }

    public double CanvasHeight { get; }

    public string BackgroundFrom { get; }

    public string BackgroundTo { get; }

    public IReadOnlyDictionary<Guid, MediaAsset> MediaAssets { get; }

    public IReadOnlyList<RenderableLayerSample> Samples { get; }

    public IReadOnlyList<RenderableLayerSample> GuideSamples { get; }

    public LayerSnapshot? ActiveCamera { get; }
}

public static class CompositeFrameRenderer
{
    private const int EffectSampleCount = 10;

    public static CompositeSceneState BuildSceneState(
        TimelineDocument document,
        IReadOnlyList<TimelineLayer> layers,
        double time,
        double duration,
        double frameRate,
        double canvasWidth,
        double canvasHeight,
        string backgroundFrom,
        string backgroundTo)
    {
        var samples = SymbolRenderService.BuildRenderSamples(document, layers, time, duration, frameRate);
        return new CompositeSceneState(
            canvasWidth,
            canvasHeight,
            backgroundFrom,
            backgroundTo,
            document.MediaAssets.ToDictionary(item => item.Id),
            samples,
            ResolveActiveCamera(samples));
    }

    public static WriteableBitmap RenderBitmap(CompositeSceneState state, bool includeBackground)
    {
        var pixelSize = new PixelSize(
            Math.Max(1, (int)Math.Ceiling(state.CanvasWidth)),
            Math.Max(1, (int)Math.Ceiling(state.CanvasHeight)));
        var width = pixelSize.Width;
        var height = pixelSize.Height;
        var stride = width * 4;
        var pixels = includeBackground
            ? RenderBackgroundPixels(width, height, state.BackgroundFrom, state.BackgroundTo)
            : new byte[stride * height];
        var renderable = state.Samples
            .Where(sample => sample.Role != LayerCompositeRole.Guide && sample.Role != LayerCompositeRole.Camera)
            .OrderBy(sample => sample.ZIndex)
            .ToList();

        for (var index = 0; index < renderable.Count; index++)
        {
            var sample = renderable[index];
            if (sample.Role == LayerCompositeRole.Mask)
            {
                var maskedPixels = RenderMaskedGroup(renderable, ref index, state);
                BlendPixels(pixels, maskedPixels, stride, LayerBlendMode.Normal);
                continue;
            }

            var layerPixels = RenderLayerPixels(sample, state, renderAsMask: false);
            BlendPixels(pixels, layerPixels, stride, sample.BlendMode);
        }

        return CreateWriteableBitmap(pixelSize, pixels);
    }

    public static LayerSnapshot? ResolveActiveCamera(IReadOnlyList<RenderableLayerSample> samples)
    {
        LayerSnapshot? activeCamera = null;
        foreach (var sample in samples)
        {
            if (sample.Role == LayerCompositeRole.Camera)
            {
                activeCamera = sample.Snapshot;
            }
        }

        return activeCamera;
    }

    public static LayerSnapshot TransformSnapshot(
        LayerSnapshot snapshot,
        LayerSnapshot? activeCamera,
        double canvasWidth,
        double canvasHeight,
        double? parallaxDepth = null)
    {
        if (activeCamera is null || snapshot.Compositing.Role == LayerCompositeRole.Camera)
        {
            return snapshot;
        }

        var camera = activeCamera.Value;
        var depth = Math.Max(0.2d, parallaxDepth ?? snapshot.Compositing.ParallaxDepth);
        var cameraWidth = Math.Max(1d, camera.Width);
        var cameraHeight = Math.Max(1d, camera.Height);
        var scaleX = canvasWidth / cameraWidth;
        var scaleY = canvasHeight / cameraHeight;
        var styleScale = (scaleX + scaleY) / 2d;
        return snapshot with
        {
            X = (snapshot.X - (camera.X / depth)) * scaleX,
            Y = (snapshot.Y - (camera.Y / depth)) * scaleY,
            Width = snapshot.Width * scaleX,
            Height = snapshot.Height * scaleY,
            CornerRadius = snapshot.CornerRadius * styleScale,
            FontSize = snapshot.FontSize * styleScale,
            StrokeThickness = Math.Max(1d, snapshot.StrokeThickness * styleScale),
            Compositing = snapshot.Compositing.Clone()
        };
    }

    public static Point TransformPoint(
        Point worldPoint,
        LayerSnapshot? activeCamera,
        double canvasWidth,
        double canvasHeight,
        double parallaxDepth = 1d)
    {
        if (activeCamera is null)
        {
            return worldPoint;
        }

        var camera = activeCamera.Value;
        var depth = Math.Max(0.2d, parallaxDepth);
        var scaleX = canvasWidth / Math.Max(1d, camera.Width);
        var scaleY = canvasHeight / Math.Max(1d, camera.Height);
        return new Point(
            (worldPoint.X - (camera.X / depth)) * scaleX,
            (worldPoint.Y - (camera.Y / depth)) * scaleY);
    }

    public static Point InverseTransformPoint(
        Point viewportPoint,
        LayerSnapshot? activeCamera,
        double canvasWidth,
        double canvasHeight,
        double parallaxDepth = 1d)
    {
        if (activeCamera is null)
        {
            return viewportPoint;
        }

        var camera = activeCamera.Value;
        var depth = Math.Max(0.2d, parallaxDepth);
        var scaleX = canvasWidth / Math.Max(1d, camera.Width);
        var scaleY = canvasHeight / Math.Max(1d, camera.Height);
        return new Point(
            (viewportPoint.X / scaleX) + (camera.X / depth),
            (viewportPoint.Y / scaleY) + (camera.Y / depth));
    }

    private static byte[] RenderMaskedGroup(IReadOnlyList<RenderableLayerSample> renderable, ref int index, CompositeSceneState state)
    {
        var maskSample = renderable[index];
        var width = Math.Max(1, (int)Math.Ceiling(state.CanvasWidth));
        var stride = width * 4;
        var groupPixels = new byte[stride * Math.Max(1, (int)Math.Ceiling(state.CanvasHeight))];
        var maskPixels = RenderLayerPixels(maskSample, state, renderAsMask: true);
        var consumed = 0;
        var lastIndex = index;

        for (var candidateIndex = index + 1; candidateIndex < renderable.Count && consumed < maskSample.MaskLayerCount; candidateIndex++)
        {
            var candidate = renderable[candidateIndex];
            if (candidate.Role == LayerCompositeRole.Mask)
            {
                break;
            }

            if (candidate.Role == LayerCompositeRole.Guide || candidate.Role == LayerCompositeRole.Camera)
            {
                continue;
            }

            var layerPixels = RenderLayerPixels(candidate, state, renderAsMask: false);
            BlendPixels(groupPixels, layerPixels, stride, candidate.BlendMode);
            consumed++;
            lastIndex = candidateIndex;
        }

        index = lastIndex;
        ApplyMask(groupPixels, maskPixels);
        return groupPixels;
    }

    private static byte[] RenderBackgroundPixels(int width, int height, string backgroundFrom, string backgroundTo)
    {
        var bitmap = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
        try
        {
            using var context = bitmap.CreateDrawingContext(true);
            DrawBackground(context, new Rect(0, 0, width, height), backgroundFrom, backgroundTo);
            return CopyPixels(bitmap, width, height);
        }
        finally
        {
            bitmap.Dispose();
        }
    }

    private static byte[] RenderLayerPixels(RenderableLayerSample sample, CompositeSceneState state, bool renderAsMask)
    {
        var width = Math.Max(1, (int)Math.Ceiling(state.CanvasWidth));
        var height = Math.Max(1, (int)Math.Ceiling(state.CanvasHeight));
        var bitmap = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
        try
        {
            using var context = bitmap.CreateDrawingContext(true);
            var snapshot = TransformSnapshot(sample.Snapshot, state.ActiveCamera, state.CanvasWidth, state.CanvasHeight);
            DrawSample(context, sample, state, snapshot, renderAsMask);
            return CopyPixels(bitmap, width, height);
        }
        finally
        {
            bitmap.Dispose();
        }
    }

    private static byte[] CopyPixels(RenderTargetBitmap bitmap, int width, int height)
    {
        using var staging = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);
        using var framebuffer = staging.Lock();
        bitmap.CopyPixels(framebuffer, AlphaFormat.Unpremul);

        var pixels = new byte[framebuffer.RowBytes * framebuffer.Size.Height];
        Marshal.Copy(framebuffer.Address, pixels, 0, pixels.Length);
        return pixels;
    }

    private static WriteableBitmap CreateWriteableBitmap(PixelSize pixelSize, byte[] pixels)
    {
        var bitmap = new WriteableBitmap(
            pixelSize,
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);
        using var framebuffer = bitmap.Lock();
        Marshal.Copy(pixels, 0, framebuffer.Address, Math.Min(pixels.Length, framebuffer.RowBytes * framebuffer.Size.Height));
        return bitmap;
    }

    private static void DrawBackground(DrawingContext context, Rect stageRect, string backgroundFrom, string backgroundTo)
    {
        var stageBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0.15, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0.9, 1, RelativeUnit.Relative),
            GradientStops =
            [
                new GradientStop(ColorHelpers.Parse(backgroundFrom, "#09111F"), 0),
                new GradientStop(ColorHelpers.Parse(backgroundTo, "#182748"), 1)
            ]
        };

        context.DrawRectangle(stageBrush, null, stageRect);
        context.DrawEllipse(new SolidColorBrush(Color.FromArgb(30, 36, 229, 193)), null, new Point(stageRect.Width * 0.22, stageRect.Height * 0.2), 150, 120);
        context.DrawEllipse(new SolidColorBrush(Color.FromArgb(24, 255, 138, 76)), null, new Point(stageRect.Width * 0.84, stageRect.Height * 0.76), 170, 130);
    }

    private static void DrawSample(
        DrawingContext context,
        RenderableLayerSample sample,
        CompositeSceneState state,
        LayerSnapshot snapshot,
        bool renderAsMask)
    {
        var kind = sample.Kind;
        var compositing = snapshot.Compositing;
        var fillBase = renderAsMask
            ? Color.FromArgb((byte)(Math.Clamp(snapshot.Opacity, 0, 1) * 255), 255, 255, 255)
            : ApplyVisualAdjustments(ColorHelpers.Parse(snapshot.Fill, "#FFFFFF"), compositing);
        var strokeBase = renderAsMask
            ? fillBase
            : ApplyVisualAdjustments(ColorHelpers.Parse(snapshot.Stroke, "#FFFFFF"), compositing);
        var gradientFrom = renderAsMask
            ? fillBase
            : ApplyVisualAdjustments(ColorHelpers.Parse(snapshot.GradientFrom, snapshot.Fill), compositing);
        var gradientTo = renderAsMask
            ? fillBase
            : ApplyVisualAdjustments(ColorHelpers.Parse(snapshot.GradientTo, snapshot.Stroke), compositing);
        var opacity = Math.Clamp(snapshot.Opacity, 0, 1);

        if (!renderAsMask)
        {
            DrawShadowPass(context, sample, state, snapshot, fillBase, strokeBase, compositing, opacity);
            DrawBlurPass(context, sample, state, snapshot, fillBase, strokeBase, gradientFrom, gradientTo, opacity);
            DrawGlowPass(context, sample, state, snapshot, compositing, opacity);
        }

        DrawLayerCore(context, sample, state, snapshot, fillBase, strokeBase, gradientFrom, gradientTo, opacity, renderAsMask);
    }

    private static void DrawShadowPass(
        DrawingContext context,
        RenderableLayerSample sample,
        CompositeSceneState state,
        LayerSnapshot snapshot,
        Color fillBase,
        Color strokeBase,
        LayerCompositeSettings compositing,
        double opacity)
    {
        if (compositing.ShadowOpacity <= 0d && (Math.Abs(compositing.ShadowOffsetX) > 0.01d || Math.Abs(compositing.ShadowOffsetY) > 0.01d) is false)
        {
            return;
        }

        var shadowColor = ColorHelpers.Parse(compositing.ShadowColor, "#09111F");
        var shadowOpacity = Math.Clamp(opacity * Math.Max(0d, compositing.ShadowOpacity), 0d, 1d);
        if (shadowOpacity <= 0d)
        {
            return;
        }

        var shadowSnapshot = snapshot with
        {
            X = snapshot.X + compositing.ShadowOffsetX,
            Y = snapshot.Y + compositing.ShadowOffsetY
        };
        var offsets = GetEffectOffsets(compositing.ShadowBlur);
        if (offsets.Count == 0)
        {
            offsets = [new Point(0, 0)];
        }

        var alphaPerSample = shadowOpacity / offsets.Count;
        foreach (var offset in offsets)
        {
            var passSnapshot = shadowSnapshot with
            {
                X = shadowSnapshot.X + offset.X,
                Y = shadowSnapshot.Y + offset.Y
            };
            DrawLayerCore(
                context,
                sample,
                state,
                passSnapshot,
                shadowColor,
                shadowColor,
                shadowColor,
                shadowColor,
                alphaPerSample,
                renderAsMask: false,
                includeAmbientGlow: false);
        }
    }

    private static void DrawBlurPass(
        DrawingContext context,
        RenderableLayerSample sample,
        CompositeSceneState state,
        LayerSnapshot snapshot,
        Color fillBase,
        Color strokeBase,
        Color gradientFrom,
        Color gradientTo,
        double opacity)
    {
        var blurRadius = snapshot.Compositing.BlurRadius;
        if (blurRadius <= 0.1d)
        {
            return;
        }

        var offsets = GetEffectOffsets(blurRadius);
        if (offsets.Count == 0)
        {
            return;
        }

        var alphaPerSample = Math.Clamp(opacity * 0.24d / offsets.Count, 0d, 0.12d);
        foreach (var offset in offsets)
        {
            var passSnapshot = snapshot with
            {
                X = snapshot.X + offset.X,
                Y = snapshot.Y + offset.Y
            };
            DrawLayerCore(
                context,
                sample,
                state,
                passSnapshot,
                fillBase,
                strokeBase,
                gradientFrom,
                gradientTo,
                alphaPerSample,
                renderAsMask: false,
                includeAmbientGlow: false);
        }
    }

    private static void DrawGlowPass(
        DrawingContext context,
        RenderableLayerSample sample,
        CompositeSceneState state,
        LayerSnapshot snapshot,
        LayerCompositeSettings compositing,
        double opacity)
    {
        if (compositing.GlowOpacity <= 0d || compositing.GlowSize <= 0.1d)
        {
            return;
        }

        var glowColor = ColorHelpers.Parse(compositing.GlowColor, snapshot.Fill);
        var offsets = GetEffectOffsets(compositing.GlowSize);
        if (offsets.Count == 0)
        {
            offsets = [new Point(0, 0)];
        }

        var alphaPerSample = Math.Clamp(opacity * compositing.GlowOpacity / offsets.Count, 0d, 0.18d);
        foreach (var offset in offsets)
        {
            var passSnapshot = snapshot with
            {
                X = snapshot.X + offset.X,
                Y = snapshot.Y + offset.Y
            };
            DrawLayerCore(
                context,
                sample,
                state,
                passSnapshot,
                glowColor,
                glowColor,
                glowColor,
                glowColor,
                alphaPerSample,
                renderAsMask: false,
                includeAmbientGlow: false);
        }
    }

    private static void DrawLayerCore(
        DrawingContext context,
        RenderableLayerSample sample,
        CompositeSceneState state,
        LayerSnapshot snapshot,
        Color fillBase,
        Color strokeBase,
        Color gradientFrom,
        Color gradientTo,
        double opacity,
        bool renderAsMask,
        bool includeAmbientGlow = true)
    {
        var rect = new Rect(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height);
        var fillColor = ApplyAlpha(fillBase, (byte)(Math.Clamp(opacity, 0d, 1d) * 255));
        var fillBrush = renderAsMask
            ? new SolidColorBrush(fillColor)
            : CreateFillBrush(snapshot, fillColor, gradientFrom, gradientTo, opacity);
        var strokeBrush = new SolidColorBrush(ApplyAlpha(strokeBase, (byte)(Math.Clamp(opacity, 0d, 1d) * 220)));
        var strokePen = new Pen(
            strokeBrush,
            Math.Max(1d, snapshot.StrokeThickness),
            lineCap: PenLineCap.Round,
            lineJoin: PenLineJoin.Round);

        switch (sample.Kind)
        {
            case LayerKind.Rectangle:
                if (includeAmbientGlow && !renderAsMask)
                {
                    var glowBrush = new SolidColorBrush(ApplyAlpha(fillColor, 32));
                    context.DrawRectangle(glowBrush, null, rect.Inflate(10), snapshot.CornerRadius + 10, snapshot.CornerRadius + 10);
                }

                using (PushRotation(context, rect, snapshot.Rotation))
                {
                    context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                }

                return;
            case LayerKind.Ellipse:
                if (includeAmbientGlow && !renderAsMask)
                {
                    var glowBrush = new SolidColorBrush(ApplyAlpha(fillColor, 32));
                    context.DrawRectangle(glowBrush, null, rect.Inflate(10), snapshot.CornerRadius + 10, snapshot.CornerRadius + 10);
                }

                using (PushRotation(context, rect, snapshot.Rotation))
                {
                    context.DrawEllipse(fillBrush, strokePen, rect.Center, rect.Width / 2d, rect.Height / 2d);
                }

                return;
            case LayerKind.Path:
                DrawPath(context, rect, snapshot, fillBrush, strokeBrush, includeAmbientGlow && !renderAsMask);
                return;
            case LayerKind.Video:
                if (sample.SourceMediaAssetId is Guid mediaAssetId &&
                    state.MediaAssets.TryGetValue(mediaAssetId, out var mediaAsset))
                {
                    using (PushRotation(context, rect, snapshot.Rotation))
                    {
                        MediaAssetRenderService.DrawVideoFrame(
                            context,
                            rect,
                            snapshot,
                            mediaAsset,
                            sample.MediaTime ?? 0d,
                            opacity,
                            fillBase,
                            strokeBase,
                            renderAsMask);
                    }
                }
                else
                {
                    using (PushRotation(context, rect, snapshot.Rotation))
                    {
                        MediaAssetRenderService.DrawSyntheticVideoFrame(
                            context,
                            rect,
                            snapshot,
                            "MEDIA",
                            snapshot.Fill,
                            snapshot.Stroke,
                            opacity,
                            renderAsMask);
                    }
                }

                return;
            case LayerKind.Text:
                var plateColor = renderAsMask
                    ? fillColor
                    : ApplyAlpha(Color.Parse("#102038"), (byte)(Math.Clamp(opacity, 0d, 1d) * 180));
                context.DrawRectangle(new SolidColorBrush(plateColor), strokePen, rect, 24, 24);
                DrawText(context, rect, snapshot.Text, fillBrush, snapshot.FontSize);
                return;
            default:
                return;
        }
    }

    private static void DrawText(DrawingContext context, Rect rect, string text, IBrush fillBrush, double fontSize)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default),
            Math.Max(14d, fontSize),
            fillBrush);
        var point = new Point(rect.X + 22, rect.Y + Math.Max(16d, (rect.Height - formattedText.Height) / 2d));
        context.DrawText(formattedText, point);
    }

    private static Color ApplyVisualAdjustments(Color color, LayerCompositeSettings compositing)
    {
        var adjusted = color;
        if (Math.Abs(compositing.Saturation - 1d) > 0.001d)
        {
            var luminance = (0.299d * adjusted.R) + (0.587d * adjusted.G) + (0.114d * adjusted.B);
            adjusted = Color.FromArgb(
                adjusted.A,
                ClampChannel(luminance + ((adjusted.R - luminance) * compositing.Saturation)),
                ClampChannel(luminance + ((adjusted.G - luminance) * compositing.Saturation)),
                ClampChannel(luminance + ((adjusted.B - luminance) * compositing.Saturation)));
        }

        if (Math.Abs(compositing.Brightness) > 0.001d)
        {
            var shift = 255d * compositing.Brightness;
            adjusted = Color.FromArgb(
                adjusted.A,
                ClampChannel(adjusted.R + shift),
                ClampChannel(adjusted.G + shift),
                ClampChannel(adjusted.B + shift));
        }

        if (compositing.TintStrength > 0d)
        {
            var tint = ColorHelpers.Parse(compositing.TintColor, "#FFFFFF");
            adjusted = Blend(adjusted, tint, compositing.TintStrength);
        }

        return adjusted;
    }

    private static IReadOnlyList<Point> GetEffectOffsets(double radius)
    {
        var normalized = Math.Clamp(radius, 0d, 32d);
        if (normalized <= 0.1d)
        {
            return [];
        }

        var points = new List<Point>(EffectSampleCount);
        for (var sample = 0; sample < EffectSampleCount; sample++)
        {
            var angle = (Math.PI * 2d * sample) / EffectSampleCount;
            points.Add(new Point(Math.Cos(angle) * normalized, Math.Sin(angle) * normalized));
        }

        return points;
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

    private static IBrush CreateFillBrush(LayerSnapshot snapshot, Color fillColor, Color gradientFrom, Color gradientTo, double opacity)
    {
        if (!snapshot.UseGradient)
        {
            return new SolidColorBrush(fillColor);
        }

        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops =
            [
                new GradientStop(ApplyAlpha(gradientFrom, (byte)(opacity * 255)), 0),
                new GradientStop(ApplyAlpha(gradientTo, (byte)(opacity * 255)), 1)
            ]
        };
    }

    private static void DrawPath(DrawingContext context, Rect rect, LayerSnapshot snapshot, IBrush fillBrush, SolidColorBrush strokeBrush, bool includeGlow)
    {
        var points = VectorPathService.GetAbsolutePoints(snapshot)
            .Select(point => new Point(point.X, point.Y))
            .ToList();
        if (points.Count < 2)
        {
            return;
        }

        var geometry = new StreamGeometry();
        using (var geometryContext = geometry.Open())
        {
            geometryContext.BeginFigure(points[0], isFilled: snapshot.IsClosed);
            foreach (var point in points.Skip(1))
            {
                geometryContext.LineTo(point);
            }

            geometryContext.EndFigure(snapshot.IsClosed);
        }

        var strokePen = new Pen(strokeBrush, Math.Max(1d, snapshot.StrokeThickness), lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        using (PushRotation(context, rect, snapshot.Rotation))
        {
            if (includeGlow)
            {
                var glowPen = new Pen(
                    new SolidColorBrush(ApplyAlpha(ColorHelpers.Parse(snapshot.Stroke, "#FFFFFF"), 48)),
                    Math.Max(10d, snapshot.StrokeThickness + 10d),
                    lineCap: PenLineCap.Round,
                    lineJoin: PenLineJoin.Round);
                context.DrawGeometry(null, glowPen, geometry);
            }

            context.DrawGeometry(snapshot.IsClosed ? fillBrush : null, strokePen, geometry);
        }
    }

    private static void ApplyMask(byte[] contentPixels, byte[] maskPixels)
    {
        var length = Math.Min(contentPixels.Length, maskPixels.Length);
        for (var index = 0; index < length; index += 4)
        {
            var maskAlpha = maskPixels[index + 3] / 255d;
            contentPixels[index + 3] = ClampChannel(contentPixels[index + 3] * maskAlpha);
        }
    }

    private static void BlendPixels(byte[] destination, byte[] source, int stride, LayerBlendMode blendMode)
    {
        var length = Math.Min(destination.Length, source.Length);
        for (var index = 0; index < length; index += 4)
        {
            var sourceAlpha = source[index + 3] / 255d;
            if (sourceAlpha <= 0d)
            {
                continue;
            }

            var destinationAlpha = destination[index + 3] / 255d;
            var backdropBlue = destination[index] / 255d;
            var backdropGreen = destination[index + 1] / 255d;
            var backdropRed = destination[index + 2] / 255d;
            var sourceBlue = source[index] / 255d;
            var sourceGreen = source[index + 1] / 255d;
            var sourceRed = source[index + 2] / 255d;
            var outAlpha = sourceAlpha + (destinationAlpha * (1d - sourceAlpha));

            if (outAlpha <= 0d)
            {
                destination[index] = 0;
                destination[index + 1] = 0;
                destination[index + 2] = 0;
                destination[index + 3] = 0;
                continue;
            }

            destination[index] = BlendChannel(backdropBlue, sourceBlue, destinationAlpha, sourceAlpha, outAlpha, blendMode);
            destination[index + 1] = BlendChannel(backdropGreen, sourceGreen, destinationAlpha, sourceAlpha, outAlpha, blendMode);
            destination[index + 2] = BlendChannel(backdropRed, sourceRed, destinationAlpha, sourceAlpha, outAlpha, blendMode);
            destination[index + 3] = ClampChannel(outAlpha * 255d);
        }
    }

    private static byte BlendChannel(
        double backdrop,
        double source,
        double backdropAlpha,
        double sourceAlpha,
        double outAlpha,
        LayerBlendMode blendMode)
    {
        var blended = blendMode switch
        {
            LayerBlendMode.Add => Math.Min(1d, backdrop + source),
            LayerBlendMode.Screen => 1d - ((1d - backdrop) * (1d - source)),
            LayerBlendMode.Multiply => backdrop * source,
            LayerBlendMode.Overlay => backdrop <= 0.5d
                ? 2d * backdrop * source
                : 1d - (2d * (1d - backdrop) * (1d - source)),
            _ => source
        };

        var result =
            ((sourceAlpha * (1d - backdropAlpha) * source) +
             (sourceAlpha * backdropAlpha * blended) +
             ((1d - sourceAlpha) * backdropAlpha * backdrop)) / outAlpha;

        return ClampChannel(result * 255d);
    }

    private static Color Blend(Color baseColor, Color tint, double amount)
    {
        var normalized = Math.Clamp(amount, 0d, 1d);
        return Color.FromArgb(
            baseColor.A,
            ClampChannel(baseColor.R + ((tint.R - baseColor.R) * normalized)),
            ClampChannel(baseColor.G + ((tint.G - baseColor.G) * normalized)),
            ClampChannel(baseColor.B + ((tint.B - baseColor.B) * normalized)));
    }

    private static Color ApplyAlpha(Color color, byte alpha)
    {
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private static byte ClampChannel(double value)
    {
        return (byte)Math.Clamp(Math.Round(value), 0d, 255d);
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
