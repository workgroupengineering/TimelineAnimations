namespace TimelineAnimations.Core.Models;

public sealed class LayerWarpSettings
{
    public bool IsEnabled { get; set; }

    public int MeshColumns { get; set; } = 3;

    public int MeshRows { get; set; } = 3;

    public List<WarpPinModel> Pins { get; set; } = [];
}
