namespace TimelineAnimations.Core.Models;

public readonly record struct AudioMeterSnapshot(
    double Peak,
    double Rms,
    double LeftGain,
    double RightGain,
    double EffectiveGain,
    bool IsAudible)
{
    public static readonly AudioMeterSnapshot Silent = new(0d, 0d, 0d, 0d, 0d, false);
}

