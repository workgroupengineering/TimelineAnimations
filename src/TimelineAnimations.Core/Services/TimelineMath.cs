namespace TimelineAnimations.Core.Services;

public static class TimelineMath
{
    public static double Clamp(double value, double min, double max)
    {
        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }

    public static double Snap(double value, double step)
    {
        if (step <= 0)
        {
            return value;
        }

        return Math.Round(value / step) * step;
    }
}
