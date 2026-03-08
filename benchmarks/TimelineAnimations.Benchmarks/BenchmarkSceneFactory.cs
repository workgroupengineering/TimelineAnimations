using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;
using TimelineAnimations.Rendering.Models;
using TimelineAnimations.Rendering.Services;

namespace TimelineAnimations.Benchmarks;

internal sealed class BenchmarkSceneSet
{
    public required TimelineDocument SmallDocument { get; init; }

    public required TimelineDocument MediumDocument { get; init; }

    public required TimelineDocument LargeDocument { get; init; }

    public required SceneRenderRequest SmallRequest { get; init; }

    public required SceneRenderRequest MediumRequest { get; init; }

    public required SceneRenderRequest LargeRequest { get; init; }

    public required SceneRenderRequest NativeSmallRequest { get; init; }

    public required SceneRenderRequest NativeMediumRequest { get; init; }

    public required SceneRenderRequest NativeLargeRequest { get; init; }

    public required SceneRenderRequest NativeBlendMediumRequest { get; init; }

    public required SceneRenderRequest NativeBlendLargeRequest { get; init; }

    public required SceneRenderRequest NativeEffectsMediumRequest { get; init; }

    public required SceneRenderRequest NativeEffectsLargeRequest { get; init; }

    public required TimelineLayer SmallSampleLayer { get; init; }

    public required TimelineLayer MediumSampleLayer { get; init; }

    public required TimelineLayer LargeSampleLayer { get; init; }
}

internal static class BenchmarkSceneFactory
{
    public static BenchmarkSceneSet Create()
    {
        var small = SampleProjectFactory.Create();
        var medium = DocumentSerializer.Clone(small);
        ExpandDocumentLayers(medium, copies: 5);
        var large = DocumentSerializer.Clone(small);
        ExpandDocumentLayers(large, copies: 12);
        var nativeSmall = CreateNativeRenderDocument(small, copies: 1);
        var nativeMedium = CreateNativeRenderDocument(small, copies: 5);
        var nativeLarge = CreateNativeRenderDocument(small, copies: 12);
        var nativeBlendMedium = CreateNativeBlendRenderDocument(small, copies: 5);
        var nativeBlendLarge = CreateNativeBlendRenderDocument(small, copies: 12);
        var nativeEffectsMedium = CreateNativeEffectRenderDocument(small, copies: 5);
        var nativeEffectsLarge = CreateNativeEffectRenderDocument(small, copies: 12);

        return new BenchmarkSceneSet
        {
            SmallDocument = small,
            MediumDocument = medium,
            LargeDocument = large,
            SmallRequest = BuildRequest(small),
            MediumRequest = BuildRequest(medium),
            LargeRequest = BuildRequest(large),
            NativeSmallRequest = BuildRequest(nativeSmall),
            NativeMediumRequest = BuildRequest(nativeMedium),
            NativeLargeRequest = BuildRequest(nativeLarge),
            NativeBlendMediumRequest = BuildRequest(nativeBlendMedium),
            NativeBlendLargeRequest = BuildRequest(nativeBlendLarge),
            NativeEffectsMediumRequest = BuildRequest(nativeEffectsMedium),
            NativeEffectsLargeRequest = BuildRequest(nativeEffectsLarge),
            SmallSampleLayer = SelectSampleLayer(small),
            MediumSampleLayer = SelectSampleLayer(medium),
            LargeSampleLayer = SelectSampleLayer(large)
        };
    }

    private static SceneRenderRequest BuildRequest(TimelineDocument document)
    {
        return SceneRenderRequestBuilder.Build(
            document,
            document.Layers,
            time: 1.5d,
            duration: document.Duration,
            frameRate: GetFrameRate(document),
            canvasWidth: document.CanvasWidth,
            canvasHeight: document.CanvasHeight,
            transparentStageBackground: document.TransparentStageBackground,
            backgroundFrom: document.BackgroundFrom,
            backgroundTo: document.BackgroundTo);
    }

    private static void ExpandDocumentLayers(TimelineDocument document, int copies)
    {
        if (copies <= 1 || document.Layers.Count == 0)
        {
            return;
        }

        var originals = DocumentSerializer.Clone(document.Layers);
        var maxZ = document.Layers.Count == 0 ? 0 : document.Layers.Max(static item => item.ZIndex);

        for (var copyIndex = 1; copyIndex < copies; copyIndex++)
        {
            var clonedBatch = DocumentSerializer.Clone(originals);
            var idMap = new Dictionary<Guid, Guid>(clonedBatch.Count);
            for (var layerIndex = 0; layerIndex < clonedBatch.Count; layerIndex++)
            {
                var clone = clonedBatch[layerIndex];
                var oldId = clone.Id;
                var newId = Guid.NewGuid();
                idMap[oldId] = newId;
                clone.Id = newId;
                clone.Name = $"{clone.Name} {copyIndex + 1}";
                clone.ZIndex = clone.ZIndex + ((maxZ + 1) * copyIndex);
            }

            for (var layerIndex = 0; layerIndex < clonedBatch.Count; layerIndex++)
            {
                var clone = clonedBatch[layerIndex];
                clone.ParentLayerId = Remap(idMap, clone.ParentLayerId);
                clone.GuidedByLayerId = Remap(idMap, clone.GuidedByLayerId);
                clone.TransformParenting.ParentLayerId = Remap(idMap, clone.TransformParenting.ParentLayerId);
            }

            document.Layers.AddRange(clonedBatch);
        }
    }

    private static TimelineDocument CreateNativeRenderDocument(TimelineDocument source, int copies)
    {
        var document = DocumentSerializer.Clone(source);
        document.LibraryItems.Clear();
        document.MediaAssets.Clear();

        var filteredLayers = new List<TimelineLayer>();
        for (var index = 0; index < document.Layers.Count; index++)
        {
            var layer = document.Layers[index];
            if (layer.Kind is not (LayerKind.Rectangle or LayerKind.Ellipse or LayerKind.Path or LayerKind.Text))
            {
                continue;
            }

            layer.SourceLibraryItemId = null;
            layer.ParentLayerId = null;
            layer.GuidedByLayerId = null;
            layer.TransformParenting = new TransformParentingSettings();
            layer.Warp = new LayerWarpSettings();
            layer.Rig = new LayerRigSettings();
            layer.Media = new LayerMediaSettings();
            layer.Compositing = new LayerCompositeSettings
            {
                BlendMode = layer.Compositing.BlendMode is LayerBlendMode.Normal or LayerBlendMode.Layer
                    ? layer.Compositing.BlendMode
                    : LayerBlendMode.Normal
            };
            filteredLayers.Add(layer);
        }

        document.Layers = filteredLayers;
        ExpandDocumentLayers(document, copies);
        return document;
    }

    private static TimelineDocument CreateNativeBlendRenderDocument(TimelineDocument source, int copies)
    {
        var document = CreateNativeRenderDocument(source, copies);
        LayerBlendMode[] supportedBlendModes =
        [
            LayerBlendMode.Normal,
            LayerBlendMode.Add,
            LayerBlendMode.Screen,
            LayerBlendMode.Multiply,
            LayerBlendMode.Overlay,
            LayerBlendMode.Darken,
            LayerBlendMode.Lighten,
            LayerBlendMode.Difference,
            LayerBlendMode.HardLight
        ];

        for (var index = 0; index < document.Layers.Count; index++)
        {
            var layer = document.Layers[index];
            layer.Compositing.BlendMode = supportedBlendModes[index % supportedBlendModes.Length];
        }

        return document;
    }

    private static TimelineDocument CreateNativeEffectRenderDocument(TimelineDocument source, int copies)
    {
        var document = CreateNativeRenderDocument(source, copies);

        for (var index = 0; index < document.Layers.Count; index++)
        {
            var compositing = document.Layers[index].Compositing;
            switch (index % 4)
            {
                case 0:
                    compositing.BlurRadius = 12d;
                    compositing.BlurQuality = LayerFilterQuality.Medium;
                    break;
                case 1:
                    compositing.ShadowBlur = 14d;
                    compositing.ShadowOpacity = 0.35d;
                    compositing.ShadowOffsetX = 12d;
                    compositing.ShadowOffsetY = 6d;
                    compositing.ShadowQuality = LayerFilterQuality.Medium;
                    compositing.ShadowColor = "#0B1424";
                    break;
                case 2:
                    compositing.GlowSize = 18d;
                    compositing.GlowOpacity = 0.42d;
                    compositing.GlowQuality = LayerFilterQuality.Medium;
                    compositing.GlowColor = "#61E6FF";
                    break;
                default:
                    compositing.BlurRadius = 10d;
                    compositing.BlurQuality = LayerFilterQuality.Low;
                    compositing.ShadowBlur = 10d;
                    compositing.ShadowOpacity = 0.28d;
                    compositing.ShadowOffsetX = 8d;
                    compositing.ShadowOffsetY = 4d;
                    compositing.ShadowQuality = LayerFilterQuality.Low;
                    compositing.ShadowColor = "#09111F";
                    compositing.GlowSize = 14d;
                    compositing.GlowOpacity = 0.26d;
                    compositing.GlowQuality = LayerFilterQuality.Low;
                    compositing.GlowColor = "#FFD166";
                    break;
            }
        }

        return document;
    }

    private static Guid? Remap(IReadOnlyDictionary<Guid, Guid> idMap, Guid? sourceId)
    {
        if (sourceId is not Guid value || !idMap.TryGetValue(value, out var remapped))
        {
            return null;
        }

        return remapped;
    }

    private static TimelineLayer SelectSampleLayer(TimelineDocument document)
    {
        return document.Layers.First(static layer => layer.Kind is LayerKind.Rectangle or LayerKind.Path or LayerKind.Text);
    }

    private static double GetFrameRate(TimelineDocument document)
    {
        return document.Scenes.Count > 0 ? document.Scenes[0].FrameRate : 24d;
    }
}
