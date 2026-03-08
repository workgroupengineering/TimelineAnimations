using System.Numerics;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class LayerParentingService
{
    private const double Tolerance = 0.0001d;

    public static void Normalize(IList<TimelineLayer> layers)
    {
        if (layers.Count == 0)
        {
            return;
        }

        var byId = BuildLookup(layers, layers.Count);
        for (var index = 0; index < layers.Count; index++)
        {
            var layer = layers[index];
            var parentId = layer.TransformParenting.ParentLayerId;
            if (parentId is not Guid resolvedParentId ||
                !byId.ContainsKey(resolvedParentId) ||
                resolvedParentId == layer.Id)
            {
                layer.TransformParenting.ParentLayerId = null;
                continue;
            }

            if (CreatesCycle(byId, layer.Id, resolvedParentId))
            {
                layer.TransformParenting.ParentLayerId = null;
            }
        }
    }

    public static bool CanSetParent(IReadOnlyList<TimelineLayer> layers, Guid layerId, Guid? parentLayerId)
    {
        if (parentLayerId is null)
        {
            return true;
        }

        if (layerId == parentLayerId.Value)
        {
            return false;
        }

        var byId = BuildLookup(layers, layers.Count);
        return byId.TryGetValue(parentLayerId.Value, out var parent) &&
               !CreatesCycle(byId, layerId, parent.Id);
    }

    public static void BindToParent(
        TimelineLayer child,
        TimelineLayer? parent,
        LayerSnapshot childSnapshot,
        LayerSnapshot? parentSnapshot)
    {
        if (parent is null || parentSnapshot is null)
        {
            child.TransformParenting = new TransformParentingSettings();
            return;
        }

        child.TransformParenting = new TransformParentingSettings
        {
            ParentLayerId = parent.Id,
            BindParentX = parentSnapshot.Value.X,
            BindParentY = parentSnapshot.Value.Y,
            BindParentScaleX = NormalizeScale(parentSnapshot.Value.ScaleX),
            BindParentScaleY = NormalizeScale(parentSnapshot.Value.ScaleY),
            BindParentRotation = parentSnapshot.Value.Rotation,
            BindParentOpacity = Math.Max(Tolerance, parentSnapshot.Value.Opacity),
            BindChildX = childSnapshot.X,
            BindChildY = childSnapshot.Y
        };
    }

    public static IReadOnlyDictionary<Guid, LayerSnapshot?> BuildWorldSnapshots(
        IReadOnlyList<TimelineLayer> layers,
        double time,
        double frameRate,
        int totalFrames)
    {
        if (layers.Count == 0)
        {
            return new Dictionary<Guid, LayerSnapshot?>();
        }

        Dictionary<Guid, TimelineLayer> byId;
        if (layers is IList<TimelineLayer> mutableLayers)
        {
            Normalize(mutableLayers);
            byId = BuildLookup(mutableLayers, mutableLayers.Count);
        }
        else
        {
            var normalizedLayers = new List<TimelineLayer>(layers.Count);
            for (var index = 0; index < layers.Count; index++)
            {
                normalizedLayers.Add(layers[index]);
            }

            Normalize(normalizedLayers);
            byId = BuildLookup(normalizedLayers, normalizedLayers.Count);
        }

        var cache = new Dictionary<Guid, LayerSnapshot?>(layers.Count);
        var visiting = new HashSet<Guid>();

        for (var index = 0; index < layers.Count; index++)
        {
            Resolve(layers[index], byId, time, frameRate, totalFrames, cache, visiting);
        }

        return cache;
    }

    public static LayerSnapshot? SampleWorldSnapshot(
        IReadOnlyList<TimelineLayer> layers,
        TimelineLayer layer,
        double time,
        double frameRate,
        int totalFrames)
    {
        return BuildWorldSnapshots(layers, time, frameRate, totalFrames).GetValueOrDefault(layer.Id);
    }

    public static LayerSnapshot ApplyParentTransform(
        LayerSnapshot localSnapshot,
        TransformParentingSettings settings,
        LayerSnapshot parentSnapshot)
    {
        var scaleRatioX = NormalizeScale(parentSnapshot.ScaleX) / NormalizeScale(settings.BindParentScaleX);
        var scaleRatioY = NormalizeScale(parentSnapshot.ScaleY) / NormalizeScale(settings.BindParentScaleY);
        var angleDelta = parentSnapshot.Rotation - settings.BindParentRotation;
        var opacityRatio = parentSnapshot.Opacity / Math.Max(Tolerance, settings.BindParentOpacity);

        var bindOffset = new Vector2(
            (float)(settings.BindChildX - settings.BindParentX),
            (float)(settings.BindChildY - settings.BindParentY));
        var childDelta = new Vector2(
            (float)(localSnapshot.X - settings.BindChildX),
            (float)(localSnapshot.Y - settings.BindChildY));
        var localVector = bindOffset + childDelta;
        var scaledVector = new Vector2(
            localVector.X * (float)scaleRatioX,
            localVector.Y * (float)scaleRatioY);
        var rotatedVector = Vector2.Transform(
            scaledVector,
            Matrix3x2.CreateRotation((float)(angleDelta * Math.PI / 180d)));

        return localSnapshot with
        {
            X = parentSnapshot.X + rotatedVector.X,
            Y = parentSnapshot.Y + rotatedVector.Y,
            Width = Math.Max(1d, localSnapshot.Width * Math.Abs(scaleRatioX)),
            Height = Math.Max(1d, localSnapshot.Height * Math.Abs(scaleRatioY)),
            ScaleX = localSnapshot.ScaleX * scaleRatioX,
            ScaleY = localSnapshot.ScaleY * scaleRatioY,
            Rotation = localSnapshot.Rotation + angleDelta,
            Opacity = TimelineMath.Clamp(localSnapshot.Opacity * opacityRatio, 0d, 1d)
        };
    }

    private static LayerSnapshot? Resolve(
        TimelineLayer layer,
        IReadOnlyDictionary<Guid, TimelineLayer> byId,
        double time,
        double frameRate,
        int totalFrames,
        IDictionary<Guid, LayerSnapshot?> cache,
        ISet<Guid> visiting)
    {
        if (cache.TryGetValue(layer.Id, out var cached))
        {
            return cached;
        }

        if (!visiting.Add(layer.Id))
        {
            cache[layer.Id] = null;
            return null;
        }

        try
        {
            var localSnapshot = FrameTimelineService.SampleLayer(layer, time, frameRate, totalFrames) ??
                                TimelineInterpolationService.SampleLayer(layer, time);
            var parentId = layer.TransformParenting.ParentLayerId;
            if (parentId is not Guid resolvedParentId ||
                !byId.TryGetValue(resolvedParentId, out var parentLayer))
            {
                cache[layer.Id] = localSnapshot;
                return localSnapshot;
            }

            var parentSnapshot = Resolve(parentLayer, byId, time, frameRate, totalFrames, cache, visiting);
            if (parentSnapshot is null)
            {
                cache[layer.Id] = localSnapshot;
                return localSnapshot;
            }

            var worldSnapshot = ApplyParentTransform(localSnapshot, layer.TransformParenting, parentSnapshot.Value);
            cache[layer.Id] = worldSnapshot;
            return worldSnapshot;
        }
        finally
        {
            visiting.Remove(layer.Id);
        }
    }

    private static double NormalizeScale(double value)
    {
        return Math.Abs(value) < Tolerance ? (value < 0d ? -1d : 1d) : value;
    }

    private static Dictionary<Guid, TimelineLayer> BuildLookup(IEnumerable<TimelineLayer> layers, int capacity)
    {
        var byId = new Dictionary<Guid, TimelineLayer>(capacity);
        foreach (var layer in layers)
        {
            byId[layer.Id] = layer;
        }

        return byId;
    }

    private static bool CreatesCycle(IReadOnlyDictionary<Guid, TimelineLayer> byId, Guid sourceId, Guid parentId)
    {
        var currentId = parentId;
        var visited = new HashSet<Guid> { sourceId };
        while (true)
        {
            if (!visited.Add(currentId))
            {
                return true;
            }

            if (!byId.TryGetValue(currentId, out var current) ||
                current.TransformParenting.ParentLayerId is not Guid nextId)
            {
                return false;
            }

            currentId = nextId;
        }
    }
}
