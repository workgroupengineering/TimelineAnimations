namespace TimelineAnimations.App.Controls;

public sealed class FrameTimelineInteractionStateChangedEventArgs(bool isActive) : EventArgs
{
    public bool IsActive { get; } = isActive;
}

public enum FrameTimelineRulerInteractionKind
{
    Playhead,
    InPoint,
    OutPoint,
    WorkAreaStart,
    WorkAreaEnd,
    OnionBefore,
    OnionAfter
}

public sealed class FrameTimelineFrameRequestedEventArgs(int frame) : EventArgs
{
    public int Frame { get; } = frame;
}

public sealed class FrameTimelineRulerInteractionRequestedEventArgs(FrameTimelineRulerInteractionKind interactionKind, int frame) : EventArgs
{
    public FrameTimelineRulerInteractionKind InteractionKind { get; } = interactionKind;

    public int Frame { get; } = frame;
}

public sealed class FrameTimelineLayerSelectionRequestedEventArgs(Guid layerId) : EventArgs
{
    public Guid LayerId { get; } = layerId;
}

public sealed class FrameTimelineRangeSelectionRequestedEventArgs(Guid layerId, int startFrame, int endFrame) : EventArgs
{
    public Guid LayerId { get; } = layerId;

    public int StartFrame { get; } = startFrame;

    public int EndFrame { get; } = endFrame;
}

public sealed class FrameTimelineHierarchyToggleRequestedEventArgs(Guid layerId) : EventArgs
{
    public Guid LayerId { get; } = layerId;
}
