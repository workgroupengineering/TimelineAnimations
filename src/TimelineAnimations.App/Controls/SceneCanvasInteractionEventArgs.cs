using Avalonia;
using TimelineAnimations.App.Models;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.Controls;

public sealed class CanvasInteractionStateChangedEventArgs(bool isActive, string completionStatusMessage = "") : EventArgs
{
    public bool IsActive { get; } = isActive;

    public string CompletionStatusMessage { get; } = completionStatusMessage;
}

public sealed class CanvasLayerSelectionRequestedEventArgs(Guid? layerId) : EventArgs
{
    public Guid? LayerId { get; } = layerId;
}

public sealed class CanvasLayerTransformRequestedEventArgs(Guid layerId, Rect bounds) : EventArgs
{
    public Guid LayerId { get; } = layerId;

    public Rect Bounds { get; } = bounds;
}

public sealed class CanvasPaletteDropRequestedEventArgs(LayerKind kind, Point documentPosition) : EventArgs
{
    public LayerKind Kind { get; } = kind;

    public Point DocumentPosition { get; } = documentPosition;
}

public sealed class CanvasDrawingRequestedEventArgs(
    DrawingTool tool,
    Rect documentBounds,
    IReadOnlyList<Point> documentPoints) : EventArgs
{
    public DrawingTool Tool { get; } = tool;

    public Rect DocumentBounds { get; } = documentBounds;

    public IReadOnlyList<Point> DocumentPoints { get; } = documentPoints;
}

public sealed class CanvasPathPointMoveRequestedEventArgs(Guid layerId, int pointIndex, Point documentPoint) : EventArgs
{
    public Guid LayerId { get; } = layerId;

    public int PointIndex { get; } = pointIndex;

    public Point DocumentPoint { get; } = documentPoint;
}

public sealed class CanvasPrototypeTriggerRequestedEventArgs(Guid? layerId, InteractionTriggerKind trigger) : EventArgs
{
    public Guid? LayerId { get; } = layerId;

    public InteractionTriggerKind Trigger { get; } = trigger;
}
