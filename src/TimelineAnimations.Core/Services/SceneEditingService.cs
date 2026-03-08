using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class SceneEditingService
{
    public const double MinFrameRate = 1d;
    public const double MinDuration = 0.1d;
    public const double MinCanvasExtent = 64d;
    public const string DefaultStageBackgroundColor = "#FFFFFF";

    public static void EnsureScenes(TimelineDocument document)
    {
        if (document.Scenes.Count == 0)
        {
            var fallbackName = string.IsNullOrWhiteSpace(document.Name) ? "Scene 1" : "Scene 1";
            var scene = CreateSceneFromDocumentState(document, fallbackName);
            document.Scenes.Add(scene);
            document.ActiveSceneId = scene.Id;
        }

        foreach (var scene in document.Scenes)
        {
            scene.FrameRate = NormalizeFrameRate(scene.FrameRate);
            scene.Duration = NormalizeDuration(scene.Duration);
            scene.CanvasWidth = NormalizeCanvasExtent(scene.CanvasWidth);
            scene.CanvasHeight = NormalizeCanvasExtent(scene.CanvasHeight);
            scene.BackgroundFrom = NormalizeStageBackgroundColor(scene.BackgroundFrom);
            scene.BackgroundTo = NormalizeLegacyBackgroundAccent(scene.BackgroundTo, scene.BackgroundFrom);
            SceneTimelineService.EnsureTimelineMetadata(
                scene,
                FrameTimelineService.GetTotalFrames(scene.Duration, scene.FrameRate));
        }

        document.BackgroundFrom = NormalizeStageBackgroundColor(document.BackgroundFrom);
        document.BackgroundTo = NormalizeLegacyBackgroundAccent(document.BackgroundTo, document.BackgroundFrom);

        var activeScene = document.Scenes.FirstOrDefault(scene => scene.Id == document.ActiveSceneId)
            ?? document.Scenes[0];
        SceneTimelineService.EnsureTimelineMetadata(
            activeScene,
            FrameTimelineService.GetTotalFrames(activeScene.Duration, activeScene.FrameRate));
        document.ActiveSceneId = activeScene.Id;
        ApplySceneToDocument(document, activeScene);
    }

    public static double NormalizeFrameRate(double frameRate)
    {
        return Math.Max(MinFrameRate, frameRate);
    }

    public static double NormalizeDuration(double duration)
    {
        return Math.Max(MinDuration, duration);
    }

    public static double NormalizeCanvasExtent(double extent)
    {
        return Math.Max(MinCanvasExtent, extent);
    }

    public static SceneModel CreateSceneFromDocumentState(TimelineDocument document, string name)
    {
        var inheritedFrameRate = document.Scenes.FirstOrDefault(scene => scene.Id == document.ActiveSceneId)?.FrameRate ?? 24;
        return new SceneModel
        {
            Name = name,
            FrameRate = NormalizeFrameRate(inheritedFrameRate),
            Duration = NormalizeDuration(document.Duration),
            CanvasWidth = NormalizeCanvasExtent(document.CanvasWidth),
            CanvasHeight = NormalizeCanvasExtent(document.CanvasHeight),
            TransparentStageBackground = document.TransparentStageBackground,
            BackgroundFrom = document.BackgroundFrom,
            BackgroundTo = document.BackgroundTo,
            FrameLabels = [],
            Markers = [],
            OutgoingTransition = new SceneTransitionModel(),
            Layers = CloneLayers(document.Layers)
        };
    }

    public static SceneModel CreateScene(string name, TimelineDocument document)
    {
        var inheritedFrameRate = document.Scenes.FirstOrDefault(scene => scene.Id == document.ActiveSceneId)?.FrameRate ?? 24;
        return new SceneModel
        {
            Name = name,
            FrameRate = NormalizeFrameRate(inheritedFrameRate),
            Duration = NormalizeDuration(document.Duration),
            CanvasWidth = NormalizeCanvasExtent(document.CanvasWidth),
            CanvasHeight = NormalizeCanvasExtent(document.CanvasHeight),
            TransparentStageBackground = document.TransparentStageBackground,
            BackgroundFrom = document.BackgroundFrom,
            BackgroundTo = document.BackgroundTo,
            FrameLabels = [],
            Markers = [],
            OutgoingTransition = new SceneTransitionModel(),
            Layers = []
        };
    }

    public static SceneModel DuplicateScene(SceneModel source, string name)
    {
        return new SceneModel
        {
            Name = name,
            FrameRate = NormalizeFrameRate(source.FrameRate),
            Duration = NormalizeDuration(source.Duration),
            CanvasWidth = NormalizeCanvasExtent(source.CanvasWidth),
            CanvasHeight = NormalizeCanvasExtent(source.CanvasHeight),
            TransparentStageBackground = source.TransparentStageBackground,
            BackgroundFrom = source.BackgroundFrom,
            BackgroundTo = source.BackgroundTo,
            FrameLabels = DocumentSerializer.Clone(source.FrameLabels),
            Markers = DocumentSerializer.Clone(source.Markers),
            OutgoingTransition = source.OutgoingTransition.Clone(),
            Layers = CloneLayers(source.Layers)
        };
    }

    public static void PersistDocumentStateToScene(TimelineDocument document, SceneModel scene)
    {
        scene.FrameRate = NormalizeFrameRate(scene.FrameRate);
        scene.Duration = NormalizeDuration(document.Duration);
        scene.CanvasWidth = NormalizeCanvasExtent(document.CanvasWidth);
        scene.CanvasHeight = NormalizeCanvasExtent(document.CanvasHeight);
        scene.TransparentStageBackground = document.TransparentStageBackground;
        scene.BackgroundFrom = document.BackgroundFrom;
        scene.BackgroundTo = document.BackgroundTo;
        scene.Layers = document.Layers;
        SceneTimelineService.EnsureTimelineMetadata(scene, FrameTimelineService.GetTotalFrames(scene.Duration, scene.FrameRate));
    }

    public static void ApplySceneToDocument(TimelineDocument document, SceneModel scene)
    {
        document.Duration = NormalizeDuration(scene.Duration);
        document.CanvasWidth = NormalizeCanvasExtent(scene.CanvasWidth);
        document.CanvasHeight = NormalizeCanvasExtent(scene.CanvasHeight);
        document.TransparentStageBackground = scene.TransparentStageBackground;
        document.BackgroundFrom = scene.BackgroundFrom;
        document.BackgroundTo = scene.BackgroundTo;
        document.Layers = scene.Layers;
        document.ActiveSceneId = scene.Id;
    }

    public static string NormalizeStageBackgroundColor(string? color)
    {
        return string.IsNullOrWhiteSpace(color)
            ? DefaultStageBackgroundColor
            : color.Trim();
    }

    private static string NormalizeLegacyBackgroundAccent(string? accentColor, string stageColor)
    {
        return string.IsNullOrWhiteSpace(accentColor)
            ? stageColor
            : accentColor.Trim();
    }

    public static void AddScene(TimelineDocument document, SceneModel scene)
    {
        document.Scenes.Add(scene);
    }

    public static bool RemoveScene(TimelineDocument document, Guid sceneId)
    {
        return document.Scenes.RemoveAll(scene => scene.Id == sceneId) > 0;
    }

    public static void BringForward(TimelineDocument document, Guid sceneId)
    {
        var index = document.Scenes.FindIndex(scene => scene.Id == sceneId);
        if (index < 0 || index == document.Scenes.Count - 1)
        {
            return;
        }

        (document.Scenes[index], document.Scenes[index + 1]) = (document.Scenes[index + 1], document.Scenes[index]);
    }

    public static void SendBackward(TimelineDocument document, Guid sceneId)
    {
        var index = document.Scenes.FindIndex(scene => scene.Id == sceneId);
        if (index <= 0)
        {
            return;
        }

        (document.Scenes[index], document.Scenes[index - 1]) = (document.Scenes[index - 1], document.Scenes[index]);
    }

    private static List<TimelineLayer> CloneLayers(IEnumerable<TimelineLayer> layers)
    {
        var clone = DocumentSerializer.Clone(layers.ToList());
        foreach (var layer in clone)
        {
            layer.Id = Guid.NewGuid();
            foreach (var track in layer.Tracks)
            {
                foreach (var keyframe in track.Keyframes)
                {
                    keyframe.Id = Guid.NewGuid();
                }
            }
        }

        return clone;
    }
}
