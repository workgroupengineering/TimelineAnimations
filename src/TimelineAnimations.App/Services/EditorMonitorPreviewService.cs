using Avalonia.Media.Imaging;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.Services;

public static class EditorMonitorPreviewService
{
    public static Bitmap RenderProgramMonitor(
        TimelineDocument document,
        double time,
        bool playAllScenes,
        int width = 360,
        int height = 204)
    {
        return FrameExportService.RenderFrameBitmap(
            document,
            time,
            width,
            height,
            playAllScenes: playAllScenes);
    }

    public static SourceMonitorPreviewResult BuildSourcePreview(
        TimelineDocument document,
        TimelineLayer? selectedLayer,
        LibraryItem? selectedLibraryItem,
        MediaAsset? selectedMediaAsset,
        double sourceTime,
        int width = 360,
        int height = 204)
    {
        if (selectedMediaAsset is not null)
        {
            var mediaDocument = BuildMediaPreviewDocument(document, selectedMediaAsset);
            return new SourceMonitorPreviewResult(
                FrameExportService.RenderFrameBitmap(mediaDocument, TimelineMath.Clamp(sourceTime, 0d, mediaDocument.Duration), width, height),
                selectedMediaAsset.Name,
                $"{selectedMediaAsset.Kind} • {selectedMediaAsset.Duration:0.00}s • {selectedMediaAsset.Width}×{selectedMediaAsset.Height}",
                Math.Max(0.05d, selectedMediaAsset.Duration));
        }

        if (selectedLibraryItem is not null)
        {
            var libraryDocument = BuildLibraryPreviewDocument(document, selectedLibraryItem);
            return new SourceMonitorPreviewResult(
                FrameExportService.RenderFrameBitmap(libraryDocument, TimelineMath.Clamp(sourceTime, 0d, libraryDocument.Duration), width, height),
                selectedLibraryItem.Name,
                $"{selectedLibraryItem.SymbolKind} • {selectedLibraryItem.Duration:0.00}s • {selectedLibraryItem.FrameRate:0.#} fps",
                Math.Max(0.05d, selectedLibraryItem.Duration));
        }

        if (selectedLayer is not null)
        {
            var layerDocument = BuildLayerPreviewDocument(document, selectedLayer);
            return new SourceMonitorPreviewResult(
                FrameExportService.RenderFrameBitmap(layerDocument, TimelineMath.Clamp(sourceTime, 0d, layerDocument.Duration), width, height),
                selectedLayer.Name,
                $"{selectedLayer.Kind} • isolated source preview",
                Math.Max(0.05d, layerDocument.Duration));
        }

        return new SourceMonitorPreviewResult(null, "No source", "Select a stage layer, library symbol, or media asset.", 0d);
    }

    private static TimelineDocument BuildLayerPreviewDocument(TimelineDocument document, TimelineLayer layer)
    {
        var clone = DocumentSerializer.Clone(document);
        SceneEditingService.EnsureScenes(clone);
        var scene = clone.Scenes.FirstOrDefault(item => item.Id == clone.ActiveSceneId) ?? clone.Scenes[0];
        scene.Layers = [DocumentSerializer.Clone(layer)];
        clone.Layers = scene.Layers;
        clone.ActiveSceneId = scene.Id;
        return clone;
    }

    private static TimelineDocument BuildLibraryPreviewDocument(TimelineDocument document, LibraryItem item)
    {
        var clone = DocumentSerializer.Clone(document);
        SceneEditingService.EnsureScenes(clone);
        var scene = clone.Scenes.FirstOrDefault(itemScene => itemScene.Id == clone.ActiveSceneId) ?? clone.Scenes[0];
        var previewLayer = TimelineEditingService.CreateLayerFromLibraryItem(item, 240d, 120d, 0);
        scene.Layers = [previewLayer];
        scene.Duration = Math.Max(0.1d, item.Duration);
        scene.FrameRate = Math.Max(1d, item.FrameRate);
        clone.Duration = scene.Duration;
        clone.Layers = scene.Layers;
        clone.ActiveSceneId = scene.Id;
        return clone;
    }

    private static TimelineDocument BuildMediaPreviewDocument(TimelineDocument document, MediaAsset asset)
    {
        var clone = DocumentSerializer.Clone(document);
        SceneEditingService.EnsureScenes(clone);
        var scene = clone.Scenes.FirstOrDefault(item => item.Id == clone.ActiveSceneId) ?? clone.Scenes[0];
        scene.Duration = Math.Max(0.1d, asset.Duration);
        scene.FrameRate = Math.Max(1d, asset.FrameRate > 0d ? asset.FrameRate : scene.FrameRate);
        scene.Layers = asset.Kind switch
        {
            MediaAssetKind.Audio => BuildAudioPreviewLayers(asset),
            _ => [TimelineEditingService.CreateVideoLayer(asset, 180d, 90d, 0, 0d)]
        };
        clone.Duration = scene.Duration;
        clone.Layers = scene.Layers;
        clone.ActiveSceneId = scene.Id;
        return clone;
    }

    private static List<TimelineLayer> BuildAudioPreviewLayers(MediaAsset asset)
    {
        var panel = TimelineEditingService.CreateLayer(
            LayerKind.Rectangle,
            asset.Name,
            asset.PreviewFill,
            string.Empty,
            120d,
            112d,
            0);
        panel.Defaults.Width = 720d;
        panel.Defaults.Height = 210d;
        panel.Style.CornerRadius = 26d;
        panel.Style.Stroke = asset.PreviewAccent;

        var caption = TimelineEditingService.CreateLayer(
            LayerKind.Text,
            $"{asset.Name} Caption",
            "#F7F5ED",
            asset.Name,
            154d,
            168d,
            1);
        caption.Defaults.Width = 620d;
        caption.Defaults.Height = 84d;
        caption.Style.FontSize = 38d;
        caption.Style.Stroke = asset.PreviewAccent;

        return [panel, caption];
    }
}

public sealed record SourceMonitorPreviewResult(
    Bitmap? Bitmap,
    string Title,
    string Summary,
    double Duration);
