using BenchmarkDotNet.Attributes;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;
using TimelineAnimations.Rendering.Models;
using TimelineAnimations.Rendering.Services;

namespace TimelineAnimations.Benchmarks;

[MemoryDiagnoser]
public class AnimationEngineBenchmarks
{
    private BenchmarkSceneSet _sceneSet = null!;
    private double _smallFrameRate;
    private double _mediumFrameRate;
    private double _largeFrameRate;
    private int _smallTotalFrames;
    private int _mediumTotalFrames;
    private int _largeTotalFrames;

    [GlobalSetup]
    public void Setup()
    {
        _sceneSet = BenchmarkSceneFactory.Create();
        _smallFrameRate = GetFrameRate(_sceneSet.SmallDocument);
        _mediumFrameRate = GetFrameRate(_sceneSet.MediumDocument);
        _largeFrameRate = GetFrameRate(_sceneSet.LargeDocument);
        _smallTotalFrames = FrameTimelineService.GetTotalFrames(_sceneSet.SmallDocument.Duration, _smallFrameRate);
        _mediumTotalFrames = FrameTimelineService.GetTotalFrames(_sceneSet.MediumDocument.Duration, _mediumFrameRate);
        _largeTotalFrames = FrameTimelineService.GetTotalFrames(_sceneSet.LargeDocument.Duration, _largeFrameRate);
    }

    [Benchmark(Baseline = true)]
    public LayerSnapshot? FrameTimelineService_SampleLayer_Small()
    {
        return FrameTimelineService.SampleLayer(
            _sceneSet.SmallSampleLayer,
            _sceneSet.SmallRequest.Time,
            _smallFrameRate,
            _smallTotalFrames);
    }

    [Benchmark]
    public IReadOnlyDictionary<Guid, LayerSnapshot?> LayerParentingService_BuildWorldSnapshots_Medium()
    {
        return LayerParentingService.BuildWorldSnapshots(
            _sceneSet.MediumDocument.Layers,
            _sceneSet.MediumRequest.Time,
            _mediumFrameRate,
            _mediumTotalFrames);
    }

    [Benchmark]
    public IReadOnlyDictionary<Guid, LayerSnapshot?> LayerParentingService_BuildWorldSnapshots_Large()
    {
        return LayerParentingService.BuildWorldSnapshots(
            _sceneSet.LargeDocument.Layers,
            _sceneSet.LargeRequest.Time,
            _largeFrameRate,
            _largeTotalFrames);
    }

    [Benchmark]
    public IReadOnlyList<TimelineLayer> LayerHierarchyService_GetRenderableLayers_Medium()
    {
        return LayerHierarchyService.GetRenderableLayers(_sceneSet.MediumDocument.Layers);
    }

    [Benchmark]
    public IReadOnlyList<TimelineLayer> LayerHierarchyService_GetRenderableLayers_Large()
    {
        return LayerHierarchyService.GetRenderableLayers(_sceneSet.LargeDocument.Layers);
    }

    [Benchmark]
    public IReadOnlyList<RenderableLayerSample> SymbolRenderService_BuildRenderSamples_Medium()
    {
        return SymbolRenderService.BuildRenderSamples(
            _sceneSet.MediumDocument,
            _sceneSet.MediumDocument.Layers,
            _sceneSet.MediumRequest.Time,
            _sceneSet.MediumDocument.Duration,
            _mediumFrameRate);
    }

    [Benchmark]
    public IReadOnlyList<RenderableLayerSample> SymbolRenderService_BuildRenderSamples_Large()
    {
        return SymbolRenderService.BuildRenderSamples(
            _sceneSet.LargeDocument,
            _sceneSet.LargeDocument.Layers,
            _sceneSet.LargeRequest.Time,
            _sceneSet.LargeDocument.Duration,
            _largeFrameRate);
    }

    [Benchmark]
    public SceneRenderPreparation SceneRenderPreparationBuilder_Build_Medium()
    {
        return SceneRenderPreparationBuilder.Build(
            _sceneSet.MediumDocument,
            _sceneSet.MediumDocument.Layers,
            _sceneSet.MediumRequest.Time,
            _sceneSet.MediumDocument.Duration,
            _mediumFrameRate);
    }

    [Benchmark]
    public SceneRenderPreparation SceneRenderPreparationBuilder_Build_Large()
    {
        return SceneRenderPreparationBuilder.Build(
            _sceneSet.LargeDocument,
            _sceneSet.LargeDocument.Layers,
            _sceneSet.LargeRequest.Time,
            _sceneSet.LargeDocument.Duration,
            _largeFrameRate);
    }

    [Benchmark]
    public SceneRenderRequest SceneRenderRequestBuilder_Build_Medium()
    {
        return SceneRenderRequestBuilder.Build(
            _sceneSet.MediumDocument,
            _sceneSet.MediumDocument.Layers,
            _sceneSet.MediumRequest.Time,
            _sceneSet.MediumDocument.Duration,
            _mediumFrameRate,
            _sceneSet.MediumDocument.CanvasWidth,
            _sceneSet.MediumDocument.CanvasHeight,
            _sceneSet.MediumDocument.TransparentStageBackground,
            _sceneSet.MediumDocument.BackgroundFrom,
            _sceneSet.MediumDocument.BackgroundTo);
    }

    [Benchmark]
    public SceneRenderRequest SceneRenderRequestBuilder_Build_Large()
    {
        return SceneRenderRequestBuilder.Build(
            _sceneSet.LargeDocument,
            _sceneSet.LargeDocument.Layers,
            _sceneSet.LargeRequest.Time,
            _sceneSet.LargeDocument.Duration,
            _largeFrameRate,
            _sceneSet.LargeDocument.CanvasWidth,
            _sceneSet.LargeDocument.CanvasHeight,
            _sceneSet.LargeDocument.TransparentStageBackground,
            _sceneSet.LargeDocument.BackgroundFrom,
            _sceneSet.LargeDocument.BackgroundTo);
    }

    private static double GetFrameRate(TimelineDocument document)
    {
        return document.Scenes.Count > 0 ? document.Scenes[0].FrameRate : 24d;
    }
}
