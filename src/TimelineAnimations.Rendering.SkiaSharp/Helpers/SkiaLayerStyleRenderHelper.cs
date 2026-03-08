using SkiaSharp;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Rendering.SkiaSharp.Helpers;

public static class SkiaLayerStyleRenderHelper
{
    public static SKPaint? CreateFillPaint(
        LayerSnapshot snapshot,
        SKColor solidFill,
        SKColor gradientFrom,
        SKColor gradientTo,
        double opacity,
        SKRect bounds)
    {
        if (!snapshot.HasFill)
        {
            return null;
        }

        var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = SkiaColorHelpers.ApplyOpacity(solidFill, opacity)
        };

        if (!snapshot.UseGradient)
        {
            return paint;
        }

        paint.Shader = snapshot.GradientKind == LayerGradientKind.Radial
            ? CreateRadialGradient(snapshot, gradientFrom, gradientTo, opacity, bounds)
            : CreateLinearGradient(snapshot, gradientFrom, gradientTo, opacity, bounds);
        return paint;
    }

    public static SKPaint? CreateStrokePaint(LayerSnapshot snapshot, SKColor strokeColor, double thickness)
    {
        if (!snapshot.HasStroke)
        {
            return null;
        }

        return new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            Color = strokeColor,
            StrokeWidth = (float)Math.Max(0.01d, thickness),
            StrokeCap = snapshot.StrokeCapStyle switch
            {
                LayerStrokeCapStyle.Butt => SKStrokeCap.Butt,
                LayerStrokeCapStyle.Square => SKStrokeCap.Square,
                _ => SKStrokeCap.Round
            },
            StrokeJoin = snapshot.StrokeJoinStyle switch
            {
                LayerStrokeJoinStyle.Bevel => SKStrokeJoin.Bevel,
                LayerStrokeJoinStyle.Miter => SKStrokeJoin.Miter,
                _ => SKStrokeJoin.Round
            },
            StrokeMiter = (float)Math.Max(1d, snapshot.StrokeMiterLimit)
        };
    }

    public static SKRoundRect CreateRoundedRect(SKRect rect, LayerSnapshot snapshot)
    {
        var radii = new[]
        {
            new SKPoint((float)Math.Max(0d, snapshot.CornerRadiusTopLeft), (float)Math.Max(0d, snapshot.CornerRadiusTopLeft)),
            new SKPoint((float)Math.Max(0d, snapshot.CornerRadiusTopRight), (float)Math.Max(0d, snapshot.CornerRadiusTopRight)),
            new SKPoint((float)Math.Max(0d, snapshot.CornerRadiusBottomRight), (float)Math.Max(0d, snapshot.CornerRadiusBottomRight)),
            new SKPoint((float)Math.Max(0d, snapshot.CornerRadiusBottomLeft), (float)Math.Max(0d, snapshot.CornerRadiusBottomLeft))
        };
        var roundRect = new SKRoundRect();
        roundRect.SetRectRadii(rect, radii);
        return roundRect;
    }

    public static float GetMaxCornerRadius(LayerSnapshot snapshot)
    {
        return (float)Math.Max(
            snapshot.CornerRadius,
            Math.Max(
                snapshot.CornerRadiusTopLeft,
                Math.Max(snapshot.CornerRadiusTopRight, Math.Max(snapshot.CornerRadiusBottomRight, snapshot.CornerRadiusBottomLeft))));
    }

    public static SKPath? CreateEllipsePath(SKRect rect, LayerSnapshot snapshot, bool includeCenterForFill)
    {
        if (Math.Abs(Math.Abs(snapshot.EllipseSweepAngle) - 360d) < 0.1d)
        {
            return null;
        }

        var path = new SKPath();
        var center = new SKPoint(rect.MidX, rect.MidY);
        var startAngle = snapshot.EllipseStartAngle;
        var sweepAngle = snapshot.EllipseSweepAngle;
        var startRadians = startAngle * Math.PI / 180d;
        var startPoint = new SKPoint(
            (float)(center.X + (Math.Cos(startRadians) * rect.Width / 2d)),
            (float)(center.Y + (Math.Sin(startRadians) * rect.Height / 2d)));

        if (includeCenterForFill)
        {
            path.MoveTo(center);
            path.LineTo(startPoint);
        }
        else
        {
            path.MoveTo(startPoint);
        }

        path.ArcTo(rect, (float)startAngle, (float)sweepAngle, false);
        if (includeCenterForFill)
        {
            path.LineTo(center);
            path.Close();
        }

        return path;
    }

    private static SKShader CreateLinearGradient(
        LayerSnapshot snapshot,
        SKColor gradientFrom,
        SKColor gradientTo,
        double opacity,
        SKRect bounds)
    {
        var radians = snapshot.GradientAngle * Math.PI / 180d;
        var centerX = bounds.Left + (bounds.Width * (float)snapshot.GradientCenterX);
        var centerY = bounds.Top + (bounds.Height * (float)snapshot.GradientCenterY);
        var dx = Math.Cos(radians) * bounds.Width * 0.5d * snapshot.GradientScaleX;
        var dy = Math.Sin(radians) * bounds.Height * 0.5d * snapshot.GradientScaleY;
        return SKShader.CreateLinearGradient(
            new SKPoint((float)(centerX - dx), (float)(centerY - dy)),
            new SKPoint((float)(centerX + dx), (float)(centerY + dy)),
            [SkiaColorHelpers.ApplyOpacity(gradientFrom, opacity), SkiaColorHelpers.ApplyOpacity(gradientTo, opacity)],
            SKShaderTileMode.Clamp);
    }

    private static SKShader CreateRadialGradient(
        LayerSnapshot snapshot,
        SKColor gradientFrom,
        SKColor gradientTo,
        double opacity,
        SKRect bounds)
    {
        var center = new SKPoint(
            bounds.Left + (bounds.Width * (float)snapshot.GradientCenterX),
            bounds.Top + (bounds.Height * (float)snapshot.GradientCenterY));
        var radius = (float)Math.Max(8d, Math.Max(bounds.Width, bounds.Height) * 0.5d * Math.Max(snapshot.GradientScaleX, snapshot.GradientScaleY));
        return SKShader.CreateRadialGradient(
            center,
            radius,
            [SkiaColorHelpers.ApplyOpacity(gradientFrom, opacity), SkiaColorHelpers.ApplyOpacity(gradientTo, opacity)],
            SKShaderTileMode.Clamp);
    }
}
