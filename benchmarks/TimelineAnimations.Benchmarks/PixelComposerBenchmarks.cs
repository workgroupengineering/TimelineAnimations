using BenchmarkDotNet.Attributes;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Rendering.Services;

namespace TimelineAnimations.Benchmarks;

[MemoryDiagnoser]
public class PixelComposerBenchmarks
{
    private byte[] _destinationTemplate = null!;
    private byte[] _sourceTemplate = null!;
    private byte[] _maskTemplate = null!;

    [GlobalSetup]
    public void Setup()
    {
        const int width = 1280;
        const int height = 720;
        var pixelCount = width * height * 4;
        _destinationTemplate = new byte[pixelCount];
        _sourceTemplate = new byte[pixelCount];
        _maskTemplate = new byte[pixelCount];
        var random = new Random(12345);
        random.NextBytes(_destinationTemplate);
        random.NextBytes(_sourceTemplate);
        random.NextBytes(_maskTemplate);
    }

    [Benchmark(Baseline = true)]
    public byte[] BlendPixels_Normal()
    {
        var destination = (byte[])_destinationTemplate.Clone();
        RenderPixelBufferComposer.BlendPixels(destination, _sourceTemplate, LayerBlendMode.Normal);
        return destination;
    }

    [Benchmark]
    public byte[] BlendPixels_Multiply()
    {
        var destination = (byte[])_destinationTemplate.Clone();
        RenderPixelBufferComposer.BlendPixels(destination, _sourceTemplate, LayerBlendMode.Multiply);
        return destination;
    }

    [Benchmark]
    public byte[] BlendPixels_Screen()
    {
        var destination = (byte[])_destinationTemplate.Clone();
        RenderPixelBufferComposer.BlendPixels(destination, _sourceTemplate, LayerBlendMode.Screen);
        return destination;
    }

    [Benchmark]
    public byte[] BlendPixels_Overlay()
    {
        var destination = (byte[])_destinationTemplate.Clone();
        RenderPixelBufferComposer.BlendPixels(destination, _sourceTemplate, LayerBlendMode.Overlay);
        return destination;
    }

    [Benchmark]
    public byte[] BlendPixels_HardLight()
    {
        var destination = (byte[])_destinationTemplate.Clone();
        RenderPixelBufferComposer.BlendPixels(destination, _sourceTemplate, LayerBlendMode.HardLight);
        return destination;
    }

    [Benchmark]
    public byte[] ApplyMask()
    {
        var destination = (byte[])_destinationTemplate.Clone();
        RenderPixelBufferComposer.ApplyMask(destination, _maskTemplate);
        return destination;
    }
}
