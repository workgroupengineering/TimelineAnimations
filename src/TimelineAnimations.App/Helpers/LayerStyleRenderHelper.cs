using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.Helpers;

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

    private static ImmutableLinearGradientBrush CreateLinearGradientBrush(LayerSnapshot snapshot, Color gradientFrom, Color gradientTo, double opacity)
    {
        var (startPoint, endPoint) = CreateLinearGradientEndpoints(snapshot.GradientAngle);
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
        var origin = new RelativePoint(
            0.5d + (Math.Cos(radians) * 0.14d),
            0.5d + (Math.Sin(radians) * 0.14d),
            RelativeUnit.Relative);
        return new ImmutableRadialGradientBrush(
            [
                new ImmutableGradientStop(0d, ApplyAlpha(gradientFrom, opacity)),
                new ImmutableGradientStop(1d, ApplyAlpha(gradientTo, opacity))
            ],
            opacity: 1d,
            center: new RelativePoint(0.5d, 0.5d, RelativeUnit.Relative),
            gradientOrigin: origin,
            radius: 0.72d);
    }

    private static (RelativePoint Start, RelativePoint End) CreateLinearGradientEndpoints(double angle)
    {
        var radians = angle * Math.PI / 180d;
        var dx = Math.Cos(radians) * 0.5d;
        var dy = Math.Sin(radians) * 0.5d;
        return (
            new RelativePoint(0.5d - dx, 0.5d - dy, RelativeUnit.Relative),
            new RelativePoint(0.5d + dx, 0.5d + dy, RelativeUnit.Relative));
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
