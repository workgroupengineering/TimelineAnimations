using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class PublishProfileService
{
    public static void EnsureProfiles(TimelineDocument document)
    {
        if (document.PublishProfiles.Count > 0)
        {
            return;
        }

        var width = Math.Max(1, (int)Math.Round(document.CanvasWidth));
        var height = Math.Max(1, (int)Math.Round(document.CanvasHeight));
        var frameRate = document.Scenes.FirstOrDefault(scene => scene.Id == document.ActiveSceneId)?.FrameRate
            ?? document.Scenes.FirstOrDefault()?.FrameRate
            ?? 24d;

        document.PublishProfiles =
        [
            new PublishProfile
            {
                Name = "Studio Preview",
                OutputKind = PublishOutputKind.Mp4,
                Width = width,
                Height = height,
                FrameRate = frameRate,
                PlayAllScenes = true,
                IncludeSceneGraph = true,
                IncludeValidationReport = true
            },
            new PublishProfile
            {
                Name = "PNG Sequence",
                OutputKind = PublishOutputKind.PngSequence,
                Width = width,
                Height = height,
                FrameRate = frameRate,
                PlayAllScenes = false,
                IncludeSceneGraph = true,
                IncludeValidationReport = true
            },
            new PublishProfile
            {
                Name = "Sprite Atlas",
                OutputKind = PublishOutputKind.SpriteSheet,
                Width = width,
                Height = height,
                FrameRate = frameRate,
                PlayAllScenes = false,
                SpriteSheetColumns = 6,
                IncludeSceneGraph = true
            },
            new PublishProfile
            {
                Name = "Interaction Package",
                OutputKind = PublishOutputKind.Package,
                Width = width,
                Height = height,
                FrameRate = frameRate,
                PlayAllScenes = true,
                IncludeSceneGraph = true,
                IncludeValidationReport = true
            }
        ];
    }

    public static PublishProfile CreateProfile(TimelineDocument document, string name)
    {
        EnsureProfiles(document);
        var seed = document.PublishProfiles.FirstOrDefault()?.Clone() ?? new PublishProfile();
        seed.Name = name;
        return seed;
    }
}
