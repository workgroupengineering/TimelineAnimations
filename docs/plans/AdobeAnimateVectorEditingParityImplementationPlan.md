# Adobe Animate Vector Editing Parity Implementation Plan

Status: completed
Owner: Codex
Date: 2026-03-08

## Objective

Implement the missing Adobe Animate-style vector-editing capabilities across
models, services, rendering, timeline interpolation, and both editor shells.

## Schedule

| Phase | Scope | Duration | Status |
| --- | --- | --- | --- |
| 44 | Bezier data model and renderer parity | 0.5 day | completed |
| 45 | Stage interaction and selected-point editing | 0.5 day | completed |
| 46 | Gradient transform and variable-width stroke parity | 0.5 day | completed |
| 47 | Inspector parity, validation, and regression tests | 0.5 day | completed |

## Phase 44: Bezier data model and renderer parity

### Tasks

- extend vector points with in/out handles, handle mode, and per-point width scale
- extend snapshots/interpolation to preserve the new vector metadata
- update path geometry generation to emit Bezier segments instead of only lines
- update export/render paths to use the same Bezier-capable helper

### Implemented

- `VectorHandleMode` and `VectorHandleKind`
- extended `VectorPointModel`
- extended `LayerSnapshot` and `LayerStyle`
- updated `VectorPathService`
- added `VectorPathRenderHelper`
- updated `TimelineInterpolationService`
- updated `CompositeFrameRenderer`
- updated `SceneCanvasControl`

## Phase 45: Stage interaction and selected-point editing

### Tasks

- support selected vector-point tracking on the canvas
- allow Subselect to hit-test and drag Bezier handles
- surface selected point state into the view model
- bind the selected-point index through both docked and classic stage hosts

### Implemented

- `SceneCanvasControl.SelectedPathPointIndex`
- `CanvasPathPointSelectionRequestedEventArgs`
- `CanvasPathHandleMoveRequestedEventArgs`
- stage event wiring in `MainWindow.axaml.cs`
- stage event wiring in `DockStageDocumentView.axaml.cs`
- `MainWindowViewModel.SelectPathPoint`
- `MainWindowViewModel.UpdatePathHandle`

## Phase 46: Gradient transform and variable-width stroke parity

### Tasks

- add gradient center and scale metadata
- expose gradient transform editing in the inspector
- add per-point width-scale editing
- preserve curved and width-based data through shape tween sampling

### Implemented

- gradient center/scale on `LayerStyle`
- gradient transform editors in both inspectors
- point width-scale editor
- variable-width stroke rendering using sampled stroke segments
- interpolation of handles and width scale in `TimelineInterpolationService`

## Phase 47: Inspector parity, cleanup commands, and validation

### Tasks

- add selected-point summary, handle mode editor, and path operation commands
- add smooth, straighten, reverse, optimize, insert, and delete path operations
- add focused tests for Bezier insertion, handle modes, gradient persistence,
  and interpolation behavior

### Implemented

- `VectorPathEditingService`
- path commands in `TimelineEditingService`
- command wiring in `MainWindowViewModel`
- docked inspector parity in `DockInspectorToolView.axaml`
- classic inspector parity in `MainWindow.axaml`
- focused regressions in `TimelineCoreTests.cs`

## Validation plan

1. build the solution
2. run focused vector regression tests
3. smoke-test the app launch

## Validation status

- `dotnet build /Users/wieslawsoltes/GitHub/TimelineAnimations/TimelineAnimations.sln`
- focused vector tests added in `TimelineCoreTests.cs`
- app smoke run scheduled after test pass
