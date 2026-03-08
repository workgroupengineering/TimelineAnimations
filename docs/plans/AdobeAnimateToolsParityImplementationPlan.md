# Adobe Animate Tools Parity Implementation Plan

## Objective

Close the remaining Adobe Animate authoring-tool gaps in this application.

## Schedule

| Phase | Scope | Status | Target in this pass |
| --- | --- | --- | --- |
| 34 | Navigation tools: Hand, Zoom, Rotate View | completed | yes |
| 35 | Selection tools: Subselection and Lasso | completed | yes |
| 36 | Drawing/edit tools: Pencil and Eraser | completed | yes |
| 37 | Tool options and UX parity | completed | yes |

## Phase 34. Navigation Tools

### Deliverables

- add `Hand`, `Zoom`, and `RotateView` to the authored tool set
- allow left-drag viewport pan with the hand tool
- allow click-to-zoom and marquee zoom with the zoom tool
- add stage view rotation with reset support

### Core changes

- extend `DrawingTool`
- add tool-option models for zoom mode and view rotation state
- extend `SceneCanvasControl` viewport math to support explicit tools and
  rotated stage-view transforms

### Acceptance

- users can select `Hand`, `Zoom`, and `Rotate View` from the tool rail and menus
- the stage can be rotated and reset without breaking hit testing

## Phase 35. Selection Tools

### Deliverables

- add `Subselect`
- add `Lasso`
- support freeform and polygon lasso modes
- support contact-sensitive lasso selection

### Core changes

- add selection-tool option models
- add a tool-authoring service for polygon hit testing and selection helpers
- extend stage interaction state for lasso drafting and subselection point moves

### Acceptance

- vector-point editing works with an explicit subselection tool
- lasso selection works on stage geometry and visible layers

## Phase 36. Drawing And Erase Tools

### Deliverables

- add `Pencil`
- add pencil modes:
  - `Straighten`
  - `Smooth`
  - `Ink`
- add `Eraser`
- add eraser modes:
  - `Object`
  - `Fill`
  - `Stroke`
- add authored eraser size and optional brush-size sync

### Core changes

- add tool authoring/refinement service methods for:
  - pencil stroke refinement
  - vector erase
  - fill/stroke/object erase semantics
- extend canvas interaction and ViewModel mutation paths

### Acceptance

- pencil output changes by mode
- erasing works directly on the stage with live feedback

## Phase 37. Tool Options And UX Parity

### Deliverables

- add a dedicated `Tool` tab to the dock inspector
- add compact active-tool summaries and options
- surface quick stage controls for view rotation and tool-sensitive settings
- update classic shell and native/fallback menus to expose the same tool set

### Acceptance

- users can discover and configure every active tool without leaving the stage
- docked and classic surfaces expose the same authored tools

## Implementation Order

1. Phase 34
2. Phase 35
3. Phase 36
4. Phase 37

## Quality Gates

- `dotnet build /Users/wieslawsoltes/GitHub/TimelineAnimations/TimelineAnimations.sln`
- targeted tests for:
  - viewport tool state
  - lasso hit testing
  - pencil refinement modes
  - eraser mutation behavior
  - ViewModel tool-state summaries
- desktop smoke launch

## Implementation Status

All four phases were implemented in this pass across:

- tool models and authoring-option enums
- stage canvas interaction and viewport math
- tool authoring services for lasso hit testing, pencil refinement, and erase
- docked workspace tool rail and inspector tool tab
- classic shell tool rail and fallback menu
- native menu tool parity
- regression tests

## Validation Status

- `dotnet build /Users/wieslawsoltes/GitHub/TimelineAnimations/TimelineAnimations.sln`: passed
- targeted tool tests: `5/5` passing
- `dotnet test /Users/wieslawsoltes/GitHub/TimelineAnimations/TimelineAnimations.sln --no-build`: `143/147` passing
- `dotnet run --project /Users/wieslawsoltes/GitHub/TimelineAnimations/src/TimelineAnimations.App --no-build`: smoke-launched successfully

Known remaining failures are not from this pass. They are the existing Avalonia
headless `fonts:SystemFonts` failures in four dock-rendering tests:

- `DockProjectToolView_Renders_Project_And_Assets_Workspace`
- `DockInspectorToolView_Renders_Classic_Property_And_Action_Surfaces`
- `DockOutputToolView_Renders_Interop_And_Publish_Workspace`
- `DockCurvesToolView_Renders_ExpandedCurveEditorSurface`
