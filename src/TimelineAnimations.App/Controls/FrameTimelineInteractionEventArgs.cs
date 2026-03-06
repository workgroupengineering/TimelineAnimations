namespace TimelineAnimations.App.Controls;

public sealed class FrameTimelineFrameRequestedEventArgs(int frame) : EventArgs
{
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
