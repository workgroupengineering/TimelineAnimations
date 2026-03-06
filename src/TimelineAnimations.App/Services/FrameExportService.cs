using Avalonia;
using Avalonia.Media.Imaging;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.Services;

public static class FrameExportService
{
    public static async Task ExportFrameAsync(TimelineDocument document, double time, Stream stream, CancellationToken cancellationToken = default)
    {
        using var bitmap = RenderFrameBitmap(document, time);
        bitmap.Save(stream);
        await stream.FlushAsync(cancellationToken);
    }

    public static async Task<int> ExportSequenceAsync(
        TimelineDocument document,
        string folderPath,
        double framesPerSecond = 0,
        bool playAllScenes = false,
        int outputWidth = 0,
        int outputHeight = 0,
        bool transparentBackground = false,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(folderPath);

        var exportFrameRate = GetPlaybackFrameRate(document, framesPerSecond);
        var frameCount = GetFrameCount(document, exportFrameRate, playAllScenes);
        for (var frame = 0; frame < frameCount; frame++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var time = Math.Min(frame / exportFrameRate, GetPlaybackDuration(document, playAllScenes));
            var filePath = Path.Combine(folderPath, $"frame_{frame:0000}.png");

            await using var stream = File.Create(filePath);
            using var bitmap = RenderFrameBitmap(
                document,
                time,
                outputWidth,
                outputHeight,
                exportFrameRate,
                playAllScenes,
                transparentBackground);
            bitmap.Save(stream);
            await stream.FlushAsync(cancellationToken);
        }

        return frameCount;
    }

    public static async Task<int> ExportProjectSequenceAsync(
        TimelineDocument document,
        string folderPath,
        double framesPerSecond = 0,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(folderPath);
        SceneEditingService.EnsureScenes(document);

        var sceneCount = 0;
        foreach (var scene in document.Scenes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sceneFolderName = $"{sceneCount + 1:00}_{SanitizeFileName(scene.Name)}";
            var sceneFolderPath = Path.Combine(folderPath, sceneFolderName);
            await ExportSequenceAsync(
                BuildSceneDocument(document, scene),
                sceneFolderPath,
                framesPerSecond,
                playAllScenes: false,
                cancellationToken: cancellationToken);
            sceneCount++;
        }

        return sceneCount;
    }

    public static RenderTargetBitmap RenderFrameBitmap(
        TimelineDocument document,
        double time,
        int outputWidth = 0,
        int outputHeight = 0,
        double framesPerSecond = 0,
        bool playAllScenes = false,
        bool transparentBackground = false)
    {
        SceneEditingService.EnsureScenes(document);
        var playbackFrame = ResolvePlaybackFrame(document, time, playAllScenes);
        var sourceWidth = Math.Max(1, (int)Math.Round(playbackFrame.Scene.CanvasWidth));
        var sourceHeight = Math.Max(1, (int)Math.Round(playbackFrame.Scene.CanvasHeight));
        var targetWidth = outputWidth > 0 ? outputWidth : sourceWidth;
        var targetHeight = outputHeight > 0 ? outputHeight : sourceHeight;

        using var rendered = RenderSceneBitmap(document, playbackFrame.Scene, playbackFrame.LocalTime, transparentBackground);
        var bitmap = new RenderTargetBitmap(new PixelSize(targetWidth, targetHeight), new Vector(96, 96));
        using var context = bitmap.CreateDrawingContext(transparentBackground);
        context.DrawImage(rendered, new Rect(0, 0, targetWidth, targetHeight));
        return bitmap;
    }

    public static double GetPlaybackDuration(TimelineDocument document, bool playAllScenes)
    {
        SceneEditingService.EnsureScenes(document);
        if (!playAllScenes)
        {
            return GetActiveScene(document)?.Duration ?? document.Duration;
        }

        return Math.Max(0.1d, document.Scenes.Sum(scene => Math.Max(0.1d, scene.Duration)));
    }

    public static double GetPlaybackFrameRate(TimelineDocument document, double framesPerSecond = 0)
    {
        if (framesPerSecond > 0d)
        {
            return framesPerSecond;
        }

        SceneEditingService.EnsureScenes(document);
        return GetActiveScene(document)?.FrameRate
            ?? document.Scenes.FirstOrDefault()?.FrameRate
            ?? 24d;
    }

    public static int GetFrameCount(TimelineDocument document, double framesPerSecond = 0, bool playAllScenes = false)
    {
        var frameRate = GetPlaybackFrameRate(document, framesPerSecond);
        var duration = GetPlaybackDuration(document, playAllScenes);
        return Math.Max(1, (int)Math.Ceiling(duration * frameRate) + 1);
    }

    private static WriteableBitmap RenderSceneBitmap(TimelineDocument document, SceneModel scene, double time, bool transparentBackground)
    {
        var stageRect = new Rect(0, 0, scene.CanvasWidth, scene.CanvasHeight);
        var sceneDocument = BuildSceneDocument(document, scene);
        var state = CompositeFrameRenderer.BuildSceneState(
            sceneDocument,
            scene.Layers,
            time,
            scene.Duration,
            scene.FrameRate,
            stageRect.Width,
            stageRect.Height,
            scene.BackgroundFrom,
            scene.BackgroundTo);
        return CompositeFrameRenderer.RenderBitmap(state, includeBackground: !transparentBackground);
    }

    private static TimelineDocument BuildSceneDocument(TimelineDocument document, SceneModel scene)
    {
        return new TimelineDocument
        {
            Name = document.Name,
            Duration = scene.Duration,
            CanvasWidth = scene.CanvasWidth,
            CanvasHeight = scene.CanvasHeight,
            BackgroundFrom = scene.BackgroundFrom,
            BackgroundTo = scene.BackgroundTo,
            ActiveSceneId = scene.Id,
            Scenes = [DocumentSerializer.Clone(scene)],
            LibraryItems = [.. document.LibraryItems.Select(DocumentSerializer.Clone)],
            MediaAssets = [.. document.MediaAssets.Select(DocumentSerializer.Clone)],
            PublishProfiles = [.. document.PublishProfiles.Select(DocumentSerializer.Clone)],
            Layers = [.. scene.Layers.Select(DocumentSerializer.Clone)]
        };
    }

    private static PlaybackFrame ResolvePlaybackFrame(TimelineDocument document, double time, bool playAllScenes)
    {
        SceneEditingService.EnsureScenes(document);
        var scenes = document.Scenes;
        if (!playAllScenes || scenes.Count == 0)
        {
            var activeScene = GetActiveScene(document) ?? scenes.First();
            return new PlaybackFrame(activeScene, TimelineMath.Clamp(time, 0d, activeScene.Duration));
        }

        var remaining = Math.Max(0d, time);
        foreach (var scene in scenes)
        {
            var duration = Math.Max(0.1d, scene.Duration);
            if (remaining <= duration)
            {
                return new PlaybackFrame(scene, TimelineMath.Clamp(remaining, 0d, duration));
            }

            remaining -= duration;
        }

        var lastScene = scenes[^1];
        return new PlaybackFrame(lastScene, lastScene.Duration);
    }

    private static SceneModel? GetActiveScene(TimelineDocument document)
    {
        return document.Scenes.FirstOrDefault(scene => scene.Id == document.ActiveSceneId)
            ?? document.Scenes.FirstOrDefault();
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "scene" : sanitized;
    }

    private readonly record struct PlaybackFrame(SceneModel Scene, double LocalTime);
}
