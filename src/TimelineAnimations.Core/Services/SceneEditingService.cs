using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class SceneEditingService
{
    public static void EnsureScenes(TimelineDocument document)
    {
        if (document.Scenes.Count == 0)
        {
            var fallbackName = string.IsNullOrWhiteSpace(document.Name) ? "Scene 1" : "Scene 1";
            var scene = CreateSceneFromDocumentState(document, fallbackName);
            document.Scenes.Add(scene);
            document.ActiveSceneId = scene.Id;
        }

        var activeScene = document.Scenes.FirstOrDefault(scene => scene.Id == document.ActiveSceneId)
            ?? document.Scenes[0];
        activeScene.FrameRate = Math.Max(1, activeScene.FrameRate);
        document.ActiveSceneId = activeScene.Id;
        ApplySceneToDocument(document, activeScene);
    }

    public static SceneModel CreateSceneFromDocumentState(TimelineDocument document, string name)
    {
        var inheritedFrameRate = document.Scenes.FirstOrDefault(scene => scene.Id == document.ActiveSceneId)?.FrameRate ?? 24;
        return new SceneModel
        {
            Name = name,
            FrameRate = inheritedFrameRate,
            Duration = document.Duration,
            CanvasWidth = document.CanvasWidth,
            CanvasHeight = document.CanvasHeight,
            BackgroundFrom = document.BackgroundFrom,
            BackgroundTo = document.BackgroundTo,
            FrameLabels = [],
            Layers = CloneLayers(document.Layers)
        };
    }

    public static SceneModel CreateScene(string name, TimelineDocument document)
    {
        var inheritedFrameRate = document.Scenes.FirstOrDefault(scene => scene.Id == document.ActiveSceneId)?.FrameRate ?? 24;
        return new SceneModel
        {
            Name = name,
            FrameRate = inheritedFrameRate,
            Duration = document.Duration,
            CanvasWidth = document.CanvasWidth,
            CanvasHeight = document.CanvasHeight,
            BackgroundFrom = document.BackgroundFrom,
            BackgroundTo = document.BackgroundTo,
            FrameLabels = [],
            Layers = []
        };
    }

    public static SceneModel DuplicateScene(SceneModel source, string name)
    {
        return new SceneModel
        {
            Name = name,
            FrameRate = source.FrameRate,
            Duration = source.Duration,
            CanvasWidth = source.CanvasWidth,
            CanvasHeight = source.CanvasHeight,
            BackgroundFrom = source.BackgroundFrom,
            BackgroundTo = source.BackgroundTo,
            FrameLabels = DocumentSerializer.Clone(source.FrameLabels),
            Layers = CloneLayers(source.Layers)
        };
    }

    public static void PersistDocumentStateToScene(TimelineDocument document, SceneModel scene)
    {
        scene.FrameRate = Math.Max(1, scene.FrameRate);
        scene.Duration = document.Duration;
        scene.CanvasWidth = document.CanvasWidth;
        scene.CanvasHeight = document.CanvasHeight;
        scene.BackgroundFrom = document.BackgroundFrom;
        scene.BackgroundTo = document.BackgroundTo;
        scene.Layers = document.Layers;
    }

    public static void ApplySceneToDocument(TimelineDocument document, SceneModel scene)
    {
        document.Duration = scene.Duration;
        document.CanvasWidth = scene.CanvasWidth;
        document.CanvasHeight = scene.CanvasHeight;
        document.BackgroundFrom = scene.BackgroundFrom;
        document.BackgroundTo = scene.BackgroundTo;
        document.Layers = scene.Layers;
        document.ActiveSceneId = scene.Id;
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
