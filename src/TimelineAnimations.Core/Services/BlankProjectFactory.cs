using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class BlankProjectFactory
{
    public static TimelineDocument Create()
    {
        var document = new TimelineDocument
        {
            Name = "Blank Animation",
            Duration = 6d,
            CanvasWidth = 1280d,
            CanvasHeight = 720d,
            BackgroundFrom = "#0D1220",
            BackgroundTo = "#171F3D",
            Layers = [],
            Scenes = [],
            LibraryItems = [],
            MediaAssets = [],
            PublishProfiles = []
        };

        SceneEditingService.EnsureScenes(document);
        PublishProfileService.EnsureProfiles(document);
        return document;
    }
}
