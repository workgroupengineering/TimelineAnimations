namespace TimelineAnimations.Core.Models;

public sealed class LayerTrack
{
    public AnimatedProperty Property { get; set; }

    public List<KeyframeModel> Keyframes { get; set; } = [];
}
