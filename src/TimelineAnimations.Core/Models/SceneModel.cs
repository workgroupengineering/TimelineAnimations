namespace TimelineAnimations.Core.Models;

public sealed class SceneModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "Scene 1";

    public double FrameRate { get; set; } = 24;

    public double Duration { get; set; } = 6;

    public double CanvasWidth { get; set; } = 1280;

    public double CanvasHeight { get; set; } = 720;

    public string BackgroundFrom { get; set; } = "#0D1220";

    public string BackgroundTo { get; set; } = "#171F3D";

    public List<FrameLabelModel> FrameLabels { get; set; } = [];

    public List<TimelineLayer> Layers { get; set; } = [];
}
