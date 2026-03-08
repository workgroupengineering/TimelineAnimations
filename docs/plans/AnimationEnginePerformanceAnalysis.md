# Animation Engine Performance Analysis

## Scope

This report audits the current animation engine, render-request preparation
path, pixel composition path, and pluggable rendering engines. It is based on
the current codebase and the latest benchmark suite/results in
`/Users/wieslawsoltes/GitHub/TimelineAnimations/benchmarks/TimelineAnimations.Benchmarks`.

## Audit Summary

The engine is now in a strong state for core sampling, request preparation,
normal composition, and native Skia rendering of typical authored scenes. The
main remaining hotspot is effect-heavy native rendering, followed by mixed
scenes that still fall back to Avalonia for unsupported features.

Current cost clusters:

1. allocation-heavy sample/request construction at export and preview boundaries
2. repeated filtered effect passes on large effect-heavy scenes
3. mixed scenes that leave the native Skia fast path

## Current Hotspots By Area

| Area | Main code | Current shape |
| --- | --- | --- |
| frame sampling | `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Services/FrameTimelineService.cs` | already below target |
| parenting/world transforms | `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Services/LayerParentingService.cs` | within budget, allocations still material |
| symbol flattening | `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Services/SymbolRenderService.cs` | CPU within target, still a noticeable allocation source |
| request preparation | `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Rendering/Services/SceneRenderPreparationBuilder.cs`, `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Rendering/Services/SceneRenderRequestBuilder.cs` | lookup churn removed; remaining pressure is per-sample allocation volume |
| pixel composition | `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Rendering/Services/RenderPixelBufferComposer.cs` | normal and table-driven blend modes are in a good range at 720p |
| Skia rendering | `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Rendering.SkiaSharp/Services/SkiaSceneRenderEngine.cs` | native direct scenes are fast; blend-heavy scenes are acceptable; effect-heavy large scenes remain the biggest renderer hotspot |
| Avalonia rendering | `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Rendering.Avalonia/Services/AvaloniaSceneRenderEngine.cs` | runtime path works, but headless benchmark extraction is still constrained |

## Benchmark Harness

The benchmark suite currently covers:

- frame sampling
- renderable-layer derivation
- parenting/world snapshot building
- symbol render sample building
- scene render preparation building
- scene render request building
- pixel blending and masking
- native Skia rendering on small/medium/large scenes
- blend-heavy native Skia scenes
- effect-heavy native Skia scenes

Main benchmark sources:

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/benchmarks/TimelineAnimations.Benchmarks/AnimationEngineBenchmarks.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/benchmarks/TimelineAnimations.Benchmarks/PixelComposerBenchmarks.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/benchmarks/TimelineAnimations.Benchmarks/RenderingBenchmarks.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/benchmarks/TimelineAnimations.Benchmarks/BenchmarkSceneFactory.cs`

Latest artifact outputs:

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/builds/benchmark-artifacts/results/TimelineAnimations.Benchmarks.AnimationEngineBenchmarks-report-github.md`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/builds/benchmark-artifacts/results/TimelineAnimations.Benchmarks.PixelComposerBenchmarks-report-github.md`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/builds/benchmark-artifacts/results/TimelineAnimations.Benchmarks.RenderingBenchmarks-report-github.md`

## Current Measured Baseline

Measured on Apple M3 Pro, .NET 9, BenchmarkDotNet `ShortRun`.

### Engine and preparation

| Benchmark | Mean | Allocated |
| --- | ---: | ---: |
| `LayerHierarchyService_GetRenderableLayers_Medium` | `518.324 ns` | `0 B` |
| `FrameTimelineService_SampleLayer_Small` | `955.916 ns` | `3.81 KB` |
| `LayerHierarchyService_GetRenderableLayers_Large` | `1.237 us` | `0 B` |
| `LayerParentingService_BuildWorldSnapshots_Medium` | `52.069 us` | `191.37 KB` |
| `SceneRenderRequestBuilder_Build_Medium` | `106.553 us` | `437.53 KB` |
| `SceneRenderPreparationBuilder_Build_Medium` | `107.761 us` | `437.05 KB` |
| `SymbolRenderService_BuildRenderSamples_Medium` | `116.006 us` | `437.28 KB` |
| `LayerParentingService_BuildWorldSnapshots_Large` | `124.900 us` | `455.80 KB` |
| `SceneRenderPreparationBuilder_Build_Large` | `264.294 us` | `1044.55 KB` |
| `SceneRenderRequestBuilder_Build_Large` | `265.567 us` | `1045.03 KB` |
| `SymbolRenderService_BuildRenderSamples_Large` | `267.417 us` | `1044.78 KB` |

### Pixel composition

| Benchmark | Mean | Allocated |
| --- | ---: | ---: |
| `ApplyMask` | `1.245 ms` | `3.52 MB` |
| `BlendPixels_Normal` | `3.992 ms` | `3.52 MB` |
| `BlendPixels_Screen` | `7.585 ms` | `3.52 MB` |
| `BlendPixels_Multiply` | `7.745 ms` | `3.52 MB` |
| `BlendPixels_HardLight` | `7.923 ms` | `3.52 MB` |
| `BlendPixels_Overlay` | `7.951 ms` | `3.52 MB` |

### Rendering

Rendering benchmarks are normalized to native-renderable scenes so the Skia
engine is measured on its direct path. Effect-heavy scenes now run natively for
blur, glow, and shadow, but still expose the remaining filtered-pass cost.

| Benchmark | Mean | Allocated |
| --- | ---: | ---: |
| `Skia_Render_SmallScene` | `2.350 ms` | `3.55 MB` |
| `Skia_Render_MediumScene` | `10.099 ms` | `3.68 MB` |
| `Skia_Render_BlendHeavy_MediumScene` | `17.819 ms` | `3.69 MB` |
| `Skia_Render_LargeScene` | `20.515 ms` | `3.91 MB` |
| `Skia_Render_BlendHeavy_LargeScene` | `41.870 ms` | `3.92 MB` |
| `Skia_Render_EffectHeavy_MediumScene` | `57.143 ms` | `3.86 MB` |
| `Skia_Render_EffectHeavy_LargeScene` | `135.688 ms` | `4.34 MB` |

## Before vs After Deltas

Using the earlier optimization baseline from this performance work:

| Benchmark | Before | After | Delta |
| --- | ---: | ---: | ---: |
| `SceneRenderRequestBuilder_Build_Medium` | `147.215 us` | `106.553 us` | `-27.6%` |
| `LayerParentingService_BuildWorldSnapshots_Medium` | `56.822 us` | `52.069 us` | `-8.4%` |
| `SymbolRenderService_BuildRenderSamples_Medium` | `122.611 us` | `116.006 us` | `-5.4%` |
| `BlendPixels_Normal` | `7.808 ms` | `3.992 ms` | `-48.9%` |
| `BlendPixels_Screen` | `11.383 ms` | `7.585 ms` | `-33.4%` |
| `Skia_Render_SmallScene` | `8.454 ms` | `2.350 ms` | `-72.2%` |
| `Skia_Render_MediumScene` | `39.541 ms` | `10.099 ms` | `-74.5%` |
| `Skia_Render_LargeScene` | `93.457 ms` | `20.515 ms` | `-78.0%` |
| `Skia_Render_EffectHeavy_MediumScene` | `273.894 ms` | `57.143 ms` | `-79.1%` |
| `Skia_Render_EffectHeavy_LargeScene` | `680.608 ms` | `135.688 ms` | `-80.1%` |
| `Skia_Render_MediumScene` allocation | `109.19 MB` | `3.68 MB` | `-96.6%` |
| `Skia_Render_LargeScene` allocation | `257.10 MB` | `3.91 MB` | `-98.5%` |
| `Skia_Render_EffectHeavy_LargeScene` allocation | `257.10 MB` | `4.34 MB` | `-98.3%` |

## Implemented Optimizations

### 1. Preparation pipeline reuse

Added:

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Rendering/Models/SceneRenderPreparation.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Rendering/Services/SceneRenderPreparationBuilder.cs`

This removed duplicated lookup construction and active-camera resolution from the
render-request path.

### 2. Lower-allocation symbol sampling and hierarchy reuse

Refactored:

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Services/SymbolRenderService.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Services/LayerHierarchyService.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Services/LayerParentingService.cs`

### 3. Document asset lookup caching

Refactored:

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Rendering/Services/SceneRenderPreparationBuilder.cs`

### 4. Renderable-layer caching

Refactored:

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Services/LayerHierarchyService.cs`

### 5. Hot-path pixel composer rewrite

Refactored:

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Rendering/Services/RenderPixelBufferComposer.cs`

Key improvements:

- integer fast path for `Normal` and `Layer`
- lookup-table driven `Screen`, `Multiply`, `Overlay`, and `HardLight`
- explicit-length overloads for pooled-buffer usage

### 6. Pooled renderer intermediates

Refactored:

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Rendering.SkiaSharp/Services/SkiaSceneRenderEngine.cs`

Key improvements:

- pooled per-layer buffers
- pooled mask-group buffers
- explicit release of transient pixel leases

### 7. Direct-scene native Skia fast path

Refactored:

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Rendering.SkiaSharp/Services/SkiaSceneRenderEngine.cs`

### 8. Blend-heavy renderer optimization

Refactored:

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Rendering/Services/RenderPixelBufferComposer.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Rendering.SkiaSharp/Services/SkiaSceneRenderEngine.cs`

### 9. Native effect-pass optimization

Refactored:

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Rendering.SkiaSharp/Services/SkiaSceneRenderEngine.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/benchmarks/TimelineAnimations.Benchmarks/BenchmarkSceneFactory.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/benchmarks/TimelineAnimations.Benchmarks/RenderingBenchmarks.cs`

Key improvements:

- blur, glow, and shadow stay on the native Skia path
- bounded `SaveLayer` regions instead of full-canvas filtered passes
- recorded effect-source content to avoid repeated geometry construction across filtered passes
- dedicated effect-heavy renderer benchmarks

### 10. Direct filtered passes for native primitives

Refactored:

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Rendering.SkiaSharp/Services/SkiaSceneRenderEngine.cs`

Key improvements:

- rectangles, ellipses, paths, and text now execute effect-heavy blur, shadow,
  and glow passes directly through bounded filtered layers
- those native layer kinds no longer pay the intermediate picture-recording
  cost for effect passes
- medium effect-heavy scenes now meet the transition target, and large
  effect-heavy scenes are materially closer to it

### 11. Tighter direct-effect save-layer bounds

Refactored:

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Rendering.SkiaSharp/Services/SkiaSceneRenderEngine.cs`

Key improvements:

- direct blur/glow/shadow passes now use effect-specific bounds instead of the
  generic fully-expanded layer bounds
- effect save-layers no longer pay bevel/gradient-filter padding that the
  direct path does not use
- large effect-heavy scenes now meet the current transition target

## Professional-Level Outcome

The engine now reaches a professional range for:

- frame sampling
- render preparation/request building
- normal composition
- non-normal composition at 720p
- native Skia rendering for small/medium/large typical scenes
- native Skia rendering for blend-heavy medium scenes

The engine now reaches a professional range for the current benchmark set,
including the current effect-heavy transition target. Large effect-heavy scenes
remain the dominant renderer hotspot, but they are now within the stated budget
and allocation-stable.

## Remaining Constraints

### Avalonia renderer benchmarkability

The Avalonia renderer still cannot be benchmarked headlessly through the current
`RenderTargetBitmap` extraction path because `Bitmap.CopyPixels(ILockedFramebuffer, ...)`
is not supported for that bitmap type in the headless backend used here.

### Remaining optimization opportunities

The next meaningful opportunities are:

1. native Skia support for bevel and gradient-filter families so more scenes stay off fallback
2. effect-pass reuse or caching across adjacent playback frames
3. revision-based request/sample caching for repeated export/preview loops
4. headless-safe Avalonia renderer extraction for mixed-scene benchmarks

## Rejected Experiments

These variants were measured and intentionally not kept:

- request-level transformed-snapshot/effect-picture caching in the Skia renderer:
  regressed effect-heavy scenes
- immutable blur/drop-shadow filter caching in the Skia renderer: regressed
  effect-heavy scenes
- reusing a single recorded base-content picture for both blur and final base
  draw: regressed effect-heavy scenes versus the current direct-pass/native-path
  mix
