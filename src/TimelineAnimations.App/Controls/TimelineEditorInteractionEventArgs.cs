using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.Controls;

public sealed class TimelineInteractionStateChangedEventArgs(bool isActive) : EventArgs
{
    public bool IsActive { get; } = isActive;
}

public sealed class TimelineScrubRequestedEventArgs(double time) : EventArgs
{
    public double Time { get; } = time;
}

public sealed class TimelineTrackSelectionRequestedEventArgs(Guid layerId, AnimatedProperty property) : EventArgs
{
    public Guid LayerId { get; } = layerId;

    public AnimatedProperty Property { get; } = property;
}

public sealed class TimelineKeyframeSelectionRequestedEventArgs(Guid layerId, AnimatedProperty property, Guid keyframeId) : EventArgs
{
    public Guid LayerId { get; } = layerId;

    public AnimatedProperty Property { get; } = property;

    public Guid KeyframeId { get; } = keyframeId;
}

public sealed class TimelineKeyframeMoveRequestedEventArgs(Guid layerId, AnimatedProperty property, Guid keyframeId, double time) : EventArgs
{
    public Guid LayerId { get; } = layerId;

    public AnimatedProperty Property { get; } = property;

    public Guid KeyframeId { get; } = keyframeId;

    public double Time { get; } = time;
}

public sealed class TimelineKeyframeAddRequestedEventArgs(Guid layerId, AnimatedProperty property, double time) : EventArgs
{
    public Guid LayerId { get; } = layerId;

    public AnimatedProperty Property { get; } = property;

    public double Time { get; } = time;
}
