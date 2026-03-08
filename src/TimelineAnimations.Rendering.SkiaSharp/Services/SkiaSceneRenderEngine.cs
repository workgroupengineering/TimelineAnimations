using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Buffers;
using SkiaSharp;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Rendering.Models;
using TimelineAnimations.Rendering.Services;
using TimelineAnimations.Rendering.SkiaSharp.Helpers;

namespace TimelineAnimations.Rendering.SkiaSharp.Services;

public sealed class SkiaSceneRenderEngine : ISceneRenderEngine
{
    private readonly ILayerRenderFallback _fallbackRenderer;

    public SkiaSceneRenderEngine()
        : this(TransparentLayerRenderFallback.Instance)
    {
    }

    public SkiaSceneRenderEngine(ILayerRenderFallback fallbackRenderer)
    {
        _fallbackRenderer = fallbackRenderer ?? throw new ArgumentNullException(nameof(fallbackRenderer));
    }

    public RenderingEngineKind EngineKind => RenderingEngineKind.SkiaSharp;

    public RenderedFrame Render(SceneRenderRequest request, bool includeBackground)
    {
        return CanRenderSceneDirect(request)
            ? RenderDirectScene(request, includeBackground)
            : RenderComposedScene(request, includeBackground);
    }

    private RenderedFrame RenderComposedScene(SceneRenderRequest request, bool includeBackground)
    {
        var width = Math.Max(1, (int)Math.Ceiling(request.CanvasWidth));
        var height = Math.Max(1, (int)Math.Ceiling(request.CanvasHeight));
        var stride = width * 4;
        var pixels = includeBackground
            ? CreateBackgroundPixels(width, height, request.BackgroundFrom, request.TransparentStageBackground)
            : new byte[stride * height];

        for (var index = 0; index < request.Samples.Count; index++)
        {
            var sample = request.Samples[index];
            if (sample.Role is LayerCompositeRole.Guide or LayerCompositeRole.Camera)
            {
                continue;
            }

            if (sample.Role == LayerCompositeRole.Mask)
            {
                var maskedPixels = RenderMaskedGroup(request.Samples, ref index, request);
                try
                {
                    RenderPixelBufferComposer.BlendPixels(pixels, maskedPixels.Buffer, LayerBlendMode.Normal, maskedPixels.Length);
                }
                finally
                {
                    ReturnPixelBuffer(maskedPixels);
                }

                continue;
            }

            var layerPixels = RenderLayerPixels(sample, request, renderAsMask: false);
            try
            {
                RenderPixelBufferComposer.BlendPixels(pixels, layerPixels.Buffer, sample.BlendMode, layerPixels.Length);
            }
            finally
            {
                ReturnPixelBuffer(layerPixels);
            }
        }

        return new RenderedFrame(width, height, stride, pixels);
    }

    private RenderedFrame RenderDirectScene(SceneRenderRequest request, bool includeBackground)
    {
        var width = Math.Max(1, (int)Math.Ceiling(request.CanvasWidth));
        var height = Math.Max(1, (int)Math.Ceiling(request.CanvasHeight));
        using var bitmap = CreateBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        if (includeBackground && !request.TransparentStageBackground)
        {
            using var backgroundPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SkiaColorHelpers.Parse(request.BackgroundFrom, "#FFFFFF")
            };
            canvas.DrawRect(SKRect.Create(width, height), backgroundPaint);
        }

        for (var index = 0; index < request.Samples.Count; index++)
        {
            var sample = request.Samples[index];
            if (sample.Role is LayerCompositeRole.Guide or LayerCompositeRole.Camera)
            {
                continue;
            }

            var snapshot = SceneRenderTransformService.TransformSnapshot(
                sample.Snapshot,
                request.ActiveCamera,
                request.Animate,
                request.CanvasWidth,
                request.CanvasHeight,
                sample.ParallaxDepth);
            DrawSample(canvas, sample, request, snapshot, renderAsMask: false);
        }

        return new RenderedFrame(width, height, width * 4, CopyPixelsOwned(bitmap));
    }

    private PooledPixelBuffer RenderMaskedGroup(IReadOnlyList<RenderableLayerSample> renderable, ref int index, SceneRenderRequest request)
    {
        var maskSample = renderable[index];
        var width = Math.Max(1, (int)Math.Ceiling(request.CanvasWidth));
        var height = Math.Max(1, (int)Math.Ceiling(request.CanvasHeight));
        var groupPixels = RentPixelBuffer(width * height * 4);
        var maskPixels = RenderLayerPixels(maskSample, request, renderAsMask: true);
        var consumed = 0;
        var lastIndex = index;

        try
        {
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

                var layerPixels = RenderLayerPixels(candidate, request, renderAsMask: false);
                try
                {
                    RenderPixelBufferComposer.BlendPixels(groupPixels.Buffer, layerPixels.Buffer, candidate.BlendMode, Math.Min(groupPixels.Length, layerPixels.Length));
                }
                finally
                {
                    ReturnPixelBuffer(layerPixels);
                }

                consumed++;
                lastIndex = candidateIndex;
            }

            index = lastIndex;
            RenderPixelBufferComposer.ApplyMask(groupPixels.Buffer, maskPixels.Buffer, Math.Min(groupPixels.Length, maskPixels.Length));
            return groupPixels;
        }
        finally
        {
            ReturnPixelBuffer(maskPixels);
        }
    }

    private static byte[] CreateBackgroundPixels(int width, int height, string backgroundColor, bool transparentStageBackground)
    {
        if (transparentStageBackground)
        {
            return new byte[width * height * 4];
        }

        return CreateSolidPixelBuffer(width, height, SkiaColorHelpers.Parse(backgroundColor, "#FFFFFF"));
    }

    private PooledPixelBuffer RenderLayerPixels(RenderableLayerSample sample, SceneRenderRequest request, bool renderAsMask)
    {
        if (!CanRenderNatively(sample, request))
        {
            return RenderFallbackPixels(sample, request, renderAsMask);
        }

        var width = Math.Max(1, (int)Math.Ceiling(request.CanvasWidth));
        var height = Math.Max(1, (int)Math.Ceiling(request.CanvasHeight));
        using var bitmap = CreateBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        var snapshot = SceneRenderTransformService.TransformSnapshot(
            sample.Snapshot,
            request.ActiveCamera,
            request.Animate,
            request.CanvasWidth,
            request.CanvasHeight,
            sample.ParallaxDepth);
        DrawSample(canvas, sample, request, snapshot, renderAsMask);
        return CopyPixels(bitmap);
    }

    private PooledPixelBuffer RenderFallbackPixels(RenderableLayerSample sample, SceneRenderRequest request, bool renderAsMask)
    {
        var fallbackSample = renderAsMask ? CreateMaskFallbackSample(sample) : sample;
        var rendered = _fallbackRenderer.RenderLayer(request, fallbackSample, renderAsMask);
        return new PooledPixelBuffer(rendered.Pixels, rendered.Pixels.Length, false);
    }

    private static RenderableLayerSample CreateMaskFallbackSample(RenderableLayerSample sample)
    {
        var compositing = sample.Snapshot.Compositing.Clone();
        compositing.Role = LayerCompositeRole.Normal;
        compositing.BlendMode = LayerBlendMode.Normal;
        var snapshot = sample.Snapshot with
        {
            Fill = "#FFFFFF",
            Stroke = "#FFFFFF",
            GradientFrom = "#FFFFFF",
            GradientTo = "#FFFFFF",
            OutlineColor = "#FFFFFF",
            UseGradient = false,
            ShowAsOutline = false,
            Compositing = compositing
        };

        return new RenderableLayerSample
        {
            SourceLayerId = sample.SourceLayerId,
            SourceLibraryItemId = sample.SourceLibraryItemId,
            SourceMediaAssetId = sample.SourceMediaAssetId,
            Kind = sample.Kind,
            ZIndex = sample.ZIndex,
            Snapshot = snapshot,
            MediaTime = sample.MediaTime,
            Warp = sample.Warp,
            Rig = sample.Rig
        };
    }

    private static bool CanRenderNatively(RenderableLayerSample sample, SceneRenderRequest request)
    {
        if ((sample.Warp.IsEnabled && sample.Warp.Pins.Count > 0) ||
            (sample.Rig.IsEnabled && sample.Rig.Bones.Count > 0))
        {
            return false;
        }

        if (RequiresEffectFallback(sample.Snapshot.Compositing))
        {
            return false;
        }

        return sample.Kind switch
        {
            LayerKind.Rectangle => true,
            LayerKind.Ellipse => true,
            LayerKind.Path => true,
            LayerKind.Text => true,
            LayerKind.AvaloniaControl => true,
            LayerKind.Video => !sample.SourceMediaAssetId.HasValue || !request.MediaAssets.ContainsKey(sample.SourceMediaAssetId.Value),
            LayerKind.Audio => true,
            _ => false
        };
    }

    private static bool CanRenderSceneDirect(SceneRenderRequest request)
    {
        for (var index = 0; index < request.Samples.Count; index++)
        {
            var sample = request.Samples[index];
            if (sample.Role is LayerCompositeRole.Guide or LayerCompositeRole.Camera)
            {
                continue;
            }

            if (sample.Role == LayerCompositeRole.Mask ||
                !SupportsNativeBlendMode(sample.BlendMode) ||
                !CanRenderNatively(sample, request))
            {
                return false;
            }
        }

        return true;
    }

    private static bool RequiresEffectFallback(LayerCompositeSettings compositing)
    {
        return (compositing.BevelStrength > 0.001d && compositing.BevelSize > 0.1d) ||
               (compositing.GradientGlowStrength > 0.001d && compositing.GradientGlowSize > 0.1d) ||
               (compositing.GradientBevelStrength > 0.001d && compositing.GradientBevelSize > 0.1d);
    }

    private void DrawSample(
        SKCanvas canvas,
        RenderableLayerSample sample,
        SceneRenderRequest request,
        LayerSnapshot snapshot,
        bool renderAsMask)
    {
        var compositing = snapshot.Compositing;
        var opacity = Math.Clamp(snapshot.Opacity, 0d, 1d);
        var fillBase = renderAsMask
            ? new SKColor(255, 255, 255, RenderPixelBufferComposer.ClampChannel(opacity * 255d))
            : SkiaColorHelpers.ApplyCompositeColorEffects(SkiaColorHelpers.Parse(snapshot.Fill, "#FFFFFF"), compositing);
        var strokeBase = renderAsMask
            ? fillBase
            : SkiaColorHelpers.ApplyCompositeColorEffects(SkiaColorHelpers.Parse(snapshot.Stroke, "#FFFFFF"), compositing);
        var gradientFrom = renderAsMask
            ? fillBase
            : SkiaColorHelpers.ApplyCompositeColorEffects(SkiaColorHelpers.Parse(snapshot.GradientFrom, snapshot.Fill), compositing);
        var gradientTo = renderAsMask
            ? fillBase
            : SkiaColorHelpers.ApplyCompositeColorEffects(SkiaColorHelpers.Parse(snapshot.GradientTo, snapshot.Stroke), compositing);
        var layerBounds = GetLayerBounds(snapshot);
        var layerPaint = renderAsMask ? null : CreateLayerBlendPaint(TryGetNativeBlendMode(sample.BlendMode));
        var layerSaveCount = layerPaint is null ? -1 : canvas.SaveLayer(layerBounds, layerPaint);
        SKPicture? effectSourcePicture = null;
        SKPicture? blurSourcePicture = null;
        try
        {
            if (!renderAsMask)
            {
                if (CanUseDirectEffectPass(sample))
                {
                    DrawShadowPassDirect(canvas, sample, request, snapshot, compositing, opacity, layerBounds);
                    DrawBlurPassDirect(canvas, sample, request, snapshot, compositing, layerBounds, fillBase, strokeBase, gradientFrom, gradientTo, opacity);
                    DrawGlowPassDirect(canvas, sample, request, snapshot, compositing, opacity, layerBounds);
                }
                else
                {
                    if (NeedsShadowPass(compositing, opacity) || NeedsGlowPass(compositing, opacity))
                    {
                        effectSourcePicture = RecordLayerPicture(
                            sample,
                            request,
                            snapshot,
                            CreateEffectSourceColor(),
                            CreateEffectSourceColor(),
                            CreateEffectSourceColor(),
                            CreateEffectSourceColor(),
                            1d,
                            renderAsMask: false,
                            includeAmbientGlow: false,
                            layerBounds);
                    }

                    if (NeedsBlurPass(compositing))
                    {
                        blurSourcePicture = RecordLayerPicture(
                            sample,
                            request,
                            snapshot,
                            fillBase,
                            strokeBase,
                            gradientFrom,
                            gradientTo,
                            Math.Clamp(opacity * 0.24d, 0d, 0.24d),
                            renderAsMask: false,
                            includeAmbientGlow: false,
                            layerBounds);
                    }

                    DrawShadowPass(canvas, compositing, opacity, layerBounds, effectSourcePicture);
                    DrawBlurPass(canvas, compositing, layerBounds, blurSourcePicture);
                    DrawGlowPass(canvas, snapshot, compositing, opacity, layerBounds, effectSourcePicture);
                }
            }

            if (!ShouldHideBaseObject(compositing) || renderAsMask)
            {
                DrawLayerCore(
                    canvas,
                    sample,
                    request,
                    snapshot,
                    fillBase,
                    strokeBase,
                    gradientFrom,
                    gradientTo,
                    opacity,
                    renderAsMask,
                    includeAmbientGlow: true);
            }
        }
        finally
        {
            effectSourcePicture?.Dispose();
            blurSourcePicture?.Dispose();
            if (layerSaveCount >= 0)
            {
                canvas.RestoreToCount(layerSaveCount);
            }

            layerPaint?.Dispose();
        }
    }

    private static bool CanUseDirectEffectPass(RenderableLayerSample sample)
    {
        return sample.Kind is LayerKind.Rectangle or LayerKind.Ellipse or LayerKind.Path or LayerKind.Text;
    }

    private void DrawLayerCore(
        SKCanvas canvas,
        RenderableLayerSample sample,
        SceneRenderRequest request,
        LayerSnapshot snapshot,
        SKColor fillBase,
        SKColor strokeBase,
        SKColor gradientFrom,
        SKColor gradientTo,
        double opacity,
        bool renderAsMask,
        bool includeAmbientGlow)
    {
        var rect = SKRect.Create((float)snapshot.X, (float)snapshot.Y, (float)snapshot.Width, (float)snapshot.Height);
        var isOutlineMode = snapshot.ShowAsOutline && !renderAsMask;
        var outlineBase = renderAsMask
            ? fillBase
            : SkiaColorHelpers.ApplyCompositeColorEffects(SkiaColorHelpers.Parse(snapshot.OutlineColor, snapshot.Stroke), snapshot.Compositing);
        if (isOutlineMode)
        {
            fillBase = outlineBase;
            strokeBase = outlineBase;
            gradientFrom = outlineBase;
            gradientTo = outlineBase;
        }

        var fillColor = SkiaColorHelpers.ApplyOpacity(fillBase, opacity);
        var strokeColor = SkiaColorHelpers.ApplyOpacity(strokeBase, opacity * (220d / 255d));

        using var fillPaint = renderAsMask
            ? new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = fillColor }
            : isOutlineMode && sample.Kind is not LayerKind.Text
                ? null
                : SkiaLayerStyleRenderHelper.CreateFillPaint(snapshot, fillColor, gradientFrom, gradientTo, opacity, rect);
        using var strokePaint = SkiaLayerStyleRenderHelper.CreateStrokePaint(snapshot, strokeColor, Math.Max(1d, snapshot.StrokeThickness));

        var sourceLibraryItem = ResolveLibraryItem(request, sample.SourceLibraryItemId);
        var saveCount = canvas.Save();
        SkiaLayerTransformHelper.ApplyTransform(canvas, rect, snapshot, sourceLibraryItem);
        try
        {
            switch (sample.Kind)
            {
                case LayerKind.Rectangle:
                    DrawRectangle(canvas, rect, snapshot, fillPaint, strokePaint, fillColor, renderAsMask, isOutlineMode, includeAmbientGlow);
                    return;
                case LayerKind.Ellipse:
                    DrawEllipse(canvas, rect, snapshot, fillPaint, strokePaint, fillColor, renderAsMask, isOutlineMode, includeAmbientGlow);
                    return;
                case LayerKind.Path:
                    DrawPath(canvas, snapshot, fillPaint, strokePaint, fillColor, renderAsMask, request.Time, includeAmbientGlow);
                    return;
                case LayerKind.AvaloniaControl:
                    DrawAvaloniaControl(canvas, rect, snapshot, fillPaint, strokePaint, fillColor, strokeColor, opacity);
                    return;
                case LayerKind.Video:
                    DrawSyntheticVideoFrame(canvas, rect, snapshot, fillPaint, strokePaint, fillColor, strokeColor, opacity, renderAsMask);
                    return;
                case LayerKind.Text:
                    DrawTextLayer(canvas, rect, snapshot, fillPaint, strokePaint, fillColor, opacity, isOutlineMode);
                    return;
                case LayerKind.Audio:
                default:
                    return;
            }
        }
        finally
        {
            canvas.RestoreToCount(saveCount);
        }
    }

    private static void DrawRectangle(SKCanvas canvas, SKRect rect, LayerSnapshot snapshot, SKPaint? fillPaint, SKPaint? strokePaint, SKColor fillColor, bool renderAsMask, bool isOutlineMode, bool includeAmbientGlow)
    {
        if (includeAmbientGlow && !renderAsMask && !isOutlineMode)
        {
            using var ambientPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = fillColor.WithAlpha(32)
            };
            var ambientRect = rect;
            ambientRect.Inflate(10f, 10f);
            using var ambientPath = new SKPath();
            ambientPath.AddRoundRect(SkiaLayerStyleRenderHelper.CreateRoundedRect(ambientRect, snapshot));
            canvas.DrawPath(ambientPath, ambientPaint);
        }

        using var path = new SKPath();
        path.AddRoundRect(SkiaLayerStyleRenderHelper.CreateRoundedRect(rect, snapshot));
        if (fillPaint is not null)
        {
            canvas.DrawPath(path, fillPaint);
        }

        if (strokePaint is not null)
        {
            canvas.DrawPath(path, strokePaint);
        }
    }

    private static void DrawEllipse(SKCanvas canvas, SKRect rect, LayerSnapshot snapshot, SKPaint? fillPaint, SKPaint? strokePaint, SKColor fillColor, bool renderAsMask, bool isOutlineMode, bool includeAmbientGlow)
    {
        if (includeAmbientGlow && !renderAsMask && !isOutlineMode)
        {
            using var ambientPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = fillColor.WithAlpha(32)
            };
            var ambientRect = rect;
            ambientRect.Inflate(10f, 10f);
            canvas.DrawOval(ambientRect, ambientPaint);
        }

        using var ellipsePath = SkiaLayerStyleRenderHelper.CreateEllipsePath(rect, snapshot, includeCenterForFill: snapshot.HasFill && !renderAsMask && !isOutlineMode);
        if (ellipsePath is not null)
        {
            if (fillPaint is not null && snapshot.HasFill && !isOutlineMode)
            {
                canvas.DrawPath(ellipsePath, fillPaint);
            }

            if (strokePaint is not null)
            {
                canvas.DrawPath(ellipsePath, strokePaint);
            }

            return;
        }

        if (fillPaint is not null)
        {
            canvas.DrawOval(rect, fillPaint);
        }

        if (strokePaint is not null)
        {
            canvas.DrawOval(rect, strokePaint);
        }
    }

    private static void DrawPath(SKCanvas canvas, LayerSnapshot snapshot, SKPaint? fillPaint, SKPaint? strokePaint, SKColor fillColor, bool renderAsMask, double time, bool includeAmbientGlow)
    {
        var points = snapshot.PathPoints;
        if (points.Count < 2)
        {
            return;
        }

        using var path = SkiaVectorPathRenderHelper.BuildPath(
            points,
            snapshot.IsClosed,
            point => new SKPoint((float)point.X, (float)point.Y),
            (point, handleKind) => new SKPoint(
                (float)(handleKind == VectorHandleKind.In ? point.InHandleX ?? point.X : point.OutHandleX ?? point.X),
                (float)(handleKind == VectorHandleKind.In ? point.InHandleY ?? point.Y : point.OutHandleY ?? point.Y)));
        if (path is null)
        {
            return;
        }

        if (includeAmbientGlow && !renderAsMask && snapshot.HasStroke)
        {
            using var glowPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = fillColor.WithAlpha(48),
                StrokeCap = strokePaint?.StrokeCap ?? SKStrokeCap.Round,
                StrokeJoin = strokePaint?.StrokeJoin ?? SKStrokeJoin.Round,
                StrokeMiter = strokePaint?.StrokeMiter ?? 3f,
                StrokeWidth = Math.Max(10f, (float)snapshot.StrokeThickness + 10f)
            };
            canvas.DrawPath(path, glowPaint);
        }

        if (fillPaint is not null && snapshot.IsClosed)
        {
            canvas.DrawPath(path, fillPaint);
        }

        if (strokePaint is null)
        {
            return;
        }

        if (snapshot.HasStroke && SkiaVectorPathRenderHelper.HasVariableWidth(points))
        {
            SkiaVectorPathRenderHelper.DrawVariableStroke(
                canvas,
                points,
                snapshot.IsClosed,
                point => new SKPoint((float)point.X, (float)point.Y),
                (point, handleKind) => new SKPoint(
                    (float)(handleKind == VectorHandleKind.In ? point.InHandleX ?? point.X : point.OutHandleX ?? point.X),
                    (float)(handleKind == VectorHandleKind.In ? point.InHandleY ?? point.Y : point.OutHandleY ?? point.Y)),
                strokePaint,
                strokePaint.StrokeWidth);
            return;
        }

        canvas.DrawPath(path, strokePaint);
    }

    private void DrawShadowPass(
        SKCanvas canvas,
        LayerCompositeSettings compositing,
        double opacity,
        SKRect layerBounds,
        SKPicture? effectSourcePicture)
    {
        var shadowOpacity = Math.Clamp(opacity * Math.Max(0d, compositing.ShadowOpacity), 0d, 1d);
        if (shadowOpacity <= 0d || effectSourcePicture is null)
        {
            return;
        }

        var sigma = GetEffectSigma(compositing.ShadowBlur, compositing.ShadowQuality);
        using var filter = SKImageFilter.CreateDropShadowOnly(
            (float)compositing.ShadowOffsetX,
            (float)compositing.ShadowOffsetY,
            sigma,
            sigma,
            SkiaColorHelpers.ApplyOpacity(SkiaColorHelpers.Parse(compositing.ShadowColor, "#09111F"), shadowOpacity));
        DrawFilteredPicture(canvas, effectSourcePicture, layerBounds, filter);
    }

    private void DrawBlurPass(
        SKCanvas canvas,
        LayerCompositeSettings compositing,
        SKRect layerBounds,
        SKPicture? blurSourcePicture)
    {
        if (compositing.BlurRadius <= 0.1d || blurSourcePicture is null)
        {
            return;
        }

        var sigma = GetEffectSigma(compositing.BlurRadius, compositing.BlurQuality);
        using var filter = SKImageFilter.CreateBlur(sigma, sigma);
        DrawFilteredPicture(canvas, blurSourcePicture, layerBounds, filter);
    }

    private void DrawGlowPass(
        SKCanvas canvas,
        LayerSnapshot snapshot,
        LayerCompositeSettings compositing,
        double opacity,
        SKRect layerBounds,
        SKPicture? effectSourcePicture)
    {
        if (compositing.GlowOpacity <= 0d || compositing.GlowSize <= 0.1d || effectSourcePicture is null)
        {
            return;
        }

        var glowOpacity = Math.Clamp(opacity * compositing.GlowOpacity, 0d, 1d);
        var sigma = GetEffectSigma(compositing.GlowSize, compositing.GlowQuality);
        using var filter = SKImageFilter.CreateDropShadowOnly(
            0f,
            0f,
            sigma,
            sigma,
            SkiaColorHelpers.ApplyOpacity(SkiaColorHelpers.Parse(compositing.GlowColor, snapshot.Fill), glowOpacity));
        DrawFilteredPicture(canvas, effectSourcePicture, layerBounds, filter);
    }

    private void DrawShadowPassDirect(
        SKCanvas canvas,
        RenderableLayerSample sample,
        SceneRenderRequest request,
        LayerSnapshot snapshot,
        LayerCompositeSettings compositing,
        double opacity,
        SKRect layerBounds)
    {
        var shadowOpacity = Math.Clamp(opacity * Math.Max(0d, compositing.ShadowOpacity), 0d, 1d);
        if (shadowOpacity <= 0d)
        {
            return;
        }

        var sigma = GetEffectSigma(compositing.ShadowBlur, compositing.ShadowQuality);
        using var filter = SKImageFilter.CreateDropShadowOnly(
            (float)compositing.ShadowOffsetX,
            (float)compositing.ShadowOffsetY,
            sigma,
            sigma,
            SkiaColorHelpers.ApplyOpacity(SkiaColorHelpers.Parse(compositing.ShadowColor, "#09111F"), shadowOpacity));
        var effectSource = CreateEffectSourceColor();
        DrawFilteredLayerDirect(
            canvas,
            sample,
            request,
            snapshot,
            GetDirectShadowBounds(snapshot, compositing),
            filter,
            effectSource,
            effectSource,
            effectSource,
            effectSource,
            1d);
    }

    private void DrawBlurPassDirect(
        SKCanvas canvas,
        RenderableLayerSample sample,
        SceneRenderRequest request,
        LayerSnapshot snapshot,
        LayerCompositeSettings compositing,
        SKRect layerBounds,
        SKColor fillBase,
        SKColor strokeBase,
        SKColor gradientFrom,
        SKColor gradientTo,
        double opacity)
    {
        if (compositing.BlurRadius <= 0.1d)
        {
            return;
        }

        var sigma = GetEffectSigma(compositing.BlurRadius, compositing.BlurQuality);
        using var filter = SKImageFilter.CreateBlur(sigma, sigma);
        DrawFilteredLayerDirect(
            canvas,
            sample,
            request,
            snapshot,
            GetDirectBlurBounds(snapshot, compositing),
            filter,
            fillBase,
            strokeBase,
            gradientFrom,
            gradientTo,
            Math.Clamp(opacity * 0.24d, 0d, 0.24d));
    }

    private void DrawGlowPassDirect(
        SKCanvas canvas,
        RenderableLayerSample sample,
        SceneRenderRequest request,
        LayerSnapshot snapshot,
        LayerCompositeSettings compositing,
        double opacity,
        SKRect layerBounds)
    {
        if (compositing.GlowOpacity <= 0d || compositing.GlowSize <= 0.1d)
        {
            return;
        }

        var glowOpacity = Math.Clamp(opacity * compositing.GlowOpacity, 0d, 1d);
        var sigma = GetEffectSigma(compositing.GlowSize, compositing.GlowQuality);
        using var filter = SKImageFilter.CreateDropShadowOnly(
            0f,
            0f,
            sigma,
            sigma,
            SkiaColorHelpers.ApplyOpacity(SkiaColorHelpers.Parse(compositing.GlowColor, snapshot.Fill), glowOpacity));
        var effectSource = CreateEffectSourceColor();
        DrawFilteredLayerDirect(
            canvas,
            sample,
            request,
            snapshot,
            GetDirectGlowBounds(snapshot, compositing),
            filter,
            effectSource,
            effectSource,
            effectSource,
            effectSource,
            1d);
    }

    private void DrawFilteredLayerDirect(
        SKCanvas canvas,
        RenderableLayerSample sample,
        SceneRenderRequest request,
        LayerSnapshot snapshot,
        SKRect layerBounds,
        SKImageFilter filter,
        SKColor fillBase,
        SKColor strokeBase,
        SKColor gradientFrom,
        SKColor gradientTo,
        double opacity)
    {
        using var effectPaint = new SKPaint
        {
            ImageFilter = filter
        };
        var saveCount = canvas.SaveLayer(layerBounds, effectPaint);
        try
        {
            DrawLayerCore(
                canvas,
                sample,
                request,
                snapshot,
                fillBase,
                strokeBase,
                gradientFrom,
                gradientTo,
                opacity,
                renderAsMask: false,
                includeAmbientGlow: false);
        }
        finally
        {
            canvas.RestoreToCount(saveCount);
        }
    }

    private static void DrawFilteredPicture(
        SKCanvas canvas,
        SKPicture picture,
        SKRect layerBounds,
        SKImageFilter? filter)
    {
        using var effectPaint = filter is null
            ? null
            : new SKPaint
            {
                ImageFilter = filter
            };
        if (effectPaint is null)
        {
            canvas.DrawPicture(picture);
            return;
        }

        canvas.DrawPicture(picture, effectPaint);
    }

    private static void DrawTextLayer(SKCanvas canvas, SKRect rect, LayerSnapshot snapshot, SKPaint? fillPaint, SKPaint? strokePaint, SKColor fillColor, double opacity, bool isOutlineMode)
    {
        if (!isOutlineMode)
        {
            using var platePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SkiaColorHelpers.ApplyOpacity(new SKColor(16, 32, 56), opacity * (180d / 255d))
            };
            using var path = new SKPath();
            path.AddRoundRect(SkiaLayerStyleRenderHelper.CreateRoundedRect(rect, snapshot));
            canvas.DrawPath(path, platePaint);
            if (strokePaint is not null)
            {
                canvas.DrawPath(path, strokePaint);
            }
        }

        DrawText(canvas, rect, snapshot.Text, fillPaint, snapshot.FontSize, snapshot.TextSettings);
    }

    private static void DrawAvaloniaControl(SKCanvas canvas, SKRect rect, LayerSnapshot snapshot, SKPaint? fillPaint, SKPaint? strokePaint, SKColor fillColor, SKColor strokeColor, double opacity)
    {
        var settings = snapshot.AvaloniaControl;
        var textBrushColor = SkiaColorHelpers.ApplyOpacity(new SKColor(245, 247, 250), opacity);
        var mutedBrushColor = SkiaColorHelpers.ApplyOpacity(SkiaColorHelpers.Blend(strokeColor, new SKColor(245, 247, 250), 0.35d), opacity * (210d / 255d));

        switch (settings.Kind)
        {
            case AvaloniaControlKind.Button:
                DrawRoundedRect(canvas, rect, snapshot, fillPaint, strokePaint);
                DrawCenteredText(canvas, rect, string.IsNullOrWhiteSpace(settings.Content) ? "Button" : settings.Content, textBrushColor, 15d);
                return;
            case AvaloniaControlKind.TextBlock:
                DrawText(canvas, rect, string.IsNullOrWhiteSpace(settings.Content) ? snapshot.Text : settings.Content, CreateSolidTextPaint(textBrushColor), Math.Max(16d, snapshot.FontSize));
                return;
            case AvaloniaControlKind.TextBox:
                DrawRoundedRect(canvas, rect, snapshot, fillPaint, strokePaint);
                DrawText(canvas, rect, string.IsNullOrWhiteSpace(settings.Content) ? "Type here" : settings.Content, CreateSolidTextPaint(textBrushColor), 14d);
                return;
            case AvaloniaControlKind.CheckBox:
            {
                var boxSize = Math.Min(24f, rect.Height - 10f);
                var boxRect = new SKRect(rect.Left + 8f, rect.MidY - (boxSize / 2f), rect.Left + 8f + boxSize, rect.MidY + (boxSize / 2f));
                using var boxFill = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    Color = fillColor.WithAlpha(48)
                };
                using var boxStroke = strokePaint is null ? null : ClonePaint(strokePaint);
                var boxSnapshot = snapshot with
                {
                    CornerRadiusTopLeft = 6d,
                    CornerRadiusTopRight = 6d,
                    CornerRadiusBottomRight = 6d,
                    CornerRadiusBottomLeft = 6d
                };
                DrawRoundedRect(canvas, boxRect, boxSnapshot, boxFill, boxStroke);
                if (settings.IsChecked)
                {
                    using var checkPaint = new SKPaint
                    {
                        IsAntialias = true,
                        Style = SKPaintStyle.Stroke,
                        Color = textBrushColor,
                        StrokeWidth = 2.2f,
                        StrokeCap = SKStrokeCap.Round
                    };
                    canvas.DrawLine(boxRect.Left + 5f, boxRect.MidY, boxRect.Left + 10f, boxRect.Bottom - 6f, checkPaint);
                    canvas.DrawLine(boxRect.Left + 10f, boxRect.Bottom - 6f, boxRect.Right - 5f, boxRect.Top + 6f, checkPaint);
                }

                DrawText(canvas, new SKRect(boxRect.Right + 10f, rect.Top, rect.Right, rect.Bottom), string.IsNullOrWhiteSpace(settings.Content) ? "CheckBox" : settings.Content, CreateSolidTextPaint(textBrushColor), 14d);
                return;
            }
            case AvaloniaControlKind.ToggleButton:
            {
                var trackRect = new SKRect(rect.Left + 10f, rect.MidY - 13f, rect.Left + 62f, rect.MidY + 13f);
                var knobRect = settings.IsChecked
                    ? new SKRect(trackRect.Right - 22f, trackRect.Top + 2f, trackRect.Right - 2f, trackRect.Bottom - 2f)
                    : new SKRect(trackRect.Left + 2f, trackRect.Top + 2f, trackRect.Left + 22f, trackRect.Bottom - 2f);
                using var trackFill = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    Color = fillColor.WithAlpha(settings.IsChecked ? (byte)168 : (byte)72)
                };
                var toggleSnapshot = snapshot with
                {
                    CornerRadiusTopLeft = 13d,
                    CornerRadiusTopRight = 13d,
                    CornerRadiusBottomRight = 13d,
                    CornerRadiusBottomLeft = 13d
                };
                DrawRoundedRect(canvas, trackRect, toggleSnapshot, trackFill, strokePaint);
                using var knobPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = textBrushColor };
                canvas.DrawOval(knobRect, knobPaint);
                DrawText(canvas, new SKRect(trackRect.Right + 10f, rect.Top, rect.Right, rect.Bottom), string.IsNullOrWhiteSpace(settings.Content) ? "Toggle" : settings.Content, CreateSolidTextPaint(textBrushColor), 14d);
                return;
            }
            case AvaloniaControlKind.Slider:
            {
                var trackRect = new SKRect(rect.Left + 14f, rect.MidY - 2f, rect.Right - 14f, rect.MidY + 2f);
                var normalized = NormalizeRange(settings.Value, settings.Minimum, settings.Maximum);
                var knobX = trackRect.Left + ((trackRect.Width) * (float)normalized);
                using var trackPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = strokeColor.WithAlpha(96) };
                using var progressPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = fillColor.WithAlpha(210) };
                using var knobPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = fillColor };
                canvas.DrawRoundRect(trackRect, 2f, 2f, trackPaint);
                canvas.DrawRoundRect(new SKRect(trackRect.Left, trackRect.Top, Math.Max(trackRect.Left + 6f, knobX), trackRect.Bottom), 2f, 2f, progressPaint);
                canvas.DrawCircle(knobX, trackRect.MidY, 9f, knobPaint);
                if (strokePaint is not null)
                {
                    canvas.DrawCircle(knobX, trackRect.MidY, 9f, strokePaint);
                }

                return;
            }
            case AvaloniaControlKind.ProgressBar:
            {
                DrawRoundedRect(canvas, rect, snapshot, CreateSolidPaint(strokeColor.WithAlpha(72)), strokePaint);
                var normalized = NormalizeRange(settings.Value, settings.Minimum, settings.Maximum);
                var fillRect = new SKRect(rect.Left + 2f, rect.Top + 2f, rect.Left + Math.Max(8f, (rect.Width - 4f) * (float)normalized), rect.Bottom - 2f);
                var innerSnapshot = snapshot with
                {
                    CornerRadiusTopLeft = Math.Max(4d, snapshot.CornerRadiusTopLeft - 2d),
                    CornerRadiusTopRight = Math.Max(4d, snapshot.CornerRadiusTopRight - 2d),
                    CornerRadiusBottomRight = Math.Max(4d, snapshot.CornerRadiusBottomRight - 2d),
                    CornerRadiusBottomLeft = Math.Max(4d, snapshot.CornerRadiusBottomLeft - 2d)
                };
                DrawRoundedRect(canvas, fillRect, innerSnapshot, fillPaint, null);
                return;
            }
            case AvaloniaControlKind.ComboBox:
            {
                DrawRoundedRect(canvas, rect, snapshot, fillPaint, strokePaint);
                using var chevronPaint = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    Color = textBrushColor,
                    StrokeWidth = 1.8f,
                    StrokeCap = SKStrokeCap.Round
                };
                var arrowCenterX = rect.Right - 20f;
                var arrowCenterY = rect.MidY;
                canvas.DrawLine(arrowCenterX - 6f, arrowCenterY - 3f, arrowCenterX, arrowCenterY + 3f, chevronPaint);
                canvas.DrawLine(arrowCenterX, arrowCenterY + 3f, arrowCenterX + 6f, arrowCenterY - 3f, chevronPaint);
                DrawText(canvas, new SKRect(rect.Left + 14f, rect.Top, rect.Right - 28f, rect.Bottom), string.IsNullOrWhiteSpace(settings.Content) ? "Selected Item" : settings.Content, CreateSolidTextPaint(textBrushColor), 14d);
                return;
            }
            case AvaloniaControlKind.ListBox:
            {
                DrawRoundedRect(canvas, rect, snapshot, fillPaint, strokePaint);
                var items = SplitControlItems(settings.Content, "Item 1", "Item 2", "Item 3");
                var rowHeight = Math.Max(22f, (rect.Height - 16f) / Math.Max(1, items.Count));
                for (var index = 0; index < items.Count; index++)
                {
                    var rowRect = new SKRect(rect.Left + 8f, rect.Top + 8f + (index * rowHeight), rect.Right - 8f, rect.Top + 8f + (index * rowHeight) + rowHeight - 4f);
                    if (index == 0)
                    {
                        DrawRoundedRect(canvas, rowRect, snapshot with
                        {
                            CornerRadiusTopLeft = 8d,
                            CornerRadiusTopRight = 8d,
                            CornerRadiusBottomRight = 8d,
                            CornerRadiusBottomLeft = 8d
                        }, CreateSolidPaint(fillColor.WithAlpha(86)), null);
                    }

                    DrawText(canvas, rowRect, items[index], CreateSolidTextPaint(textBrushColor), 13d);
                }

                return;
            }
            case AvaloniaControlKind.TabControl:
            {
                DrawRoundedRect(canvas, rect, snapshot, fillPaint, strokePaint);
                var tabs = SplitControlItems(settings.Content, "Overview", "Settings", "Export");
                var active = string.IsNullOrWhiteSpace(settings.SecondaryContent) ? tabs[0] : settings.SecondaryContent;
                var tabWidth = Math.Max(54f, (rect.Width - 16f) / Math.Max(1, tabs.Count));
                for (var index = 0; index < tabs.Count; index++)
                {
                    var tabRect = new SKRect(rect.Left + 8f + (index * tabWidth), rect.Top + 8f, rect.Left + 8f + (index * tabWidth) + tabWidth - 4f, rect.Top + 36f);
                    var tabFill = string.Equals(tabs[index], active, StringComparison.OrdinalIgnoreCase)
                        ? fillColor.WithAlpha(96)
                        : strokeColor.WithAlpha(44);
                    DrawRoundedRect(canvas, tabRect, snapshot with
                    {
                        CornerRadiusTopLeft = 9d,
                        CornerRadiusTopRight = 9d,
                        CornerRadiusBottomRight = 9d,
                        CornerRadiusBottomLeft = 9d
                    }, CreateSolidPaint(tabFill), null);
                    DrawCenteredText(canvas, tabRect, tabs[index], textBrushColor, 12d);
                }

                var bodyRect = new SKRect(rect.Left + 8f, rect.Top + 44f, rect.Right - 8f, rect.Bottom - 8f);
                DrawRoundedRect(canvas, bodyRect, snapshot with
                {
                    CornerRadiusTopLeft = Math.Max(8d, snapshot.CornerRadiusTopLeft - 4d),
                    CornerRadiusTopRight = Math.Max(8d, snapshot.CornerRadiusTopRight - 4d),
                    CornerRadiusBottomRight = Math.Max(8d, snapshot.CornerRadiusBottomRight - 4d),
                    CornerRadiusBottomLeft = Math.Max(8d, snapshot.CornerRadiusBottomLeft - 4d)
                }, CreateSolidPaint(strokeColor.WithAlpha(34)), null);
                DrawCenteredText(canvas, bodyRect, $"{active} content", mutedBrushColor, 12d);
                return;
            }
            case AvaloniaControlKind.Grid:
            {
                DrawRoundedRect(canvas, rect, snapshot, fillPaint, strokePaint);
                using var cellPaint = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    Color = strokeColor.WithAlpha(92),
                    StrokeWidth = 1.2f
                };
                var verticalMid = rect.Left + (rect.Width / 2f);
                var horizontalMid = rect.Top + (rect.Height / 2f);
                canvas.DrawLine(verticalMid, rect.Top + 10f, verticalMid, rect.Bottom - 10f, cellPaint);
                canvas.DrawLine(rect.Left + 10f, horizontalMid, rect.Right - 10f, horizontalMid, cellPaint);
                DrawCenteredText(canvas, rect, string.IsNullOrWhiteSpace(settings.Content) ? "2x2 Grid" : settings.Content, mutedBrushColor, 12d);
                return;
            }
            case AvaloniaControlKind.StackPanel:
            {
                DrawRoundedRect(canvas, rect, snapshot, fillPaint, strokePaint);
                var items = SplitControlItems(settings.SecondaryContent, "Item A", "Item B", "Item C");
                var rowHeight = Math.Max(22f, (rect.Height - 18f) / Math.Max(1, items.Count));
                for (var index = 0; index < items.Count; index++)
                {
                    var rowRect = new SKRect(rect.Left + 10f, rect.Top + 8f + (index * rowHeight), rect.Right - 10f, rect.Top + 8f + (index * rowHeight) + rowHeight - 6f);
                    DrawRoundedRect(canvas, rowRect, snapshot with
                    {
                        CornerRadiusTopLeft = 7d,
                        CornerRadiusTopRight = 7d,
                        CornerRadiusBottomRight = 7d,
                        CornerRadiusBottomLeft = 7d
                    }, CreateSolidPaint(fillColor.WithAlpha(index == 0 ? (byte)88 : (byte)52)), null);
                    DrawText(canvas, rowRect, items[index], CreateSolidTextPaint(textBrushColor), 12d);
                }

                return;
            }
            case AvaloniaControlKind.PathIcon:
            {
                DrawRoundedRect(canvas, rect, snapshot, CreateSolidPaint(fillColor.WithAlpha(36)), strokePaint);
                var points = ParseControlPathGeometry(settings.SecondaryContent)
                    .Select(point => new SKPoint(rect.Left + ((float)point.X * rect.Width), rect.Top + ((float)point.Y * rect.Height)))
                    .ToList();
                using var path = BuildSimplePath(points, isClosed: true);
                if (path is not null)
                {
                    using var iconPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = textBrushColor };
                    canvas.DrawPath(path, iconPaint);
                }
                else
                {
                    DrawCenteredText(canvas, rect, string.IsNullOrWhiteSpace(settings.Content) ? "Icon" : settings.Content, textBrushColor, 14d);
                }

                return;
            }
            case AvaloniaControlKind.Image:
                DrawRoundedRect(canvas, rect, snapshot, fillPaint, strokePaint);
                using (var linePaint = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    Color = mutedBrushColor,
                    StrokeWidth = 1.8f,
                    StrokeCap = SKStrokeCap.Round
                })
                {
                    canvas.DrawLine(rect.Left + 18f, rect.Bottom - 18f, rect.MidX - 8f, rect.MidY, linePaint);
                    canvas.DrawLine(rect.MidX - 8f, rect.MidY, rect.Right - 18f, rect.Bottom - 34f, linePaint);
                }

                using (var dotPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = mutedBrushColor })
                {
                    canvas.DrawCircle(rect.Right - 28f, rect.Top + 28f, 9f, dotPaint);
                }

                DrawCenteredText(canvas, rect, string.IsNullOrWhiteSpace(settings.Source) ? "Image" : settings.Source, textBrushColor, 12d);
                return;
            case AvaloniaControlKind.Panel:
            case AvaloniaControlKind.Border:
                DrawRoundedRect(canvas, rect, snapshot, fillPaint, strokePaint);
                if (!string.IsNullOrWhiteSpace(settings.Content))
                {
                    DrawText(canvas, new SKRect(rect.Left + 12f, rect.Top + 8f, rect.Right - 12f, rect.Top + 32f), settings.Content, CreateSolidTextPaint(textBrushColor), 12d);
                }

                return;
            default:
                DrawRoundedRect(canvas, rect, snapshot, fillPaint, strokePaint);
                return;
        }
    }

    private static void DrawSyntheticVideoFrame(SKCanvas canvas, SKRect rect, LayerSnapshot snapshot, SKPaint? fillPaint, SKPaint? strokePaint, SKColor fillColor, SKColor strokeColor, double opacity, bool renderAsMask)
    {
        DrawRoundedRect(canvas, rect, snapshot, fillPaint, strokePaint);
        using var horizonPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            Color = SkiaColorHelpers.ApplyOpacity(strokeColor, opacity * (180d / 255d)),
            StrokeWidth = 2.6f,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };
        canvas.DrawLine(rect.Left + 18f, rect.Bottom - 18f, rect.MidX - 18f, rect.MidY + 6f, horizonPaint);
        canvas.DrawLine(rect.MidX - 18f, rect.MidY + 6f, rect.Right - 18f, rect.Bottom - 34f, horizonPaint);
        using var orbPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = fillColor.WithAlpha(renderAsMask ? (byte)255 : (byte)96)
        };
        canvas.DrawCircle(rect.Right - 46f, rect.Top + 42f, 18f, orbPaint);
        DrawCenteredText(canvas, rect, "MEDIA", renderAsMask ? SKColors.White : new SKColor(245, 247, 250), 13d);
    }

    private static void DrawRoundedRect(SKCanvas canvas, SKRect rect, LayerSnapshot snapshot, SKPaint? fillPaint, SKPaint? strokePaint)
    {
        using var path = new SKPath();
        path.AddRoundRect(SkiaLayerStyleRenderHelper.CreateRoundedRect(rect, snapshot));
        if (fillPaint is not null)
        {
            canvas.DrawPath(path, fillPaint);
        }

        if (strokePaint is not null)
        {
            canvas.DrawPath(path, strokePaint);
        }
    }

    private static void DrawCenteredText(SKCanvas canvas, SKRect rect, string text, SKColor color, double fontSize)
    {
        using var paint = CreateSolidTextPaint(color, fontSize);
        var metrics = paint.FontMetrics;
        var width = paint.MeasureText(text);
        var x = rect.Left + Math.Max(8f, (rect.Width - width) / 2f);
        var y = rect.Top + Math.Max(6f, (rect.Height - (metrics.Descent - metrics.Ascent)) / 2f) - metrics.Ascent;
        canvas.DrawText(text, x, y, paint);
    }

    private static void DrawText(SKCanvas canvas, SKRect rect, string text, SKPaint? basePaint, double fontSize, LayerTextSettings? textSettings = null)
    {
        var settings = textSettings ?? new LayerTextSettings();
        var contentRect = DrawFlashTextFieldChrome(canvas, rect, settings);
        if (string.IsNullOrWhiteSpace(text) || basePaint is null)
        {
            return;
        }

        using var typeface = BuildTypeface(settings);
        using var textPaint = ClonePaint(basePaint);
        textPaint.IsAntialias = true;
        textPaint.TextSize = (float)Math.Max(14d, fontSize);
        textPaint.Typeface = typeface;
        textPaint.Style = SKPaintStyle.Fill;

        var lines = GetRenderedTextLines(text, settings);
        var lineHeight = settings.LineHeight > 0d
            ? (float)settings.LineHeight
            : textPaint.FontSpacing;
        var totalHeight = lines.Length == 0 ? 0f : lines.Length * lineHeight;
        var y = contentRect.Top + Math.Max(16f, (contentRect.Height - totalHeight) / 2f) - textPaint.FontMetrics.Ascent;

        foreach (var line in lines)
        {
            var width = textPaint.MeasureText(line);
            var x = settings.Alignment switch
            {
                LayerTextAlignment.Center => contentRect.Left + Math.Max(8f, (contentRect.Width - width) / 2f),
                LayerTextAlignment.Right => contentRect.Right - width - 16f,
                _ => contentRect.Left + 18f
            };

            if (Math.Abs(settings.LetterSpacing) > 0.001d)
            {
                DrawSpacedText(canvas, line, textPaint, x, y, (float)settings.LetterSpacing);
            }
            else
            {
                canvas.DrawText(line, x, y, textPaint);
            }

            y += lineHeight;
        }
    }

    private static SKRect DrawFlashTextFieldChrome(SKCanvas canvas, SKRect rect, LayerTextSettings settings)
    {
        if (settings.FieldKind == FlashTextFieldKind.Static)
        {
            return rect;
        }

        var borderColor = settings.ShowBorder
            ? new SKColor(142, 234, 255)
            : new SKColor(68, 86, 111);
        var fillColor = settings.FieldKind == FlashTextFieldKind.Input
            ? new SKColor(16, 39, 68)
            : new SKColor(13, 27, 49);
        var chromeRect = rect;
        chromeRect.Inflate(-4f, -4f);
        using var chromeFill = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = fillColor.WithAlpha(settings.ShowBorder ? (byte)68 : (byte)34)
        };
        using var chromeStroke = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.2f,
            Color = borderColor.WithAlpha(settings.ShowBorder ? (byte)192 : (byte)112)
        };
        var chromeSnapshot = new LayerSnapshot(
            0, 0, chromeRect.Width, chromeRect.Height,
            1, 1, 0, 0, 0, 1, 12, 12, 12, 12, 12,
            "#000000", "#000000", string.Empty, 12, 1, false, "#000000", "#000000",
            ShapeDrawingMode.Merge, PrimitiveShapeType.None, true, 0, 360, false, "#000000", [], 5, 0.46d, true,
            new AvaloniaControlSettings(), new LayerCompositeSettings(), new LayerTextSettings())
        {
            HasFill = true,
            HasStroke = true
        };
        DrawRoundedRect(canvas, chromeRect, chromeSnapshot, chromeFill, chromeStroke);

        if (!string.IsNullOrWhiteSpace(settings.VariableName))
        {
            using var labelPaint = CreateSolidTextPaint(new SKColor(182, 215, 255, 176), 10d, mediumWeight: true);
            var y = chromeRect.Bottom - 6f;
            canvas.DrawText(settings.VariableName, chromeRect.Left + 10f, y, labelPaint);
        }

        chromeRect.Inflate(-8f, -8f);
        return chromeRect;
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

    private static void DrawSpacedText(SKCanvas canvas, string text, SKPaint paint, float originX, float baselineY, float letterSpacing)
    {
        var x = originX;
        foreach (var character in text)
        {
            var glyph = character.ToString();
            canvas.DrawText(glyph, x, baselineY, paint);
            x += paint.MeasureText(glyph) + letterSpacing;
        }
    }

    private static SKTypeface BuildTypeface(LayerTextSettings settings)
    {
        if (settings.UseDeviceFonts || string.IsNullOrWhiteSpace(settings.FontFamily))
        {
            return SKTypeface.Default;
        }

        var style = new SKFontStyle(
            settings.IsBold ? (int)SKFontStyleWeight.Bold : (int)SKFontStyleWeight.Normal,
            (int)SKFontStyleWidth.Normal,
            settings.IsItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
        return SKTypeface.FromFamilyName(settings.FontFamily, style) ?? SKTypeface.Default;
    }

    private static SKPaint CreateSolidTextPaint(SKColor color, double fontSize = 14d, bool mediumWeight = false)
    {
        return new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = color,
            TextSize = (float)fontSize,
            Typeface = mediumWeight
                ? SKTypeface.FromFamilyName(null, new SKFontStyle((int)SKFontStyleWeight.Medium, (int)SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)) ?? SKTypeface.Default
                : SKTypeface.Default
        };
    }

    private static SKPaint CreateSolidPaint(SKColor color)
    {
        return new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = color
        };
    }

    private static double NormalizeRange(double value, double minimum, double maximum)
    {
        var range = Math.Max(0.0001d, maximum - minimum);
        return Math.Clamp((value - minimum) / range, 0d, 1d);
    }

    private static List<string> SplitControlItems(string? value, params string[] fallback)
    {
        var items = string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(['|', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        return items.Count > 0 ? items : [.. fallback];
    }

    private static IReadOnlyList<(double X, double Y)> ParseControlPathGeometry(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return
            [
                (0.5d, 0.08d),
                (0.88d, 0.5d),
                (0.5d, 0.92d),
                (0.12d, 0.5d)
            ];
        }

        var values = Regex.Matches(data, @"[-+]?\d*\.?\d+")
            .Select(match => double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : double.NaN)
            .Where(static value => !double.IsNaN(value))
            .ToList();
        if (values.Count < 4)
        {
            return [];
        }

        var points = new List<(double X, double Y)>();
        for (var index = 0; index + 1 < values.Count; index += 2)
        {
            points.Add((values[index], values[index + 1]));
        }

        return points;
    }

    private static SKPath? BuildSimplePath(IReadOnlyList<SKPoint> points, bool isClosed)
    {
        if (points.Count < 2)
        {
            return null;
        }

        var path = new SKPath();
        path.MoveTo(points[0]);
        for (var index = 1; index < points.Count; index++)
        {
            path.LineTo(points[index]);
        }

        if (isClosed)
        {
            path.Close();
        }

        return path;
    }

    private static SKBitmap CreateBitmap(int width, int height)
    {
        return new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul));
    }

    private static SKRect GetLayerBounds(LayerSnapshot snapshot)
    {
        var rect = SKRect.Create((float)snapshot.X, (float)snapshot.Y, (float)snapshot.Width, (float)snapshot.Height);
        var compositing = snapshot.Compositing;
        var directionalSpread = Math.Max(
            Math.Max(Math.Abs(compositing.ShadowOffsetX), Math.Abs(compositing.ShadowOffsetY)),
            Math.Max(
                Math.Max(Math.Abs(compositing.BevelDistance), Math.Abs(compositing.GradientGlowDistance)),
                Math.Abs(compositing.GradientBevelDistance)));
        var expansion = Math.Max(
            Math.Max(snapshot.StrokeThickness * 1.5d, compositing.BlurRadius),
            Math.Max(
                Math.Max(compositing.GlowSize, compositing.ShadowBlur),
                Math.Max(
                    Math.Max(compositing.BevelSize, compositing.GradientGlowSize),
                    compositing.GradientBevelSize))) + directionalSpread + 24d;
        rect.Inflate((float)expansion, (float)expansion);
        return rect;
    }

    private static SKRect GetBaseLayerBounds(LayerSnapshot snapshot)
    {
        var rect = SKRect.Create((float)snapshot.X, (float)snapshot.Y, (float)snapshot.Width, (float)snapshot.Height);
        var strokeExpansion = (float)Math.Max(2d, snapshot.StrokeThickness + 2d);
        rect.Inflate(strokeExpansion, strokeExpansion);
        return rect;
    }

    private static SKRect GetDirectBlurBounds(LayerSnapshot snapshot, LayerCompositeSettings compositing)
    {
        var rect = GetBaseLayerBounds(snapshot);
        var expansion = (float)Math.Max(6d, compositing.BlurRadius + 6d);
        rect.Inflate(expansion, expansion);
        return rect;
    }

    private static SKRect GetDirectGlowBounds(LayerSnapshot snapshot, LayerCompositeSettings compositing)
    {
        var rect = GetBaseLayerBounds(snapshot);
        var expansion = (float)Math.Max(8d, compositing.GlowSize + 8d);
        rect.Inflate(expansion, expansion);
        return rect;
    }

    private static SKRect GetDirectShadowBounds(LayerSnapshot snapshot, LayerCompositeSettings compositing)
    {
        var rect = GetBaseLayerBounds(snapshot);
        var expansion = (float)Math.Max(8d, compositing.ShadowBlur + 8d);
        rect.Inflate(expansion, expansion);
        rect.Offset((float)(compositing.ShadowOffsetX * 0.5d), (float)(compositing.ShadowOffsetY * 0.5d));
        rect.Inflate((float)Math.Abs(compositing.ShadowOffsetX), (float)Math.Abs(compositing.ShadowOffsetY));
        return rect;
    }

    private static SKColor CreateEffectSourceColor()
    {
        return new SKColor(255, 255, 255, 255);
    }

    private SKPicture RecordLayerPicture(
        RenderableLayerSample sample,
        SceneRenderRequest request,
        LayerSnapshot snapshot,
        SKColor fillBase,
        SKColor strokeBase,
        SKColor gradientFrom,
        SKColor gradientTo,
        double opacity,
        bool renderAsMask,
        bool includeAmbientGlow,
        SKRect layerBounds)
    {
        using var recorder = new SKPictureRecorder();
        var recordingCanvas = recorder.BeginRecording(layerBounds);
        DrawLayerCore(
            recordingCanvas,
            sample,
            request,
            snapshot,
            fillBase,
            strokeBase,
            gradientFrom,
            gradientTo,
            opacity,
            renderAsMask,
            includeAmbientGlow);
        return recorder.EndRecording();
    }

    private static bool NeedsShadowPass(LayerCompositeSettings compositing, double opacity)
    {
        return Math.Clamp(opacity * Math.Max(0d, compositing.ShadowOpacity), 0d, 1d) > 0d;
    }

    private static bool NeedsBlurPass(LayerCompositeSettings compositing)
    {
        return compositing.BlurRadius > 0.1d;
    }

    private static bool NeedsGlowPass(LayerCompositeSettings compositing, double opacity)
    {
        return compositing.GlowSize > 0.1d &&
               Math.Clamp(opacity * Math.Max(0d, compositing.GlowOpacity), 0d, 1d) > 0d;
    }

    private static float GetEffectSigma(double radius, LayerFilterQuality quality)
    {
        var normalized = Math.Clamp(radius, 0d, 32d);
        if (normalized <= 0.1d)
        {
            return 0.01f;
        }

        var qualityScale = quality switch
        {
            LayerFilterQuality.High => 0.72d,
            LayerFilterQuality.Medium => 0.58d,
            _ => 0.46d
        };
        return (float)Math.Max(0.01d, normalized * qualityScale);
    }

    private static bool ShouldHideBaseObject(LayerCompositeSettings compositing)
    {
        return (compositing.GlowKnockout && compositing.GlowOpacity > 0d && compositing.GlowSize > 0.1d) ||
               (compositing.ShadowHideObject && (compositing.ShadowOpacity > 0d || Math.Abs(compositing.ShadowOffsetX) > 0.01d || Math.Abs(compositing.ShadowOffsetY) > 0.01d)) ||
               (compositing.ShadowKnockout && compositing.ShadowOpacity > 0d) ||
               (compositing.BevelKnockout && compositing.BevelStrength > 0.001d && compositing.BevelSize > 0.1d) ||
               (compositing.GradientGlowKnockout && compositing.GradientGlowStrength > 0.001d && compositing.GradientGlowSize > 0.1d) ||
               (compositing.GradientBevelKnockout && compositing.GradientBevelStrength > 0.001d && compositing.GradientBevelSize > 0.1d);
    }

    private static bool SupportsNativeBlendMode(LayerBlendMode blendMode)
    {
        return blendMode is LayerBlendMode.Normal or
            LayerBlendMode.Layer or
            LayerBlendMode.Add or
            LayerBlendMode.Screen or
            LayerBlendMode.Multiply or
            LayerBlendMode.Overlay or
            LayerBlendMode.Darken or
            LayerBlendMode.Lighten or
            LayerBlendMode.Difference or
            LayerBlendMode.HardLight;
    }

    private static SKBlendMode? TryGetNativeBlendMode(LayerBlendMode blendMode)
    {
        return blendMode switch
        {
            LayerBlendMode.Normal or LayerBlendMode.Layer => null,
            LayerBlendMode.Add => SKBlendMode.Plus,
            LayerBlendMode.Screen => SKBlendMode.Screen,
            LayerBlendMode.Multiply => SKBlendMode.Multiply,
            LayerBlendMode.Overlay => SKBlendMode.Overlay,
            LayerBlendMode.Darken => SKBlendMode.Darken,
            LayerBlendMode.Lighten => SKBlendMode.Lighten,
            LayerBlendMode.Difference => SKBlendMode.Difference,
            LayerBlendMode.HardLight => SKBlendMode.HardLight,
            _ => null
        };
    }

    private static SKPaint? CreateLayerBlendPaint(SKBlendMode? nativeBlendMode)
    {
        return nativeBlendMode is not SKBlendMode blendMode
            ? null
            : new SKPaint
            {
                BlendMode = blendMode
            };
    }

    private static PooledPixelBuffer CopyPixels(SKBitmap bitmap)
    {
        var pixels = RentPixelBuffer(bitmap.ByteCount);
        Marshal.Copy(bitmap.GetPixels(), pixels.Buffer, 0, pixels.Length);
        return pixels;
    }

    private static byte[] CopyPixelsOwned(SKBitmap bitmap)
    {
        var pixels = new byte[bitmap.ByteCount];
        Marshal.Copy(bitmap.GetPixels(), pixels, 0, pixels.Length);
        return pixels;
    }

    private static PooledPixelBuffer RentPixelBuffer(int length)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(length);
        Array.Clear(buffer, 0, length);
        return new PooledPixelBuffer(buffer, length, true);
    }

    private static byte[] CreateSolidPixelBuffer(int width, int height, SKColor color)
    {
        var pixels = new byte[width * height * 4];
        for (var index = 0; index < pixels.Length; index += 4)
        {
            pixels[index] = color.Blue;
            pixels[index + 1] = color.Green;
            pixels[index + 2] = color.Red;
            pixels[index + 3] = color.Alpha;
        }

        return pixels;
    }

    private static void ReturnPixelBuffer(PooledPixelBuffer pixels)
    {
        if (!pixels.IsPooled)
        {
            return;
        }

        ArrayPool<byte>.Shared.Return(pixels.Buffer, clearArray: false);
    }

    private static LibraryItem? ResolveLibraryItem(SceneRenderRequest request, Guid? sourceLibraryItemId)
    {
        return sourceLibraryItemId is Guid libraryItemId && request.LibraryItems.TryGetValue(libraryItemId, out var libraryItem)
            ? libraryItem
            : null;
    }

    private static SKPaint ClonePaint(SKPaint source)
    {
        return new SKPaint
        {
            IsAntialias = source.IsAntialias,
            Style = source.Style,
            Color = source.Color,
            StrokeWidth = source.StrokeWidth,
            StrokeCap = source.StrokeCap,
            StrokeJoin = source.StrokeJoin,
            StrokeMiter = source.StrokeMiter,
            Typeface = source.Typeface,
            TextSize = source.TextSize,
            Shader = source.Shader,
            BlendMode = source.BlendMode
        };
    }

    private sealed class TransparentLayerRenderFallback : ILayerRenderFallback
    {
        public static TransparentLayerRenderFallback Instance { get; } = new();

        public RenderedFrame RenderLayer(SceneRenderRequest request, RenderableLayerSample sample, bool renderAsMask)
        {
            var width = Math.Max(1, (int)Math.Ceiling(request.CanvasWidth));
            var height = Math.Max(1, (int)Math.Ceiling(request.CanvasHeight));
            return new RenderedFrame(width, height, width * 4, new byte[width * height * 4]);
        }
    }

    private readonly record struct PooledPixelBuffer(byte[] Buffer, int Length, bool IsPooled);
}
