using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class VectorPathService
{
    private const double MinimumDimension = 24d;

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
                Y = snapshot.Y + (point.Y * height)
            })
        ];
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

        if (Math.Abs(point.X - normalizedX) < 0.0001d && Math.Abs(point.Y - normalizedY) < 0.0001d)
        {
            return false;
        }

        point.X = normalizedX;
        point.Y = normalizedY;
        return true;
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
                Y = point.Y
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
                Y = TimelineMath.Clamp((point.Y - y) / safeHeight, 0, 1)
            })
        ];
    }
}
