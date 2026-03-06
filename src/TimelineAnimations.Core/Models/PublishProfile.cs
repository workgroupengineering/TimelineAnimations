namespace TimelineAnimations.Core.Models;

public sealed class PublishProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "Preview";

    public PublishOutputKind OutputKind { get; set; } = PublishOutputKind.PngSequence;

    public int Width { get; set; } = 1280;

    public int Height { get; set; } = 720;

    public double FrameRate { get; set; } = 24d;

    public bool PlayAllScenes { get; set; }

    public bool TransparentBackground { get; set; }

    public bool IncludeSceneGraph { get; set; } = true;

    public bool IncludeValidationReport { get; set; } = true;

    public bool IncludeAudio { get; set; } = true;

    public int SpriteSheetColumns { get; set; } = 6;

    public int Quality { get; set; } = 90;

    public PublishProfile Clone()
    {
        return new PublishProfile
        {
            Name = Name,
            OutputKind = OutputKind,
            Width = Width,
            Height = Height,
            FrameRate = FrameRate,
            PlayAllScenes = PlayAllScenes,
            TransparentBackground = TransparentBackground,
            IncludeSceneGraph = IncludeSceneGraph,
            IncludeValidationReport = IncludeValidationReport,
            IncludeAudio = IncludeAudio,
            SpriteSheetColumns = SpriteSheetColumns,
            Quality = Quality
        };
    }
}
