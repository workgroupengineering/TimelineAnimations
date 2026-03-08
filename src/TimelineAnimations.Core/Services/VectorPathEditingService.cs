using System.Numerics;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class VectorPathEditingService
{
    private const double SmoothStrength = 0.28d;

    public static bool InsertPointAfter(IList<VectorPointModel> pathPoints, int pointIndex, bool isClosed)
    {
        if (pathPoints.Count < 2 || pointIndex < 0 || pointIndex >= pathPoints.Count)
        {
            return false;
        }

        var nextIndex = pointIndex == pathPoints.Count - 1
            ? (isClosed ? 0 : -1)
            : pointIndex + 1;
        if (nextIndex < 0)
        {
            return false;
        }

        var current = pathPoints[pointIndex];
        var next = pathPoints[nextIndex];
        var split = SplitSegment(current, next, 0.5d);
        pathPoints[pointIndex] = split.Start;
        if (nextIndex == 0)
        {
            pathPoints[^1] = split.Start;
            pathPoints.Add(split.Middle);
            pathPoints[0] = split.End;
            return true;
        }

        pathPoints[nextIndex] = split.End;
        pathPoints.Insert(nextIndex, split.Middle);
        return true;
    }

    public static bool DeletePoint(IList<VectorPointModel> pathPoints, int pointIndex)
    {
        if (pathPoints.Count <= 2 || pointIndex < 0 || pointIndex >= pathPoints.Count)
        {
            return false;
        }

        pathPoints.RemoveAt(pointIndex);
        return true;
    }

    public static bool ReversePath(IList<VectorPointModel> pathPoints)
    {
        if (pathPoints.Count < 2)
        {
            return false;
        }

        var reversed = pathPoints
            .Reverse()
            .Select(static point => new VectorPointModel
            {
                X = point.X,
                Y = point.Y,
                InHandleX = point.OutHandleX,
                InHandleY = point.OutHandleY,
                OutHandleX = point.InHandleX,
                OutHandleY = point.InHandleY,
                HandleMode = point.HandleMode,
                StrokeWidthScale = point.StrokeWidthScale
            })
            .ToList();

        pathPoints.Clear();
        foreach (var point in reversed)
        {
            pathPoints.Add(point);
        }

        return true;
    }

    public static bool SetPointHandleMode(IList<VectorPointModel> pathPoints, int pointIndex, bool isClosed, VectorHandleMode mode)
    {
        if (pointIndex < 0 || pointIndex >= pathPoints.Count)
        {
            return false;
        }

        var point = pathPoints[pointIndex];
        if (point.HandleMode == mode && (mode != VectorHandleMode.Corner || (!point.InHandleX.HasValue && !point.OutHandleX.HasValue)))
        {
            return false;
        }

        point.HandleMode = mode;
        if (mode == VectorHandleMode.Corner)
        {
            point.InHandleX = null;
            point.InHandleY = null;
            point.OutHandleX = null;
            point.OutHandleY = null;
            return true;
        }

        ApplySmoothHandle(pathPoints, pointIndex, isClosed);
        return true;
    }

    public static bool SetPointStrokeWidthScale(IList<VectorPointModel> pathPoints, int pointIndex, double scale)
    {
        if (pointIndex < 0 || pointIndex >= pathPoints.Count)
        {
            return false;
        }

        var normalized = Math.Clamp(scale, 0.2d, 4d);
        if (Math.Abs(pathPoints[pointIndex].StrokeWidthScale - normalized) < 0.0001d)
        {
            return false;
        }

        pathPoints[pointIndex].StrokeWidthScale = normalized;
        return true;
    }

    public static bool SmoothPath(IList<VectorPointModel> pathPoints, bool isClosed)
    {
        if (pathPoints.Count < 2)
        {
            return false;
        }

        for (var index = 0; index < pathPoints.Count; index++)
        {
            pathPoints[index].HandleMode = VectorHandleMode.Smooth;
            ApplySmoothHandle(pathPoints, index, isClosed);
        }

        return true;
    }

    public static bool OptimizePath(IList<VectorPointModel> pathPoints, LayerSnapshot snapshot, double minimumDistance = 0.02d)
    {
        if (pathPoints.Count < 3)
        {
            return false;
        }

        var absolute = VectorPathService.GetAbsolutePoints(snapshot);
        var simplified = VectorPathService.SimplifyStroke(absolute, Math.Max(2d, Math.Min(snapshot.Width, snapshot.Height) * minimumDistance));
        if (simplified.Count < 2 || simplified.Count == pathPoints.Count)
        {
            return false;
        }

        var normalized = VectorPathService.NormalizePointsToSnapshot(simplified, snapshot);
        pathPoints.Clear();
        foreach (var point in normalized)
        {
            pathPoints.Add(point);
        }

        return true;
    }

    public static bool StraightenPath(IList<VectorPointModel> pathPoints, LayerSnapshot snapshot)
    {
        if (pathPoints.Count < 2)
        {
            return false;
        }

        var absolute = VectorPathService.GetAbsolutePoints(snapshot);
        var straightened = ToolAuthoringService.RefinePencilStroke(absolute, PencilToolMode.Straighten);
        if (straightened.Count < 2)
        {
            return false;
        }

        var normalized = VectorPathService.NormalizePointsToSnapshot(straightened, snapshot);
        pathPoints.Clear();
        foreach (var point in normalized)
        {
            pathPoints.Add(point);
        }

        return true;
    }

    public static bool SmoothPathGeometry(IList<VectorPointModel> pathPoints, LayerSnapshot snapshot)
    {
        if (pathPoints.Count < 3)
        {
            return false;
        }

        var absolute = VectorPathService.GetAbsolutePoints(snapshot);
        var smoothed = ToolAuthoringService.RefinePencilStroke(absolute, PencilToolMode.Smooth);
        var normalized = VectorPathService.NormalizePointsToSnapshot(smoothed, snapshot);
        pathPoints.Clear();
        foreach (var point in normalized)
        {
            pathPoints.Add(point);
        }

        return SmoothPath(pathPoints, false);
    }

    private static void ApplySmoothHandle(IList<VectorPointModel> pathPoints, int pointIndex, bool isClosed)
    {
        if (pathPoints.Count == 0)
        {
            return;
        }

        var point = pathPoints[pointIndex];
        var previousIndex = pointIndex == 0 ? (isClosed ? pathPoints.Count - 1 : -1) : pointIndex - 1;
        var nextIndex = pointIndex == pathPoints.Count - 1 ? (isClosed ? 0 : -1) : pointIndex + 1;
        if (previousIndex < 0 && nextIndex < 0)
        {
            point.InHandleX = null;
            point.InHandleY = null;
            point.OutHandleX = null;
            point.OutHandleY = null;
            return;
        }

        var anchor = new Vector2((float)point.X, (float)point.Y);
        var previous = previousIndex >= 0
            ? new Vector2((float)pathPoints[previousIndex].X, (float)pathPoints[previousIndex].Y)
            : anchor;
        var next = nextIndex >= 0
            ? new Vector2((float)pathPoints[nextIndex].X, (float)pathPoints[nextIndex].Y)
            : anchor;
        var tangent = Vector2.Normalize(next - previous);
        if (float.IsNaN(tangent.X) || float.IsNaN(tangent.Y))
        {
            tangent = new Vector2(1f, 0f);
        }

        var inLength = previousIndex >= 0 ? Vector2.Distance(anchor, previous) * (float)SmoothStrength : 0f;
        var outLength = nextIndex >= 0 ? Vector2.Distance(anchor, next) * (float)SmoothStrength : 0f;

        if (inLength > 0f)
        {
            var handle = anchor - (tangent * inLength);
            point.InHandleX = handle.X;
            point.InHandleY = handle.Y;
        }
        else
        {
            point.InHandleX = null;
            point.InHandleY = null;
        }

        if (outLength > 0f)
        {
            var handle = anchor + (tangent * outLength);
            point.OutHandleX = handle.X;
            point.OutHandleY = handle.Y;
        }
        else
        {
            point.OutHandleX = null;
            point.OutHandleY = null;
        }
    }

    private static (VectorPointModel Start, VectorPointModel Middle, VectorPointModel End) SplitSegment(VectorPointModel start, VectorPointModel end, double t)
    {
        var startAnchor = new Vector2((float)start.X, (float)start.Y);
        var startHandle = new Vector2((float)(start.OutHandleX ?? start.X), (float)(start.OutHandleY ?? start.Y));
        var endHandle = new Vector2((float)(end.InHandleX ?? end.X), (float)(end.InHandleY ?? end.Y));
        var endAnchor = new Vector2((float)end.X, (float)end.Y);
        var parameter = (float)Math.Clamp(t, 0d, 1d);

        var q0 = Vector2.Lerp(startAnchor, startHandle, parameter);
        var q1 = Vector2.Lerp(startHandle, endHandle, parameter);
        var q2 = Vector2.Lerp(endHandle, endAnchor, parameter);
        var r0 = Vector2.Lerp(q0, q1, parameter);
        var r1 = Vector2.Lerp(q1, q2, parameter);
        var s = Vector2.Lerp(r0, r1, parameter);

        var updatedStart = VectorPathService.ClonePoints([start])[0];
        updatedStart.OutHandleX = q0.X;
        updatedStart.OutHandleY = q0.Y;

        var inserted = new VectorPointModel
        {
            X = s.X,
            Y = s.Y,
            InHandleX = r0.X,
            InHandleY = r0.Y,
            OutHandleX = r1.X,
            OutHandleY = r1.Y,
            HandleMode = VectorHandleMode.Smooth,
            StrokeWidthScale = (start.StrokeWidthScale + end.StrokeWidthScale) / 2d
        };

        var updatedEnd = VectorPathService.ClonePoints([end])[0];
        updatedEnd.InHandleX = q2.X;
        updatedEnd.InHandleY = q2.Y;

        return (updatedStart, inserted, updatedEnd);
    }
}
