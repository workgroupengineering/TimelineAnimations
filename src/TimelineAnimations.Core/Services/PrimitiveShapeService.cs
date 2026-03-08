using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class PrimitiveShapeService
{
    public static List<VectorPointModel> CreatePolyStarPoints(int sides, double innerRadius, bool isStar)
    {
        var normalizedSides = Math.Clamp(sides, 3, 16);
        var normalizedInnerRadius = TimelineMath.Clamp(innerRadius, 0.08d, 0.95d);
        if (!isStar)
        {
            normalizedInnerRadius = 1d;
        }

        var pointCount = isStar ? normalizedSides * 2 : normalizedSides;
        var angleStep = (Math.PI * 2d) / pointCount;
        var points = new List<VectorPointModel>(pointCount);
        for (var index = 0; index < pointCount; index++)
        {
            var angle = (-Math.PI / 2d) + (angleStep * index);
            var radius = !isStar || index % 2 == 0 ? 0.5d : 0.5d * normalizedInnerRadius;
            var x = 0.5d + (Math.Cos(angle) * radius);
            var y = 0.5d + (Math.Sin(angle) * radius);
            points.Add(new VectorPointModel
            {
                X = TimelineMath.Clamp(x, 0d, 1d),
                Y = TimelineMath.Clamp(y, 0d, 1d)
            });
        }

        return points;
    }

    public static void NormalizeStyle(LayerStyle style)
    {
        style.DrawingMode = Enum.IsDefined(style.DrawingMode) ? style.DrawingMode : ShapeDrawingMode.Merge;
        style.PrimitiveShape = Enum.IsDefined(style.PrimitiveShape) ? style.PrimitiveShape : PrimitiveShapeType.None;
        style.CornerRadiusTopLeft = Math.Max(0d, style.CornerRadiusTopLeft);
        style.CornerRadiusTopRight = Math.Max(0d, style.CornerRadiusTopRight);
        style.CornerRadiusBottomRight = Math.Max(0d, style.CornerRadiusBottomRight);
        style.CornerRadiusBottomLeft = Math.Max(0d, style.CornerRadiusBottomLeft);
        style.EllipseStartAngle = NormalizeAngle(style.EllipseStartAngle);
        style.EllipseSweepAngle = Math.Clamp(style.EllipseSweepAngle, -360d, 360d);
        if (Math.Abs(style.EllipseSweepAngle) < 0.1d)
        {
            style.EllipseSweepAngle = 360d;
        }

        style.PolyStarSides = Math.Clamp(style.PolyStarSides, 3, 16);
        style.PolyStarInnerRadius = TimelineMath.Clamp(style.PolyStarInnerRadius, 0.08d, 0.95d);
    }

    public static void UpdatePolyStarGeometry(TimelineLayer layer)
    {
        if (layer.Kind != LayerKind.Path || layer.Style.PrimitiveShape != PrimitiveShapeType.PolyStar)
        {
            return;
        }

        layer.Style.PathPoints = CreatePolyStarPoints(layer.Style.PolyStarSides, layer.Style.PolyStarInnerRadius, layer.Style.PolyStarIsStar);
        layer.Style.IsClosed = true;
    }

    private static double NormalizeAngle(double angle)
    {
        var normalized = angle % 360d;
        return normalized < 0d ? normalized + 360d : normalized;
    }
}
