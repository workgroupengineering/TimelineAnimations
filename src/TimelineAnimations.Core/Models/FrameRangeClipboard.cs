namespace TimelineAnimations.Core.Models;

public sealed class FrameRangeClipboard
{
    public int FrameCount { get; set; }

    public List<FrameSpanModel> Spans { get; set; } = [];

    public List<ShapeKeyframeModel> ShapeKeyframes { get; set; } = [];

    public List<LayerTrack> Tracks { get; set; } = [];
}
