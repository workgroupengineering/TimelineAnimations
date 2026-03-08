using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.Services;

public static class MediaAssetRenderService
{
    public static MediaFrameModel? GetFrame(MediaAsset asset, double mediaTime)
    {
        return TimelineAnimations.Rendering.Avalonia.Services.MediaAssetRenderService.GetFrame(asset, mediaTime);
    }

    public static Bitmap? GetFrameBitmap(MediaAsset asset, double mediaTime)
    {
        return TimelineAnimations.Rendering.Avalonia.Services.MediaAssetRenderService.GetFrameBitmap(asset, mediaTime);
    }

    public static void DrawVideoFrame(
        DrawingContext context,
        Rect rect,
        LayerSnapshot snapshot,
        MediaAsset asset,
        double mediaTime,
        double opacity,
        Color fallbackFill,
        Color fallbackAccent,
        bool renderAsMask)
    {
        TimelineAnimations.Rendering.Avalonia.Services.MediaAssetRenderService.DrawVideoFrame(
            context,
            rect,
            snapshot,
            asset,
            mediaTime,
            opacity,
            fallbackFill,
            fallbackAccent,
            renderAsMask);
    }

    public static void DrawSyntheticVideoFrame(
        DrawingContext context,
        Rect rect,
        LayerSnapshot snapshot,
        string label,
        string fill,
        string accent,
        double opacity,
        bool renderAsMask)
    {
        TimelineAnimations.Rendering.Avalonia.Services.MediaAssetRenderService.DrawSyntheticVideoFrame(
            context,
            rect,
            snapshot,
            label,
            fill,
            accent,
            opacity,
            renderAsMask);
    }
}
