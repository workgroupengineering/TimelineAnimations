namespace TimelineAnimations.Core.Models;

public sealed class TimelineDocument
{
    public string Name { get; set; } = "New Motion Study";

    public double Duration { get; set; } = 6;

    public double CanvasWidth { get; set; } = 1280;

    public double CanvasHeight { get; set; } = 720;

    public bool TransparentStageBackground { get; set; }

    public string BackgroundFrom { get; set; } = "#0D1220";

    public string BackgroundTo { get; set; } = "#171F3D";

    public Guid? ActiveSceneId { get; set; }

    public AnimateDocumentSettings Animate { get; set; } = new();

    public List<SceneModel> Scenes { get; set; } = [];

    public List<LibraryItem> LibraryItems { get; set; } = [];

    public List<MediaAsset> MediaAssets { get; set; } = [];

    public List<PublishProfile> PublishProfiles { get; set; } = [];

    public List<MotionPresetModel> MotionPresets { get; set; } = [];

    public List<FlashAuthoringSupplementalFile> FlashAuthoringSupplementalFiles { get; set; } = [];

    public List<TimelineLayer> Layers { get; set; } = [];
}
