namespace TimelineAnimations.Core.Models;

public sealed class TimelineDocument
{
    public string Name { get; set; } = "New Motion Study";

    public double Duration { get; set; } = 6;

    public double CanvasWidth { get; set; } = 1280;

    public double CanvasHeight { get; set; } = 720;

    public string BackgroundFrom { get; set; } = "#0D1220";

    public string BackgroundTo { get; set; } = "#171F3D";

    public List<TimelineLayer> Layers { get; set; } = [];
}
