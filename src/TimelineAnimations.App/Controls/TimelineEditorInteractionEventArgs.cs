using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.Controls;

public sealed class TimelineScrubRequestedEventArgs(double time) : EventArgs
{
    public double Time { get; } = time;
}

public sealed class TimelineTrackSelectionRequestedEventArgs(AnimatedProperty property) : EventArgs
{
    public AnimatedProperty Property { get; } = property;
}

public sealed class TimelineKeyframeSelectionRequestedEventArgs(AnimatedProperty property, Guid keyframeId) : EventArgs
{
    public AnimatedProperty Property { get; } = property;

    public Guid KeyframeId { get; } = keyframeId;
}

public sealed class TimelineKeyframeMoveRequestedEventArgs(AnimatedProperty property, Guid keyframeId, double time) : EventArgs
{
    public AnimatedProperty Property { get; } = property;

    public Guid KeyframeId { get; } = keyframeId;

    public double Time { get; } = time;
}

public sealed class TimelineKeyframeAddRequestedEventArgs(AnimatedProperty property, double time) : EventArgs
{
    public AnimatedProperty Property { get; } = property;

    public double Time { get; } = time;
}
