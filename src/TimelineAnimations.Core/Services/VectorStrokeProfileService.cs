using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

/// <summary>
/// Applies reusable variable-width stroke profiles and localized width edits.
/// </summary>
public static class VectorStrokeProfileService
{
    private const double WidthEpsilon = 0.0001d;

    /// <summary>
    /// Applies a reusable width profile across the provided stroke points.
    /// </summary>
    public static bool ApplyProfile(
        IList<VectorPointModel> pathPoints,
        VectorStrokeProfilePreset preset,
        double strength = 1d,
        bool isClosed = false)
    {
        if (pathPoints.Count == 0)
        {
            return false;
        }

        var normalizedStrength = TimelineMath.Clamp(strength, 0.05d, 1d);
        if (preset == VectorStrokeProfilePreset.Uniform)
        {
            return NormalizeUniformWidth(pathPoints);
        }

        var minimumScale = Lerp(1d, 0.34d, normalizedStrength);
        var maximumScale = Lerp(1d, 1.92d, normalizedStrength);
        var changed = false;

        for (var index = 0; index < pathPoints.Count; index++)
        {
            var position = GetNormalizedPosition(index, pathPoints.Count, isClosed);
            var target = preset switch
            {
                VectorStrokeProfilePreset.TaperStart => Lerp(minimumScale, maximumScale, position),
                VectorStrokeProfilePreset.TaperEnd => Lerp(maximumScale, minimumScale, position),
                VectorStrokeProfilePreset.TaperBoth => Lerp(minimumScale, maximumScale, 1d - Math.Abs((position * 2d) - 1d)),
                VectorStrokeProfilePreset.Wave => Lerp(minimumScale, maximumScale, (Math.Sin((position * Math.PI * 2d) - (Math.PI / 2d)) + 1d) * 0.5d),
                _ => 1d
            };

            if (Math.Abs(pathPoints[index].StrokeWidthScale - target) <= WidthEpsilon)
            {
                continue;
            }

            pathPoints[index].StrokeWidthScale = target;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// Applies a localized width edit around a single width point.
    /// </summary>
    public static bool ApplyLocalizedWidthScale(
        IList<VectorPointModel> pathPoints,
        int pointIndex,
        double scale)
    {
        if (pointIndex < 0 || pointIndex >= pathPoints.Count)
        {
            return false;
        }

        var normalizedScale = Math.Clamp(scale, 0.2d, 4d);
        var changed = false;
        var influences = new[]
        {
            (Offset: 0, Weight: 1d),
            (Offset: -1, Weight: 0.55d),
            (Offset: 1, Weight: 0.55d),
            (Offset: -2, Weight: 0.24d),
            (Offset: 2, Weight: 0.24d)
        };

        foreach (var influence in influences)
        {
            var index = pointIndex + influence.Offset;
            if (index < 0 || index >= pathPoints.Count)
            {
                continue;
            }

            var point = pathPoints[index];
            var target = influence.Offset == 0
                ? normalizedScale
                : Lerp(point.StrokeWidthScale, normalizedScale, influence.Weight);

            if (Math.Abs(point.StrokeWidthScale - target) <= WidthEpsilon)
            {
                continue;
            }

            point.StrokeWidthScale = Math.Clamp(target, 0.2d, 4d);
            changed = true;
        }

        return changed;
    }

    private static bool NormalizeUniformWidth(IList<VectorPointModel> pathPoints)
    {
        var changed = false;
        for (var index = 0; index < pathPoints.Count; index++)
        {
            if (Math.Abs(pathPoints[index].StrokeWidthScale - 1d) <= WidthEpsilon)
            {
                continue;
            }

            pathPoints[index].StrokeWidthScale = 1d;
            changed = true;
        }

        return changed;
    }

    private static double GetNormalizedPosition(int index, int count, bool isClosed)
    {
        if (count <= 1)
        {
            return 0.5d;
        }

        if (isClosed)
        {
            return index / (double)count;
        }

        return index / Math.Max(1d, count - 1d);
    }

    private static double Lerp(double start, double end, double progress)
    {
        return start + ((end - start) * TimelineMath.Clamp(progress, 0d, 1d));
    }
}
