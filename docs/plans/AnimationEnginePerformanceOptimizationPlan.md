# Animation Engine Performance Optimization Plan

## Goal

Keep the animation engine and rendering stack in a professional performance
range under repeatable benchmarks, while making the remaining hotspots explicit
instead of hiding them behind aggregate averages.

## Principles

- optimize from measured data only
- reduce allocations before micro-optimizing arithmetic
- keep rendering code backend-reusable and framework-decoupled
- preserve output fidelity while changing hot paths

## Benchmark Sources

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/benchmarks/TimelineAnimations.Benchmarks/AnimationEngineBenchmarks.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/benchmarks/TimelineAnimations.Benchmarks/PixelComposerBenchmarks.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/benchmarks/TimelineAnimations.Benchmarks/RenderingBenchmarks.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/benchmarks/TimelineAnimations.Benchmarks/BenchmarkSceneFactory.cs`

## Target Budgets

These budgets apply to the current benchmark scene set.

| Area | Target |
| --- | --- |
| frame sampling | `< 1 us` |
| renderable-layer derivation | `< 2 us` medium, `< 5 us` large |
| world snapshot build | `< 60 us` medium, `< 140 us` large |
| render sample build | `< 125 us` medium, `< 300 us` large |
| render preparation build | `< 120 us` medium, `< 300 us` large |
| render request build | `< 120 us` medium, `< 280 us` large |
| normal full-frame blend | `< 4.0 ms` at `1280x720` |
| non-normal full-frame blend | `< 8.0 ms` at `1280x720` |
| native Skia render | `< 3 ms` small, `< 12 ms` medium, `< 25 ms` large |
| native Skia blend-heavy render | `< 18 ms` medium, `< 40 ms` large |
| native Skia effect-heavy render | `< 60 ms` medium, `< 140 ms` large as current transition target; later bring large under `100 ms` |
| render-time allocation | final-frame-size scale, not scene-complexity scale |

## Phase Plan

### Phase 1. Benchmark infrastructure
Status: completed

Deliverables:

- dedicated benchmark project
- markdown/csv artifact output
- small/medium/large scene sets
- explicit engine, pixel-composer, and renderer suites

### Phase 2. Preparation-path optimization
Status: completed

Deliverables:

- shared preparation object
- reusable library/media lookups
- lower-allocation parenting and hierarchy walks
- lower-allocation symbol sampling

### Phase 3. Pixel-composer optimization
Status: completed

Deliverables:

- fast opaque copy path
- integer fast path for normal/layer blending
- table-driven non-normal blend implementations
- explicit-length overloads for pooled buffers

### Phase 4. Renderer allocation reduction
Status: completed

Deliverables:

- pooled renderer intermediates
- pooled mask-group buffers
- explicit release of transient buffers

### Phase 5. Direct-scene native Skia fast path
Status: completed

Deliverables:

- native direct-scene rendering when every sample is Skia-renderable
- composed renderer fallback only for unsupported paths

### Phase 6. Blend-heavy renderer optimization
Status: completed

Deliverables:

- native supported blend modes through Skia `SaveLayer`
- direct-scene blend-heavy benchmarks
- refreshed baseline after compositor work

### Phase 7. Native effect-path optimization
Status: completed

Deliverables:

- native blur, glow, and shadow support
- bounded filtered-layer regions
- recorded effect-source content reuse
- dedicated effect-heavy benchmarks

Implemented in:

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Rendering.SkiaSharp/Services/SkiaSceneRenderEngine.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/benchmarks/TimelineAnimations.Benchmarks/BenchmarkSceneFactory.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/benchmarks/TimelineAnimations.Benchmarks/RenderingBenchmarks.cs`

### Phase 9. Native primitive direct-effect optimization
Status: completed

Deliverables:

- direct filtered blur/shadow/glow passes for native rectangles, ellipses, paths, and text
- reduced picture-recording overhead on effect-heavy native scenes
- refreshed effect-heavy benchmark baseline

Implemented in:

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Rendering.SkiaSharp/Services/SkiaSceneRenderEngine.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/benchmarks/TimelineAnimations.Benchmarks/RenderingBenchmarks.cs`

### Phase 10. Direct-effect bounds tightening
Status: completed

Deliverables:

- effect-specific save-layer bounds for direct blur/glow/shadow passes
- reduced filtered pixel area for large native effect-heavy scenes
- refreshed effect-heavy benchmark baseline

Implemented in:

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Rendering.SkiaSharp/Services/SkiaSceneRenderEngine.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/benchmarks/TimelineAnimations.Benchmarks/RenderingBenchmarks.cs`

### Phase 8. Reporting and validation
Status: completed

Deliverables:

- rerun benchmarks after each real optimization slice
- keep markdown artifact outputs current
- track deltas in the performance report

## Current Outcome

| Metric | Result | Status |
| --- | ---: | --- |
| frame sampling | `955.916 ns` | met |
| medium renderable-layer derivation | `518.324 ns` | met |
| large renderable-layer derivation | `1.237 us` | met |
| medium world snapshot build | `52.069 us` | met |
| large world snapshot build | `124.900 us` | met |
| medium render sample build | `116.006 us` | met |
| large render sample build | `267.417 us` | met |
| medium render preparation build | `107.761 us` | met |
| large render preparation build | `264.294 us` | met |
| medium render request build | `106.553 us` | met |
| large render request build | `265.567 us` | met |
| normal full-frame blend | `3.992 ms` | met |
| screen full-frame blend | `7.585 ms` | met |
| multiply full-frame blend | `7.745 ms` | met |
| hard-light full-frame blend | `7.923 ms` | met |
| overlay full-frame blend | `7.951 ms` | met |
| native Skia small render | `2.350 ms` | met |
| native Skia medium render | `10.099 ms` | met |
| native Skia large render | `20.515 ms` | met |
| native Skia blend-heavy medium render | `17.819 ms` | met |
| native Skia blend-heavy large render | `41.870 ms` | near target |
| native Skia effect-heavy medium render | `57.143 ms` | met |
| native Skia effect-heavy large render | `135.688 ms` | met |

## Current Benchmark Notes

- core engine prep and request building are within the intended budget
- normal and table-driven blend modes are now stable at 720p
- direct-scene Skia rendering is in a professional range for typical scenes
- effect-heavy scenes are no longer catastrophic, and medium effect-heavy
  playback now meets the transition target, and large effect-heavy playback now
  also meets the current transition target while remaining the main renderer bottleneck
- allocations stay near final-frame-size scale even on blend-heavy and effect-heavy scenes
- a request-level transformed-snapshot/effect-picture cache was measured and rejected because it regressed effect-heavy scenes
- an immutable blur/drop-shadow filter cache was measured and rejected because it regressed effect-heavy scenes
- a follow-up experiment to reuse a single recorded base-content picture for both blur and final base draw was measured and rejected because it regressed effect-heavy scenes versus the current bounded-layer picture pipeline

## Remaining Follow-On Work

This plan is complete for the current optimization wave. The next meaningful
work is:

1. add native Skia support for bevel and gradient-filter families
2. cache effect-source pictures or filtered intermediates across adjacent playback frames
3. add revision-based render-request/sample caching for repeated export and preview loops
4. rework Avalonia renderer pixel extraction for headless-safe mixed-scene benchmarks

## Validation Commands

Use these commands to reproduce the current baseline:

```bash
dotnet build /Users/wieslawsoltes/GitHub/TimelineAnimations/TimelineAnimations.sln
dotnet run -c Release --project /Users/wieslawsoltes/GitHub/TimelineAnimations/benchmarks/TimelineAnimations.Benchmarks/TimelineAnimations.Benchmarks.csproj -- --filter "*AnimationEngineBenchmarks*"
dotnet run -c Release --project /Users/wieslawsoltes/GitHub/TimelineAnimations/benchmarks/TimelineAnimations.Benchmarks/TimelineAnimations.Benchmarks.csproj -- --filter "*PixelComposerBenchmarks*"
dotnet run -c Release --project /Users/wieslawsoltes/GitHub/TimelineAnimations/benchmarks/TimelineAnimations.Benchmarks/TimelineAnimations.Benchmarks.csproj -- --filter "*RenderingBenchmarks*"
dotnet test /Users/wieslawsoltes/GitHub/TimelineAnimations/tests/TimelineAnimations.App.Tests/TimelineAnimations.App.Tests.csproj --no-build --filter "SkiaSceneRenderEngine_Uses_InjectedFallback_ForUnsupportedSamples|SkiaSceneRenderEngine_Renders_BlurShadowAndGlow_Natively_WithoutFallback|RenderingEngineHostService_Renders_With_Avalonia_And_Skia_Engines|RenderingEngineHostService_Renders_Skia_Native_Text_Path_And_Control_Content"
```
