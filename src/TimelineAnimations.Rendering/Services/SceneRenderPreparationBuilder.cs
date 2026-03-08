using System.Runtime.CompilerServices;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;
using TimelineAnimations.Rendering.Models;

namespace TimelineAnimations.Rendering.Services;

public static class SceneRenderPreparationBuilder
{
    private static readonly ConditionalWeakTable<TimelineDocument, DocumentAssetLookupCache> s_lookupCache = new();

    public static SceneRenderPreparation Build(
        TimelineDocument document,
        IReadOnlyList<TimelineLayer> layers,
        double time,
        double duration,
        double frameRate)
    {
        var lookupCache = s_lookupCache.GetValue(document, static _ => new DocumentAssetLookupCache());
        var libraryLookup = lookupCache.GetOrBuildLibraryLookup(document.LibraryItems);
        var mediaLookup = lookupCache.GetOrBuildMediaLookup(document.MediaAssets);
        var totalFrames = FrameTimelineService.GetTotalFrames(duration, frameRate);
        var samples = SymbolRenderService.BuildRenderSamples(
            layers,
            libraryLookup,
            mediaLookup,
            time,
            frameRate,
            totalFrames,
            out var activeCamera);
        return new SceneRenderPreparation(libraryLookup, mediaLookup, samples, activeCamera);
    }

    private sealed class DocumentAssetLookupCache
    {
        private IReadOnlyList<LibraryItem>? _libraryItems;
        private int _libraryCount;
        private int _librarySignature;
        private Dictionary<Guid, LibraryItem>? _libraryLookup;

        private IReadOnlyList<MediaAsset>? _mediaAssets;
        private int _mediaCount;
        private int _mediaSignature;
        private Dictionary<Guid, MediaAsset>? _mediaLookup;

        public Dictionary<Guid, LibraryItem> GetOrBuildLibraryLookup(IReadOnlyList<LibraryItem> items)
        {
            var signature = ComputeLibrarySignature(items);
            if (_libraryLookup is not null &&
                ReferenceEquals(_libraryItems, items) &&
                _libraryCount == items.Count &&
                _librarySignature == signature)
            {
                return _libraryLookup;
            }

            _libraryItems = items;
            _libraryCount = items.Count;
            _librarySignature = signature;
            _libraryLookup = BuildLibraryLookup(items);
            return _libraryLookup;
        }

        public Dictionary<Guid, MediaAsset> GetOrBuildMediaLookup(IReadOnlyList<MediaAsset> items)
        {
            var signature = ComputeMediaSignature(items);
            if (_mediaLookup is not null &&
                ReferenceEquals(_mediaAssets, items) &&
                _mediaCount == items.Count &&
                _mediaSignature == signature)
            {
                return _mediaLookup;
            }

            _mediaAssets = items;
            _mediaCount = items.Count;
            _mediaSignature = signature;
            _mediaLookup = BuildMediaLookup(items);
            return _mediaLookup;
        }
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

    private static int ComputeLibrarySignature(IReadOnlyList<LibraryItem> items)
    {
        var hash = new HashCode();
        hash.Add(items.Count);
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            hash.Add(item.Id);
        }

        return hash.ToHashCode();
    }

    private static int ComputeMediaSignature(IReadOnlyList<MediaAsset> items)
    {
        var hash = new HashCode();
        hash.Add(items.Count);
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            hash.Add(item.Id);
        }

        return hash.ToHashCode();
    }
}
