using SkiaSharp;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Rendering.SkiaSharp.Helpers;

public static class SkiaVectorPathRenderHelper
{
    public static SKPath? BuildPath(
        IReadOnlyList<VectorPointModel> points,
        bool isClosed,
        Func<VectorPointModel, SKPoint> anchorSelector,
        Func<VectorPointModel, VectorHandleKind, SKPoint> handleSelector)
    {
        if (points.Count < 2)
        {
            return null;
        }

        var path = new SKPath();
        path.MoveTo(anchorSelector(points[0]));

        for (var index = 0; index < points.Count - 1; index++)
        {
            AppendSegment(path, points[index], points[index + 1], anchorSelector, handleSelector);
        }

        if (isClosed)
        {
            AppendSegment(path, points[^1], points[0], anchorSelector, handleSelector);
            path.Close();
        }

        return path;
    }

    public static bool HasVariableWidth(IReadOnlyList<VectorPointModel> points)
    {
        return points.Any(static point => Math.Abs(point.StrokeWidthScale - 1d) > 0.0001d);
    }

    public static void DrawVariableStroke(
        SKCanvas canvas,
        IReadOnlyList<VectorPointModel> points,
        bool isClosed,
        Func<VectorPointModel, SKPoint> anchorSelector,
        Func<VectorPointModel, VectorHandleKind, SKPoint> handleSelector,
        SKPaint basePaint,
        float baseThickness)
    {
        if (points.Count < 2)
        {
            return;
        }

        for (var index = 0; index < points.Count - 1; index++)
        {
            DrawVariableStrokeSegment(canvas, points[index], points[index + 1], anchorSelector, handleSelector, basePaint, baseThickness);
        }

        if (isClosed)
        {
            DrawVariableStrokeSegment(canvas, points[^1], points[0], anchorSelector, handleSelector, basePaint, baseThickness);
        }
    }

    private static void AppendSegment(
        SKPath path,
        VectorPointModel start,
        VectorPointModel end,
        Func<VectorPointModel, SKPoint> anchorSelector,
        Func<VectorPointModel, VectorHandleKind, SKPoint> handleSelector)
    {
        var startPoint = anchorSelector(start);
        var endPoint = anchorSelector(end);
        var control1 = handleSelector(start, VectorHandleKind.Out);
        var control2 = handleSelector(end, VectorHandleKind.In);
        var isCurve = !PointsClose(control1, startPoint) || !PointsClose(control2, endPoint);
        if (isCurve)
        {
            path.CubicTo(control1, control2, endPoint);
            return;
        }

        path.LineTo(endPoint);
    }

    private static void DrawVariableStrokeSegment(
        SKCanvas canvas,
        VectorPointModel start,
        VectorPointModel end,
        Func<VectorPointModel, SKPoint> anchorSelector,
        Func<VectorPointModel, VectorHandleKind, SKPoint> handleSelector,
        SKPaint basePaint,
        float baseThickness)
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

        using var paint = ClonePaint(basePaint);
        for (var sampleIndex = 0; sampleIndex < samples.Count - 1; sampleIndex++)
        {
            var progress = (double)sampleIndex / Math.Max(1, samples.Count - 1);
            var nextProgress = (double)(sampleIndex + 1) / Math.Max(1, samples.Count - 1);
            paint.StrokeWidth = Math.Max(
                0.8f,
                (float)((baseThickness * Lerp(start.StrokeWidthScale, end.StrokeWidthScale, progress) +
                         baseThickness * Lerp(start.StrokeWidthScale, end.StrokeWidthScale, nextProgress)) / 2d));
            canvas.DrawLine(samples[sampleIndex], samples[sampleIndex + 1], paint);
        }

        canvas.DrawCircle(startPoint, Math.Max(1.2f, (float)(baseThickness * start.StrokeWidthScale) / 2f), paint);
        canvas.DrawCircle(endPoint, Math.Max(1.2f, (float)(baseThickness * end.StrokeWidthScale) / 2f), paint);
    }

    private static List<SKPoint> SampleSegment(SKPoint start, SKPoint control1, SKPoint control2, SKPoint end, int steps)
    {
        var points = new List<SKPoint>(steps + 1);
        for (var step = 0; step <= steps; step++)
        {
            var t = step / (double)steps;
            points.Add(EvaluateBezier(start, control1, control2, end, t));
        }

        return points;
    }

    private static SKPoint EvaluateBezier(SKPoint start, SKPoint control1, SKPoint control2, SKPoint end, double t)
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
        return new SKPoint((float)x, (float)y);
    }

    private static double Lerp(double start, double end, double progress)
    {
        return start + ((end - start) * progress);
    }

    private static bool PointsClose(SKPoint left, SKPoint right)
    {
        return Math.Abs(left.X - right.X) < 0.0001f &&
               Math.Abs(left.Y - right.Y) < 0.0001f;
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
}
