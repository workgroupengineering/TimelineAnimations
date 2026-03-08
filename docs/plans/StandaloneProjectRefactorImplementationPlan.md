# Standalone Project Refactor Implementation Plan

Status: completed
Owner: Codex
Date: 2026-03-08

## Objective

Complete the next standalone-project refactor slice by removing the renderer
backend coupling that still exists after the earlier extraction work.

## Schedule

| Phase | Scope | Status |
| --- | --- | --- |
| 1 | Document the current standalone-project state and identify remaining coupling | completed |
| 2 | Add a rendering-level fallback contract in `TimelineAnimations.Rendering` | completed |
| 3 | Update Avalonia and Skia renderers to use the contract | completed |
| 4 | Remove the direct backend project reference and rewire app composition | completed |
| 5 | Add focused regression coverage and validate the solution | completed |

## Phase 1: Analysis and scope lock

### Tasks

- review current solution projects
- confirm which extraction seams are already done
- isolate the highest-value remaining coupling seam
- limit the pass to a complete, safe boundary cleanup instead of partial churn

## Phase 2: Rendering contract extraction

### Tasks

- add `ILayerRenderFallback` to `TimelineAnimations.Rendering`
- keep the contract backend-neutral
- use existing render request/sample models instead of introducing UI types

## Phase 3: Backend decoupling

### Tasks

- update `AvaloniaSceneRenderEngine` to implement `ILayerRenderFallback`
- update `SkiaSceneRenderEngine` constructors to accept the fallback contract
- keep a transparent no-op fallback for isolated use and tests

## Phase 4: Composition root update

### Tasks

- remove the `TimelineAnimations.Rendering.SkiaSharp` reference to
  `TimelineAnimations.Rendering.Avalonia`
- update `RenderingEngineHostService` so the app injects the Avalonia fallback
  explicitly

## Phase 5: Validation

### Tasks

- add a unit test proving unsupported Skia samples use the injected fallback
- build the solution
- run focused tests that cover the new seam

## Validation result

- `dotnet build /Users/wieslawsoltes/GitHub/TimelineAnimations/TimelineAnimations.sln`
  passed
- focused regression validation passed:
  `dotnet test /Users/wieslawsoltes/GitHub/TimelineAnimations/tests/TimelineAnimations.App.Tests/TimelineAnimations.App.Tests.csproj --no-build --filter "SkiaSceneRenderEngine_Uses_InjectedFallback_ForUnsupportedSamples|MainWindowViewModel_Updates_RenderingEngineHost_WhenSelectionChanges|RenderingEngineHostService_Renders_With_Avalonia_And_Skia_Engines|RenderingEngineHostService_Renders_Skia_Native_Text_Path_And_Control_Content"`

## Notes

- This pass intentionally fixes one full architectural seam rather than trying
  to rename namespaces and split oversized files in the same change set.
- The next logical refactor pass is namespace normalization and large-file
  decomposition, especially for `MainWindowViewModel`, `SceneCanvasControl`,
  and the interop services.
