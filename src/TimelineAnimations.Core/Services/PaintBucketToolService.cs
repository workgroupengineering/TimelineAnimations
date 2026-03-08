using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

/// <summary>
/// Computes fill eligibility for open vector paths using Animate-like gap rules.
/// </summary>
public static class PaintBucketToolService
{
    /// <summary>
    /// Determines whether a layer can be filled and whether the path should be
    /// treated as closed for the fill operation.
    /// </summary>
    public static PaintBucketFillDecision AnalyzeFillTarget(LayerSnapshot snapshot, PaintBucketGapSize gapSize)
    {
        if (snapshot.PathPoints.Count == 0)
        {
            return new PaintBucketFillDecision(true, false, 0d, 0d);
        }

        if (snapshot.IsClosed)
        {
            return new PaintBucketFillDecision(true, false, 0d, 0d);
        }

        var absolutePoints = VectorPathService.GetAbsolutePoints(snapshot);
        if (absolutePoints.Count < 2)
        {
            return new PaintBucketFillDecision(true, false, 0d, 0d);
        }

        var start = absolutePoints[0];
        var end = absolutePoints[^1];
        var deltaX = end.X - start.X;
        var deltaY = end.Y - start.Y;
        var gapDistance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        var threshold = GetGapThreshold(snapshot, gapSize);
        var closePath = gapDistance <= threshold;

        return new PaintBucketFillDecision(closePath, closePath, gapDistance, threshold);
    }

    /// <summary>
    /// Returns the document-space distance threshold for the configured gap size.
    /// </summary>
    public static double GetGapThreshold(LayerSnapshot snapshot, PaintBucketGapSize gapSize)
    {
        var scale = Math.Max(12d, Math.Min(snapshot.Width, snapshot.Height));
        return gapSize switch
        {
            PaintBucketGapSize.Small => Math.Max(12d, scale * 0.08d),
            PaintBucketGapSize.Medium => Math.Max(28d, scale * 0.18d),
            PaintBucketGapSize.Large => Math.Max(56d, scale * 0.34d),
            _ => 0d
        };
    }
}

/// <summary>
/// Result of applying Paint Bucket gap analysis to a target path.
/// </summary>
public readonly record struct PaintBucketFillDecision(
    bool CanApply,
    bool ClosePath,
    double GapDistance,
    double Threshold);
