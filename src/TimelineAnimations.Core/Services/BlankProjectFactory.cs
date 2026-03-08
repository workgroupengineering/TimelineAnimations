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
            TransparentStageBackground = false,
            BackgroundFrom = "#FFFFFF",
            BackgroundTo = "#FFFFFF",
            Layers = [],
            Scenes = [],
            LibraryItems = [],
            MediaAssets = [],
            PublishProfiles = []
        };

        AnimateDocumentProfileService.ApplyTargetDefaults(document, AnimateDocumentType.Html5Canvas);
        SceneEditingService.EnsureScenes(document);
        PublishProfileService.EnsureProfiles(document);
        return document;
    }
}
