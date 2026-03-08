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
        var totalFrames = FrameTimelineService.GetTotalFrames(duration, frameRate);
        var libraryLookup = BuildLibraryLookup(document.LibraryItems);
        var mediaLookup = BuildMediaLookup(document.MediaAssets);
        return BuildRenderSamples(
            layers,
            libraryLookup,
            mediaLookup,
            time,
            frameRate,
            totalFrames,
            out _);
    }

    public static IReadOnlyList<RenderableLayerSample> BuildRenderSamples(
        IReadOnlyList<TimelineLayer> layers,
        IReadOnlyDictionary<Guid, LibraryItem> libraryLookup,
        IReadOnlyDictionary<Guid, MediaAsset> mediaLookup,
        double time,
        double frameRate,
        int totalFrames,
        out LayerSnapshot? activeCamera)
    {
        var renderableLayers = LayerHierarchyService.GetRenderableLayers(layers);
        var parentedSnapshots = LayerParentingService.BuildWorldSnapshots(layers, time, frameRate, totalFrames);
        var samples = new List<RenderableLayerSample>(renderableLayers.Count);
        var activePath = new HashSet<Guid>();

        for (var index = 0; index < renderableLayers.Count; index++)
        {
            AppendLayerSamples(
                samples,
                renderableLayers[index],
                libraryLookup,
                mediaLookup,
                time,
                frameRate,
                totalFrames,
                parentedSnapshots,
                parentTransform: null,
                componentBindings: null,
                depth: 0,
                activePath);
        }

        samples.Sort(static (left, right) => left.ZIndex.CompareTo(right.ZIndex));
        activeCamera = ResolveActiveCameraSample(samples);
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
        IReadOnlyDictionary<Guid, LayerSnapshot?> worldSnapshots,
        SymbolTransformContext? parentTransform,
        IReadOnlyList<ComponentParameterBinding>? componentBindings,
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

        if (!worldSnapshots.TryGetValue(layer.Id, out var snapshot) || snapshot is null)
        {
            snapshot = FrameTimelineService.SampleLayer(layer, time, frameRate, totalFrames);
        }

        if (snapshot is null)
        {
            return;
        }

        var transformed = parentTransform is null
            ? snapshot.Value
            : ApplyParentTransform(snapshot.Value, parentTransform.Value);
        if (componentBindings is not null)
        {
            transformed = ComponentParameterService.ApplyToSnapshot(layer.Name, transformed, componentBindings);
        }
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
                MediaTime = mediaTime,
                Warp = layer.Warp,
                Rig = layer.Rig
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
            var nestedSnapshots = LayerParentingService.BuildWorldSnapshots(nestedLayers, nestedTime, nestedFrameRate, nestedTotalFrames);
            var templateSnapshot = FrameTimelineService.SampleLayer(
                                   libraryItem.Template,
                                   nestedTime,
                                   nestedFrameRate,
                                   nestedTotalFrames) ??
                               TimelineInterpolationService.SampleLayer(libraryItem.Template, nestedTime);
            var nextComponentBindings = componentBindings;
            if (libraryItem.IsComponent)
            {
                nextComponentBindings = ComponentParameterService.BuildBindings(layer, libraryItem);
            }

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
                transformed.Rotation,
                transformed.RotationX,
                transformed.RotationY,
                transformed.ZDepth);

            var orderedNestedLayers = nestedLayers as List<TimelineLayer> ?? [.. nestedLayers];
            orderedNestedLayers.Sort(static (left, right) => left.ZIndex.CompareTo(right.ZIndex));
            for (var index = 0; index < orderedNestedLayers.Count; index++)
            {
                AppendLayerSamples(
                    samples,
                    orderedNestedLayers[index],
                    libraryLookup,
                    mediaLookup,
                    nestedTime,
                    nestedFrameRate,
                    nestedTotalFrames,
                    nestedSnapshots,
                    transform,
                    nextComponentBindings,
                    depth + 1,
                    activePath);
            }
        }
        finally
        {
            activePath.Remove(libraryItemId);
        }
    }

    private static LayerSnapshot? ResolveActiveCameraSample(IReadOnlyList<RenderableLayerSample> samples)
    {
        for (var index = samples.Count - 1; index >= 0; index--)
        {
            if (samples[index].Role == LayerCompositeRole.Camera)
            {
                return samples[index].Snapshot;
            }
        }

        return null;
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
            RotationX = snapshot.RotationX + parent.RotationX,
            RotationY = snapshot.RotationY + parent.RotationY,
            ZDepth = snapshot.ZDepth + parent.ZDepth,
            Opacity = snapshot.Opacity * parent.Opacity
        };
    }

    private static double Mod(double value, double divisor)
    {
        var remainder = value % divisor;
        return remainder < 0 ? remainder + divisor : remainder;
    }

    private static Dictionary<Guid, LibraryItem> BuildLibraryLookup(IReadOnlyList<LibraryItem> items)
    {
        if (items.Count == 0)
        {
            return [];
        }

        var lookup = new Dictionary<Guid, LibraryItem>(items.Count);
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            lookup[item.Id] = item;
        }

        return lookup;
    }

    private static Dictionary<Guid, MediaAsset> BuildMediaLookup(IReadOnlyList<MediaAsset> items)
    {
        if (items.Count == 0)
        {
            return [];
        }

        var lookup = new Dictionary<Guid, MediaAsset>(items.Count);
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            lookup[item.Id] = item;
        }

        return lookup;
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
        double Rotation,
        double RotationX,
        double RotationY,
        double ZDepth);
}
