using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.Controls;

public enum TimelineEditorInteractionKind
{
    Scrub,
    KeyframeDrag
}

public sealed class TimelineInteractionStateChangedEventArgs(bool isActive, TimelineEditorInteractionKind interactionKind) : EventArgs
{
    public bool IsActive { get; } = isActive;

    public TimelineEditorInteractionKind InteractionKind { get; } = interactionKind;
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

public sealed class TimelineLayerSelectionRequestedEventArgs(Guid layerId) : EventArgs
{
    public Guid LayerId { get; } = layerId;
}

public sealed class TimelineHierarchyToggleRequestedEventArgs(Guid layerId) : EventArgs
{
    public Guid LayerId { get; } = layerId;
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
