using System.Runtime.CompilerServices;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public sealed record LayerHierarchyEntry(
    TimelineLayer Layer,
    int Depth,
    bool HasChildren,
    bool IsVisibleInTree,
    bool IsMutedByHierarchy,
    bool IsVisibleBySolo);

public static class LayerHierarchyService
{
    private static readonly ConditionalWeakTable<object, RenderableLayerCacheEntry> s_renderableLayerCache = new();

    public static void Normalize(IList<TimelineLayer> layers)
    {
        if (layers.Count == 0)
        {
            return;
        }

        var ids = new HashSet<Guid>(layers.Count);
        for (var index = 0; index < layers.Count; index++)
        {
            ids.Add(layers[index].Id);
        }

        for (var index = 0; index < layers.Count; index++)
        {
            var layer = layers[index];
            if (layer.ParentLayerId is not Guid parentId || !ids.Contains(parentId) || parentId == layer.Id)
            {
                layer.ParentLayerId = null;
                continue;
            }

            if (CreatesCycle(layers, layer.Id, parentId))
            {
                layer.ParentLayerId = null;
            }
        }
    }

    public static IReadOnlyList<LayerHierarchyEntry> Flatten(IReadOnlyList<TimelineLayer> layers)
    {
        if (layers.Count == 0)
        {
            return [];
        }

        var normalizedLayers = new List<TimelineLayer>(layers);
        normalizedLayers.Sort(static (left, right) => right.ZIndex.CompareTo(left.ZIndex));
        Normalize(normalizedLayers);

        var byParent = new Dictionary<Guid, List<TimelineLayer>>();
        var roots = new List<TimelineLayer>();
        foreach (var layer in normalizedLayers)
        {
            if (layer.ParentLayerId is not Guid parentId)
            {
                roots.Add(layer);
                continue;
            }

            if (!byParent.TryGetValue(parentId, out var children))
            {
                children = [];
                byParent[parentId] = children;
            }

            children.Add(layer);
        }

        var soloActive = normalizedLayers.Any(item => item.IsSolo);
        var flattened = new List<LayerHierarchyEntry>(normalizedLayers.Count);

        foreach (var root in roots)
        {
            Append(flattened, root, 0, parentExpanded: true, parentMuted: false, soloActive, parentSolo: false, byParent);
        }

        return flattened;
    }

    public static IReadOnlyList<TimelineLayer> GetRenderableLayers(IReadOnlyList<TimelineLayer> layers)
    {
        if (layers.Count == 0)
        {
            return [];
        }

        var cacheKey = (object)layers;
        var signature = ComputeRenderableSignature(layers);
        if (s_renderableLayerCache.TryGetValue(cacheKey, out var cacheEntry) &&
            cacheEntry.Signature == signature)
        {
            return cacheEntry.RenderableLayers;
        }

        var renderable = BuildRenderableLayers(layers);
        s_renderableLayerCache.Remove(cacheKey);
        s_renderableLayerCache.Add(cacheKey, new RenderableLayerCacheEntry(signature, renderable));
        return renderable;
    }

    public static bool HasSoloLayers(IReadOnlyList<TimelineLayer> layers)
    {
        return layers.Any(item => item.IsSolo);
    }

    public static bool IsEffectivelyVisible(IReadOnlyList<TimelineLayer> layers, Guid layerId)
    {
        return Flatten(layers).FirstOrDefault(item => item.Layer.Id == layerId)?.IsVisibleInTree ?? false;
    }

    public static bool IsEffectivelyMuted(IReadOnlyList<TimelineLayer> layers, Guid layerId)
    {
        return Flatten(layers).FirstOrDefault(item => item.Layer.Id == layerId)?.IsMutedByHierarchy ?? false;
    }

    public static bool TrySetParent(IList<TimelineLayer> layers, Guid layerId, Guid? parentLayerId)
    {
        Normalize(layers);
        var layer = layers.FirstOrDefault(item => item.Id == layerId);
        if (layer is null)
        {
            return false;
        }

        if (parentLayerId is null)
        {
            layer.ParentLayerId = null;
            return true;
        }

        var parent = layers.FirstOrDefault(item => item.Id == parentLayerId.Value);
        if (parent is null || parent.Id == layer.Id || parent.Kind != LayerKind.Folder || CreatesCycle(layers, layer.Id, parent.Id))
        {
            return false;
        }

        layer.ParentLayerId = parent.Id;
        return true;
    }

    private static void Append(
        List<LayerHierarchyEntry> flattened,
        TimelineLayer layer,
        int depth,
        bool parentExpanded,
        bool parentMuted,
        bool soloActive,
        bool parentSolo,
        IReadOnlyDictionary<Guid, List<TimelineLayer>> byParent)
    {
        byParent.TryGetValue(layer.Id, out var children);
        var hasChildren = children is { Count: > 0 };
        var isVisibleInTree = parentExpanded && layer.IsVisible;
        var isMutedByHierarchy = parentMuted || layer.IsMuted;
        var isVisibleBySolo = !soloActive || parentSolo || layer.IsSolo;
        flattened.Add(new LayerHierarchyEntry(
            layer,
            depth,
            hasChildren,
            isVisibleInTree,
            isMutedByHierarchy,
            isVisibleBySolo));

        if (!hasChildren || !layer.IsExpanded)
        {
            return;
        }

        foreach (var child in children!)
        {
            Append(flattened, child, depth + 1, isVisibleInTree, isMutedByHierarchy, soloActive, parentSolo || layer.IsSolo, byParent);
        }
    }

    private static bool CreatesCycle(IEnumerable<TimelineLayer> layers, Guid sourceId, Guid parentId)
    {
        var currentId = parentId;
        var visited = new HashSet<Guid> { sourceId };
        while (true)
        {
            if (!visited.Add(currentId))
            {
                return true;
            }

            var current = layers.FirstOrDefault(item => item.Id == currentId);
            if (current?.ParentLayerId is not Guid nextId)
            {
                return false;
            }

            currentId = nextId;
        }
    }

    private static List<TimelineLayer> BuildRenderableLayers(IReadOnlyList<TimelineLayer> layers)
    {
        var normalizedLayers = new List<TimelineLayer>(layers);
        normalizedLayers.Sort(static (left, right) => right.ZIndex.CompareTo(left.ZIndex));
        Normalize(normalizedLayers);

        var byParent = new Dictionary<Guid, List<TimelineLayer>>();
        var roots = new List<TimelineLayer>();
        var soloActive = false;

        for (var index = 0; index < normalizedLayers.Count; index++)
        {
            var layer = normalizedLayers[index];
            if (layer.IsSolo)
            {
                soloActive = true;
            }

            if (layer.ParentLayerId is not Guid parentId)
            {
                roots.Add(layer);
                continue;
            }

            if (!byParent.TryGetValue(parentId, out var children))
            {
                children = [];
                byParent[parentId] = children;
            }

            children.Add(layer);
        }

        var renderable = new List<TimelineLayer>(normalizedLayers.Count);
        for (var index = 0; index < roots.Count; index++)
        {
            AppendRenderable(renderable, roots[index], true, false, soloActive, false, byParent);
        }

        renderable.Sort(static (left, right) => left.ZIndex.CompareTo(right.ZIndex));
        return renderable;
    }

    private static void AppendRenderable(
        List<TimelineLayer> renderable,
        TimelineLayer layer,
        bool parentExpanded,
        bool parentMuted,
        bool soloActive,
        bool parentSolo,
        IReadOnlyDictionary<Guid, List<TimelineLayer>> byParent)
    {
        var isVisibleInTree = parentExpanded && layer.IsVisible;
        var isMutedByHierarchy = parentMuted || layer.IsMuted;
        var isVisibleBySolo = !soloActive || parentSolo || layer.IsSolo;
        var hasChildren = byParent.TryGetValue(layer.Id, out var children) && children.Count > 0;

        if (layer.Kind != LayerKind.Folder &&
            isVisibleInTree &&
            !isMutedByHierarchy &&
            isVisibleBySolo)
        {
            renderable.Add(layer);
        }

        if (!hasChildren || !layer.IsExpanded)
        {
            return;
        }

        var childLayers = children!;
        for (var index = 0; index < childLayers.Count; index++)
        {
            AppendRenderable(
                renderable,
                childLayers[index],
                isVisibleInTree,
                isMutedByHierarchy,
                soloActive,
                parentSolo || layer.IsSolo,
                byParent);
        }
    }

    private static int ComputeRenderableSignature(IReadOnlyList<TimelineLayer> layers)
    {
        var hash = new HashCode();
        hash.Add(layers.Count);
        for (var index = 0; index < layers.Count; index++)
        {
            var layer = layers[index];
            hash.Add(layer.Id);
            hash.Add(layer.ParentLayerId);
            hash.Add(layer.Kind);
            hash.Add(layer.ZIndex);
            hash.Add(layer.IsVisible);
            hash.Add(layer.IsMuted);
            hash.Add(layer.IsSolo);
            hash.Add(layer.IsExpanded);
        }

        return hash.ToHashCode();
    }

    private sealed record RenderableLayerCacheEntry(int Signature, IReadOnlyList<TimelineLayer> RenderableLayers);
}
