```

BenchmarkDotNet v0.14.0, macOS Sequoia 15.6 (24G84) [Darwin 24.6.0]
Apple M3 Pro, 1 CPU, 11 logical and 11 physical cores
.NET SDK 10.0.101
  [Host]   : .NET 9.0.6 (9.0.625.26613), Arm64 RyuJIT AdvSIMD
  ShortRun : .NET 9.0.6 (9.0.625.26613), Arm64 RyuJIT AdvSIMD

Job=ShortRun  IterationCount=6  LaunchCount=1  
WarmupCount=3  

```
| Method                | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|---------------------- |---------:|----------:|----------:|------:|--------:|---------:|---------:|---------:|----------:|------------:|
| ApplyMask             | 1.192 ms | 0.0075 ms | 0.0027 ms |  0.31 |    0.00 | 244.1406 | 244.1406 | 244.1406 |   3.52 MB |        1.00 |
| BlendPixels_Normal    | 3.815 ms | 0.0204 ms | 0.0053 ms |  1.00 |    0.00 | 238.2813 | 238.2813 | 238.2813 |   3.52 MB |        1.00 |
| BlendPixels_Multiply  | 6.958 ms | 0.0286 ms | 0.0074 ms |  1.82 |    0.00 | 226.5625 | 226.5625 | 226.5625 |   3.52 MB |        1.00 |
| BlendPixels_Overlay   | 7.010 ms | 0.0547 ms | 0.0195 ms |  1.84 |    0.01 | 226.5625 | 226.5625 | 226.5625 |   3.52 MB |        1.00 |
| BlendPixels_HardLight | 7.058 ms | 0.0663 ms | 0.0237 ms |  1.85 |    0.01 | 226.5625 | 226.5625 | 226.5625 |   3.52 MB |        1.00 |
| BlendPixels_Screen    | 7.072 ms | 0.2655 ms | 0.0947 ms |  1.85 |    0.02 | 226.5625 | 226.5625 | 226.5625 |   3.52 MB |        1.00 |
