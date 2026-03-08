```

BenchmarkDotNet v0.14.0, macOS Sequoia 15.6 (24G84) [Darwin 24.6.0]
Apple M3 Pro, 1 CPU, 11 logical and 11 physical cores
.NET SDK 10.0.101
  [Host]   : .NET 9.0.6 (9.0.625.26613), Arm64 RyuJIT AdvSIMD
  ShortRun : .NET 9.0.6 (9.0.625.26613), Arm64 RyuJIT AdvSIMD

Job=ShortRun  IterationCount=6  LaunchCount=1  
WarmupCount=3  

```
| Method                              | Mean      | Error    | StdDev   | Gen0     | Gen1     | Gen2     | Allocated |
|------------------------------------ |----------:|---------:|---------:|---------:|---------:|---------:|----------:|
| Skia_Render_EffectHeavy_MediumScene |  57.14 ms | 7.793 ms | 2.779 ms | 200.0000 | 200.0000 | 200.0000 |   3.86 MB |
| Skia_Render_EffectHeavy_LargeScene  | 135.69 ms | 8.898 ms | 2.311 ms |        - |        - |        - |   4.34 MB |
