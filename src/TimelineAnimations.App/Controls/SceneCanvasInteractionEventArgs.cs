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

public sealed class CanvasPaletteDropRequestedEventArgs(LayerKind kind, Point documentPosition, AvaloniaControlKind? avaloniaControlKind = null) : EventArgs
{
    public LayerKind Kind { get; } = kind;

    public Point DocumentPosition { get; } = documentPosition;

    public AvaloniaControlKind? AvaloniaControlKind { get; } = avaloniaControlKind;
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

public enum CanvasStyleApplicationKind
{
    Fill,
    Stroke
}

public sealed class CanvasLayerStyleSampleRequestedEventArgs(Guid layerId) : EventArgs
{
    public Guid LayerId { get; } = layerId;
}

public sealed class CanvasLayerStyleApplyRequestedEventArgs(Guid layerId, CanvasStyleApplicationKind applicationKind) : EventArgs
{
    public Guid LayerId { get; } = layerId;

    public CanvasStyleApplicationKind ApplicationKind { get; } = applicationKind;
}

public sealed class CanvasPathPointMoveRequestedEventArgs(Guid layerId, int pointIndex, Point documentPoint) : EventArgs
{
    public Guid LayerId { get; } = layerId;

    public int PointIndex { get; } = pointIndex;

    public Point DocumentPoint { get; } = documentPoint;
}

public sealed class CanvasPathPointSelectionRequestedEventArgs(Guid layerId, int pointIndex) : EventArgs
{
    public Guid LayerId { get; } = layerId;

    public int PointIndex { get; } = pointIndex;
}

public sealed class CanvasPathHandleMoveRequestedEventArgs(Guid layerId, int pointIndex, VectorHandleKind handleKind, Point documentPoint) : EventArgs
{
    public Guid LayerId { get; } = layerId;

    public int PointIndex { get; } = pointIndex;

    public VectorHandleKind HandleKind { get; } = handleKind;

    public Point DocumentPoint { get; } = documentPoint;
}

public sealed class CanvasPathPointWidthScaleRequestedEventArgs(Guid layerId, int pointIndex, double scale) : EventArgs
{
    public Guid LayerId { get; } = layerId;

    public int PointIndex { get; } = pointIndex;

    public double Scale { get; } = scale;
}

public sealed class CanvasLassoSelectionRequestedEventArgs(IReadOnlyList<Guid> layerIds) : EventArgs
{
    public IReadOnlyList<Guid> LayerIds { get; } = layerIds;
}

public sealed class CanvasWarpPinMoveRequestedEventArgs(Guid layerId, Guid pinId, Point normalizedPoint) : EventArgs
{
    public Guid LayerId { get; } = layerId;

    public Guid PinId { get; } = pinId;

    public Point NormalizedPoint { get; } = normalizedPoint;
}

public sealed class CanvasRigBoneMoveRequestedEventArgs(Guid layerId, Guid boneId, RigBoneHandleKind handleKind, Point normalizedPoint) : EventArgs
{
    public Guid LayerId { get; } = layerId;

    public Guid BoneId { get; } = boneId;

    public RigBoneHandleKind HandleKind { get; } = handleKind;

    public Point NormalizedPoint { get; } = normalizedPoint;
}

public sealed class CanvasLayerEraseRequestedEventArgs(Guid layerId, Point documentPoint, double radius) : EventArgs
{
    public Guid LayerId { get; } = layerId;

    public Point DocumentPoint { get; } = documentPoint;

    public double Radius { get; } = radius;
}

public sealed class CanvasResizeRequestedEventArgs(double width, double height) : EventArgs
{
    public double Width { get; } = width;

    public double Height { get; } = height;
}

public sealed class CanvasPrototypeTriggerRequestedEventArgs(Guid? layerId, InteractionTriggerKind trigger) : EventArgs
{
    public Guid? LayerId { get; } = layerId;

    public InteractionTriggerKind Trigger { get; } = trigger;
}
