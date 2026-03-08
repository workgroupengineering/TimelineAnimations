using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class VectorPathService
{
    private const double MinimumDimension = 24d;
    private const double Epsilon = 0.0001d;

    public static (LayerDefaults Defaults, List<VectorPointModel> PathPoints) CreateLayerGeometry(IEnumerable<VectorPointModel> documentPoints)
    {
        var points = ClonePoints(documentPoints);
        if (points.Count == 0)
        {
            return (new LayerDefaults { Width = MinimumDimension, Height = MinimumDimension, Opacity = 1 }, []);
        }

        var minX = points.Min(point => point.X);
        var minY = points.Min(point => point.Y);
        var maxX = points.Max(point => point.X);
        var maxY = points.Max(point => point.Y);

        var width = Math.Max(MinimumDimension, maxX - minX);
        var height = Math.Max(MinimumDimension, maxY - minY);
        var centerX = (minX + maxX) / 2d;
        var centerY = (minY + maxY) / 2d;
        var x = centerX - (width / 2d);
        var y = centerY - (height / 2d);

        return (
            new LayerDefaults
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Rotation = 0,
                Opacity = 1
            },
            NormalizePoints(points, x, y, width, height));
    }

    public static List<VectorPointModel> GetAbsolutePoints(LayerSnapshot snapshot)
    {
        if (snapshot.PathPoints.Count == 0)
        {
            return [];
        }

        var width = Math.Max(1d, snapshot.Width);
        var height = Math.Max(1d, snapshot.Height);

        return
        [
            .. snapshot.PathPoints.Select(point => new VectorPointModel
            {
                X = snapshot.X + (point.X * width),
                Y = snapshot.Y + (point.Y * height),
                InHandleX = point.InHandleX.HasValue ? snapshot.X + (point.InHandleX.Value * width) : null,
                InHandleY = point.InHandleY.HasValue ? snapshot.Y + (point.InHandleY.Value * height) : null,
                OutHandleX = point.OutHandleX.HasValue ? snapshot.X + (point.OutHandleX.Value * width) : null,
                OutHandleY = point.OutHandleY.HasValue ? snapshot.Y + (point.OutHandleY.Value * height) : null,
                HandleMode = point.HandleMode,
                StrokeWidthScale = point.StrokeWidthScale
            })
        ];
    }

    public static VectorPointModel GetAbsolutePoint(LayerSnapshot snapshot, VectorPointModel point)
    {
        var width = Math.Max(1d, snapshot.Width);
        var height = Math.Max(1d, snapshot.Height);

        return new VectorPointModel
        {
            X = snapshot.X + (point.X * width),
            Y = snapshot.Y + (point.Y * height),
            InHandleX = point.InHandleX.HasValue ? snapshot.X + (point.InHandleX.Value * width) : null,
            InHandleY = point.InHandleY.HasValue ? snapshot.Y + (point.InHandleY.Value * height) : null,
            OutHandleX = point.OutHandleX.HasValue ? snapshot.X + (point.OutHandleX.Value * width) : null,
            OutHandleY = point.OutHandleY.HasValue ? snapshot.Y + (point.OutHandleY.Value * height) : null,
            HandleMode = point.HandleMode,
            StrokeWidthScale = point.StrokeWidthScale
        };
    }

    public static VectorPointModel GetAbsoluteHandle(LayerSnapshot snapshot, VectorPointModel point, VectorHandleKind handleKind)
    {
        var absolutePoint = GetAbsolutePoint(snapshot, point);
        var handleX = handleKind == VectorHandleKind.In
            ? absolutePoint.InHandleX ?? absolutePoint.X
            : absolutePoint.OutHandleX ?? absolutePoint.X;
        var handleY = handleKind == VectorHandleKind.In
            ? absolutePoint.InHandleY ?? absolutePoint.Y
            : absolutePoint.OutHandleY ?? absolutePoint.Y;

        return new VectorPointModel
        {
            X = handleX,
            Y = handleY
        };
    }

    public static bool TryMovePoint(TimelineLayer layer, LayerSnapshot snapshot, int pointIndex, VectorPointModel documentPoint)
    {
        return TryMovePoint(layer.Style.PathPoints, snapshot, pointIndex, documentPoint);
    }

    public static bool TryMovePoint(IList<VectorPointModel> pathPoints, LayerSnapshot snapshot, int pointIndex, VectorPointModel documentPoint)
    {
        if (pointIndex < 0 || pointIndex >= pathPoints.Count)
        {
            return false;
        }

        var width = Math.Max(1d, snapshot.Width);
        var height = Math.Max(1d, snapshot.Height);
        var normalizedX = TimelineMath.Clamp((documentPoint.X - snapshot.X) / width, 0, 1);
        var normalizedY = TimelineMath.Clamp((documentPoint.Y - snapshot.Y) / height, 0, 1);
        var point = pathPoints[pointIndex];
        var deltaX = normalizedX - point.X;
        var deltaY = normalizedY - point.Y;

        if (Math.Abs(deltaX) < Epsilon && Math.Abs(deltaY) < Epsilon)
        {
            return false;
        }

        point.X = normalizedX;
        point.Y = normalizedY;
        if (point.InHandleX.HasValue)
        {
            point.InHandleX = TimelineMath.Clamp(point.InHandleX.Value + deltaX, 0d, 1d);
        }

        if (point.InHandleY.HasValue)
        {
            point.InHandleY = TimelineMath.Clamp(point.InHandleY.Value + deltaY, 0d, 1d);
        }

        if (point.OutHandleX.HasValue)
        {
            point.OutHandleX = TimelineMath.Clamp(point.OutHandleX.Value + deltaX, 0d, 1d);
        }

        if (point.OutHandleY.HasValue)
        {
            point.OutHandleY = TimelineMath.Clamp(point.OutHandleY.Value + deltaY, 0d, 1d);
        }

        return true;
    }

    public static bool TryMoveHandle(IList<VectorPointModel> pathPoints, LayerSnapshot snapshot, int pointIndex, VectorHandleKind handleKind, VectorPointModel documentPoint)
    {
        if (pointIndex < 0 || pointIndex >= pathPoints.Count)
        {
            return false;
        }

        var width = Math.Max(1d, snapshot.Width);
        var height = Math.Max(1d, snapshot.Height);
        var normalizedX = TimelineMath.Clamp((documentPoint.X - snapshot.X) / width, 0, 1);
        var normalizedY = TimelineMath.Clamp((documentPoint.Y - snapshot.Y) / height, 0, 1);
        var point = pathPoints[pointIndex];
        var currentX = handleKind == VectorHandleKind.In ? point.InHandleX ?? point.X : point.OutHandleX ?? point.X;
        var currentY = handleKind == VectorHandleKind.In ? point.InHandleY ?? point.Y : point.OutHandleY ?? point.Y;

        if (Math.Abs(currentX - normalizedX) < Epsilon && Math.Abs(currentY - normalizedY) < Epsilon)
        {
            return false;
        }

        if (handleKind == VectorHandleKind.In)
        {
            point.InHandleX = normalizedX;
            point.InHandleY = normalizedY;
        }
        else
        {
            point.OutHandleX = normalizedX;
            point.OutHandleY = normalizedY;
        }

        if (point.HandleMode == VectorHandleMode.Smooth)
        {
            var mirroredX = TimelineMath.Clamp((point.X * 2d) - normalizedX, 0d, 1d);
            var mirroredY = TimelineMath.Clamp((point.Y * 2d) - normalizedY, 0d, 1d);
            if (handleKind == VectorHandleKind.In)
            {
                point.OutHandleX = mirroredX;
                point.OutHandleY = mirroredY;
            }
            else
            {
                point.InHandleX = mirroredX;
                point.InHandleY = mirroredY;
            }
        }

        return true;
    }

    public static List<VectorPointModel> NormalizePointsToSnapshot(IEnumerable<VectorPointModel> points, LayerSnapshot snapshot)
    {
        return NormalizePoints(points, snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height);
    }

    public static List<VectorPointModel> SimplifyStroke(IEnumerable<VectorPointModel> points, double minimumDistance = 6d)
    {
        var source = ClonePoints(points);
        if (source.Count <= 2)
        {
            return source;
        }

        var simplified = new List<VectorPointModel> { source[0] };
        foreach (var point in source.Skip(1))
        {
            var previous = simplified[^1];
            var deltaX = point.X - previous.X;
            var deltaY = point.Y - previous.Y;
            if (Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY)) >= minimumDistance)
            {
                simplified.Add(point);
            }
        }

        if (!ReferenceEquals(simplified[^1], source[^1]))
        {
            simplified.Add(source[^1]);
        }

        return simplified;
    }

    public static List<VectorPointModel> ClonePoints(IEnumerable<VectorPointModel> points)
    {
        return
        [
            .. points.Select(point => new VectorPointModel
            {
                X = point.X,
                Y = point.Y,
                InHandleX = point.InHandleX,
                InHandleY = point.InHandleY,
                OutHandleX = point.OutHandleX,
                OutHandleY = point.OutHandleY,
                HandleMode = point.HandleMode,
                StrokeWidthScale = point.StrokeWidthScale
            })
        ];
    }

    private static List<VectorPointModel> NormalizePoints(IEnumerable<VectorPointModel> points, double x, double y, double width, double height)
    {
        var safeWidth = Math.Max(1d, width);
        var safeHeight = Math.Max(1d, height);

        return
        [
            .. points.Select(point => new VectorPointModel
            {
                X = TimelineMath.Clamp((point.X - x) / safeWidth, 0, 1),
                Y = TimelineMath.Clamp((point.Y - y) / safeHeight, 0, 1),
                InHandleX = point.InHandleX.HasValue ? TimelineMath.Clamp((point.InHandleX.Value - x) / safeWidth, 0, 1) : null,
                InHandleY = point.InHandleY.HasValue ? TimelineMath.Clamp((point.InHandleY.Value - y) / safeHeight, 0, 1) : null,
                OutHandleX = point.OutHandleX.HasValue ? TimelineMath.Clamp((point.OutHandleX.Value - x) / safeWidth, 0, 1) : null,
                OutHandleY = point.OutHandleY.HasValue ? TimelineMath.Clamp((point.OutHandleY.Value - y) / safeHeight, 0, 1) : null,
                HandleMode = point.HandleMode,
                StrokeWidthScale = Math.Max(0.2d, point.StrokeWidthScale)
            })
        ];
    }
}
