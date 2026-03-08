using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public readonly record struct ToolEraseResult(bool Changed, bool RemoveLayer);

public static class ToolAuthoringService
{
    public static List<VectorPointModel> RefinePencilStroke(IEnumerable<VectorPointModel> points, PencilToolMode mode)
    {
        var source = VectorPathService.ClonePoints(points);
        if (source.Count <= 2)
        {
            return source;
        }

        return mode switch
        {
            PencilToolMode.Straighten => StraightenStroke(source),
            PencilToolMode.Smooth => SmoothStroke(source),
            _ => InkStroke(source)
        };
    }

    public static bool LayerIntersectsLasso(LayerKind kind, LayerSnapshot snapshot, IReadOnlyList<VectorPointModel> polygon, bool contactSensitive)
    {
        if (polygon.Count < 3)
        {
            return false;
        }

        var outline = kind == LayerKind.Path
            ? VectorPathService.GetAbsolutePoints(snapshot)
            : GetBoundsOutline(snapshot);

        if (outline.Count == 0)
        {
            outline = GetBoundsOutline(snapshot);
        }

        if (outline.Count == 0)
        {
            return false;
        }

        if (outline.Any(point => ContainsPoint(polygon, point)))
        {
            return true;
        }

        var centerPoint = new VectorPointModel
        {
            X = snapshot.X + (snapshot.Width / 2d),
            Y = snapshot.Y + (snapshot.Height / 2d)
        };

        if (ContainsPoint(polygon, centerPoint))
        {
            return true;
        }

        if (polygon.Any(point => PointInRect(point, snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height)))
        {
            return true;
        }

        if (!contactSensitive)
        {
            return false;
        }

        return IntersectsOutline(polygon, outline, kind == LayerKind.Path ? snapshot.IsClosed : true);
    }

    public static ToolEraseResult ApplyEraser(
        TimelineLayer layer,
        LayerSnapshot snapshot,
        VectorPointModel documentPoint,
        double radius,
        EraserToolMode mode,
        double time,
        bool createKeyframe,
        double duration)
    {
        if (radius <= 0.25d)
        {
            return default;
        }

        switch (mode)
        {
            case EraserToolMode.Object:
                return new ToolEraseResult(true, true);
            case EraserToolMode.Fill:
                return ApplyFillErase(layer);
            case EraserToolMode.Stroke:
                return ApplyStrokeErase(layer, snapshot, documentPoint, radius, time, createKeyframe, duration);
            default:
                return default;
        }
    }

    private static List<VectorPointModel> InkStroke(IReadOnlyList<VectorPointModel> source)
    {
        return VectorPathService.SimplifyStroke(source, 2.5d);
    }

    private static List<VectorPointModel> SmoothStroke(IReadOnlyList<VectorPointModel> source)
    {
        var smoothed = ApplyMovingAverage(source, 2);
        return VectorPathService.SimplifyStroke(smoothed, 4.5d);
    }

    private static List<VectorPointModel> StraightenStroke(IReadOnlyList<VectorPointModel> source)
    {
        if (source.Count <= 2)
        {
            return VectorPathService.ClonePoints(source);
        }

        var start = source[0];
        var end = source[^1];
        var deltaX = end.X - start.X;
        var deltaY = end.Y - start.Y;

        if (Math.Abs(deltaX) >= Math.Abs(deltaY) * 1.8d)
        {
            end = new VectorPointModel { X = end.X, Y = start.Y };
        }
        else if (Math.Abs(deltaY) >= Math.Abs(deltaX) * 1.8d)
        {
            end = new VectorPointModel { X = start.X, Y = end.Y };
        }
        else
        {
            var diagonal = Math.Min(Math.Abs(deltaX), Math.Abs(deltaY));
            end = new VectorPointModel
            {
                X = start.X + (Math.Sign(deltaX) == 0 ? diagonal : Math.Sign(deltaX) * diagonal),
                Y = start.Y + (Math.Sign(deltaY) == 0 ? diagonal : Math.Sign(deltaY) * diagonal)
            };
        }

        return
        [
            new VectorPointModel { X = start.X, Y = start.Y },
            new VectorPointModel { X = end.X, Y = end.Y }
        ];
    }

    private static List<VectorPointModel> ApplyMovingAverage(IReadOnlyList<VectorPointModel> source, int radius)
    {
        if (source.Count <= 2 || radius <= 0)
        {
            return VectorPathService.ClonePoints(source);
        }

        var smoothed = new List<VectorPointModel>(source.Count)
        {
            new() { X = source[0].X, Y = source[0].Y }
        };

        for (var index = 1; index < source.Count - 1; index++)
        {
            var sumX = 0d;
            var sumY = 0d;
            var count = 0;

            for (var sampleIndex = Math.Max(0, index - radius); sampleIndex <= Math.Min(source.Count - 1, index + radius); sampleIndex++)
            {
                sumX += source[sampleIndex].X;
                sumY += source[sampleIndex].Y;
                count++;
            }

            smoothed.Add(new VectorPointModel
            {
                X = sumX / Math.Max(1, count),
                Y = sumY / Math.Max(1, count)
            });
        }

        smoothed.Add(new VectorPointModel { X = source[^1].X, Y = source[^1].Y });
        return smoothed;
    }

    private static ToolEraseResult ApplyFillErase(TimelineLayer layer)
    {
        if (!layer.Style.HasFill)
        {
            return default;
        }

        layer.Style.HasFill = false;
        return new ToolEraseResult(true, !layer.Style.HasStroke);
    }

    private static ToolEraseResult ApplyStrokeErase(
        TimelineLayer layer,
        LayerSnapshot snapshot,
        VectorPointModel documentPoint,
        double radius,
        double time,
        bool createKeyframe,
        double duration)
    {
        if (!layer.Style.HasStroke)
        {
            return default;
        }

        if (layer.Kind != LayerKind.Path)
        {
            layer.Style.HasStroke = false;
            return new ToolEraseResult(true, !layer.Style.HasFill);
        }

        var targetPoints = createKeyframe
            ? TimelineEditingService.CaptureShapeKeyframe(layer, time, duration).PathPoints
            : layer.Style.PathPoints;
        var absolutePoints = VectorPathService.GetAbsolutePoints(snapshot with { PathPoints = targetPoints });
        if (absolutePoints.Count == 0)
        {
            return default;
        }

        var radiusSquared = radius * radius;
        var keptPoints = new List<VectorPointModel>(targetPoints.Count);

        for (var index = 0; index < absolutePoints.Count; index++)
        {
            var absolutePoint = absolutePoints[index];
            var deltaX = absolutePoint.X - documentPoint.X;
            var deltaY = absolutePoint.Y - documentPoint.Y;
            if ((deltaX * deltaX) + (deltaY * deltaY) > radiusSquared)
            {
                keptPoints.Add(targetPoints[index]);
            }
        }

        if (keptPoints.Count == targetPoints.Count)
        {
            return default;
        }

        if (keptPoints.Count < 2)
        {
            return new ToolEraseResult(true, true);
        }

        targetPoints.Clear();
        targetPoints.AddRange(keptPoints);
        return new ToolEraseResult(true, false);
    }

    private static List<VectorPointModel> GetBoundsOutline(LayerSnapshot snapshot)
    {
        var width = Math.Max(1d, snapshot.Width);
        var height = Math.Max(1d, snapshot.Height);
        return
        [
            new VectorPointModel { X = snapshot.X, Y = snapshot.Y },
            new VectorPointModel { X = snapshot.X + width, Y = snapshot.Y },
            new VectorPointModel { X = snapshot.X + width, Y = snapshot.Y + height },
            new VectorPointModel { X = snapshot.X, Y = snapshot.Y + height }
        ];
    }

    private static bool ContainsPoint(IReadOnlyList<VectorPointModel> polygon, VectorPointModel point)
    {
        var contains = false;
        for (var index = 0; index < polygon.Count; index++)
        {
            var current = polygon[index];
            var previous = polygon[(index + polygon.Count - 1) % polygon.Count];

            var intersects = ((current.Y > point.Y) != (previous.Y > point.Y)) &&
                point.X < ((previous.X - current.X) * (point.Y - current.Y) / Math.Max(0.000001d, previous.Y - current.Y)) + current.X;

            if (intersects)
            {
                contains = !contains;
            }
        }

        return contains;
    }

    private static bool PointInRect(VectorPointModel point, double x, double y, double width, double height)
    {
        return point.X >= x &&
            point.X <= x + width &&
            point.Y >= y &&
            point.Y <= y + height;
    }

    private static bool IntersectsOutline(IReadOnlyList<VectorPointModel> polygon, IReadOnlyList<VectorPointModel> outline, bool closeOutline)
    {
        for (var polygonIndex = 0; polygonIndex < polygon.Count; polygonIndex++)
        {
            var polygonStart = polygon[polygonIndex];
            var polygonEnd = polygon[(polygonIndex + 1) % polygon.Count];

            var maxOutlineIndex = closeOutline ? outline.Count : outline.Count - 1;
            for (var outlineIndex = 0; outlineIndex < maxOutlineIndex; outlineIndex++)
            {
                var outlineStart = outline[outlineIndex];
                var outlineEnd = outline[(outlineIndex + 1) % outline.Count];
                if (SegmentsIntersect(polygonStart, polygonEnd, outlineStart, outlineEnd))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool SegmentsIntersect(VectorPointModel a1, VectorPointModel a2, VectorPointModel b1, VectorPointModel b2)
    {
        var denominator = ((b2.Y - b1.Y) * (a2.X - a1.X)) - ((b2.X - b1.X) * (a2.Y - a1.Y));
        if (Math.Abs(denominator) < 0.000001d)
        {
            return false;
        }

        var numeratorA = ((b2.X - b1.X) * (a1.Y - b1.Y)) - ((b2.Y - b1.Y) * (a1.X - b1.X));
        var numeratorB = ((a2.X - a1.X) * (a1.Y - b1.Y)) - ((a2.Y - a1.Y) * (a1.X - b1.X));
        var ua = numeratorA / denominator;
        var ub = numeratorB / denominator;
        return ua is >= 0d and <= 1d && ub is >= 0d and <= 1d;
    }
}
