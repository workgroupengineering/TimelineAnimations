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
        IReadOnlyDictionary<Guid, LibraryItem> libraryItems,
        IReadOnlyDictionary<Guid, MediaAsset> mediaAssets,
        IReadOnlyList<RenderableLayerSample> samples,
        LayerSnapshot? activeCamera)
    {
        CanvasWidth = canvasWidth;
        CanvasHeight = canvasHeight;
        BackgroundFrom = backgroundFrom;
        BackgroundTo = backgroundTo;
        LibraryItems = libraryItems;
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

    public IReadOnlyDictionary<Guid, LibraryItem> LibraryItems { get; }

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
            document.LibraryItems.ToDictionary(item => item.Id),
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
        var isOutlineMode = snapshot.ShowAsOutline && !renderAsMask;
        var outlineBase = renderAsMask
            ? fillBase
            : ColorHelpers.ApplyCompositeColorEffects(ColorHelpers.Parse(snapshot.OutlineColor, snapshot.Stroke), snapshot.Compositing);
        if (isOutlineMode)
        {
            fillBase = outlineBase;
            strokeBase = outlineBase;
            gradientFrom = outlineBase;
            gradientTo = outlineBase;
        }

        var fillColor = ApplyAlpha(fillBase, (byte)(Math.Clamp(opacity, 0d, 1d) * 255));
        var fillBrush = renderAsMask
            ? new SolidColorBrush(fillColor)
            : isOutlineMode && sample.Kind is not LayerKind.Text
                ? new SolidColorBrush(Colors.Transparent)
                : CreateFillBrush(snapshot, fillColor, gradientFrom, gradientTo, opacity);
        var strokeBrush = new SolidColorBrush(ApplyAlpha(strokeBase, (byte)(Math.Clamp(opacity, 0d, 1d) * 220)));
        var strokePen = LayerStyleRenderHelper.CreateStrokePen(snapshot, strokeBrush, Math.Max(1d, snapshot.StrokeThickness));

        switch (sample.Kind)
        {
            case LayerKind.Rectangle:
                if (includeAmbientGlow && !renderAsMask && !isOutlineMode)
                {
                    var glowBrush = new SolidColorBrush(ApplyAlpha(fillColor, 32));
                    context.DrawRectangle(glowBrush, null, rect.Inflate(10), snapshot.CornerRadius + 10, snapshot.CornerRadius + 10);
                }

                using (PushLayerTransform(context, rect, snapshot, ResolveLibraryItem(state, sample.SourceLibraryItemId)))
                {
                    context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                }

                return;
            case LayerKind.Ellipse:
                if (includeAmbientGlow && !renderAsMask && !isOutlineMode)
                {
                    var glowBrush = new SolidColorBrush(ApplyAlpha(fillColor, 32));
                    context.DrawRectangle(glowBrush, null, rect.Inflate(10), snapshot.CornerRadius + 10, snapshot.CornerRadius + 10);
                }

                using (PushLayerTransform(context, rect, snapshot, ResolveLibraryItem(state, sample.SourceLibraryItemId)))
                {
                    context.DrawEllipse(fillBrush, strokePen, rect.Center, rect.Width / 2d, rect.Height / 2d);
                }

                return;
            case LayerKind.Path:
                DrawPath(context, rect, snapshot, fillBrush, strokeBrush, strokePen, includeAmbientGlow && !renderAsMask && snapshot.HasStroke, ResolveLibraryItem(state, sample.SourceLibraryItemId));
                return;
            case LayerKind.AvaloniaControl:
                DrawAvaloniaControl(context, rect, snapshot, fillBrush, strokePen, fillColor, strokeBase, opacity, ResolveLibraryItem(state, sample.SourceLibraryItemId));
                return;
            case LayerKind.Video:
                if (sample.SourceMediaAssetId is Guid mediaAssetId &&
                    state.MediaAssets.TryGetValue(mediaAssetId, out var mediaAsset))
                {
                    using (PushLayerTransform(context, rect, snapshot, ResolveLibraryItem(state, sample.SourceLibraryItemId)))
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
                    using (PushLayerTransform(context, rect, snapshot, ResolveLibraryItem(state, sample.SourceLibraryItemId)))
                    {
                        MediaAssetRenderService.DrawSyntheticVideoFrame(
                            context,
                            rect,
                            snapshot,
                            "MEDIA",
                            ColorHelpers.ToHex(fillBase),
                            ColorHelpers.ToHex(strokeBase),
                            opacity,
                            renderAsMask);
                    }
                }

                return;
            case LayerKind.Text:
                var plateColor = renderAsMask
                    ? fillColor
                    : ApplyAlpha(Color.Parse("#102038"), (byte)(Math.Clamp(opacity, 0d, 1d) * 180));
                using (PushLayerTransform(context, rect, snapshot, ResolveLibraryItem(state, sample.SourceLibraryItemId)))
                {
                    if (!isOutlineMode)
                    {
                        context.DrawRectangle(new SolidColorBrush(plateColor), strokePen, rect, 24, 24);
                    }

                    DrawText(context, rect, snapshot.Text, fillBrush, snapshot.FontSize, snapshot.TextSettings);
                }
                return;
            default:
                return;
        }
    }

    private static void DrawAvaloniaControl(
        DrawingContext context,
        Rect rect,
        LayerSnapshot snapshot,
        IBrush fillBrush,
        IPen strokePen,
        Color fillColor,
        Color strokeBase,
        double opacity,
        LibraryItem? sourceLibraryItem)
    {
        var settings = snapshot.AvaloniaControl;
        var textBrush = new SolidColorBrush(ApplyAlpha(Color.Parse("#F5F7FA"), (byte)(Math.Clamp(opacity, 0d, 1d) * 255)));
        var mutedBrush = new SolidColorBrush(ApplyAlpha(Blend(strokeBase, Color.Parse("#F5F7FA"), 0.35d), (byte)(Math.Clamp(opacity, 0d, 1d) * 210)));

        using var _ = PushLayerTransform(context, rect, snapshot, sourceLibraryItem);
        switch (settings.Kind)
        {
            case AvaloniaControlKind.Button:
                context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                DrawCenteredText(context, rect, string.IsNullOrWhiteSpace(settings.Content) ? "Button" : settings.Content, textBrush, 15d);
                return;
            case AvaloniaControlKind.TextBlock:
                DrawText(context, rect, string.IsNullOrWhiteSpace(settings.Content) ? snapshot.Text : settings.Content, textBrush, Math.Max(16d, snapshot.FontSize));
                return;
            case AvaloniaControlKind.TextBox:
                context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                DrawText(context, rect, string.IsNullOrWhiteSpace(settings.Content) ? "Type here" : settings.Content, textBrush, 14d);
                return;
            case AvaloniaControlKind.CheckBox:
            {
                var boxSize = Math.Min(24d, rect.Height - 10d);
                var boxRect = new Rect(rect.X + 8d, rect.Center.Y - (boxSize / 2d), boxSize, boxSize);
                context.DrawRectangle(new SolidColorBrush(ApplyAlpha(fillColor, 48)), strokePen, boxRect, 6d, 6d);
                if (settings.IsChecked)
                {
                    var checkPen = new Pen(textBrush, 2.2d);
                    context.DrawLine(checkPen, new Point(boxRect.X + 5d, boxRect.Center.Y), new Point(boxRect.X + 10d, boxRect.Bottom - 6d));
                    context.DrawLine(checkPen, new Point(boxRect.X + 10d, boxRect.Bottom - 6d), new Point(boxRect.Right - 5d, boxRect.Y + 6d));
                }

                DrawText(context, new Rect(boxRect.Right + 10d, rect.Y, rect.Width - boxSize - 24d, rect.Height), string.IsNullOrWhiteSpace(settings.Content) ? "CheckBox" : settings.Content, textBrush, 14d);
                return;
            }
            case AvaloniaControlKind.ToggleButton:
            {
                var trackRect = new Rect(rect.X + 10d, rect.Center.Y - 13d, 52d, 26d);
                var knobRect = new Rect(settings.IsChecked ? trackRect.Right - 22d : trackRect.X + 2d, trackRect.Y + 2d, 20d, 20d);
                context.DrawRectangle(new SolidColorBrush(ApplyAlpha(fillColor, settings.IsChecked ? (byte)168 : (byte)72)), strokePen, trackRect, 13d, 13d);
                context.DrawEllipse(textBrush, null, knobRect.Center, knobRect.Width / 2d, knobRect.Height / 2d);
                DrawText(context, new Rect(trackRect.Right + 10d, rect.Y, Math.Max(40d, rect.Width - 74d), rect.Height), string.IsNullOrWhiteSpace(settings.Content) ? "Toggle" : settings.Content, textBrush, 14d);
                return;
            }
            case AvaloniaControlKind.Slider:
            {
                var trackRect = new Rect(rect.X + 14d, rect.Center.Y - 2d, Math.Max(28d, rect.Width - 28d), 4d);
                var normalized = NormalizeRange(settings.Value, settings.Minimum, settings.Maximum);
                var knobX = trackRect.X + (trackRect.Width * normalized);
                context.DrawRectangle(new SolidColorBrush(ApplyAlpha(strokeBase, 96)), null, trackRect, 2d, 2d);
                context.DrawRectangle(new SolidColorBrush(ApplyAlpha(fillColor, 210)), null, new Rect(trackRect.X, trackRect.Y, Math.Max(6d, knobX - trackRect.X), trackRect.Height), 2d, 2d);
                context.DrawEllipse(fillBrush, strokePen, new Point(knobX, trackRect.Center.Y), 9d, 9d);
                return;
            }
            case AvaloniaControlKind.ProgressBar:
            {
                context.DrawRectangle(new SolidColorBrush(ApplyAlpha(strokeBase, 72)), strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                var normalized = NormalizeRange(settings.Value, settings.Minimum, settings.Maximum);
                var fillRect = new Rect(rect.X + 2d, rect.Y + 2d, Math.Max(8d, (rect.Width - 4d) * normalized), Math.Max(8d, rect.Height - 4d));
                context.DrawRectangle(fillBrush, null, fillRect, Math.Max(4d, snapshot.CornerRadius - 2d), Math.Max(4d, snapshot.CornerRadius - 2d));
                return;
            }
            case AvaloniaControlKind.ComboBox:
            {
                context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                var chevronPen = new Pen(textBrush, 1.8d);
                var arrowCenterX = rect.Right - 20d;
                var arrowCenterY = rect.Center.Y;
                context.DrawLine(chevronPen, new Point(arrowCenterX - 6d, arrowCenterY - 3d), new Point(arrowCenterX, arrowCenterY + 3d));
                context.DrawLine(chevronPen, new Point(arrowCenterX, arrowCenterY + 3d), new Point(arrowCenterX + 6d, arrowCenterY - 3d));
                DrawText(context, new Rect(rect.X + 14d, rect.Y, rect.Width - 42d, rect.Height), string.IsNullOrWhiteSpace(settings.Content) ? "Selected Item" : settings.Content, textBrush, 14d);
                return;
            }
            case AvaloniaControlKind.ListBox:
            {
                context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                var items = SplitControlItems(settings.Content, "Item 1", "Item 2", "Item 3");
                var rowHeight = Math.Max(22d, (rect.Height - 16d) / Math.Max(1, items.Count));
                for (var index = 0; index < items.Count; index++)
                {
                    var rowRect = new Rect(rect.X + 8d, rect.Y + 8d + (index * rowHeight), rect.Width - 16d, rowHeight - 4d);
                    if (index == 0)
                    {
                        context.DrawRectangle(new SolidColorBrush(ApplyAlpha(fillColor, 86)), null, rowRect, 8d, 8d);
                    }

                    DrawText(context, rowRect, items[index], textBrush, 13d);
                }

                return;
            }
            case AvaloniaControlKind.TabControl:
            {
                context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                var tabs = SplitControlItems(settings.Content, "Overview", "Settings", "Export");
                var active = string.IsNullOrWhiteSpace(settings.SecondaryContent) ? tabs[0] : settings.SecondaryContent;
                var tabWidth = Math.Max(54d, (rect.Width - 16d) / Math.Max(1, tabs.Count));
                for (var index = 0; index < tabs.Count; index++)
                {
                    var tabRect = new Rect(rect.X + 8d + (index * tabWidth), rect.Y + 8d, tabWidth - 4d, 28d);
                    var tabFill = string.Equals(tabs[index], active, StringComparison.OrdinalIgnoreCase)
                        ? new SolidColorBrush(ApplyAlpha(fillColor, 96))
                        : new SolidColorBrush(ApplyAlpha(strokeBase, 44));
                    context.DrawRectangle(tabFill, null, tabRect, 9d, 9d);
                    DrawCenteredText(context, tabRect, tabs[index], textBrush, 12d);
                }

                var bodyRect = new Rect(rect.X + 8d, rect.Y + 44d, rect.Width - 16d, Math.Max(28d, rect.Height - 52d));
                context.DrawRectangle(new SolidColorBrush(ApplyAlpha(strokeBase, 34)), null, bodyRect, Math.Max(8d, snapshot.CornerRadius - 4d), Math.Max(8d, snapshot.CornerRadius - 4d));
                DrawCenteredText(context, bodyRect, $"{active} content", mutedBrush, 12d);
                return;
            }
            case AvaloniaControlKind.Grid:
            {
                context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                var cellPen = new Pen(new SolidColorBrush(ApplyAlpha(strokeBase, 92)), 1.2d);
                var verticalMid = rect.X + (rect.Width / 2d);
                var horizontalMid = rect.Y + (rect.Height / 2d);
                context.DrawLine(cellPen, new Point(verticalMid, rect.Y + 10d), new Point(verticalMid, rect.Bottom - 10d));
                context.DrawLine(cellPen, new Point(rect.X + 10d, horizontalMid), new Point(rect.Right - 10d, horizontalMid));
                DrawCenteredText(context, rect, string.IsNullOrWhiteSpace(settings.Content) ? "2x2 Grid" : settings.Content, mutedBrush, 12d);
                return;
            }
            case AvaloniaControlKind.StackPanel:
            {
                context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                var items = SplitControlItems(settings.SecondaryContent, "Item A", "Item B", "Item C");
                var rowHeight = Math.Max(22d, (rect.Height - 18d) / Math.Max(1, items.Count));
                for (var index = 0; index < items.Count; index++)
                {
                    var rowRect = new Rect(rect.X + 10d, rect.Y + 8d + (index * rowHeight), rect.Width - 20d, rowHeight - 6d);
                    context.DrawRectangle(new SolidColorBrush(ApplyAlpha(fillColor, (byte)(index == 0 ? 88 : 52))), null, rowRect, 7d, 7d);
                    DrawText(context, rowRect, items[index], textBrush, 12d);
                }

                return;
            }
            case AvaloniaControlKind.PathIcon:
            {
                context.DrawRectangle(new SolidColorBrush(ApplyAlpha(fillColor, 36)), strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                var points = ParseControlPathGeometry(settings.SecondaryContent)
                    .Select(point => new Point(rect.X + (point.X * rect.Width), rect.Y + (point.Y * rect.Height)))
                    .ToList();
                var geometry = BuildPathGeometry(points, true);
                if (geometry is not null)
                {
                    context.DrawGeometry(textBrush, null, geometry);
                }
                else
                {
                    DrawCenteredText(context, rect, string.IsNullOrWhiteSpace(settings.Content) ? "Icon" : settings.Content, textBrush, 14d);
                }

                return;
            }
            case AvaloniaControlKind.Image:
                context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                context.DrawLine(new Pen(mutedBrush, 1.8d), new Point(rect.X + 18d, rect.Bottom - 18d), new Point(rect.Center.X - 8d, rect.Center.Y));
                context.DrawLine(new Pen(mutedBrush, 1.8d), new Point(rect.Center.X - 8d, rect.Center.Y), new Point(rect.Right - 18d, rect.Bottom - 34d));
                context.DrawEllipse(mutedBrush, null, new Point(rect.Right - 28d, rect.Y + 28d), 9d, 9d);
                DrawCenteredText(context, rect, string.IsNullOrWhiteSpace(settings.Source) ? "Image" : settings.Source, textBrush, 12d);
                return;
            case AvaloniaControlKind.Panel:
            case AvaloniaControlKind.Border:
                context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                if (!string.IsNullOrWhiteSpace(settings.Content))
                {
                    DrawText(context, new Rect(rect.X + 12d, rect.Y + 8d, rect.Width - 24d, 24d), settings.Content, textBrush, 12d);
                }

                return;
            default:
                context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                return;
        }
    }

    private static double NormalizeRange(double value, double minimum, double maximum)
    {
        var range = Math.Max(0.0001d, maximum - minimum);
        return TimelineMath.Clamp((value - minimum) / range, 0d, 1d);
    }

    private static List<string> SplitControlItems(string? value, params string[] fallback)
    {
        var items = string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(['|', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        return items.Count > 0 ? items : [.. fallback];
    }

    private static IReadOnlyList<Point> ParseControlPathGeometry(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return
            [
                new Point(0.5d, 0.08d),
                new Point(0.88d, 0.5d),
                new Point(0.5d, 0.92d),
                new Point(0.12d, 0.5d)
            ];
        }

        var values = System.Text.RegularExpressions.Regex.Matches(data, @"[-+]?\d*\.?\d+")
            .Select(match => double.TryParse(match.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : double.NaN)
            .Where(value => !double.IsNaN(value))
            .ToList();
        if (values.Count < 4)
        {
            return [];
        }

        var points = new List<Point>();
        for (var index = 0; index + 1 < values.Count; index += 2)
        {
            points.Add(new Point(values[index], values[index + 1]));
        }

        return points;
    }

    private static StreamGeometry? BuildPathGeometry(IReadOnlyList<Point> points, bool isClosed)
    {
        if (points.Count < 2)
        {
            return null;
        }

        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(points[0], isFilled: isClosed);
        foreach (var point in points.Skip(1))
        {
            context.LineTo(point);
        }

        context.EndFigure(isClosed);

        return geometry;
    }

    private static void DrawCenteredText(DrawingContext context, Rect rect, string text, IBrush fillBrush, double fontSize)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default),
            Math.Max(11d, fontSize),
            fillBrush);
        context.DrawText(formattedText, new Point(
            rect.X + Math.Max(8d, (rect.Width - formattedText.Width) / 2d),
            rect.Y + Math.Max(6d, (rect.Height - formattedText.Height) / 2d)));
    }

    private static void DrawText(DrawingContext context, Rect rect, string text, IBrush fillBrush, double fontSize, LayerTextSettings? textSettings = null)
    {
        var settings = textSettings ?? new LayerTextSettings();
        var contentRect = DrawFlashTextFieldChrome(context, rect, settings);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var typeface = BuildTypeface(settings);
        var lines = GetRenderedTextLines(text, settings);
        var measuredLines = lines
            .Select(line => new FormattedText(
                line,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                Math.Max(14d, fontSize),
                fillBrush))
            .ToList();
        var lineHeight = settings.LineHeight > 0d
            ? settings.LineHeight
            : measuredLines.Count == 0 ? Math.Max(14d, fontSize) : measuredLines.Max(item => item.Height);
        var totalHeight = measuredLines.Count == 0 ? 0d : measuredLines.Count * lineHeight;
        var y = contentRect.Y + Math.Max(16d, (contentRect.Height - totalHeight) / 2d);

        for (var index = 0; index < measuredLines.Count; index++)
        {
            var formattedLine = measuredLines[index];
            var x = settings.Alignment switch
            {
                LayerTextAlignment.Center => contentRect.X + Math.Max(8d, (contentRect.Width - formattedLine.Width) / 2d),
                LayerTextAlignment.Right => contentRect.Right - formattedLine.Width - 16d,
                _ => contentRect.X + 18d
            };

            if (Math.Abs(settings.LetterSpacing) > 0.001d)
            {
                DrawSpacedText(context, lines[index], fillBrush, typeface, Math.Max(14d, fontSize), new Point(x, y), settings.LetterSpacing);
            }
            else
            {
                context.DrawText(formattedLine, new Point(x, y));
            }

            y += lineHeight;
        }
    }

    private static Rect DrawFlashTextFieldChrome(DrawingContext context, Rect rect, LayerTextSettings settings)
    {
        if (settings.FieldKind == FlashTextFieldKind.Static)
        {
            return rect;
        }

        var borderColor = settings.ShowBorder
            ? Color.Parse("#8EEAFF")
            : Color.Parse("#44566F");
        var fillColor = settings.FieldKind == FlashTextFieldKind.Input
            ? Color.Parse("#102744")
            : Color.Parse("#0D1B31");
        var chromeRect = rect.Deflate(4d);
        context.DrawRectangle(
            new SolidColorBrush(ApplyAlpha(fillColor, settings.ShowBorder ? (byte)68 : (byte)34)),
            new Pen(new SolidColorBrush(ApplyAlpha(borderColor, settings.ShowBorder ? (byte)192 : (byte)112)), 1.2d),
            chromeRect,
            12d,
            12d);

        if (!string.IsNullOrWhiteSpace(settings.VariableName))
        {
            var label = new FormattedText(
                settings.VariableName,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Medium),
                10d,
                new SolidColorBrush(ApplyAlpha(Color.Parse("#B6D7FF"), 176)));
            context.DrawText(label, new Point(chromeRect.X + 10d, chromeRect.Bottom - label.Height - 6d));
        }

        return chromeRect.Deflate(8d);
    }

    private static string[] GetRenderedTextLines(string text, LayerTextSettings settings)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        if (settings.MaxCharacters > 0 && normalized.Length > settings.MaxCharacters)
        {
            normalized = normalized[..settings.MaxCharacters];
        }

        if (settings.IsPassword)
        {
            normalized = new string([.. normalized.Select(character => character == '\n' ? '\n' : '•')]);
        }

        if (settings.LineMode == FlashTextLineMode.SingleLine)
        {
            normalized = normalized.Replace('\n', ' ');
        }

        return normalized.Split('\n');
    }

    private static Typeface BuildTypeface(LayerTextSettings settings)
    {
        var family = settings.UseDeviceFonts || string.IsNullOrWhiteSpace(settings.FontFamily)
            ? FontFamily.Default
            : new FontFamily(settings.FontFamily);
        return new Typeface(
            family,
            settings.IsItalic ? FontStyle.Italic : FontStyle.Normal,
            settings.IsBold ? FontWeight.Bold : FontWeight.Normal);
    }

    private static void DrawSpacedText(
        DrawingContext context,
        string text,
        IBrush fillBrush,
        Typeface typeface,
        double fontSize,
        Point origin,
        double letterSpacing)
    {
        var x = origin.X;
        foreach (var character in text)
        {
            var glyph = new FormattedText(
                character.ToString(),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                fillBrush);
            context.DrawText(glyph, new Point(x, origin.Y));
            x += glyph.Width + letterSpacing;
        }
    }

    private static Color ApplyVisualAdjustments(Color color, LayerCompositeSettings compositing)
    {
        return ColorHelpers.ApplyCompositeColorEffects(color, compositing);
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

    private static IDisposable PushLayerTransform(DrawingContext context, Rect rect, LayerSnapshot snapshot, LibraryItem? sourceLibraryItem)
    {
        if (!LayerTransformHelper.TryCreateMatrix(rect, snapshot, sourceLibraryItem, out var matrix))
        {
            return EmptyDisposable.Instance;
        }

        return context.PushTransform(matrix);
    }

    private static IBrush CreateFillBrush(LayerSnapshot snapshot, Color fillColor, Color gradientFrom, Color gradientTo, double opacity)
    {
        return LayerStyleRenderHelper.CreateFillBrush(snapshot, fillColor, gradientFrom, gradientTo, opacity);
    }

    private static void DrawPath(DrawingContext context, Rect rect, LayerSnapshot snapshot, IBrush fillBrush, SolidColorBrush strokeBrush, IPen strokePen, bool includeGlow, LibraryItem? sourceLibraryItem)
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

        using (PushLayerTransform(context, rect, snapshot, sourceLibraryItem))
        {
            if (includeGlow)
            {
                var glowPen = new Pen(
                    new SolidColorBrush(ApplyAlpha(ColorHelpers.Parse(snapshot.Stroke, "#FFFFFF"), 48)),
                    Math.Max(10d, snapshot.StrokeThickness + 10d),
                    lineCap: strokePen.LineCap,
                    lineJoin: strokePen.LineJoin,
                    miterLimit: strokePen.MiterLimit);
                context.DrawGeometry(null, glowPen, geometry);
            }

            context.DrawGeometry(snapshot.IsClosed ? fillBrush : null, strokePen, geometry);
        }
    }

    private static LibraryItem? ResolveLibraryItem(CompositeSceneState state, Guid? sourceLibraryItemId)
    {
        return sourceLibraryItemId is Guid libraryItemId &&
               state.LibraryItems.TryGetValue(libraryItemId, out var libraryItem)
            ? libraryItem
            : null;
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
