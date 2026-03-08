using BenchmarkDotNet.Running;
using TimelineAnimations.Benchmarks;

BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args, BenchmarkConfig.Instance);
