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
    public static void Normalize(IList<TimelineLayer> layers)
    {
        if (layers.Count == 0)
        {
            return;
        }

        var ids = layers.Select(item => item.Id).ToHashSet();
        foreach (var layer in layers)
        {
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

        var normalizedLayers = layers
            .OrderByDescending(item => item.ZIndex)
            .ToList();
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
        var flattened = Flatten(layers);
        return
        [
            .. flattened
                .Where(item =>
                    item.Layer.Kind != LayerKind.Folder &&
                    item.IsVisibleInTree &&
                    !item.IsMutedByHierarchy &&
                    item.IsVisibleBySolo)
                .Select(item => item.Layer)
        ];
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
}
