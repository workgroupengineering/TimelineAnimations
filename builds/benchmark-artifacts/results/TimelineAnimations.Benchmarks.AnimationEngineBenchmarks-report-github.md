```

BenchmarkDotNet v0.14.0, macOS Sequoia 15.6 (24G84) [Darwin 24.6.0]
Apple M3 Pro, 1 CPU, 11 logical and 11 physical cores
.NET SDK 10.0.101
  [Host]   : .NET 9.0.6 (9.0.625.26613), Arm64 RyuJIT AdvSIMD
  ShortRun : .NET 9.0.6 (9.0.625.26613), Arm64 RyuJIT AdvSIMD

Job=ShortRun  IterationCount=6  LaunchCount=1  
WarmupCount=3  

```
| Method                                           | Mean         | Error        | StdDev      | Ratio  | RatioSD | Gen0     | Gen1    | Allocated | Alloc Ratio |
|------------------------------------------------- |-------------:|-------------:|------------:|-------:|--------:|---------:|--------:|----------:|------------:|
| LayerHierarchyService_GetRenderableLayers_Medium |     518.3 ns |      3.57 ns |     1.27 ns |   0.63 |    0.00 |        - |       - |         - |        0.00 |
| FrameTimelineService_SampleLayer_Small           |     827.5 ns |      8.39 ns |     2.18 ns |   1.00 |    0.00 |   0.4663 |  0.0010 |    3904 B |        1.00 |
| LayerHierarchyService_GetRenderableLayers_Large  |   1,237.1 ns |     16.70 ns |     5.96 ns |   1.50 |    0.01 |        - |       - |         - |        0.00 |
| LayerParentingService_BuildWorldSnapshots_Medium |  52,069.1 ns |    483.82 ns |   125.65 ns |  62.92 |    0.21 |  23.3765 |  3.1738 |  195960 B |       50.19 |
| SceneRenderRequestBuilder_Build_Medium           | 106,552.5 ns |  2,090.45 ns |   745.47 ns | 128.77 |    0.89 |  53.4668 |  8.7891 |  448029 B |      114.76 |
| SceneRenderPreparationBuilder_Build_Medium       | 107,760.8 ns |  2,123.44 ns |   757.24 ns | 130.23 |    0.91 |  53.4668 |  9.1553 |  447541 B |      114.64 |
| SymbolRenderService_BuildRenderSamples_Medium    | 116,005.7 ns |  3,157.87 ns | 1,126.13 ns | 140.19 |    1.31 |  53.4668 |  9.6436 |  447773 B |      114.70 |
| LayerParentingService_BuildWorldSnapshots_Large  | 124,900.2 ns |  6,978.31 ns | 2,488.53 ns | 150.94 |    2.82 |  55.5420 | 13.7939 |  466736 B |      119.55 |
| SceneRenderPreparationBuilder_Build_Large        | 264,294.5 ns | 11,106.25 ns | 3,960.60 ns | 319.39 |    4.51 | 127.4414 | 40.0391 | 1069623 B |      273.98 |
| SceneRenderRequestBuilder_Build_Large            | 265,566.7 ns | 10,905.36 ns | 3,888.96 ns | 320.93 |    4.43 | 127.4414 | 40.0391 | 1070111 B |      274.11 |
| SymbolRenderService_BuildRenderSamples_Large     | 267,416.6 ns |  2,660.31 ns |   690.87 ns | 323.17 |    1.09 | 127.4414 | 38.5742 | 1069855 B |      274.04 |
