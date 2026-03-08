using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Rendering.Avalonia.Helpers;

public static class LayerStyleRenderHelper
{
    public static IBrush CreateFillBrush(LayerSnapshot snapshot, Color solidFill, Color gradientFrom, Color gradientTo, double opacity)
    {
        if (!snapshot.HasFill)
        {
            return new ImmutableSolidColorBrush(Colors.Transparent);
        }

        if (!snapshot.UseGradient)
        {
            return new ImmutableSolidColorBrush(solidFill);
        }

        return snapshot.GradientKind == LayerGradientKind.Radial
            ? CreateRadialGradientBrush(snapshot, gradientFrom, gradientTo, opacity)
            : CreateLinearGradientBrush(snapshot, gradientFrom, gradientTo, opacity);
    }

    public static IPen CreateStrokePen(LayerSnapshot snapshot, ISolidColorBrush strokeBrush, double thickness)
    {
        var brush = snapshot.HasStroke
            ? new ImmutableSolidColorBrush(strokeBrush)
            : new ImmutableSolidColorBrush(Colors.Transparent);
        return new ImmutablePen(
            brush,
            Math.Max(0.01d, thickness),
            lineCap: ToPenLineCap(snapshot.StrokeCapStyle),
            lineJoin: ToPenLineJoin(snapshot.StrokeJoinStyle),
            miterLimit: Math.Max(1d, snapshot.StrokeMiterLimit));
    }

    public static RoundedRect CreateRoundedRect(LayerSnapshot snapshot, Rect rect)
    {
        return new RoundedRect(
            rect,
            new CornerRadius(
                Math.Max(0d, snapshot.CornerRadiusTopLeft),
                Math.Max(0d, snapshot.CornerRadiusTopRight),
                Math.Max(0d, snapshot.CornerRadiusBottomRight),
                Math.Max(0d, snapshot.CornerRadiusBottomLeft)));
    }

    public static double GetMaxCornerRadius(LayerSnapshot snapshot)
    {
        return Math.Max(snapshot.CornerRadius, Math.Max(snapshot.CornerRadiusTopLeft, Math.Max(snapshot.CornerRadiusTopRight, Math.Max(snapshot.CornerRadiusBottomRight, snapshot.CornerRadiusBottomLeft))));
    }

    public static Geometry? CreateEllipseGeometry(Rect rect, LayerSnapshot snapshot, bool includeCenterForFill)
    {
        if (Math.Abs(Math.Abs(snapshot.EllipseSweepAngle) - 360d) < 0.1d)
        {
            return null;
        }

        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        var startAngle = snapshot.EllipseStartAngle * Math.PI / 180d;
        var sweepAngle = snapshot.EllipseSweepAngle * Math.PI / 180d;
        var endAngle = startAngle + sweepAngle;
        var center = rect.Center;
        var radiusX = rect.Width / 2d;
        var radiusY = rect.Height / 2d;
        var startPoint = new Point(center.X + (Math.Cos(startAngle) * radiusX), center.Y + (Math.Sin(startAngle) * radiusY));
        var endPoint = new Point(center.X + (Math.Cos(endAngle) * radiusX), center.Y + (Math.Sin(endAngle) * radiusY));
        if (includeCenterForFill)
        {
            context.BeginFigure(center, isFilled: true);
            context.LineTo(startPoint);
        }
        else
        {
            context.BeginFigure(startPoint, isFilled: false);
        }

        context.ArcTo(
            endPoint,
            new Size(radiusX, radiusY),
            rotationAngle: 0d,
            isLargeArc: Math.Abs(snapshot.EllipseSweepAngle) > 180d,
            sweepDirection: snapshot.EllipseSweepAngle >= 0d ? SweepDirection.Clockwise : SweepDirection.CounterClockwise);
        if (includeCenterForFill)
        {
            context.LineTo(center);
        }

        context.EndFigure(includeCenterForFill);
        return geometry;
    }

    private static ImmutableLinearGradientBrush CreateLinearGradientBrush(LayerSnapshot snapshot, Color gradientFrom, Color gradientTo, double opacity)
    {
        var (startPoint, endPoint) = CreateLinearGradientEndpoints(snapshot.GradientAngle, snapshot.GradientCenterX, snapshot.GradientCenterY, snapshot.GradientScaleX, snapshot.GradientScaleY);
        return new ImmutableLinearGradientBrush(
            [
                new ImmutableGradientStop(0d, ApplyAlpha(gradientFrom, opacity)),
                new ImmutableGradientStop(1d, ApplyAlpha(gradientTo, opacity))
            ],
            opacity: 1d,
            startPoint: startPoint,
            endPoint: endPoint);
    }

    private static ImmutableRadialGradientBrush CreateRadialGradientBrush(LayerSnapshot snapshot, Color gradientFrom, Color gradientTo, double opacity)
    {
        var radians = snapshot.GradientAngle * Math.PI / 180d;
        var center = new RelativePoint(snapshot.GradientCenterX, snapshot.GradientCenterY, RelativeUnit.Relative);
        var radius = Math.Max(0.08d, 0.72d * Math.Max(snapshot.GradientScaleX, snapshot.GradientScaleY));
        var origin = new RelativePoint(
            snapshot.GradientCenterX + (Math.Cos(radians) * 0.14d * snapshot.GradientScaleX),
            snapshot.GradientCenterY + (Math.Sin(radians) * 0.14d * snapshot.GradientScaleY),
            RelativeUnit.Relative);
        return new ImmutableRadialGradientBrush(
            [
                new ImmutableGradientStop(0d, ApplyAlpha(gradientFrom, opacity)),
                new ImmutableGradientStop(1d, ApplyAlpha(gradientTo, opacity))
            ],
            opacity: 1d,
            center: center,
            gradientOrigin: origin,
            radius: radius);
    }

    private static (RelativePoint Start, RelativePoint End) CreateLinearGradientEndpoints(double angle, double centerX, double centerY, double scaleX, double scaleY)
    {
        var radians = angle * Math.PI / 180d;
        var dx = Math.Cos(radians) * 0.5d * scaleX;
        var dy = Math.Sin(radians) * 0.5d * scaleY;
        return (
            new RelativePoint(centerX - dx, centerY - dy, RelativeUnit.Relative),
            new RelativePoint(centerX + dx, centerY + dy, RelativeUnit.Relative));
    }

    private static PenLineCap ToPenLineCap(LayerStrokeCapStyle style)
    {
        return style switch
        {
            LayerStrokeCapStyle.Butt => PenLineCap.Flat,
            LayerStrokeCapStyle.Square => PenLineCap.Square,
            _ => PenLineCap.Round
        };
    }

    private static PenLineJoin ToPenLineJoin(LayerStrokeJoinStyle style)
    {
        return style switch
        {
            LayerStrokeJoinStyle.Bevel => PenLineJoin.Bevel,
            LayerStrokeJoinStyle.Round => PenLineJoin.Round,
            _ => PenLineJoin.Miter
        };
    }

    private static Color ApplyAlpha(Color color, double opacity)
    {
        return Color.FromArgb((byte)Math.Clamp(opacity * 255d, 0d, 255d), color.R, color.G, color.B);
    }
}
