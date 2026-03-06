using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class TimelineEasingService
{
    public static double Apply(EasingKind easing, double progress)
    {
        return Apply(easing, progress, 0.25d, 0.1d, 0.25d, 1d);
    }

    public static double Apply(KeyframeModel keyframe, double progress)
    {
        ArgumentNullException.ThrowIfNull(keyframe);
        return Apply(
            keyframe.Easing,
            progress,
            keyframe.ControlPoint1X,
            keyframe.ControlPoint1Y,
            keyframe.ControlPoint2X,
            keyframe.ControlPoint2Y);
    }

    public static double Apply(ShapeKeyframeModel keyframe, double progress)
    {
        ArgumentNullException.ThrowIfNull(keyframe);
        return Apply(
            keyframe.Easing,
            progress,
            keyframe.ControlPoint1X,
            keyframe.ControlPoint1Y,
            keyframe.ControlPoint2X,
            keyframe.ControlPoint2Y);
    }

    public static double Apply(
        EasingKind easing,
        double progress,
        double controlPoint1X,
        double controlPoint1Y,
        double controlPoint2X,
        double controlPoint2Y)
    {
        var clamped = TimelineMath.Clamp(progress, 0, 1);

        return easing switch
        {
            EasingKind.Linear => clamped,
            EasingKind.EaseIn => clamped * clamped,
            EasingKind.EaseOut => 1 - ((1 - clamped) * (1 - clamped)),
            EasingKind.EaseInOut => clamped < 0.5
                ? 2 * clamped * clamped
                : 1 - (Math.Pow(-2 * clamped + 2, 2) / 2),
            EasingKind.BackOut => 1 + (2.70158 * Math.Pow(clamped - 1, 3)) + (1.70158 * Math.Pow(clamped - 1, 2)),
            EasingKind.Hold => 0,
            EasingKind.Custom => ApplyCubicBezier(clamped, controlPoint1X, controlPoint1Y, controlPoint2X, controlPoint2Y),
            _ => clamped
        };
    }

    private static double ApplyCubicBezier(double progress, double x1, double y1, double x2, double y2)
    {
        var controlX1 = TimelineMath.Clamp(x1, 0, 1);
        var controlY1 = TimelineMath.Clamp(y1, 0, 1);
        var controlX2 = TimelineMath.Clamp(x2, 0, 1);
        var controlY2 = TimelineMath.Clamp(y2, 0, 1);

        var lower = 0d;
        var upper = 1d;
        var t = progress;

        for (var iteration = 0; iteration < 8; iteration++)
        {
            var x = SampleCurve(t, controlX1, controlX2) - progress;
            var derivative = SampleDerivative(t, controlX1, controlX2);
            if (Math.Abs(x) < 0.0001d)
            {
                return SampleCurve(t, controlY1, controlY2);
            }

            if (Math.Abs(derivative) < 0.0001d)
            {
                break;
            }

            t -= x / derivative;
        }

        t = progress;
        for (var iteration = 0; iteration < 14; iteration++)
        {
            var x = SampleCurve(t, controlX1, controlX2);
            if (Math.Abs(x - progress) < 0.0001d)
            {
                return SampleCurve(t, controlY1, controlY2);
            }

            if (x < progress)
            {
                lower = t;
            }
            else
            {
                upper = t;
            }

            t = (lower + upper) / 2d;
        }

        return SampleCurve(t, controlY1, controlY2);
    }

    private static double SampleCurve(double t, double control1, double control2)
    {
        var inverse = 1 - t;
        return (3 * inverse * inverse * t * control1) +
            (3 * inverse * t * t * control2) +
            (t * t * t);
    }

    private static double SampleDerivative(double t, double control1, double control2)
    {
        var inverse = 1 - t;
        return (3 * inverse * inverse * control1) +
            (6 * inverse * t * (control2 - control1)) +
            (3 * t * t * (1 - control2));
    }
}
