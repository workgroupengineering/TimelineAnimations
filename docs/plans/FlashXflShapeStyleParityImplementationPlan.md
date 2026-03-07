# Flash XFL Shape Style Parity Implementation Plan

## Status

Completed on March 7, 2026.

## Phase 1: Core style model

- Add explicit style enums for:
  - gradient kind
  - stroke cap
  - stroke join
- Extend layer style and sampled snapshots with:
  - fill enabled
  - stroke enabled
  - gradient kind
  - gradient angle
  - stroke cap
  - stroke join
  - stroke miter limit

## Phase 2: Native XFL mapping

- Export the new style metadata on XFL visuals.
- Import the same metadata from external XFL files.
- Apply imported values back onto document layers and library-derived instances.

## Phase 3: Rendering parity

- Update stage preview and export rendering to respect:
  - no fill
  - no stroke
  - linear versus radial gradients
  - gradient angle
  - stroke cap, join, and miter limit

## Phase 4: Editing parity

- Add inspector controls for:
  - fill enabled
  - stroke enabled
  - gradient kind
  - gradient angle
  - stroke cap
  - stroke join
  - stroke miter limit
- Keep new-layer defaults and style application aligned with the same model.

## Phase 5: Validation

- Add regression tests for:
  - XFL style round-trip
  - renderer style behavior
  - viewmodel inspector refresh
- Run build, tests, and a smoke launch.

## Result

Implemented in:

- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Models/LayerGradientKind.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Models/LayerStrokeCapStyle.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Models/LayerStrokeJoinStyle.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Models/LayerStyle.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Models/LayerSnapshot.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Services/TimelineEditingService.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Services/TimelineInterpolationService.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.Core/Services/FlashXflExchangeService.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Helpers/LayerStyleRenderHelper.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Controls/SceneCanvasControl.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Services/CompositeFrameRenderer.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/ViewModels/MainWindowViewModel.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/ViewModels/LayerViewModel.cs`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App/Views/MainWindow.axaml`
- `/Users/wieslawsoltes/GitHub/TimelineAnimations/tests/TimelineAnimations.App.Tests/TimelineCoreTests.cs`

Verification completed with:

- `dotnet build /Users/wieslawsoltes/GitHub/TimelineAnimations/TimelineAnimations.sln`
- `dotnet test /Users/wieslawsoltes/GitHub/TimelineAnimations/TimelineAnimations.sln`
- `dotnet run --project /Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App --no-build`

## Acceptance criteria

- XFL-authored shape style metadata survives import/export without flattening into default styling.
- Stage preview and export use the same stroke/fill/gradient behavior.
- The inspector exposes the Flash shape-style surface directly.
- Newly created vector and shape layers can author the same metadata the importer preserves.
