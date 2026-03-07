using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class SymbolRenderService
{
    private const int MaxDepth = 8;

    public static IReadOnlyList<RenderableLayerSample> BuildRenderSamples(
        TimelineDocument document,
        IReadOnlyList<TimelineLayer> layers,
        double time,
        double duration,
        double frameRate)
    {
        var libraryLookup = document.LibraryItems.ToDictionary(item => item.Id);
        var mediaLookup = document.MediaAssets.ToDictionary(item => item.Id);
        var samples = new List<RenderableLayerSample>();
        var totalFrames = FrameTimelineService.GetTotalFrames(duration, frameRate);
        var renderableLayers = LayerHierarchyService.GetRenderableLayers(layers);

        foreach (var layer in renderableLayers.OrderBy(item => item.ZIndex))
        {
            AppendLayerSamples(
                samples,
                layer,
                libraryLookup,
                mediaLookup,
                time,
                frameRate,
                totalFrames,
                parentTransform: null,
                depth: 0,
                activePath: []);
        }

        return samples;
    }

    private static void AppendLayerSamples(
        List<RenderableLayerSample> samples,
        TimelineLayer layer,
        IReadOnlyDictionary<Guid, LibraryItem> libraryLookup,
        IReadOnlyDictionary<Guid, MediaAsset> mediaLookup,
        double time,
        double frameRate,
        int totalFrames,
        SymbolTransformContext? parentTransform,
        int depth,
        HashSet<Guid> activePath)
    {
        if (depth > MaxDepth || !layer.IsVisible || layer.IsMuted)
        {
            return;
        }

        if (layer.Kind is LayerKind.Audio or LayerKind.Folder)
        {
            return;
        }

        var snapshot = FrameTimelineService.SampleLayer(layer, time, frameRate, totalFrames);
        if (snapshot is null)
        {
            return;
        }

        var transformed = parentTransform is null
            ? snapshot.Value
            : ApplyParentTransform(snapshot.Value, parentTransform.Value);
        var sourceMediaAssetId = layer.Media.SourceMediaAssetId;
        double? mediaTime = null;

        if (layer.Kind == LayerKind.Video && sourceMediaAssetId is Guid videoAssetId && mediaLookup.TryGetValue(videoAssetId, out var mediaAsset))
        {
            mediaTime = MediaTimelineService.ResolveMediaTime(layer, mediaAsset, time);
            if (mediaTime is null)
            {
                return;
            }
        }

        if (layer.SourceLibraryItemId is not Guid libraryItemId ||
            !libraryLookup.TryGetValue(libraryItemId, out var libraryItem) ||
            !activePath.Add(libraryItemId))
        {
            samples.Add(new RenderableLayerSample
            {
                SourceLayerId = layer.Id,
                SourceLibraryItemId = layer.SourceLibraryItemId,
                SourceMediaAssetId = sourceMediaAssetId,
                Kind = layer.Kind,
                ZIndex = layer.ZIndex,
                Snapshot = transformed,
                MediaTime = mediaTime
            });
            return;
        }

        try
        {
            var nestedLayers = TimelineEditingService.GetSymbolLayers(libraryItem, layer.SymbolButtonState);
            if (nestedLayers.Count == 0)
            {
                return;
            }

            var nestedTime = ResolveNestedTime(layer, libraryItem, time);
            var nestedFrameRate = Math.Max(1, libraryItem.FrameRate);
            var nestedTotalFrames = FrameTimelineService.GetTotalFrames(libraryItem.Duration, nestedFrameRate);
            var templateSnapshot = FrameTimelineService.SampleLayer(
                libraryItem.Template,
                nestedTime,
                nestedFrameRate,
                nestedTotalFrames) ?? TimelineInterpolationService.SampleLayer(libraryItem.Template, nestedTime);

            var transform = new SymbolTransformContext(
                transformed.X,
                transformed.Y,
                transformed.Width,
                transformed.Height,
                templateSnapshot.X,
                templateSnapshot.Y,
                Math.Max(1, templateSnapshot.Width),
                Math.Max(1, templateSnapshot.Height),
                transformed.Opacity,
                transformed.Rotation);

            foreach (var nestedLayer in nestedLayers.OrderBy(item => item.ZIndex))
            {
                AppendLayerSamples(
                    samples,
                    nestedLayer,
                    libraryLookup,
                    mediaLookup,
                    nestedTime,
                    nestedFrameRate,
                    nestedTotalFrames,
                    transform,
                    depth + 1,
                    activePath);
            }
        }
        finally
        {
            activePath.Remove(libraryItemId);
        }
    }

    private static double ResolveNestedTime(TimelineLayer layer, LibraryItem libraryItem, double parentTime)
    {
        var duration = Math.Max(0.1d, libraryItem.Duration);
        var nestedFrameRate = Math.Max(1, libraryItem.FrameRate);

        return layer.SymbolPlaybackMode switch
        {
            SymbolPlaybackMode.IndependentLoop => Mod(parentTime + layer.SymbolPlaybackOffset, duration),
            SymbolPlaybackMode.SingleFrame => FrameTimelineService.FrameToTime(
                Math.Clamp(layer.SymbolLockedFrame, 0, FrameTimelineService.GetTotalFrames(duration, nestedFrameRate) - 1),
                nestedFrameRate),
            _ => TimelineMath.Clamp(parentTime + layer.SymbolPlaybackOffset, 0, duration)
        };
    }

    private static LayerSnapshot ApplyParentTransform(LayerSnapshot snapshot, SymbolTransformContext parent)
    {
        var scaleX = parent.Width / parent.TemplateWidth;
        var scaleY = parent.Height / parent.TemplateHeight;

        return snapshot with
        {
            X = parent.X + ((snapshot.X - parent.TemplateX) * scaleX),
            Y = parent.Y + ((snapshot.Y - parent.TemplateY) * scaleY),
            Width = snapshot.Width * scaleX,
            Height = snapshot.Height * scaleY,
            Rotation = snapshot.Rotation + parent.Rotation,
            Opacity = snapshot.Opacity * parent.Opacity
        };
    }

    private static double Mod(double value, double divisor)
    {
        var remainder = value % divisor;
        return remainder < 0 ? remainder + divisor : remainder;
    }

    private readonly record struct SymbolTransformContext(
        double X,
        double Y,
        double Width,
        double Height,
        double TemplateX,
        double TemplateY,
        double TemplateWidth,
        double TemplateHeight,
        double Opacity,
        double Rotation);
}
