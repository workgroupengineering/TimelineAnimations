using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class SceneTimelineService
{
    public static void EnsureTimelineMetadata(SceneModel scene, int totalFrames)
    {
        if (totalFrames <= 0)
        {
            scene.InFrame = 0;
            scene.OutFrame = 0;
            scene.WorkAreaStartFrame = 0;
            scene.WorkAreaEndFrame = 0;
            return;
        }

        var lastFrame = totalFrames - 1;
        if (scene.OutFrame <= 0 && scene.WorkAreaEndFrame <= 0 && totalFrames > 1)
        {
            scene.InFrame = 0;
            scene.OutFrame = lastFrame;
            scene.WorkAreaStartFrame = 0;
            scene.WorkAreaEndFrame = lastFrame;
        }
        else
        {
            scene.InFrame = Math.Clamp(scene.InFrame, 0, lastFrame);
            scene.OutFrame = Math.Clamp(scene.OutFrame, scene.InFrame, lastFrame);
            scene.WorkAreaStartFrame = Math.Clamp(scene.WorkAreaStartFrame, scene.InFrame, scene.OutFrame);
            scene.WorkAreaEndFrame = Math.Clamp(scene.WorkAreaEndFrame, scene.WorkAreaStartFrame, scene.OutFrame);
        }

        if (scene.OutgoingTransition.Duration < 0d)
        {
            scene.OutgoingTransition.Duration = 0d;
        }

        foreach (var marker in scene.Markers)
        {
            marker.Frame = Math.Clamp(marker.Frame, 0, lastFrame);
            marker.Name = marker.Name.Trim();
            marker.Notes = marker.Notes.Trim();
            marker.Color = NormalizeMarkerColor(marker.Color);
        }

        scene.Markers.RemoveAll(static marker => string.IsNullOrWhiteSpace(marker.Name));
        scene.Markers.Sort(static (left, right) =>
        {
            var frameCompare = left.Frame.CompareTo(right.Frame);
            return frameCompare != 0 ? frameCompare : string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        });
    }

    public static bool AddOrUpdateMarker(
        SceneModel scene,
        Guid? markerId,
        int frame,
        string name,
        SceneMarkerKind kind,
        string notes,
        string color,
        int totalFrames)
    {
        EnsureTimelineMetadata(scene, totalFrames);
        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return false;
        }

        var normalizedFrame = Math.Clamp(frame, 0, Math.Max(0, totalFrames - 1));
        var marker = markerId is Guid id
            ? scene.Markers.FirstOrDefault(item => item.Id == id)
            : null;
        if (marker is null)
        {
            marker = new SceneMarkerModel();
            scene.Markers.Add(marker);
        }

        marker.Frame = normalizedFrame;
        marker.Name = normalizedName;
        marker.Kind = kind;
        marker.Notes = notes.Trim();
        marker.Color = NormalizeMarkerColor(color);
        EnsureTimelineMetadata(scene, totalFrames);
        return true;
    }

    public static bool RemoveMarker(SceneModel scene, Guid markerId)
    {
        return scene.Markers.RemoveAll(marker => marker.Id == markerId) > 0;
    }

    public static SceneMarkerModel? GetMarker(SceneModel scene, Guid? markerId)
    {
        return markerId is Guid id
            ? scene.Markers.FirstOrDefault(marker => marker.Id == id)
            : null;
    }

    public static SceneMarkerModel? GetMarkerAtFrame(SceneModel scene, int frame)
    {
        return scene.Markers.FirstOrDefault(marker => marker.Frame == frame);
    }

    public static IReadOnlyList<SceneMarkerModel> GetMarkersAtFrame(SceneModel scene, int frame)
    {
        return
        [
            .. scene.Markers
                .Where(marker => marker.Frame == frame)
                .OrderBy(marker => marker.Kind)
                .ThenBy(marker => marker.Name, StringComparer.OrdinalIgnoreCase)
        ];
    }

    public static void SetWorkArea(SceneModel scene, int startFrame, int endFrame, int totalFrames)
    {
        EnsureTimelineMetadata(scene, totalFrames);
        var normalizedStart = Math.Clamp(Math.Min(startFrame, endFrame), scene.InFrame, scene.OutFrame);
        var normalizedEnd = Math.Clamp(Math.Max(startFrame, endFrame), normalizedStart, scene.OutFrame);
        scene.WorkAreaStartFrame = normalizedStart;
        scene.WorkAreaEndFrame = normalizedEnd;
    }

    public static void ClearWorkArea(SceneModel scene, int totalFrames)
    {
        EnsureTimelineMetadata(scene, totalFrames);
        scene.WorkAreaStartFrame = scene.InFrame;
        scene.WorkAreaEndFrame = scene.OutFrame;
    }

    public static void SetInPoint(SceneModel scene, int frame, int totalFrames)
    {
        EnsureTimelineMetadata(scene, totalFrames);
        scene.InFrame = Math.Clamp(frame, 0, scene.OutFrame);
        scene.WorkAreaStartFrame = Math.Max(scene.WorkAreaStartFrame, scene.InFrame);
    }

    public static void SetOutPoint(SceneModel scene, int frame, int totalFrames)
    {
        EnsureTimelineMetadata(scene, totalFrames);
        scene.OutFrame = Math.Clamp(frame, scene.InFrame, Math.Max(0, totalFrames - 1));
        scene.WorkAreaEndFrame = Math.Min(scene.WorkAreaEndFrame, scene.OutFrame);
    }

    public static (int StartFrame, int EndFrame) GetPlaybackRange(SceneModel scene, int totalFrames, bool useWorkArea)
    {
        EnsureTimelineMetadata(scene, totalFrames);
        return useWorkArea
            ? (scene.WorkAreaStartFrame, scene.WorkAreaEndFrame)
            : (scene.InFrame, scene.OutFrame);
    }

    public static double GetTransitionOverlap(SceneModel scene, SceneModel? nextScene)
    {
        if (nextScene is null || scene.OutgoingTransition.Kind == SceneTransitionKind.None)
        {
            return 0d;
        }

        return Math.Min(
            Math.Max(0d, scene.OutgoingTransition.Duration),
            Math.Min(Math.Max(0.05d, scene.Duration), Math.Max(0.05d, nextScene.Duration)));
    }

    public static string NormalizeMarkerColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return "#57C9FF";
        }

        var trimmed = color.Trim();
        return trimmed.StartsWith("#", StringComparison.Ordinal) ? trimmed : $"#{trimmed}";
    }
}
