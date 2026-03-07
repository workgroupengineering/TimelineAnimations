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
        bool useWorkArea = false,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(folderPath);

        var exportFrameRate = GetPlaybackFrameRate(document, framesPerSecond);
        var frameCount = GetFrameCount(document, exportFrameRate, playAllScenes, useWorkArea);
        for (var frame = 0; frame < frameCount; frame++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var time = Math.Min(frame / exportFrameRate, GetPlaybackDuration(document, playAllScenes, useWorkArea));
            var filePath = Path.Combine(folderPath, $"frame_{frame:0000}.png");

            await using var stream = File.Create(filePath);
            using var bitmap = RenderFrameBitmap(
                document,
                time,
                outputWidth,
                outputHeight,
                exportFrameRate,
                playAllScenes,
                transparentBackground,
                useWorkArea);
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
        bool transparentBackground = false,
        bool useWorkArea = false)
    {
        SceneEditingService.EnsureScenes(document);
        var sequenceFrame = ResolvePlaybackFrame(document, time, playAllScenes, useWorkArea);
        var sourceWidth = Math.Max(1, (int)Math.Round(sequenceFrame.Scene.CanvasWidth));
        var sourceHeight = Math.Max(1, (int)Math.Round(sequenceFrame.Scene.CanvasHeight));
        var targetWidth = outputWidth > 0 ? outputWidth : sourceWidth;
        var targetHeight = outputHeight > 0 ? outputHeight : sourceHeight;

        using var rendered = RenderSceneBitmap(document, sequenceFrame.Scene, sequenceFrame.LocalTime, transparentBackground);
        if (!sequenceFrame.HasTransition || sequenceFrame.NextScene is null)
        {
            var bitmap = new RenderTargetBitmap(new PixelSize(targetWidth, targetHeight), new Vector(96, 96));
            using var context = bitmap.CreateDrawingContext(transparentBackground);
            context.DrawImage(rendered, new Rect(0, 0, targetWidth, targetHeight));
            return bitmap;
        }

        using var nextBitmap = RenderSceneBitmap(document, sequenceFrame.NextScene, sequenceFrame.NextLocalTime, transparentBackground);
        return RenderTransitionBitmap(
            rendered,
            nextBitmap,
            sequenceFrame.Scene.OutgoingTransition,
            sequenceFrame.TransitionProgress,
            targetWidth,
            targetHeight,
            transparentBackground);
    }

    public static double GetPlaybackDuration(TimelineDocument document, bool playAllScenes, bool useWorkArea = false)
    {
        SceneEditingService.EnsureScenes(document);
        if (!playAllScenes)
        {
            var activeScene = GetActiveScene(document);
            return activeScene is null ? document.Duration : GetScenePlaybackDuration(activeScene, useWorkArea);
        }

        var sequenceOffset = 0d;
        for (var index = 0; index < document.Scenes.Count; index++)
        {
            var scene = document.Scenes[index];
            var nextScene = index < document.Scenes.Count - 1 ? document.Scenes[index + 1] : null;
            var sceneDuration = GetScenePlaybackDuration(scene, useWorkArea);
            var overlap = GetSceneOverlap(scene, nextScene, useWorkArea);
            sequenceOffset += sceneDuration;
            if (nextScene is not null)
            {
                sequenceOffset -= overlap;
            }
        }

        return Math.Max(0.1d, sequenceOffset);
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

    public static int GetFrameCount(TimelineDocument document, double framesPerSecond = 0, bool playAllScenes = false, bool useWorkArea = false)
    {
        var frameRate = GetPlaybackFrameRate(document, framesPerSecond);
        var duration = GetPlaybackDuration(document, playAllScenes, useWorkArea);
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

    private static PlaybackFrame ResolvePlaybackFrame(TimelineDocument document, double time, bool playAllScenes, bool useWorkArea)
    {
        SceneEditingService.EnsureScenes(document);
        var scenes = document.Scenes;
        if (!playAllScenes || scenes.Count == 0)
        {
            var activeScene = GetActiveScene(document) ?? scenes.First();
            var localTime = ClampToSceneRange(activeScene, time, useWorkArea);
            return new PlaybackFrame(activeScene, localTime);
        }

        var clampedTime = TimelineMath.Clamp(time, 0d, GetPlaybackDuration(document, playAllScenes: true, useWorkArea));
        var offset = 0d;
        for (var index = 0; index < scenes.Count; index++)
        {
            var scene = scenes[index];
            var nextScene = index < scenes.Count - 1 ? scenes[index + 1] : null;
            var sceneDuration = GetScenePlaybackDuration(scene, useWorkArea);
            var sceneEnd = offset + sceneDuration;
            var overlap = GetSceneOverlap(scene, nextScene, useWorkArea);
            var transitionStart = sceneEnd - overlap;

            if (clampedTime <= sceneEnd || index == scenes.Count - 1)
            {
                var localTime = ConvertToSceneLocalTime(scene, clampedTime - offset, useWorkArea);
                if (nextScene is not null && overlap > 0d && clampedTime >= transitionStart)
                {
                    var transitionProgress = TimelineMath.Clamp((clampedTime - transitionStart) / overlap, 0d, 1d);
                    var nextLocalTime = ConvertToSceneLocalTime(nextScene, clampedTime - transitionStart, useWorkArea);
                    return new PlaybackFrame(scene, localTime, nextScene, nextLocalTime, transitionProgress);
                }

                return new PlaybackFrame(scene, localTime);
            }

            offset = sceneEnd - overlap;
        }

        var lastScene = scenes[^1];
        return new PlaybackFrame(lastScene, ConvertToSceneLocalTime(lastScene, GetScenePlaybackDuration(lastScene, useWorkArea), useWorkArea));
    }

    private static double GetScenePlaybackDuration(SceneModel scene, bool useWorkArea)
    {
        var totalFrames = FrameTimelineService.GetTotalFrames(scene.Duration, scene.FrameRate);
        SceneTimelineService.EnsureTimelineMetadata(scene, totalFrames);
        var range = SceneTimelineService.GetPlaybackRange(scene, totalFrames, useWorkArea);
        return Math.Max(0.05d, FrameTimelineService.FrameToTime(range.EndFrame, scene.FrameRate) - FrameTimelineService.FrameToTime(range.StartFrame, scene.FrameRate));
    }

    private static double GetSceneOverlap(SceneModel scene, SceneModel? nextScene, bool useWorkArea)
    {
        if (nextScene is null)
        {
            return 0d;
        }

        return Math.Min(
            SceneTimelineService.GetTransitionOverlap(scene, nextScene),
            Math.Min(GetScenePlaybackDuration(scene, useWorkArea), GetScenePlaybackDuration(nextScene, useWorkArea)));
    }

    private static double ClampToSceneRange(SceneModel scene, double localTime, bool useWorkArea)
    {
        var totalFrames = FrameTimelineService.GetTotalFrames(scene.Duration, scene.FrameRate);
        SceneTimelineService.EnsureTimelineMetadata(scene, totalFrames);
        var range = SceneTimelineService.GetPlaybackRange(scene, totalFrames, useWorkArea);
        var startTime = FrameTimelineService.FrameToTime(range.StartFrame, scene.FrameRate);
        var endTime = FrameTimelineService.FrameToTime(range.EndFrame, scene.FrameRate);
        return TimelineMath.Clamp(localTime, startTime, endTime);
    }

    private static double ConvertToSceneLocalTime(SceneModel scene, double relativeTime, bool useWorkArea)
    {
        var totalFrames = FrameTimelineService.GetTotalFrames(scene.Duration, scene.FrameRate);
        SceneTimelineService.EnsureTimelineMetadata(scene, totalFrames);
        var range = SceneTimelineService.GetPlaybackRange(scene, totalFrames, useWorkArea);
        var startTime = FrameTimelineService.FrameToTime(range.StartFrame, scene.FrameRate);
        var endTime = FrameTimelineService.FrameToTime(range.EndFrame, scene.FrameRate);
        return TimelineMath.Clamp(startTime + Math.Max(0d, relativeTime), startTime, endTime);
    }

    private static RenderTargetBitmap RenderTransitionBitmap(
        WriteableBitmap current,
        WriteableBitmap next,
        SceneTransitionModel transition,
        double progress,
        int width,
        int height,
        bool transparentBackground)
    {
        var bitmap = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
        using var context = bitmap.CreateDrawingContext(transparentBackground);
        var currentRect = new Rect(0, 0, width, height);
        context.DrawImage(current, currentRect);

        switch (transition.Kind)
        {
            case SceneTransitionKind.CrossDissolve:
                using (context.PushOpacity(TimelineMath.Clamp(progress, 0d, 1d)))
                {
                    context.DrawImage(next, currentRect);
                }
                break;
            case SceneTransitionKind.DipToBlack:
            {
                var color = Avalonia.Media.Color.Parse(string.IsNullOrWhiteSpace(transition.AccentColor) ? "#000000" : transition.AccentColor);
                var brush = new Avalonia.Media.SolidColorBrush(color);
                if (progress < 0.5d)
                {
                    using (context.PushOpacity(progress * 2d))
                    {
                        context.DrawRectangle(brush, null, currentRect);
                    }
                }
                else
                {
                    context.DrawRectangle(brush, null, currentRect);
                    using (context.PushOpacity((progress - 0.5d) * 2d))
                    {
                        context.DrawImage(next, currentRect);
                    }
                }
                break;
            }
            case SceneTransitionKind.WipeLeft:
            {
                var revealWidth = width * TimelineMath.Clamp(progress, 0d, 1d);
                using (context.PushClip(new Rect(0, 0, revealWidth, height)))
                {
                    context.DrawImage(next, currentRect);
                }
                break;
            }
            case SceneTransitionKind.None:
            default:
                break;
        }

        return bitmap;
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

    private readonly record struct PlaybackFrame(
        SceneModel Scene,
        double LocalTime,
        SceneModel? NextScene = null,
        double NextLocalTime = 0d,
        double TransitionProgress = 0d)
    {
        public bool HasTransition => NextScene is not null && TransitionProgress > 0d;
    }
}
