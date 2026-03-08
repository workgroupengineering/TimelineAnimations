using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;

namespace TimelineAnimations.Benchmarks;

internal sealed class BenchmarkConfig : ManualConfig
{
    public static BenchmarkConfig Instance { get; } = new();

    private BenchmarkConfig()
    {
        AddJob(Job.ShortRun
            .WithWarmupCount(3)
            .WithIterationCount(6));
        AddDiagnoser(MemoryDiagnoser.Default);
        AddLogger(ConsoleLogger.Default);
        AddColumnProvider(DefaultColumnProviders.Instance);
        AddExporter(MarkdownExporter.GitHub);
        AddExporter(CsvExporter.Default);
        WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));
        ArtifactsPath = Path.Combine(
            RepoRootLocator.Find(),
            "builds",
            "benchmark-artifacts");
    }
}
