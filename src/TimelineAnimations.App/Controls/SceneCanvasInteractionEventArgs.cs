using Avalonia;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.Controls;

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
