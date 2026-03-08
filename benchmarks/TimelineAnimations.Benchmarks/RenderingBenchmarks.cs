using BenchmarkDotNet.Attributes;
using TimelineAnimations.Rendering.Models;
using TimelineAnimations.Rendering.SkiaSharp.Services;

namespace TimelineAnimations.Benchmarks;

[MemoryDiagnoser]
public class RenderingBenchmarks
{
    private BenchmarkSceneSet _sceneSet = null!;
    private readonly SkiaSceneRenderEngine _skiaEngine = new();

    [GlobalSetup]
    public void Setup()
    {
        AvaloniaBenchmarkRuntime.EnsureInitialized();
        _sceneSet = BenchmarkSceneFactory.Create();
    }

    [Benchmark(Baseline = true)]
    public RenderedFrame Skia_Render_SmallScene()
    {
        return _skiaEngine.Render(_sceneSet.NativeSmallRequest, includeBackground: true);
    }

    [Benchmark]
    public RenderedFrame Skia_Render_MediumScene()
    {
        return _skiaEngine.Render(_sceneSet.NativeMediumRequest, includeBackground: true);
    }

    [Benchmark]
    public RenderedFrame Skia_Render_LargeScene()
    {
        return _skiaEngine.Render(_sceneSet.NativeLargeRequest, includeBackground: true);
    }

    [Benchmark]
    public RenderedFrame Skia_Render_BlendHeavy_MediumScene()
    {
        return _skiaEngine.Render(_sceneSet.NativeBlendMediumRequest, includeBackground: true);
    }

    [Benchmark]
    public RenderedFrame Skia_Render_BlendHeavy_LargeScene()
    {
        return _skiaEngine.Render(_sceneSet.NativeBlendLargeRequest, includeBackground: true);
    }

    [Benchmark]
    public RenderedFrame Skia_Render_EffectHeavy_MediumScene()
    {
        return _skiaEngine.Render(_sceneSet.NativeEffectsMediumRequest, includeBackground: true);
    }

    [Benchmark]
    public RenderedFrame Skia_Render_EffectHeavy_LargeScene()
    {
        return _skiaEngine.Render(_sceneSet.NativeEffectsLargeRequest, includeBackground: true);
    }
}
