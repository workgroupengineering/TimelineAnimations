using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class TimelineEasingService
{
    public static double Apply(EasingKind easing, double progress)
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
            _ => clamped
        };
    }
}
