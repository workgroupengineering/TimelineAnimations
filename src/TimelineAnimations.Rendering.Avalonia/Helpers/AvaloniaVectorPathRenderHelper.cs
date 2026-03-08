using Avalonia;
using Avalonia.Media;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Rendering.Avalonia.Helpers;

public static class VectorPathRenderHelper
{
    public static StreamGeometry? BuildGeometry(
        IReadOnlyList<VectorPointModel> points,
        bool isClosed,
        Func<VectorPointModel, Point> anchorSelector,
        Func<VectorPointModel, VectorHandleKind, Point> handleSelector)
    {
        if (points.Count < 2)
        {
            return null;
        }

        var geometry = new StreamGeometry();
        using var geometryContext = geometry.Open();
        geometryContext.BeginFigure(anchorSelector(points[0]), isFilled: isClosed);

        for (var index = 0; index < points.Count - 1; index++)
        {
            AppendSegment(geometryContext, points[index], points[index + 1], anchorSelector, handleSelector);
        }

        if (isClosed)
        {
            AppendSegment(geometryContext, points[^1], points[0], anchorSelector, handleSelector);
        }

        geometryContext.EndFigure(isClosed);
        return geometry;
    }

    public static bool HasVariableWidth(IReadOnlyList<VectorPointModel> points)
    {
        return points.Any(static point => Math.Abs(point.StrokeWidthScale - 1d) > 0.0001d);
    }

    public static void DrawVariableStroke(
        DrawingContext context,
        IReadOnlyList<VectorPointModel> points,
        bool isClosed,
        Func<VectorPointModel, Point> anchorSelector,
        Func<VectorPointModel, VectorHandleKind, Point> handleSelector,
        ISolidColorBrush strokeBrush,
        LayerStrokeCapStyle capStyle,
        double baseThickness)
    {
        if (points.Count < 2)
        {
            return;
        }

        var lineCap = capStyle switch
        {
            LayerStrokeCapStyle.Butt => PenLineCap.Flat,
            LayerStrokeCapStyle.Square => PenLineCap.Square,
            _ => PenLineCap.Round
        };

        for (var index = 0; index < points.Count - 1; index++)
        {
            DrawVariableStrokeSegment(context, points[index], points[index + 1], anchorSelector, handleSelector, strokeBrush, lineCap, baseThickness);
        }

        if (isClosed)
        {
            DrawVariableStrokeSegment(context, points[^1], points[0], anchorSelector, handleSelector, strokeBrush, lineCap, baseThickness);
        }
    }

    private static void AppendSegment(
        StreamGeometryContext geometryContext,
        VectorPointModel start,
        VectorPointModel end,
        Func<VectorPointModel, Point> anchorSelector,
        Func<VectorPointModel, VectorHandleKind, Point> handleSelector)
    {
        var control1 = handleSelector(start, VectorHandleKind.Out);
        var control2 = handleSelector(end, VectorHandleKind.In);
        var target = anchorSelector(end);
        var startPoint = anchorSelector(start);
        var isCurve = !PointsClose(control1, startPoint) || !PointsClose(control2, target);

        if (isCurve)
        {
            geometryContext.CubicBezierTo(control1, control2, target);
            return;
        }

        geometryContext.LineTo(target);
    }

    private static void DrawVariableStrokeSegment(
        DrawingContext context,
        VectorPointModel start,
        VectorPointModel end,
        Func<VectorPointModel, Point> anchorSelector,
        Func<VectorPointModel, VectorHandleKind, Point> handleSelector,
        ISolidColorBrush strokeBrush,
        PenLineCap lineCap,
        double baseThickness)
    {
        var startPoint = anchorSelector(start);
        var startControl = handleSelector(start, VectorHandleKind.Out);
        var endControl = handleSelector(end, VectorHandleKind.In);
        var endPoint = anchorSelector(end);
        var isCurve = !PointsClose(startPoint, startControl) || !PointsClose(endPoint, endControl);
        var samples = SampleSegment(startPoint, startControl, endControl, endPoint, isCurve ? 16 : 4);
        if (samples.Count < 2)
        {
            return;
        }

        for (var sampleIndex = 0; sampleIndex < samples.Count - 1; sampleIndex++)
        {
            var progress = (double)sampleIndex / Math.Max(1, samples.Count - 1);
            var nextProgress = (double)(sampleIndex + 1) / Math.Max(1, samples.Count - 1);
            var segmentThickness = Math.Max(
                0.8d,
                (baseThickness * Lerp(start.StrokeWidthScale, end.StrokeWidthScale, progress) +
                 baseThickness * Lerp(start.StrokeWidthScale, end.StrokeWidthScale, nextProgress)) / 2d);
            var pen = new Pen(strokeBrush, segmentThickness, lineCap: lineCap, lineJoin: PenLineJoin.Round);
            context.DrawLine(pen, samples[sampleIndex], samples[sampleIndex + 1]);
        }

        context.DrawEllipse(strokeBrush, null, startPoint, Math.Max(1.2d, (baseThickness * start.StrokeWidthScale) / 2d), Math.Max(1.2d, (baseThickness * start.StrokeWidthScale) / 2d));
        context.DrawEllipse(strokeBrush, null, endPoint, Math.Max(1.2d, (baseThickness * end.StrokeWidthScale) / 2d), Math.Max(1.2d, (baseThickness * end.StrokeWidthScale) / 2d));
    }

    private static List<Point> SampleSegment(Point start, Point control1, Point control2, Point end, int steps)
    {
        var points = new List<Point>(steps + 1);
        for (var step = 0; step <= steps; step++)
        {
            var t = step / (double)steps;
            points.Add(EvaluateBezier(start, control1, control2, end, t));
        }

        return points;
    }

    private static Point EvaluateBezier(Point start, Point control1, Point control2, Point end, double t)
    {
        var u = 1d - t;
        var tt = t * t;
        var uu = u * u;
        var uuu = uu * u;
        var ttt = tt * t;

        var x = (uuu * start.X) +
                (3d * uu * t * control1.X) +
                (3d * u * tt * control2.X) +
                (ttt * end.X);
        var y = (uuu * start.Y) +
                (3d * uu * t * control1.Y) +
                (3d * u * tt * control2.Y) +
                (ttt * end.Y);
        return new Point(x, y);
    }

    private static double Lerp(double start, double end, double progress)
    {
        return start + ((end - start) * progress);
    }

    private static bool PointsClose(Point left, Point right)
    {
        return Math.Abs(left.X - right.X) < 0.0001d &&
               Math.Abs(left.Y - right.Y) < 0.0001d;
    }
}
