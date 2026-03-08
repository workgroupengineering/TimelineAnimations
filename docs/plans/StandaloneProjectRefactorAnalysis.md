# Standalone Project Refactor Analysis

Status: completed
Owner: Codex
Date: 2026-03-08

## Goal

Review the current solution after the first extraction wave and identify what
still prevents the extracted projects from behaving like clean standalone
assemblies.

## Current project map

The solution already contains dedicated projects for the main reusable areas:

- `TimelineAnimations.Core`
- `TimelineAnimations.FlashFormats`
- `TimelineAnimations.Interop`
- `TimelineAnimations.Rendering`
- `TimelineAnimations.Rendering.Avalonia`
- `TimelineAnimations.Rendering.SkiaSharp`
- `TimelineAnimations.AppServices`
- `TimelineAnimations.Controls`
- `TimelineAnimations.App`

This means the large structural move already happened. The next refactor stage
is no longer about creating new projects, but about removing the coupling that
still ties those projects back together.

## Current extraction findings

### 1. Renderer backends are not fully independent

`TimelineAnimations.Rendering.SkiaSharp` still referenced
`TimelineAnimations.Rendering.Avalonia` directly and instantiated
`AvaloniaSceneRenderEngine` internally for unsupported layers.

Impact:

- backend plugability is weakened because the Skia backend is not truly
  independent
- the dependency direction is wrong for a plugin model
- tests can only validate fallback behavior through the Avalonia backend instead
  of a narrow contract

This is the highest-value seam to fix immediately.

### 2. Extracted assemblies still keep legacy namespaces

Several extracted projects still preserve old namespaces:

- `TimelineAnimations.Interop` types still live under
  `TimelineAnimations.Core.*`
- `TimelineAnimations.AppServices` types still live under
  `TimelineAnimations.App.*`
- `TimelineAnimations.Controls` types still live under
  `TimelineAnimations.App.*`

Impact:

- physical ownership and logical ownership do not match yet
- code navigation remains confusing
- future extraction work risks circular reasoning around where a feature really
  belongs

This is a real issue, but it is mostly churn-heavy and better handled in a
dedicated namespace-normalization pass.

### 3. A few files are still oversized and should be split by feature seams

Largest remaining hotspots:

- `MainWindowViewModel.cs`
- `MainWindow.axaml`
- `FlashXflExchangeService.cs`
- `SceneCanvasControl.cs`
- `AnimationExchangeService.cs`
- `DockInspectorToolView.axaml`
- `Theme.axaml`

Impact:

- single files still own too many responsibilities
- reviewability and unit-testability are reduced
- smaller standalone project boundaries are harder to enforce

This should be addressed after the dependency seams are clean, because moving
code before removing bad references would just spread the coupling around.

## Recommended refactor scope for this pass

To keep the work safe and complete, this pass should focus on one full seam:

1. introduce a renderer-level fallback abstraction in `TimelineAnimations.Rendering`
2. make `AvaloniaSceneRenderEngine` implement that abstraction
3. make `SkiaSceneRenderEngine` depend only on that abstraction
4. remove the project reference from `TimelineAnimations.Rendering.SkiaSharp`
   to `TimelineAnimations.Rendering.Avalonia`
5. update the app-side composition root to inject the Avalonia fallback
6. add regression coverage that proves unsupported layers are rendered through
   the injected fallback contract

## Deferred follow-up work

The following items remain important but are intentionally deferred to later
passes:

- namespace normalization for extracted projects
- splitting `MainWindowViewModel` into focused coordinators or sub-view-models
- splitting `FlashXflExchangeService` and `AnimationExchangeService` into
  feature-specific workers
- decomposing `SceneCanvasControl` into interaction, overlay, and rendering
  coordinators

## Success criteria

- `TimelineAnimations.Rendering.SkiaSharp` no longer references
  `TimelineAnimations.Rendering.Avalonia`
- fallback rendering is driven by a contract from
  `TimelineAnimations.Rendering`
- the app still resolves Avalonia and Skia engines correctly
- focused regression tests prove the new seam
- the solution builds after the dependency cleanup

## Outcome

This pass completed the renderer decoupling seam:

- added `ILayerRenderFallback` in `TimelineAnimations.Rendering`
- updated `AvaloniaSceneRenderEngine` to implement the fallback contract
- updated `SkiaSceneRenderEngine` to consume the contract instead of directly
  creating `AvaloniaSceneRenderEngine`
- removed the direct project reference from
  `TimelineAnimations.Rendering.SkiaSharp` to
  `TimelineAnimations.Rendering.Avalonia`
- updated app composition to inject the Avalonia fallback
- added focused regression coverage for the injected fallback path
